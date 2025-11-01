using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MembersHub.Application.Services;

public class CashBoxDeliveryService : ICashBoxDeliveryService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<CashBoxDeliveryService> _logger;
    private readonly IAuditService _auditService;

    public CashBoxDeliveryService(
        MembersHubContext context,
        ILogger<CashBoxDeliveryService> logger,
        IAuditService auditService)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<CashBoxDelivery> CreateDeliveryAsync(int collectorId, int receivedBy, string? notes = null)
    {
        try
        {
            // Validate collector and receiver exist
            var collector = await _context.Users.FindAsync(collectorId);
            if (collector == null)
                throw new ArgumentException("Ο εισπράκτορας δεν βρέθηκε");

            var receiver = await _context.Users.FindAsync(receivedBy);
            if (receiver == null)
                throw new ArgumentException("Ο παραλήπτης δεν βρέθηκε");

            // Get last delivery to determine period start
            var lastDelivery = await GetLastDeliveryAsync(collectorId);
            var periodStart = lastDelivery?.DeliveryDate ?? collector.CreatedAt;
            var periodEnd = DateTime.Now;

            // Calculate balance for this period
            var (collections, expenses, net) = await CalculateBalanceForPeriodAsync(collectorId, periodStart, periodEnd);

            // Generate delivery number
            var deliveryNumber = await GenerateDeliveryNumberAsync();

            // Create delivery record
            var delivery = new CashBoxDelivery
            {
                DeliveryNumber = deliveryNumber,
                CollectorId = collectorId,
                ReceivedBy = receivedBy,
                DeliveryDate = DateTime.Now,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                TotalCollections = collections,
                TotalExpenses = expenses,
                NetAmount = net,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashBoxDeliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Audit logging
            await _auditService.LogAsync(AuditAction.Create, "CashBoxDelivery", delivery.Id.ToString(),
                delivery.DeliveryNumber,
                $"Παράδοση ταμείου {delivery.DeliveryNumber} από εισπράκτορα {collector.FullName} σε {receiver.FullName}. Καθαρό ποσό: €{net:N2}",
                receivedBy);

            _logger.LogInformation(
                "Δημιουργήθηκε παράδοση ταμείου {DeliveryNumber} από εισπράκτορα {CollectorId} σε {ReceiverId}. Καθαρό ποσό: {NetAmount:C}",
                delivery.DeliveryNumber, collectorId, receivedBy, net);

            return delivery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά τη δημιουργία παράδοσης ταμείου για εισπράκτορα {CollectorId}", collectorId);
            throw;
        }
    }

    public async Task<List<CashBoxDelivery>> GetCollectorDeliveriesAsync(int collectorId)
    {
        return await _context.CashBoxDeliveries
            .Include(d => d.Collector)
            .Include(d => d.Receiver)
            .Where(d => d.CollectorId == collectorId)
            .OrderByDescending(d => d.DeliveryDate)
            .ToListAsync();
    }

    public async Task<CashBoxDelivery?> GetLastDeliveryAsync(int collectorId)
    {
        return await _context.CashBoxDeliveries
            .Include(d => d.Collector)
            .Include(d => d.Receiver)
            .Where(d => d.CollectorId == collectorId)
            .OrderByDescending(d => d.DeliveryDate)
            .FirstOrDefaultAsync();
    }

    public async Task<(decimal Collections, decimal Expenses, decimal Net)> CalculateCurrentBalanceAsync(int collectorId)
    {
        // Get last delivery to determine start date
        var lastDelivery = await GetLastDeliveryAsync(collectorId);
        var collector = await _context.Users.FindAsync(collectorId);

        var startDate = lastDelivery?.DeliveryDate ?? collector?.CreatedAt ?? DateTime.MinValue;
        var endDate = DateTime.Now;

        return await CalculateBalanceForPeriodAsync(collectorId, startDate, endDate);
    }

    public async Task<(decimal Collections, decimal Expenses, decimal Net)> CalculateBalanceForPeriodAsync(
        int collectorId, DateTime startDate, DateTime endDate)
    {
        try
        {
            // Calculate collections based on PaymentDate (not CreatedAt!)
            // This allows for backdated entries after delivery
            var collections = await _context.Payments
                .Where(p => p.CollectorId == collectorId
                    && p.PaymentDate >= startDate
                    && p.PaymentDate <= endDate
                    && p.Status == PaymentStatus.Confirmed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // Calculate expenses based on Date (not CreatedAt!)
            var expenses = await _context.Expenses
                .Where(e => e.SubmittedBy == collectorId
                    && e.Date >= startDate
                    && e.Date <= endDate
                    && e.Status == ExpenseStatus.Approved)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var net = collections - expenses;

            _logger.LogDebug(
                "Υπολογισμός υπολοίπου εισπράκτορα {CollectorId} για περίοδο {StartDate} - {EndDate}: Εισπράξεις={Collections:C}, Έξοδα={Expenses:C}, Καθαρό={Net:C}",
                collectorId, startDate, endDate, collections, expenses, net);

            return (collections, expenses, net);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Σφάλμα κατά τον υπολογισμό υπολοίπου εισπράκτορα {CollectorId} για περίοδο {StartDate} - {EndDate}",
                collectorId, startDate, endDate);
            throw;
        }
    }

    public async Task<CashBoxDelivery?> GetByDeliveryNumberAsync(string deliveryNumber)
    {
        return await _context.CashBoxDeliveries
            .Include(d => d.Collector)
            .Include(d => d.Receiver)
            .FirstOrDefaultAsync(d => d.DeliveryNumber == deliveryNumber);
    }

    public async Task<CashBoxDelivery?> GetByIdAsync(int id)
    {
        return await _context.CashBoxDeliveries
            .Include(d => d.Collector)
            .Include(d => d.Receiver)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<string> GenerateDeliveryNumberAsync()
    {
        var currentYear = DateTime.Now.Year;
        var prefix = $"DEL-{currentYear}-";

        // Find the last delivery number for this year
        var lastDelivery = await _context.CashBoxDeliveries
            .Where(d => d.DeliveryNumber.StartsWith(prefix))
            .OrderByDescending(d => d.DeliveryNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastDelivery != null)
        {
            var lastNumberPart = lastDelivery.DeliveryNumber.Split('-').Last();
            if (int.TryParse(lastNumberPart, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }
}

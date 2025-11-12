using Microsoft.EntityFrameworkCore;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Application.Services;

public class CashierHandoverService : ICashierHandoverService
{
    private readonly IDbContextFactory<MembersHubContext> _contextFactory;
    private readonly TimeZoneService _timeZone;

    public CashierHandoverService(
        IDbContextFactory<MembersHubContext> contextFactory,
        TimeZoneService timeZone)
    {
        _contextFactory = contextFactory;
        _timeZone = timeZone;
    }

    public async Task<CashierHandover?> GetActivePeriodAsync(int cashierId)
    {
        using var context = _contextFactory.CreateDbContext();

        // Active period is the most recent confirmed handover or null if this is the first period
        return await context.CashierHandovers
            .Where(h => h.CashierId == cashierId && h.Status == HandoverStatus.Confirmed)
            .OrderByDescending(h => h.PeriodEndDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CashierHandover>> GetCashierHandoversAsync(int cashierId)
    {
        using var context = _contextFactory.CreateDbContext();

        return await context.CashierHandovers
            .Include(h => h.Cashier)
            .Include(h => h.ReceivedBy)
            .Where(h => h.CashierId == cashierId)
            .OrderByDescending(h => h.PeriodEndDate)
            .ToListAsync();
    }

    public async Task<List<CashierHandover>> GetAllHandoversAsync()
    {
        using var context = _contextFactory.CreateDbContext();

        return await context.CashierHandovers
            .Include(h => h.Cashier)
            .Include(h => h.ReceivedBy)
            .OrderByDescending(h => h.PeriodEndDate)
            .ToListAsync();
    }

    public async Task<List<CashierHandover>> GetPendingHandoversAsync()
    {
        using var context = _contextFactory.CreateDbContext();

        return await context.CashierHandovers
            .Include(h => h.Cashier)
            .Where(h => h.Status == HandoverStatus.Pending)
            .OrderBy(h => h.CreatedDate)
            .ToListAsync();
    }

    public async Task<CashierHandover?> GetHandoverByIdAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();

        return await context.CashierHandovers
            .Include(h => h.Cashier)
            .Include(h => h.ReceivedBy)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<CashierHandover> CreateHandoverAsync(int cashierId, string? notes = null)
    {
        using var context = _contextFactory.CreateDbContext();

        // Get the last confirmed handover for this cashier to determine period start
        var lastHandover = await context.CashierHandovers
            .Where(h => h.CashierId == cashierId && h.Status == HandoverStatus.Confirmed)
            .OrderByDescending(h => h.PeriodEndDate)
            .FirstOrDefaultAsync();

        var periodStart = lastHandover?.PeriodEndDate ?? DateTime.MinValue;
        var periodEnd = _timeZone.ConvertToUtc(_timeZone.GetGreekNow());

        // Calculate totals for the period
        var summary = await GetCashierCurrentPeriodSummaryAsync(cashierId);

        var handover = new CashierHandover
        {
            CashierId = cashierId,
            PeriodStartDate = summary.PeriodStart,
            PeriodEndDate = periodEnd,
            TotalCollections = summary.TotalCollections,
            TotalExpenses = summary.TotalExpenses,
            NetBalance = summary.NetBalance,
            Status = HandoverStatus.Pending,
            Notes = notes,
            CreatedDate = _timeZone.ConvertToUtc(_timeZone.GetGreekNow())
        };

        context.CashierHandovers.Add(handover);
        await context.SaveChangesAsync();

        return handover;
    }

    public async Task<bool> ConfirmHandoverAsync(int handoverId, int receivedById, string? notes = null)
    {
        using var context = _contextFactory.CreateDbContext();

        var handover = await context.CashierHandovers
            .FirstOrDefaultAsync(h => h.Id == handoverId);

        if (handover == null || handover.Status != HandoverStatus.Pending)
            return false;

        handover.Status = HandoverStatus.Confirmed;
        handover.ReceivedById = receivedById;
        handover.ConfirmedDate = _timeZone.ConvertToUtc(_timeZone.GetGreekNow());

        if (!string.IsNullOrWhiteSpace(notes))
        {
            handover.Notes = string.IsNullOrWhiteSpace(handover.Notes)
                ? notes
                : $"{handover.Notes}\n\nΕπιβεβαίωση: {notes}";
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectHandoverAsync(int handoverId, int rejectedById, string reason)
    {
        using var context = _contextFactory.CreateDbContext();

        var handover = await context.CashierHandovers
            .FirstOrDefaultAsync(h => h.Id == handoverId);

        if (handover == null || handover.Status != HandoverStatus.Pending)
            return false;

        handover.Status = HandoverStatus.Rejected;
        handover.ReceivedById = rejectedById;
        handover.Notes = string.IsNullOrWhiteSpace(handover.Notes)
            ? $"Απορρίφθηκε: {reason}"
            : $"{handover.Notes}\n\nΑπορρίφθηκε: {reason}";

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<(decimal TotalCollections, decimal TotalExpenses, decimal NetBalance, DateTime PeriodStart)>
        GetCashierCurrentPeriodSummaryAsync(int cashierId)
    {
        using var context = _contextFactory.CreateDbContext();

        // Get the last confirmed handover date
        var lastHandover = await context.CashierHandovers
            .Where(h => h.CashierId == cashierId && h.Status == HandoverStatus.Confirmed)
            .OrderByDescending(h => h.PeriodEndDate)
            .FirstOrDefaultAsync();

        var periodStart = lastHandover?.PeriodEndDate ?? DateTime.MinValue;

        // Calculate collections since last handover
        var totalCollections = await context.Payments
            .Where(p => p.CollectorId == cashierId &&
                       p.PaymentDate >= periodStart &&
                       p.Status == PaymentStatus.Confirmed)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        // Calculate expenses since last handover
        var totalExpenses = await context.Expenses
            .Where(e => e.SubmittedBy == cashierId &&
                       e.Date >= periodStart &&
                       e.Status == ExpenseStatus.Approved)
            .SumAsync(e => (decimal?)e.Amount) ?? 0;

        var netBalance = totalCollections - totalExpenses;

        return (totalCollections, totalExpenses, netBalance, periodStart);
    }

    public async Task<(List<Payment> Payments, List<Expense> Expenses)> GetHandoverTransactionsAsync(int handoverId)
    {
        using var context = _contextFactory.CreateDbContext();

        var handover = await context.CashierHandovers
            .FirstOrDefaultAsync(h => h.Id == handoverId);

        if (handover == null)
            return (new List<Payment>(), new List<Expense>());

        var payments = await context.Payments
            .Include(p => p.Member)
            .Include(p => p.Subscription)
            .Where(p => p.CollectorId == handover.CashierId &&
                       p.PaymentDate >= handover.PeriodStartDate &&
                       p.PaymentDate <= handover.PeriodEndDate &&
                       p.Status == PaymentStatus.Confirmed)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();

        var expenses = await context.Expenses
            .Include(e => e.Category)
            .Where(e => e.SubmittedBy == handover.CashierId &&
                       e.Date >= handover.PeriodStartDate &&
                       e.Date <= handover.PeriodEndDate &&
                       e.Status == ExpenseStatus.Approved)
            .OrderBy(e => e.Date)
            .ToListAsync();

        return (payments, expenses);
    }
}

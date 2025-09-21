using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly IAuditService? _auditService;
    private readonly IEmailNotificationService? _emailService;

    public PaymentService(
        MembersHubContext context,
        ILogger<PaymentService> logger,
        IAuditService? auditService = null,
        IEmailNotificationService? emailService = null)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
        _emailService = emailService;
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        try
        {
            // Validation
            await ValidatePaymentAsync(payment);

            // Generate receipt number
            payment.ReceiptNumber = await GenerateReceiptNumberAsync();
            payment.CreatedAt = DateTime.UtcNow;
            payment.Status = PaymentStatus.Confirmed;

            // Start transaction for atomic operation
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Update subscription status if payment is for a subscription
                if (payment.SubscriptionId.HasValue)
                {
                    await UpdateSubscriptionStatusAsync(payment.SubscriptionId.Value, payment.Amount);
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Created payment {ReceiptNumber} for member {MemberId} amount {Amount:C}",
                    payment.ReceiptNumber, payment.MemberId, payment.Amount);

                // Audit logging if available
                if (_auditService != null)
                {
                    await _auditService.LogCreateAsync(payment, 0, "System", "System", "127.0.0.1");
                }

                return payment;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for member {MemberId}", payment.MemberId);
            throw;
        }
    }

    public async Task<Payment?> GetByReceiptNumberAsync(string receiptNumber)
    {
        return await _context.Payments
            .Include(p => p.Member)
            .Include(p => p.Collector)
            .Include(p => p.Subscription)
            .FirstOrDefaultAsync(p => p.ReceiptNumber == receiptNumber);
    }

    public async Task<IEnumerable<Payment>> GetMemberPaymentsAsync(int memberId)
    {
        _logger.LogDebug("Getting payments for member {MemberId}", memberId);

        return await _context.Payments
            .Include(p => p.Collector)
            .Include(p => p.Subscription)
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetCollectorPaymentsAsync(int collectorId)
    {
        _logger.LogDebug("Getting payments for collector {CollectorId}", collectorId);

        return await _context.Payments
            .Include(p => p.Member)
            .Include(p => p.Subscription)
            .Where(p => p.CollectorId == collectorId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<decimal> GetTodayCollectionsAsync(int? collectorId = null)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var query = _context.Payments
            .Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow && p.Status == PaymentStatus.Confirmed);

        if (collectorId.HasValue)
        {
            query = query.Where(p => p.CollectorId == collectorId);
        }

        var total = await query.SumAsync(p => p.Amount);

        _logger.LogDebug("Today's collections: {Amount:C} for collector {CollectorId}", total, collectorId?.ToString() ?? "All");
        return total;
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(int paymentId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Member)
                .Include(p => p.Collector)
                .Include(p => p.Subscription)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                throw new InvalidOperationException($"Payment with ID {paymentId} not found.");
            }

            // Generate PDF receipt
            var pdfBytes = await GeneratePdfReceiptAsync(payment);

            _logger.LogInformation("Generated PDF receipt for payment {ReceiptNumber}", payment.ReceiptNumber);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF receipt for payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task SendReceiptAsync(int paymentId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Member)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                throw new InvalidOperationException($"Payment with ID {paymentId} not found.");
            }

            if (string.IsNullOrEmpty(payment.Member.Email))
            {
                _logger.LogWarning("Cannot send receipt for payment {ReceiptNumber} - member has no email", payment.ReceiptNumber);
                return;
            }

            if (_emailService == null)
            {
                _logger.LogWarning("Email service not available - cannot send receipt");
                return;
            }

            // Generate receipt content
            var subject = $"Απόδειξη Πληρωμής #{payment.ReceiptNumber} - MembersHub";
            var body = GenerateReceiptEmailBody(payment);

            var (success, message) = await _emailService.SendEmailAsync(payment.Member.Email, subject, body);

            if (success)
            {
                _logger.LogInformation("Sent receipt email for payment {ReceiptNumber} to {Email}",
                    payment.ReceiptNumber, payment.Member.Email);
            }
            else
            {
                _logger.LogError("Failed to send receipt email for payment {ReceiptNumber}: {Message}",
                    payment.ReceiptNumber, message);
                throw new InvalidOperationException($"Failed to send receipt email: {message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending receipt for payment {PaymentId}", paymentId);
            throw;
        }
    }

    private async Task ValidatePaymentAsync(Payment payment)
    {
        var errors = new List<string>();

        // Required fields validation
        if (payment.MemberId <= 0)
            errors.Add("Το μέλος είναι υποχρεωτικό");

        if (payment.Amount <= 0)
            errors.Add("Το ποσό πρέπει να είναι μεγαλύτερο από μηδέν");

        if (payment.PaymentDate == default)
            payment.PaymentDate = DateTime.UtcNow;

        // Validate member exists
        var memberExists = await _context.Members.AnyAsync(m => m.Id == payment.MemberId);
        if (!memberExists)
            errors.Add("Το μέλος δεν υπάρχει");

        // Validate collector exists
        if (payment.CollectorId > 0)
        {
            var collectorExists = await _context.Users.AnyAsync(u => u.Id == payment.CollectorId);
            if (!collectorExists)
                errors.Add("Ο εισπράκτορας δεν υπάρχει");
        }

        // Validate subscription exists (if specified)
        if (payment.SubscriptionId.HasValue)
        {
            var subscriptionExists = await _context.Subscriptions
                .AnyAsync(s => s.Id == payment.SubscriptionId && s.MemberId == payment.MemberId);
            if (!subscriptionExists)
                errors.Add("Η συνδρομή δεν υπάρχει ή δεν ανήκει στο μέλος");
        }

        if (errors.Any())
        {
            throw new ArgumentException($"Validation errors: {string.Join(", ", errors)}");
        }
    }

    private async Task<string> GenerateReceiptNumberAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"REC-{year}-";

        // Find the last receipt number for this year
        var lastReceipt = await _context.Payments
            .Where(p => p.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(p => p.ReceiptNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastReceipt != null)
        {
            // Extract the numeric part
            var numericPart = lastReceipt.ReceiptNumber.Substring(prefix.Length);
            if (int.TryParse(numericPart, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D6}"; // Format: REC-2025-000001
    }

    private async Task UpdateSubscriptionStatusAsync(int subscriptionId, decimal paymentAmount)
    {
        var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
        if (subscription != null)
        {
            // Check if this payment covers the full subscription amount
            var totalPaid = await _context.Payments
                .Where(p => p.SubscriptionId == subscriptionId && p.Status == PaymentStatus.Confirmed)
                .SumAsync(p => p.Amount);

            if (totalPaid >= subscription.Amount)
            {
                subscription.Status = SubscriptionStatus.Paid;
                _context.Subscriptions.Update(subscription);
                _logger.LogDebug("Updated subscription {SubscriptionId} status to Paid", subscriptionId);
            }
        }
    }

    private async Task<byte[]> GeneratePdfReceiptAsync(Payment payment)
    {
        // For now, create a simple HTML-to-PDF receipt
        // In a real implementation, you would use a PDF library like iTextSharp or PdfSharp
        var htmlContent = GenerateReceiptHtml(payment);

        // This is a placeholder - you would need to implement actual PDF generation
        // For example, using a library like Puppeteer or wkhtmltopdf
        var content = System.Text.Encoding.UTF8.GetBytes(htmlContent);

        await Task.CompletedTask; // Remove this when implementing actual PDF generation
        return content;
    }

    private string GenerateReceiptHtml(Payment payment)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Απόδειξη Πληρωμής #{payment.ReceiptNumber}</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .receipt-info {{ margin-bottom: 20px; }}
                    .amount {{ font-size: 18px; font-weight: bold; color: #2e7d32; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1>MembersHub</h1>
                    <h2>Απόδειξη Πληρωμής</h2>
                </div>

                <div class='receipt-info'>
                    <p><strong>Αριθμός Απόδειξης:</strong> {payment.ReceiptNumber}</p>
                    <p><strong>Ημερομηνία:</strong> {payment.PaymentDate:dd/MM/yyyy HH:mm}</p>
                    <p><strong>Μέλος:</strong> {payment.Member.FirstName} {payment.Member.LastName}</p>
                    <p><strong>Αριθμός Μέλους:</strong> {payment.Member.MemberNumber}</p>
                    {(payment.Collector != null ? $"<p><strong>Εισπράκτορας:</strong> {payment.Collector.FirstName} {payment.Collector.LastName}</p>" : "")}
                    <p><strong>Τρόπος Πληρωμής:</strong> {payment.PaymentMethod}</p>
                    {(!string.IsNullOrEmpty(payment.Notes) ? $"<p><strong>Σημειώσεις:</strong> {payment.Notes}</p>" : "")}
                </div>

                <div class='amount'>
                    <p>Ποσό: €{payment.Amount:N2}</p>
                </div>

                <div style='margin-top: 50px; text-align: center; font-size: 12px; color: #666;'>
                    <p>Ευχαριστούμε για την πληρωμή σας!</p>
                    <p>MembersHub - Σύστημα Διαχείρισης Μελών</p>
                </div>
            </body>
            </html>";
    }

    private string GenerateReceiptEmailBody(Payment payment)
    {
        return $@"
            <h3>Αγαπητέ/ή {payment.Member.FirstName} {payment.Member.LastName},</h3>

            <p>Σας στέλνουμε την απόδειξη για την πληρωμή που πραγματοποιήσατε.</p>

            <h4>Στοιχεία Πληρωμής:</h4>
            <ul>
                <li><strong>Αριθμός Απόδειξης:</strong> {payment.ReceiptNumber}</li>
                <li><strong>Ημερομηνία:</strong> {payment.PaymentDate:dd/MM/yyyy HH:mm}</li>
                <li><strong>Ποσό:</strong> €{payment.Amount:N2}</li>
                <li><strong>Τρόπος Πληρωμής:</strong> {payment.PaymentMethod}</li>
                {(!string.IsNullOrEmpty(payment.Notes) ? $"<li><strong>Σημειώσεις:</strong> {payment.Notes}</li>" : "")}
            </ul>

            <p>Ευχαριστούμε για την εμπιστοσύνη σας!</p>

            <p>Με εκτίμηση,<br>Η Διοίκηση</p>";
    }
}
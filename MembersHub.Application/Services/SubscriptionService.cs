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

public class SubscriptionService : ISubscriptionService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly IAuditService? _auditService;
    private readonly IEmailNotificationService? _emailService;
    private readonly TimeZoneService _timeZone;

    public SubscriptionService(
        MembersHubContext context,
        ILogger<SubscriptionService> logger,
        TimeZoneService timeZone,
        IAuditService? auditService = null,
        IEmailNotificationService? emailService = null)
    {
        _context = context;
        _logger = logger;
        _timeZone = timeZone;
        _auditService = auditService;
        _emailService = emailService;
    }

    public async Task<Subscription?> GetByIdAsync(int id)
    {
        return await _context.Subscriptions
            .Include(s => s.Member)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Subscription>> GetMemberSubscriptionsAsync(int memberId)
    {
        _logger.LogDebug("Getting subscriptions for member {MemberId}", memberId);

        return await _context.Subscriptions
            .Include(s => s.Payments)
            .Where(s => s.MemberId == memberId)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsForPeriodAsync(int year, int? month = null)
    {
        var query = _context.Subscriptions
            .Include(s => s.Member)
            .Include(s => s.Payments)
            .Where(s => s.Year == year);

        if (month.HasValue)
        {
            query = query.Where(s => s.Month == month.Value);
        }

        return await query
            .OrderBy(s => s.Member.LastName)
            .ThenBy(s => s.Member.FirstName)
            .ToListAsync();
    }

    public async Task<Subscription> CreateSubscriptionAsync(Subscription subscription)
    {
        try
        {
            await ValidateSubscriptionAsync(subscription);

            subscription.CreatedAt = DateTime.UtcNow;

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created subscription for member {MemberId} for {Month}/{Year}",
                subscription.MemberId, subscription.Month, subscription.Year);

            // Audit logging if available
            if (_auditService != null)
            {
                await _auditService.LogCreateAsync(subscription, 0, "System", "System", "127.0.0.1");
            }

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for member {MemberId}", subscription.MemberId);
            throw;
        }
    }

    public async Task UpdateSubscriptionAsync(Subscription subscription)
    {
        try
        {
            var existingSubscription = await _context.Subscriptions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == subscription.Id);

            if (existingSubscription == null)
            {
                throw new InvalidOperationException($"Subscription with ID {subscription.Id} not found.");
            }

            await ValidateSubscriptionAsync(subscription, isUpdate: true);

            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated subscription {SubscriptionId}", subscription.Id);

            // Audit logging if available
            if (_auditService != null)
            {
                await _auditService.LogUpdateAsync(existingSubscription, subscription, 0, "System", "System", "127.0.0.1");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId}", subscription.Id);
            throw;
        }
    }

    public async Task DeleteSubscriptionAsync(int id)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null)
            {
                throw new InvalidOperationException($"Subscription with ID {id} not found.");
            }

            // Check if subscription has payments
            if (subscription.Payments.Any())
            {
                throw new InvalidOperationException("Cannot delete subscription with existing payments.");
            }

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted subscription {SubscriptionId}", id);

            // Audit logging if available
            if (_auditService != null)
            {
                await _auditService.LogDeleteAsync(subscription, 0, "System", "System", "127.0.0.1");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription {SubscriptionId}", id);
            throw;
        }
    }

    public async Task<int> GenerateMonthlySubscriptionsAsync(int year, int month)
    {
        try
        {
            _logger.LogInformation("Generating monthly subscriptions for {Month}/{Year}", month, year);

            // Step 1: Get all active members with active membership types
            var allActiveMembers = await _context.Members
                .Include(m => m.MembershipType)
                .Where(m => m.Status == MemberStatus.Active && m.MembershipType.IsActive)
                .ToListAsync();

            // Step 2: Get IDs of members who already have subscriptions for this period
            var existingSubscriptionMemberIds = await _context.Subscriptions
                .Where(s => s.Year == year && s.Month == month)
                .Select(s => s.MemberId)
                .ToListAsync();

            // Step 3: Filter in-memory to get members WITHOUT subscriptions
            var activeMembers = allActiveMembers
                .Where(m => !existingSubscriptionMemberIds.Contains(m.Id))
                .ToList();

            if (!activeMembers.Any())
            {
                if (existingSubscriptionMemberIds.Any())
                {
                    _logger.LogWarning("All active members already have subscriptions for {Month}/{Year}", month, year);
                    throw new InvalidOperationException($"Subscriptions already exist for {month}/{year}");
                }
                _logger.LogWarning("No active members found for subscription generation");
                return 0;
            }

            // Calculate due date (end of month in Greek timezone, stored as UTC)
            var lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            var dueDate = _timeZone.ConvertToUtc(lastDayOfMonth);

            var subscriptions = new List<Subscription>();

            foreach (var member in activeMembers)
            {
                var subscription = new Subscription
                {
                    MemberId = member.Id,
                    Year = year,
                    Month = month,
                    Amount = member.MembershipType.MonthlyFee,
                    DueDate = dueDate,
                    Status = SubscriptionStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                subscriptions.Add(subscription);
            }

            _context.Subscriptions.AddRange(subscriptions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated {Count} subscriptions for {Month}/{Year}",
                subscriptions.Count, month, year);

            return subscriptions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly subscriptions for {Month}/{Year}", month, year);
            throw;
        }
    }

    public async Task<IEnumerable<Subscription>> GetPendingSubscriptionsAsync()
    {
        return await _context.Subscriptions
            .Include(s => s.Member)
            .Where(s => s.Status == SubscriptionStatus.Pending)
            .OrderBy(s => s.DueDate)
            .ThenBy(s => s.Member.LastName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetOverdueSubscriptionsAsync()
    {
        var greekToday = _timeZone.GetGreekNow().Date;
        var today = _timeZone.ConvertToUtc(greekToday);

        return await _context.Subscriptions
            .Include(s => s.Member)
            .Where(s => s.Status == SubscriptionStatus.Overdue ||
                       (s.Status == SubscriptionStatus.Pending && s.DueDate < today))
            .OrderBy(s => s.DueDate)
            .ThenBy(s => s.Member.LastName)
            .ToListAsync();
    }

    public async Task<int> SendPaymentRemindersAsync(int? memberId = null)
    {
        try
        {
            if (_emailService == null)
            {
                _logger.LogWarning("Email service not available - cannot send payment reminders");
                return 0;
            }

            var query = _context.Subscriptions
                .Include(s => s.Member)
                .Where(s => s.Status == SubscriptionStatus.Pending || s.Status == SubscriptionStatus.Overdue);

            if (memberId.HasValue)
            {
                query = query.Where(s => s.MemberId == memberId);
            }

            var subscriptions = await query.ToListAsync();

            int sentCount = 0;

            foreach (var subscription in subscriptions)
            {
                if (string.IsNullOrEmpty(subscription.Member.Email))
                {
                    _logger.LogDebug("Skipping reminder for member {MemberId} - no email", subscription.MemberId);
                    continue;
                }

                try
                {
                    var subject = $"Υπενθύμιση Συνδρομής - {GetMonthName(subscription.Month)} {subscription.Year}";
                    var body = GenerateReminderEmailBody(subscription);

                    var (success, message) = await _emailService.SendEmailAsync(subscription.Member.Email, subject, body);

                    if (success)
                    {
                        sentCount++;
                        _logger.LogDebug("Sent payment reminder to {Email} for subscription {SubscriptionId}",
                            subscription.Member.Email, subscription.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send reminder to {Email}: {Message}",
                            subscription.Member.Email, message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending reminder for subscription {SubscriptionId}", subscription.Id);
                }
            }

            _logger.LogInformation("Sent {SentCount} payment reminders out of {TotalCount}",
                sentCount, subscriptions.Count);

            return sentCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment reminders");
            throw;
        }
    }

    public async Task<int> MarkOverdueSubscriptionsAsync()
    {
        try
        {
            var greekToday = _timeZone.GetGreekNow().Date;
            var today = _timeZone.ConvertToUtc(greekToday);

            var pendingSubscriptions = await _context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Pending && s.DueDate < today)
                .ToListAsync();

            foreach (var subscription in pendingSubscriptions)
            {
                subscription.Status = SubscriptionStatus.Overdue;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked {Count} subscriptions as overdue", pendingSubscriptions.Count);

            return pendingSubscriptions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking overdue subscriptions");
            throw;
        }
    }

    public async Task<decimal> GetMonthlyRevenueAsync(int year, int month)
    {
        var revenue = await _context.Subscriptions
            .Where(s => s.Year == year && s.Month == month && s.Status == SubscriptionStatus.Paid)
            .SumAsync(s => s.Amount);

        _logger.LogDebug("Monthly revenue for {Month}/{Year}: {Revenue:C}", month, year, revenue);
        return revenue;
    }

    public async Task<decimal> GetOutstandingAmountAsync()
    {
        return await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Pending || s.Status == SubscriptionStatus.Overdue)
            .SumAsync(s => s.Amount);
    }

    public async Task<Dictionary<int, decimal>> GetMemberOutstandingBalancesAsync()
    {
        var balances = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Pending || s.Status == SubscriptionStatus.Overdue)
            .GroupBy(s => s.MemberId)
            .Select(g => new { MemberId = g.Key, Balance = g.Sum(s => s.Amount) })
            .ToDictionaryAsync(x => x.MemberId, x => x.Balance);

        return balances;
    }

    public async Task UpdateSubscriptionStatusAsync(int subscriptionId, SubscriptionStatus status)
    {
        try
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null)
            {
                throw new InvalidOperationException($"Subscription with ID {subscriptionId} not found.");
            }

            var oldStatus = subscription.Status;
            subscription.Status = status;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated subscription {SubscriptionId} status from {OldStatus} to {NewStatus}",
                subscriptionId, oldStatus, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription status for {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<int> AutoUpdateSubscriptionStatusesAsync()
    {
        try
        {
            var updatedCount = 0;

            // Mark overdue subscriptions
            updatedCount += await MarkOverdueSubscriptionsAsync();

            // Check for fully paid subscriptions
            var paidCount = await CheckAndUpdatePaidSubscriptionsAsync();
            updatedCount += paidCount;

            _logger.LogInformation("Auto-updated {Count} subscription statuses", updatedCount);
            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in auto-updating subscription statuses");
            throw;
        }
    }

    private async Task<int> CheckAndUpdatePaidSubscriptionsAsync()
    {
        var pendingSubscriptions = await _context.Subscriptions
            .Include(s => s.Payments.Where(p => p.Status == PaymentStatus.Confirmed))
            .Where(s => s.Status == SubscriptionStatus.Pending)
            .ToListAsync();

        int updatedCount = 0;

        foreach (var subscription in pendingSubscriptions)
        {
            var totalPaid = subscription.Payments.Sum(p => p.Amount);
            if (totalPaid >= subscription.Amount)
            {
                subscription.Status = SubscriptionStatus.Paid;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogDebug("Marked {Count} subscriptions as paid", updatedCount);
        }

        return updatedCount;
    }

    private async Task ValidateSubscriptionAsync(Subscription subscription, bool isUpdate = false)
    {
        var errors = new List<string>();

        // Basic validation
        if (subscription.MemberId <= 0)
            errors.Add("Το μέλος είναι υποχρεωτικό");

        var currentGreekYear = _timeZone.GetGreekNow().Year;
        if (subscription.Year < 2020 || subscription.Year > currentGreekYear + 5)
            errors.Add("Μη έγκυρο έτος");

        if (subscription.Month < 1 || subscription.Month > 12)
            errors.Add("Μη έγκυρος μήνας");

        if (subscription.Amount <= 0)
            errors.Add("Το ποσό πρέπει να είναι μεγαλύτερο από μηδέν");

        // Check member exists
        var memberExists = await _context.Members.AnyAsync(m => m.Id == subscription.MemberId);
        if (!memberExists)
            errors.Add("Το μέλος δεν υπάρχει");

        // Check for duplicate subscription (only for create)
        if (!isUpdate)
        {
            var duplicateExists = await _context.Subscriptions.AnyAsync(s =>
                s.MemberId == subscription.MemberId &&
                s.Year == subscription.Year &&
                s.Month == subscription.Month);

            if (duplicateExists)
                errors.Add($"Υπάρχει ήδη συνδρομή για τον μήνα {subscription.Month}/{subscription.Year}");
        }

        if (errors.Any())
        {
            throw new ArgumentException($"Validation errors: {string.Join(", ", errors)}");
        }
    }

    private string GenerateReminderEmailBody(Subscription subscription)
    {
        var statusText = subscription.Status switch
        {
            SubscriptionStatus.Pending => "εκκρεμεί",
            SubscriptionStatus.Overdue => "είναι καθυστερημένη",
            _ => "χρειάζεται πληρωμή"
        };

        var greekToday = _timeZone.GetGreekNow().Date;
        var dueDateGreek = _timeZone.ConvertToGreekTime(subscription.DueDate).Date;
        var daysOverdue = subscription.Status == SubscriptionStatus.Overdue
            ? (greekToday - dueDateGreek).Days
            : 0;

        return $@"
            <h3>Αγαπητέ/ή {subscription.Member.FirstName} {subscription.Member.LastName},</h3>

            <p>Σας υπενθυμίζουμε ότι η συνδρομή σας για τον μήνα <strong>{GetMonthName(subscription.Month)} {subscription.Year}</strong> {statusText}.</p>

            <h4>Στοιχεία Συνδρομής:</h4>
            <ul>
                <li><strong>Περίοδος:</strong> {GetMonthName(subscription.Month)} {subscription.Year}</li>
                <li><strong>Ποσό:</strong> €{subscription.Amount:N2}</li>
                <li><strong>Καταληκτική Ημερομηνία:</strong> {subscription.DueDate:dd/MM/yyyy}</li>
                <li><strong>Κατάσταση:</strong> {GetStatusText(subscription.Status)}</li>
                {(daysOverdue > 0 ? $"<li><strong>Ημέρες Καθυστέρησης:</strong> {daysOverdue}</li>" : "")}
            </ul>

            <p>Παρακαλούμε προβείτε στην εξόφληση το συντομότερο δυνατό για να αποφύγετε τυχόν επιπλέον χρεώσεις.</p>

            <p>Για οποιαδήποτε απορία, μη διστάσετε να επικοινωνήσετε μαζί μας.</p>

            <p>Με εκτίμηση,<br>Η Διοίκηση</p>";
    }

    private static string GetMonthName(int month)
    {
        var monthNames = new[] { "", "Ιανουάριος", "Φεβρουάριος", "Μάρτιος", "Απρίλιος", "Μάιος", "Ιούνιος",
                                "Ιούλιος", "Αύγουστος", "Σεπτέμβριος", "Οκτώβριος", "Νοέμβριος", "Δεκέμβριος" };
        return month >= 1 && month <= 12 ? monthNames[month] : month.ToString();
    }

    private static string GetStatusText(SubscriptionStatus status)
    {
        return status switch
        {
            SubscriptionStatus.Pending => "Εκκρεμής",
            SubscriptionStatus.Paid => "Πληρωμένη",
            SubscriptionStatus.Overdue => "Καθυστερημένη",
            _ => status.ToString()
        };
    }
}
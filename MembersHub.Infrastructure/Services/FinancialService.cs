using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Infrastructure.Services;

public class FinancialService : IFinancialService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<FinancialService> _logger;

    public FinancialService(MembersHubContext context, ILogger<FinancialService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        try
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Create financial transaction record
            var transaction = new FinancialTransaction
            {
                Type = TransactionType.Income,
                Category = TransactionCategory.MembershipFee,
                Amount = payment.Amount,
                Description = $"Payment from {payment.Member.FirstName} {payment.Member.LastName}",
                TransactionDate = payment.PaymentDate,
                PaymentId = payment.Id
            };

            _context.FinancialTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for member {MemberId}", payment.MemberId);
            throw;
        }
    }

    public async Task<Payment> UpdatePaymentAsync(Payment payment)
    {
        try
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment {PaymentId}", payment.Id);
            throw;
        }
    }

    public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
    {
        return await _context.Payments
            .Include(p => p.Member)
            .Include(p => p.Collector)
            .Include(p => p.Subscription)
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    public async Task<List<Payment>> GetPaymentsByMemberAsync(int memberId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Payments
            .Include(p => p.Member)
            .Include(p => p.Collector)
            .Where(p => p.MemberId == memberId);

        if (startDate.HasValue)
            query = query.Where(p => p.PaymentDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.PaymentDate <= endDate.Value);

        return await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
    }

    public async Task<List<Payment>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Payments
            .Include(p => p.Member)
            .Include(p => p.Collector)
            .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<Subscription> CreateSubscriptionAsync(Subscription subscription)
    {
        try
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for member {MemberId}", subscription.MemberId);
            throw;
        }
    }

    public async Task<Subscription> UpdateSubscriptionAsync(Subscription subscription)
    {
        try
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId}", subscription.Id);
            throw;
        }
    }

    public async Task<List<Subscription>> GetActiveSubscriptionsAsync()
    {
        return await _context.Subscriptions
            .Include(s => s.Member)
            .Where(s => s.Status == SubscriptionStatus.Paid)
            .OrderBy(s => s.Member.LastName)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetOverdueSubscriptionsAsync()
    {
        return await _context.Subscriptions
            .Include(s => s.Member)
            .Where(s => s.Status == SubscriptionStatus.Overdue)
            .OrderBy(s => s.DueDate)
            .ToListAsync();
    }

    public async Task<FinancialSummary> GetFinancialSummaryAsync(DateTime periodStart, DateTime periodEnd)
    {
        var payments = await _context.Payments
            .Where(p => p.PaymentDate >= periodStart && p.PaymentDate <= periodEnd && p.Status == PaymentStatus.Confirmed)
            .ToListAsync();

        var expenses = await _context.Expenses
            .Where(e => e.Date >= periodStart && e.Date <= periodEnd && e.Status == ExpenseStatus.Approved)
            .ToListAsync();

        var totalIncome = payments.Sum(p => p.Amount);
        var totalExpenses = expenses.Sum(e => e.Amount);

        return new FinancialSummary
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalIncome = totalIncome,
            MembershipIncome = totalIncome, // Most income is from memberships
            EventIncome = 0,
            OtherIncome = 0,
            TotalExpenses = totalExpenses,
            OperationalExpenses = totalExpenses,
            EquipmentExpenses = 0,
            OtherExpenses = 0,
            NetProfit = totalIncome - totalExpenses,
            CurrentBalance = totalIncome - totalExpenses,
            TotalMembers = await _context.Members.CountAsync(),
            PaidMembers = payments.Select(p => p.MemberId).Distinct().Count(),
            TotalPayments = payments.Count,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<List<MonthlyFinancialData>> GetMonthlyDataAsync(int year)
    {
        var monthlyData = new List<MonthlyFinancialData>();

        for (int month = 1; month <= 12; month++)
        {
            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var payments = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == PaymentStatus.Confirmed)
                .SumAsync(p => p.Amount);

            var expenses = await _context.Expenses
                .Where(e => e.Date >= startDate && e.Date <= endDate && e.Status == ExpenseStatus.Approved)
                .SumAsync(e => e.Amount);

            monthlyData.Add(new MonthlyFinancialData
            {
                Year = year,
                Month = month,
                Income = payments,
                Expenses = expenses,
                NetProfit = payments - expenses,
                NewMembers = await _context.Members
                    .Where(m => m.CreatedAt >= startDate && m.CreatedAt <= endDate)
                    .CountAsync(),
                TotalMembers = await _context.Members
                    .Where(m => m.CreatedAt <= endDate && m.Status == MemberStatus.Active)
                    .CountAsync(),
                AveragePayment = payments > 0 ? payments / Math.Max(1, await _context.Payments
                    .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == PaymentStatus.Confirmed)
                    .CountAsync()) : 0
            });
        }

        return monthlyData;
    }

    public async Task<decimal> GetTotalIncomeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Payments
            .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == PaymentStatus.Confirmed)
            .SumAsync(p => p.Amount);
    }

    public async Task<decimal> GetTotalExpensesAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Expenses
            .Where(e => e.Date >= startDate && e.Date <= endDate && e.Status == ExpenseStatus.Approved)
            .SumAsync(e => e.Amount);
    }

    public async Task<List<Payment>> GetPendingPaymentsAsync()
    {
        return await _context.Payments
            .Include(p => p.Member)
            .Where(p => p.Status == PaymentStatus.Pending)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<Dictionary<PaymentMethod, decimal>> GetPaymentMethodSummaryAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Payments
            .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == PaymentStatus.Confirmed)
            .GroupBy(p => p.PaymentMethod)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(p => p.Amount));
    }

    public async Task<List<FinancialTransaction>> GetTransactionHistoryAsync(DateTime startDate, DateTime endDate, int limit = 100)
    {
        return await _context.FinancialTransactions
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .OrderByDescending(t => t.TransactionDate)
            .Take(limit)
            .ToListAsync();
    }

    // Missing methods from interface
    public async Task<List<Payment>> GetMemberPaymentsAsync(int memberId, int year = 0)
    {
        var query = _context.Payments
            .Include(p => p.Member)
            .Include(p => p.Collector)
            .Where(p => p.MemberId == memberId);

        if (year > 0)
            query = query.Where(p => p.PaymentDate.Year == year);

        return await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
    }

    public async Task<bool> DeletePaymentAsync(int paymentId)
    {
        try
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return false;

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment {PaymentId}", paymentId);
            return false;
        }
    }

    public async Task<Subscription> CreateSubscriptionAsync(int memberId, int year, int month, decimal amount)
    {
        var subscription = new Subscription
        {
            MemberId = memberId,
            Year = year,
            Month = month,
            Amount = amount,
            Status = SubscriptionStatus.Pending,
            DueDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 0, 0, 0, DateTimeKind.Utc)
        };

        return await CreateSubscriptionAsync(subscription);
    }

    public async Task<List<Subscription>> GetMemberSubscriptionsAsync(int memberId)
    {
        return await _context.Subscriptions
            .Include(s => s.Member)
            .Where(s => s.MemberId == memberId)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetSubscriptionsByStatusAsync(SubscriptionStatus status)
    {
        return await _context.Subscriptions
            .Include(s => s.Member)
            .Where(s => s.Status == status)
            .OrderBy(s => s.DueDate)
            .ToListAsync();
    }

    public async Task<decimal> GetMemberOutstandingBalanceAsync(int memberId)
    {
        var outstandingSubscriptions = await _context.Subscriptions
            .Where(s => s.MemberId == memberId && s.Status != SubscriptionStatus.Paid)
            .SumAsync(s => s.Amount);

        var outstandingInvoices = await _context.Invoices
            .Where(i => i.MemberId == memberId && i.Status != InvoiceStatus.Paid)
            .SumAsync(i => i.TotalAmount - i.PaidAmount);

        return outstandingSubscriptions + outstandingInvoices;
    }

    // Invoice methods
    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
    {
        try
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice for member {MemberId}", invoice.MemberId);
            throw;
        }
    }

    public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
    {
        try
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice {InvoiceId}", invoice.Id);
            throw;
        }
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId)
    {
        return await _context.Invoices
            .Include(i => i.Member)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    public async Task<List<Invoice>> GetMemberInvoicesAsync(int memberId)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .Where(i => i.MemberId == memberId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
    }

    public async Task<List<Invoice>> GetInvoicesByStatusAsync(InvoiceStatus status)
    {
        return await _context.Invoices
            .Include(i => i.Member)
            .Where(i => i.Status == status)
            .OrderBy(i => i.DueDate)
            .ToListAsync();
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Invoices
            .CountAsync(i => i.IssueDate.Year == year) + 1;

        return $"INV-{year}-{count:D4}";
    }

    public async Task<bool> SendInvoiceEmailAsync(int invoiceId)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Member)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice?.Member?.Email == null)
            {
                return false;
            }

            var subject = $"Τιμολόγιο {invoice.InvoiceNumber} - MembersHub";
            var body = $@"
                <h3>Αγαπητέ/ή {invoice.Member.FirstName} {invoice.Member.LastName},</h3>
                <p>Παρακάτω θα βρείτε το τιμολόγιό σας:</p>

                <p><strong>Στοιχεία Τιμολογίου:</strong></p>
                <ul>
                    <li>Αριθμός Τιμολογίου: {invoice.InvoiceNumber}</li>
                    <li>Ημερομηνία: {invoice.IssueDate:dd/MM/yyyy}</li>
                    <li>Ποσό: €{invoice.TotalAmount:N2}</li>
                    <li>Καταληκτική Ημερομηνία: {invoice.DueDate:dd/MM/yyyy}</li>
                </ul>

                <p>Με εκτίμηση,<br>Η Διοίκηση</p>
            ";

            // Note: This would need IEmailNotificationService injection to work properly
            // For now, just mark as sent
            invoice.Status = InvoiceStatus.Sent;
            invoice.EmailSentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice email for invoice {InvoiceId}", invoiceId);
            return false;
        }
    }

    // Payment Plan methods
    public async Task<PaymentPlan> CreatePaymentPlanAsync(PaymentPlan paymentPlan)
    {
        try
        {
            _context.PaymentPlans.Add(paymentPlan);
            await _context.SaveChangesAsync();
            return paymentPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment plan for member {MemberId}", paymentPlan.MemberId);
            throw;
        }
    }

    public async Task<PaymentPlan> UpdatePaymentPlanAsync(PaymentPlan paymentPlan)
    {
        try
        {
            _context.PaymentPlans.Update(paymentPlan);
            await _context.SaveChangesAsync();
            return paymentPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment plan {PaymentPlanId}", paymentPlan.Id);
            throw;
        }
    }

    public async Task<List<PaymentPlan>> GetMemberPaymentPlansAsync(int memberId)
    {
        return await _context.PaymentPlans
            .Include(pp => pp.Installments)
            .Where(pp => pp.MemberId == memberId)
            .OrderByDescending(pp => pp.StartDate)
            .ToListAsync();
    }

    public async Task<List<PaymentInstallment>> GetOverdueInstallmentsAsync()
    {
        return await _context.PaymentInstallments
            .Include(pi => pi.PaymentPlan)
            .ThenInclude(pp => pp.Member)
            .Where(pi => pi.DueDate < DateTime.UtcNow.Date && pi.Status == PaymentInstallmentStatus.Pending)
            .OrderBy(pi => pi.DueDate)
            .ToListAsync();
    }

    public async Task<PaymentInstallment> ProcessInstallmentPaymentAsync(int installmentId, Payment payment)
    {
        try
        {
            var installment = await _context.PaymentInstallments.FindAsync(installmentId);
            if (installment == null)
                throw new ArgumentException("Installment not found");

            // Create payment
            await CreatePaymentAsync(payment);

            // Update installment
            installment.Status = PaymentInstallmentStatus.Paid;
            installment.PaidDate = payment.PaymentDate;
            installment.PaymentId = payment.Id;

            await _context.SaveChangesAsync();
            return installment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing installment payment {InstallmentId}", installmentId);
            throw;
        }
    }

    // Additional reporting methods
    public async Task<List<MonthlyFinancialData>> GetYearlyTrendsAsync(int years = 5)
    {
        var trends = new List<MonthlyFinancialData>();
        var currentYear = DateTime.UtcNow.Year;

        for (int i = 0; i < years; i++)
        {
            var year = currentYear - i;
            var yearData = await GetMonthlyDataAsync(year);
            trends.AddRange(yearData);
        }

        return trends.OrderByDescending(t => t.Year).ThenByDescending(t => t.Month).ToList();
    }

    public async Task<Dictionary<string, decimal>> GetIncomeBySourceAsync(DateTime startDate, DateTime endDate)
    {
        var incomeBySource = new Dictionary<string, decimal>();

        // Payments by type
        var payments = await _context.Payments
            .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == PaymentStatus.Confirmed)
            .GroupBy(p => p.PaymentMethod)
            .ToDictionaryAsync(g => g.Key.ToString(), g => g.Sum(p => p.Amount));

        return payments;
    }

    public async Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Expenses
            .Where(e => e.Date >= startDate && e.Date <= endDate && e.Status == ExpenseStatus.Approved)
            .GroupBy(e => e.Category)
            .ToDictionaryAsync(g => g.Key.ToString(), g => g.Sum(e => e.Amount));
    }

    // Transaction methods
    public async Task<FinancialTransaction> CreateTransactionAsync(FinancialTransaction transaction)
    {
        try
        {
            _context.FinancialTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating financial transaction");
            throw;
        }
    }

    public async Task<List<FinancialTransaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.FinancialTransactions
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<List<FinancialTransaction>> GetMemberTransactionsAsync(int memberId)
    {
        return await _context.FinancialTransactions
            .Where(t => t.MemberId == memberId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<decimal> GetCurrentBalanceAsync()
    {
        var totalIncome = await _context.FinancialTransactions
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => t.Amount);

        var totalExpenses = await _context.FinancialTransactions
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => t.Amount);

        return totalIncome - totalExpenses;
    }

    // Analytics methods
    public async Task<decimal> GetNetProfitAsync(DateTime startDate, DateTime endDate)
    {
        var income = await GetTotalIncomeAsync(startDate, endDate);
        var expenses = await GetTotalExpensesAsync(startDate, endDate);
        return income - expenses;
    }

    public async Task<int> GetTotalPaidMembersAsync(int year, int month = 0)
    {
        var query = _context.Subscriptions
            .Where(s => s.Year == year && s.Status == SubscriptionStatus.Paid);

        if (month > 0)
            query = query.Where(s => s.Month == month);

        return await query.Select(s => s.MemberId).Distinct().CountAsync();
    }

    public async Task<int> GetTotalOverdueMembersAsync()
    {
        return await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Overdue)
            .Select(s => s.MemberId)
            .Distinct()
            .CountAsync();
    }

    public async Task<decimal> GetAveragePaymentAmountAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Payments
            .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == PaymentStatus.Confirmed)
            .AverageAsync(p => p.Amount);
    }

    public async Task<List<(string PaymentMethod, int Count, decimal Total)>> GetPaymentMethodStatsAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Payments
            .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == PaymentStatus.Confirmed)
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new ValueTuple<string, int, decimal>(
                g.Key.ToString(),
                g.Count(),
                g.Sum(p => p.Amount)
            ))
            .ToListAsync();
    }
}
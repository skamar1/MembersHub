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

public class ExpenseService : IExpenseService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<ExpenseService> _logger;
    private readonly IAuditService _auditService;
    private readonly IEmailNotificationService _emailService;

    public ExpenseService(
        MembersHubContext context,
        ILogger<ExpenseService> logger,
        IAuditService auditService,
        IEmailNotificationService emailService)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
        _emailService = emailService;
    }

    #region CRUD Operations

    public async Task<Expense> CreateExpenseAsync(Expense expense)
    {
        try
        {
            await ValidateCreateExpenseAsync(expense);

            expense.ExpenseNumber = await GenerateExpenseNumberAsync();
            expense.CreatedAt = DateTime.UtcNow;
            expense.Status = ExpenseStatus.Pending;
            expense.IsSynced = true;

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Create, "Expense", expense.Id.ToString(),
                expense.ExpenseNumber, $"Δημιουργήθηκε έξοδο {expense.ExpenseNumber} για {expense.Amount:C}", expense.SubmittedBy);

            _logger.LogInformation("Δημιουργήθηκε έξοδο {ExpenseNumber} από χρήστη {UserId}",
                expense.ExpenseNumber, expense.SubmittedBy);

            return expense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά τη δημιουργία εξόδου για χρήστη {UserId}", expense.SubmittedBy);
            throw;
        }
    }

    public async Task<Expense> UpdateExpenseAsync(Expense expense)
    {
        try
        {
            var existingExpense = await GetExpenseByIdAsync(expense.Id);
            if (existingExpense == null)
                throw new ArgumentException("Το έξοδο δεν βρέθηκε");

            if (existingExpense.Status != ExpenseStatus.Pending)
                throw new InvalidOperationException("Μόνο εκκρεμή έξοδα μπορούν να τροποποιηθούν");

            existingExpense.Amount = expense.Amount;
            existingExpense.ExpenseCategoryId = expense.ExpenseCategoryId;
            existingExpense.Description = expense.Description;
            existingExpense.Vendor = expense.Vendor;
            existingExpense.Date = expense.Date;
            existingExpense.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Update, "Expense", expense.Id.ToString(),
                existingExpense.ExpenseNumber, $"Ενημερώθηκε έξοδο {existingExpense.ExpenseNumber}", expense.SubmittedBy);

            _logger.LogInformation("Ενημερώθηκε έξοδο {ExpenseNumber}", existingExpense.ExpenseNumber);

            return existingExpense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά την ενημέρωση εξόδου {ExpenseId}", expense.Id);
            throw;
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        return await _context.Expenses
            .Include(e => e.Submitter)
            .Include(e => e.Approver)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == expenseId);
    }

    public async Task<List<Expense>> GetExpensesByUserAsync(int userId)
    {
        return await _context.Expenses
            .Include(e => e.Submitter)
            .Include(e => e.Approver)
            .Include(e => e.Category)
            .Where(e => e.SubmittedBy == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Expense>> GetExpensesByStatusAsync(ExpenseStatus status)
    {
        return await _context.Expenses
            .Include(e => e.Submitter)
            .Include(e => e.Approver)
            .Include(e => e.Category)
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Expense>> GetExpensesByCategoryAsync(int categoryId)
    {
        return await _context.Expenses
            .Include(e => e.Submitter)
            .Include(e => e.Approver)
            .Include(e => e.Category)
            .Where(e => e.ExpenseCategoryId == categoryId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Expense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Expenses
            .Include(e => e.Submitter)
            .Include(e => e.Approver)
            .Include(e => e.Category)
            .Where(e => e.Date >= startDate && e.Date <= endDate)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            var expense = await GetExpenseByIdAsync(expenseId);
            if (expense == null) return false;

            if (expense.Status != ExpenseStatus.Pending)
                throw new InvalidOperationException("Μόνο εκκρεμή έξοδα μπορούν να διαγραφούν");

            // Delete receipt image if exists
            if (!string.IsNullOrEmpty(expense.ReceiptImagePath))
                await DeleteReceiptImageAsync(expenseId);

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Delete, "Expense", expenseId.ToString(),
                expense.ExpenseNumber, $"Διαγράφηκε έξοδο {expense.ExpenseNumber}", expense.SubmittedBy);

            _logger.LogInformation("Διαγράφηκε έξοδο {ExpenseNumber}", expense.ExpenseNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά τη διαγραφή εξόδου {ExpenseId}", expenseId);
            throw;
        }
    }

    #endregion

    #region Expense Processing

    public async Task<Expense> SubmitExpenseAsync(Expense expense)
    {
        try
        {
            expense = await CreateExpenseAsync(expense);

            // Send notification to administrators for approval
            await NotifyAdministratorsForApprovalAsync(expense);

            _logger.LogInformation("Υποβλήθηκε έξοδο {ExpenseNumber} για έγκριση", expense.ExpenseNumber);

            return expense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά την υποβολή εξόδου");
            throw;
        }
    }

    public async Task<Expense> ApproveExpenseAsync(int expenseId, int approverId, string? notes = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var expense = await GetExpenseByIdAsync(expenseId);
            if (expense == null)
                throw new ArgumentException("Το έξοδο δεν βρέθηκε");

            if (expense.Status != ExpenseStatus.Pending)
                throw new InvalidOperationException("Μόνο εκκρεμή έξοδα μπορούν να εγκριθούν");

            expense.Status = ExpenseStatus.Approved;
            expense.IsApproved = true;
            expense.ApprovedBy = approverId;
            expense.ApprovedAt = DateTime.UtcNow;
            expense.ApprovalNotes = notes;
            expense.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Create financial transaction for the approved expense
            var transaction_record = new FinancialTransaction
            {
                TransactionNumber = await GenerateTransactionNumberAsync(),
                Type = TransactionType.Expense,
                Amount = -expense.Amount, // Negative for expense
                Description = $"Έγκριση εξόδου {expense.ExpenseNumber}: {expense.Description}",
                TransactionDate = expense.Date,
                Category = TransactionCategory.Miscellaneous, // Use Miscellaneous for all expenses for now
                ExpenseId = expense.Id,
                Status = TransactionStatus.Completed,
                CreatedBy = approverId,
                CreatedAt = DateTime.UtcNow
            };

            _context.FinancialTransactions.Add(transaction_record);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Update, "Expense", expenseId.ToString(),
                expense.ExpenseNumber, $"Εγκρίθηκε έξοδο {expense.ExpenseNumber} από χρήστη {approverId}", approverId);

            await transaction.CommitAsync();

            // Send approval notification
            await SendApprovalNotificationAsync(expense, true, notes);

            _logger.LogInformation("Εγκρίθηκε έξοδο {ExpenseNumber} από χρήστη {ApproverId}",
                expense.ExpenseNumber, approverId);

            return expense;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Σφάλμα κατά την έγκριση εξόδου {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<Expense> RejectExpenseAsync(int expenseId, int approverId, string reason)
    {
        try
        {
            var expense = await GetExpenseByIdAsync(expenseId);
            if (expense == null)
                throw new ArgumentException("Το έξοδο δεν βρέθηκε");

            if (expense.Status != ExpenseStatus.Pending)
                throw new InvalidOperationException("Μόνο εκκρεμή έξοδα μπορούν να απορριφθούν");

            expense.Status = ExpenseStatus.Rejected;
            expense.IsApproved = false;
            expense.ApprovedBy = approverId;
            expense.ApprovedAt = DateTime.UtcNow;
            expense.ApprovalNotes = reason;
            expense.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Update, "Expense", expenseId.ToString(),
                expense.ExpenseNumber, $"Απορρίφθηκε έξοδο {expense.ExpenseNumber}: {reason}", approverId);

            // Send rejection notification
            await SendApprovalNotificationAsync(expense, false, reason);

            _logger.LogInformation("Απορρίφθηκε έξοδο {ExpenseNumber} από χρήστη {ApproverId}",
                expense.ExpenseNumber, approverId);

            return expense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά την απόρριψη εξόδου {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<Expense> ReimburseExpenseAsync(int expenseId)
    {
        try
        {
            var expense = await GetExpenseByIdAsync(expenseId);
            if (expense == null)
                throw new ArgumentException("Το έξοδο δεν βρέθηκε");

            if (expense.Status != ExpenseStatus.Approved)
                throw new InvalidOperationException("Μόνο εγκεκριμένα έξοδα μπορούν να αποζημιωθούν");

            if (expense.IsReimbursed)
                throw new InvalidOperationException("Το έξοδο έχει ήδη αποζημιωθεί");

            expense.Status = ExpenseStatus.Reimbursed;
            expense.IsReimbursed = true;
            expense.ReimbursedAt = DateTime.UtcNow;
            expense.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Update, "Expense", expenseId.ToString(),
                expense.ExpenseNumber, $"Αποζημιώθηκε έξοδο {expense.ExpenseNumber}");

            _logger.LogInformation("Αποζημιώθηκε έξοδο {ExpenseNumber}", expense.ExpenseNumber);

            return expense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά την αποζημίωση εξόδου {ExpenseId}", expenseId);
            throw;
        }
    }

    #endregion

    #region Expense Management

    public async Task<string> GenerateExpenseNumberAsync()
    {
        var currentYear = DateTime.Now.Year;
        var lastExpense = await _context.Expenses
            .Where(e => e.ExpenseNumber.StartsWith($"EXP-{currentYear}-"))
            .OrderByDescending(e => e.ExpenseNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastExpense != null)
        {
            var lastNumberPart = lastExpense.ExpenseNumber.Split('-').Last();
            if (int.TryParse(lastNumberPart, out var lastNumber))
                nextNumber = lastNumber + 1;
        }

        return $"EXP-{currentYear}-{nextNumber:D4}";
    }

    public async Task<List<Expense>> GetPendingExpensesAsync()
    {
        return await GetExpensesByStatusAsync(ExpenseStatus.Pending);
    }

    public async Task<List<Expense>> GetExpensesAwaitingReimbursementAsync()
    {
        return await _context.Expenses
            .Include(e => e.Submitter)
            .Include(e => e.Approver)
            .Include(e => e.Category)
            .Where(e => e.Status == ExpenseStatus.Approved && !e.IsReimbursed)
            .OrderByDescending(e => e.ApprovedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalExpensesAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Expenses
            .Where(e => e.Date >= startDate && e.Date <= endDate && e.Status == ExpenseStatus.Approved)
            .SumAsync(e => e.Amount);
    }

    public async Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Expenses
            .Include(e => e.Category)
            .Where(e => e.Date >= startDate && e.Date <= endDate && e.Status == ExpenseStatus.Approved)
            .GroupBy(e => e.Category.Name)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(e => e.Amount));
    }

    #endregion

    #region Receipt Management

    public async Task<string> SaveReceiptImageAsync(int expenseId, byte[] imageData, string fileName)
    {
        try
        {
            var expense = await GetExpenseByIdAsync(expenseId);
            if (expense == null)
                throw new ArgumentException("Το έξοδο δεν βρέθηκε");

            // Create receipts directory if it doesn't exist
            var receiptsDir = Path.Combine("uploads", "receipts");
            Directory.CreateDirectory(receiptsDir);

            // Generate unique filename
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = $"{expense.ExpenseNumber}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(receiptsDir, uniqueFileName);

            await File.WriteAllBytesAsync(filePath, imageData);

            // Update expense with receipt path
            expense.ReceiptImagePath = filePath;
            expense.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Update, "Expense", expenseId.ToString(),
                expense.ExpenseNumber, $"Αποθηκεύτηκε απόδειξη για έξοδο {expense.ExpenseNumber}", expense.SubmittedBy);

            _logger.LogInformation("Αποθηκεύτηκε απόδειξη για έξοδο {ExpenseNumber}: {FilePath}",
                expense.ExpenseNumber, filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά την αποθήκευση απόδειξης για έξοδο {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<byte[]?> GetReceiptImageAsync(int expenseId)
    {
        try
        {
            var expense = await GetExpenseByIdAsync(expenseId);
            if (expense?.ReceiptImagePath == null) return null;

            if (File.Exists(expense.ReceiptImagePath))
                return await File.ReadAllBytesAsync(expense.ReceiptImagePath);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά την ανάκτηση απόδειξης για έξοδο {ExpenseId}", expenseId);
            return null;
        }
    }

    public async Task<bool> DeleteReceiptImageAsync(int expenseId)
    {
        try
        {
            var expense = await GetExpenseByIdAsync(expenseId);
            if (expense?.ReceiptImagePath == null) return false;

            if (File.Exists(expense.ReceiptImagePath))
                File.Delete(expense.ReceiptImagePath);

            expense.ReceiptImagePath = null;
            expense.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Update, "Expense", expenseId.ToString(),
                expense.ExpenseNumber, $"Διαγράφηκε απόδειξη για έξοδο {expense.ExpenseNumber}", expense.SubmittedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά τη διαγραφή απόδειξης για έξοδο {ExpenseId}", expenseId);
            return false;
        }
    }

    #endregion

    #region Bulk Operations

    public async Task<List<Expense>> BulkApproveExpensesAsync(List<int> expenseIds, int approverId)
    {
        var approvedExpenses = new List<Expense>();

        foreach (var expenseId in expenseIds)
        {
            try
            {
                var approved = await ApproveExpenseAsync(expenseId, approverId);
                approvedExpenses.Add(approved);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Αποτυχία έγκρισης εξόδου {ExpenseId} κατά τη μαζική έγκριση", expenseId);
            }
        }

        _logger.LogInformation("Μαζική έγκριση: {ApprovedCount} από {TotalCount} έξοδα εγκρίθηκαν από χρήστη {ApproverId}",
            approvedExpenses.Count, expenseIds.Count, approverId);

        return approvedExpenses;
    }

    public async Task<List<Expense>> BulkRejectExpensesAsync(List<int> expenseIds, int approverId, string reason)
    {
        var rejectedExpenses = new List<Expense>();

        foreach (var expenseId in expenseIds)
        {
            try
            {
                var rejected = await RejectExpenseAsync(expenseId, approverId, reason);
                rejectedExpenses.Add(rejected);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Αποτυχία απόρριψης εξόδου {ExpenseId} κατά τη μαζική απόρριψη", expenseId);
            }
        }

        _logger.LogInformation("Μαζική απόρριψη: {RejectedCount} από {TotalCount} έξοδα απορρίφθηκαν από χρήστη {ApproverId}",
            rejectedExpenses.Count, expenseIds.Count, approverId);

        return rejectedExpenses;
    }

    public async Task<List<Expense>> BulkReimburseExpensesAsync(List<int> expenseIds)
    {
        var reimbursedExpenses = new List<Expense>();

        foreach (var expenseId in expenseIds)
        {
            try
            {
                var reimbursed = await ReimburseExpenseAsync(expenseId);
                reimbursedExpenses.Add(reimbursed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Αποτυχία αποζημίωσης εξόδου {ExpenseId} κατά τη μαζική αποζημίωση", expenseId);
            }
        }

        _logger.LogInformation("Μαζική αποζημίωση: {ReimbursedCount} από {TotalCount} έξοδα αποζημιώθηκαν",
            reimbursedExpenses.Count, expenseIds.Count);

        return reimbursedExpenses;
    }

    #endregion

    #region Reports & Analytics

    public async Task<decimal> GetUserTotalExpensesAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _context.Expenses
            .Where(e => e.SubmittedBy == userId &&
                       e.Date >= startDate && e.Date <= endDate &&
                       e.Status == ExpenseStatus.Approved)
            .SumAsync(e => e.Amount);
    }

    public async Task<Dictionary<string, decimal>> GetMonthlyExpenseTrendsAsync(int year)
    {
        var monthlyTrends = new Dictionary<string, decimal>();

        for (int month = 1; month <= 12; month++)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var total = await GetTotalExpensesAsync(startDate, endDate);
            monthlyTrends.Add($"{year}-{month:D2}", total);
        }

        return monthlyTrends;
    }

    public async Task<List<(int CategoryId, string CategoryName, decimal Amount, int Count)>> GetExpenseCategoryStatsAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Expenses
            .Include(e => e.Category)
            .Where(e => e.Date >= startDate && e.Date <= endDate && e.Status == ExpenseStatus.Approved)
            .GroupBy(e => new { e.Category.Id, e.Category.Name })
            .Select(g => new ValueTuple<int, string, decimal, int>(
                g.Key.Id,
                g.Key.Name,
                g.Sum(e => e.Amount),
                g.Count()
            ))
            .ToListAsync();
    }

    public async Task<List<(int UserId, string UserName, decimal Amount)>> GetTopSpendersAsync(DateTime startDate, DateTime endDate, int limit = 10)
    {
        return await _context.Expenses
            .Include(e => e.Submitter)
            .Where(e => e.Date >= startDate && e.Date <= endDate && e.Status == ExpenseStatus.Approved)
            .GroupBy(e => new { e.SubmittedBy, e.Submitter.FirstName, e.Submitter.LastName })
            .Select(g => new ValueTuple<int, string, decimal>(
                g.Key.SubmittedBy,
                $"{g.Key.FirstName} {g.Key.LastName}",
                g.Sum(e => e.Amount)
            ))
            .OrderByDescending(x => x.Item3)
            .Take(limit)
            .ToListAsync();
    }

    #endregion

    #region Private Helper Methods

    private async Task ValidateCreateExpenseAsync(Expense expense)
    {
        var errors = new List<string>();

        if (expense.Amount <= 0)
            errors.Add("Το ποσό πρέπει να είναι μεγαλύτερο από το μηδέν");

        if (string.IsNullOrWhiteSpace(expense.Description))
            errors.Add("Η περιγραφή είναι υποχρεωτική");

        if (expense.Date > DateTime.Now.Date)
            errors.Add("Η ημερομηνία δεν μπορεί να είναι μελλοντική");

        if (expense.Date < DateTime.Now.AddYears(-2))
            errors.Add("Η ημερομηνία δεν μπορεί να είναι παλαιότερη των 2 ετών");

        if (expense.SubmittedBy <= 0)
            errors.Add("Ο υποβάλλων χρήστης είναι υποχρεωτικός");

        var user = await _context.Users.FindAsync(expense.SubmittedBy);
        if (user == null)
            errors.Add("Ο υποβάλλων χρήστης δεν βρέθηκε");

        if (errors.Any())
            throw new ArgumentException(string.Join(", ", errors));
    }

    private async Task<string> GenerateTransactionNumberAsync()
    {
        var currentYear = DateTime.Now.Year;
        var lastTransaction = await _context.FinancialTransactions
            .Where(t => t.TransactionNumber.StartsWith($"TXN-{currentYear}-"))
            .OrderByDescending(t => t.TransactionNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastTransaction != null)
        {
            var lastNumberPart = lastTransaction.TransactionNumber.Split('-').Last();
            if (int.TryParse(lastNumberPart, out var lastNumber))
                nextNumber = lastNumber + 1;
        }

        return $"TXN-{currentYear}-{nextNumber:D6}";
    }

    private async Task NotifyAdministratorsForApprovalAsync(Expense expense)
    {
        try
        {
            var admins = await _context.Users
                .Where(u => (u.Role == UserRole.Admin || u.Role == UserRole.Owner) && !string.IsNullOrEmpty(u.Email))
                .ToListAsync();

            foreach (var admin in admins)
            {
                var subject = $"Νέο έξοδο προς έγκριση: {expense.ExpenseNumber}";
                var body = $@"
                    <h3>Νέο Έξοδο προς Έγκριση</h3>
                    <p><strong>Αριθμός:</strong> {expense.ExpenseNumber}</p>
                    <p><strong>Ποσό:</strong> {expense.Amount:C}</p>
                    <p><strong>Κατηγορία:</strong> {expense.Category?.Name ?? "Δεν καθορίστηκε"}</p>
                    <p><strong>Περιγραφή:</strong> {expense.Description}</p>
                    <p><strong>Ημερομηνία:</strong> {expense.Date:dd/MM/yyyy}</p>
                    <p><strong>Προμηθευτής:</strong> {expense.Vendor ?? "Δεν καθορίστηκε"}</p>
                    <p><strong>Υποβλήθηκε από:</strong> {expense.Submitter?.FirstName} {expense.Submitter?.LastName}</p>
                    <p>Παρακαλώ συνδεθείτε στο σύστημα για να εγκρίνετε ή να απορρίψετε το έξοδο.</p>
                ";

                await _emailService.SendEmailAsync(admin.Email, subject, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Αποτυχία αποστολής ειδοποίησης για έγκριση εξόδου {ExpenseNumber}", expense.ExpenseNumber);
        }
    }

    private async Task SendApprovalNotificationAsync(Expense expense, bool approved, string? notes)
    {
        try
        {
            if (expense.Submitter?.Email == null) return;

            var status = approved ? "Εγκρίθηκε" : "Απορρίφθηκε";
            var subject = $"Έξοδο {expense.ExpenseNumber}: {status}";
            var body = $@"
                <h3>Ενημέρωση Κατάστασης Εξόδου</h3>
                <p><strong>Αριθμός:</strong> {expense.ExpenseNumber}</p>
                <p><strong>Ποσό:</strong> {expense.Amount:C}</p>
                <p><strong>Κατάσταση:</strong> {status}</p>
                <p><strong>Ημερομηνία απόφασης:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                {(notes != null ? $"<p><strong>Σχόλια:</strong> {notes}</p>" : "")}
                <p>Μπορείτε να δείτε περισσότερες λεπτομέρειες συνδεόμενος στο σύστημα.</p>
            ";

            await _emailService.SendEmailAsync(expense.Submitter.Email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Αποτυχία αποστολής ειδοποίησης έγκρισης για έξοδο {ExpenseNumber}", expense.ExpenseNumber);
        }
    }

    #endregion
}
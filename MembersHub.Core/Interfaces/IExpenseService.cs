using MembersHub.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MembersHub.Core.Interfaces;

/// <summary>
/// Υπηρεσία διαχείρισης εξόδων
/// </summary>
public interface IExpenseService
{
    // Expense CRUD Operations
    Task<Expense> CreateExpenseAsync(Expense expense);
    Task<Expense> UpdateExpenseAsync(Expense expense);
    Task<Expense?> GetExpenseByIdAsync(int expenseId);
    Task<List<Expense>> GetExpensesByUserAsync(int userId);
    Task<List<Expense>> GetExpensesByStatusAsync(ExpenseStatus status);
    Task<List<Expense>> GetExpensesByCategoryAsync(int categoryId);
    Task<List<Expense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<bool> DeleteExpenseAsync(int expenseId);

    // Expense Processing
    Task<Expense> SubmitExpenseAsync(Expense expense);
    Task<Expense> ApproveExpenseAsync(int expenseId, int approverId, string? notes = null);
    Task<Expense> RejectExpenseAsync(int expenseId, int approverId, string reason);
    Task<Expense> ReimburseExpenseAsync(int expenseId);

    // Expense Management
    Task<string> GenerateExpenseNumberAsync();
    Task<List<Expense>> GetPendingExpensesAsync();
    Task<List<Expense>> GetExpensesAwaitingReimbursementAsync();
    Task<decimal> GetTotalExpensesAsync(DateTime startDate, DateTime endDate);
    Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(DateTime startDate, DateTime endDate);
    
    // Receipt Management
    Task<string> SaveReceiptImageAsync(int expenseId, byte[] imageData, string fileName);
    Task<byte[]?> GetReceiptImageAsync(int expenseId);
    Task<bool> DeleteReceiptImageAsync(int expenseId);
    
    // Bulk Operations
    Task<List<Expense>> BulkApproveExpensesAsync(List<int> expenseIds, int approverId);
    Task<List<Expense>> BulkRejectExpensesAsync(List<int> expenseIds, int approverId, string reason);
    Task<List<Expense>> BulkReimburseExpensesAsync(List<int> expenseIds);
    
    // Reports & Analytics
    Task<decimal> GetUserTotalExpensesAsync(int userId, DateTime startDate, DateTime endDate);
    Task<Dictionary<string, decimal>> GetMonthlyExpenseTrendsAsync(int year);
    Task<List<(int CategoryId, string CategoryName, decimal Amount, int Count)>> GetExpenseCategoryStatsAsync(DateTime startDate, DateTime endDate);
    Task<List<(int UserId, string UserName, decimal Amount)>> GetTopSpendersAsync(DateTime startDate, DateTime endDate, int limit = 10);
}
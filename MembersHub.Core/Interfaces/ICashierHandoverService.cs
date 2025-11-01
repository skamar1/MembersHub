using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface ICashierHandoverService
{
    /// <summary>
    /// Get active (current) handover period for a cashier
    /// </summary>
    Task<CashierHandover?> GetActivePeriodAsync(int cashierId);

    /// <summary>
    /// Get all handovers for a specific cashier
    /// </summary>
    Task<List<CashierHandover>> GetCashierHandoversAsync(int cashierId);

    /// <summary>
    /// Get all handovers (Admin/Owner view)
    /// </summary>
    Task<List<CashierHandover>> GetAllHandoversAsync();

    /// <summary>
    /// Get pending handovers that need confirmation
    /// </summary>
    Task<List<CashierHandover>> GetPendingHandoversAsync();

    /// <summary>
    /// Get a specific handover by ID
    /// </summary>
    Task<CashierHandover?> GetHandoverByIdAsync(int id);

    /// <summary>
    /// Create a new handover (cashier closes their period)
    /// </summary>
    Task<CashierHandover> CreateHandoverAsync(int cashierId, string? notes = null);

    /// <summary>
    /// Confirm a handover (Admin/Owner accepts it)
    /// </summary>
    Task<bool> ConfirmHandoverAsync(int handoverId, int receivedById, string? notes = null);

    /// <summary>
    /// Reject a handover (discrepancies found)
    /// </summary>
    Task<bool> RejectHandoverAsync(int handoverId, int rejectedById, string reason);

    /// <summary>
    /// Get cashier's financial summary for current period
    /// </summary>
    Task<(decimal TotalCollections, decimal TotalExpenses, decimal NetBalance, DateTime PeriodStart)>
        GetCashierCurrentPeriodSummaryAsync(int cashierId);

    /// <summary>
    /// Get detailed transactions for a handover period
    /// </summary>
    Task<(List<Payment> Payments, List<Expense> Expenses)> GetHandoverTransactionsAsync(int handoverId);
}

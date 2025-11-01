namespace MembersHub.Core.Entities;

/// <summary>
/// Represents a cashier handover/closeout period
/// </summary>
public class CashierHandover
{
    public int Id { get; set; }

    /// <summary>
    /// The cashier who is handing over
    /// </summary>
    public int CashierId { get; set; }
    public User Cashier { get; set; } = null!;

    /// <summary>
    /// The admin/owner who is receiving the handover
    /// </summary>
    public int? ReceivedById { get; set; }
    public User? ReceivedBy { get; set; }

    /// <summary>
    /// Start date of the period
    /// </summary>
    public DateTime PeriodStartDate { get; set; }

    /// <summary>
    /// End date of the period (handover date)
    /// </summary>
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Total collections during this period
    /// </summary>
    public decimal TotalCollections { get; set; }

    /// <summary>
    /// Total expenses during this period
    /// </summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>
    /// Net balance (collections - expenses)
    /// </summary>
    public decimal NetBalance { get; set; }

    /// <summary>
    /// Status of the handover
    /// </summary>
    public HandoverStatus Status { get; set; }

    /// <summary>
    /// Notes or comments about the handover
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Date when handover was created
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Date when handover was confirmed by admin/owner
    /// </summary>
    public DateTime? ConfirmedDate { get; set; }
}

public enum HandoverStatus
{
    /// <summary>
    /// Handover is pending confirmation
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Handover has been confirmed by admin/owner
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Handover was rejected (discrepancies found)
    /// </summary>
    Rejected = 2
}

using System;

namespace MembersHub.Core.Entities;

/// <summary>
/// Παράδοση ταμείου από εισπράκτορα
/// </summary>
public class CashBoxDelivery
{
    public int Id { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty; // π.χ. "DEL-2025-0001"
    public int CollectorId { get; set; }
    public DateTime DeliveryDate { get; set; }
    public decimal TotalCollections { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int ReceivedBy { get; set; } // Admin/Treasurer που παρέλαβε
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual User Collector { get; set; } = null!;
    public virtual User Receiver { get; set; } = null!;
}

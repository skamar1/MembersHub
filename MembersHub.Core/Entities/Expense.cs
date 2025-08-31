using System;

namespace MembersHub.Core.Entities;

public class Expense
{
    public int Id { get; set; }
    public int CollectorId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty; // π.χ. "Καύσιμα", "Γεύματα", "Υλικά"
    public string Description { get; set; } = string.Empty;
    public string? ReceiptImagePath { get; set; }
    public bool IsApproved { get; set; }
    public int? ApprovedBy { get; set; }
    public bool IsSynced { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual User Collector { get; set; } = null!;
    public virtual User? Approver { get; set; }
}
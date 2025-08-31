using System;

namespace MembersHub.Core.Entities;

public class Payment
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int? SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public int CollectorId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsSynced { get; set; } = true;
    public bool EmailSent { get; set; }
    public bool SmsSent { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Member Member { get; set; } = null!;
    public virtual Subscription? Subscription { get; set; }
    public virtual User Collector { get; set; } = null!;
}

public enum PaymentMethod
{
    Cash,
    Card,
    BankTransfer
}
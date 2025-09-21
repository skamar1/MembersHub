using System;

namespace MembersHub.Core.Entities;

public class Payment
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int? SubscriptionId { get; set; }
    public int? InvoiceId { get; set; }
    public int? PaymentInstallmentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public int CollectorId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Confirmed;
    public string ReceiptNumber { get; set; } = string.Empty;
    public string? TransactionReference { get; set; } // Αναφορά τράπεζας/κάρτας
    public string? Notes { get; set; }
    public bool IsSynced { get; set; } = true;
    public bool EmailSent { get; set; }
    public bool SmsSent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Member Member { get; set; } = null!;
    public virtual Subscription? Subscription { get; set; }
    public virtual Invoice? Invoice { get; set; }
    public virtual PaymentInstallment? PaymentInstallment { get; set; }
    public virtual User Collector { get; set; } = null!;
    public virtual ICollection<FinancialTransaction> Transactions { get; set; } = new List<FinancialTransaction>();
}

public enum PaymentMethod
{
    Cash,
    Card,
    BankTransfer,
    DigitalWallet, // PayPal, Apple Pay κλπ
    Check,
    Other
}

public enum PaymentStatus
{
    Pending,    // Εκκρεμής
    Confirmed,  // Επιβεβαιωμένη
    Failed,     // Αποτυχημένη
    Refunded,   // Επιστράφηκε
    Cancelled   // Ακυρωμένη
}
using System;

namespace MembersHub.Core.Entities;

/// <summary>
/// Γενικές οικονομικές συναλλαγές για πλήρη παρακολούθηση χρημάτων
/// </summary>
public class FinancialTransaction
{
    public int Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty; // π.χ. "TXN-2024-0001"
    public DateTime TransactionDate { get; set; }
    public TransactionType Type { get; set; }
    public TransactionCategory Category { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; } // Αριθμός αναφοράς
    public int? MemberId { get; set; } // Αν αφορά συγκεκριμένο μέλος
    public int? PaymentId { get; set; } // Σύνδεση με πληρωμή
    public int? ExpenseId { get; set; } // Σύνδεση με έξοδο
    public int? InvoiceId { get; set; } // Σύνδεση με τιμολόγιο
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Member? Member { get; set; }
    public virtual Payment? Payment { get; set; }
    public virtual Expense? Expense { get; set; }
    public virtual Invoice? Invoice { get; set; }
    public virtual User Creator { get; set; } = null!;
}

public enum TransactionType
{
    Income,  // Έσοδο
    Expense  // Έξοδο
}

public enum TransactionCategory
{
    // Έσοδα
    MembershipFee,        // Συνδρομή μέλους
    Registration,         // Εγγραφή
    Event,               // Εκδήλωση
    Donation,            // Δωρεά
    Other,               // Άλλο έσοδο
    
    // Έξοδα
    OfficeSupplies,      // Γραφική ύλη
    Equipment,           // Εξοπλισμός
    Maintenance,         // Συντήρηση
    Utilities,           // Λογαριασμοί
    Travel,              // Ταξίδια
    Meals,               // Γεύματα
    Insurance,           // Ασφάλιση
    Legal,               // Νομικά
    Marketing,           // Μάρκετινγκ
    Miscellaneous        // Διάφορα
}

public enum TransactionStatus
{
    Pending,    // Εκκρεμής
    Completed,  // Ολοκληρωμένη
    Cancelled,  // Ακυρωμένη
    Failed      // Αποτυχημένη
}
using System;

namespace MembersHub.Core.Entities;

public class Expense
{
    public int Id { get; set; }
    public string ExpenseNumber { get; set; } = string.Empty; // π.χ. "EXP-2024-0001"
    public int SubmittedBy { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int ExpenseCategoryId { get; set; } // Foreign key to ExpenseCategory
    public string Description { get; set; } = string.Empty;
    public string? Vendor { get; set; } // Προμηθευτής
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;
    public string? ReceiptImagePath { get; set; }
    public bool IsApproved { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
    public bool IsReimbursed { get; set; } // Αν έχει αποζημιωθεί
    public DateTime? ReimbursedAt { get; set; }
    public bool IsSynced { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User Submitter { get; set; } = null!;
    public virtual User? Approver { get; set; }
    public virtual ExpenseCategory Category { get; set; } = null!;
    public virtual ICollection<FinancialTransaction> Transactions { get; set; } = new List<FinancialTransaction>();
}

public enum ExpenseStatus
{
    Pending,       // Εκκρεμής
    Approved,      // Εγκεκριμένο
    Rejected,      // Απορριφθέν
    Reimbursed     // Αποζημιωμένο
}
using System;
using System.Collections.Generic;

namespace MembersHub.Core.Entities;

/// <summary>
/// Σχέδια πληρωμής για μέλη που θέλουν να πληρώνουν σε δόσεις
/// </summary>
public class PaymentPlan
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string Name { get; set; } = string.Empty; // π.χ. "Εξάμηνη συνδρομή σε 6 δόσεις"
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public PaymentPlanStatus Status { get; set; } = PaymentPlanStatus.Active;
    public DateTime StartDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Member Member { get; set; } = null!;
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<PaymentInstallment> Installments { get; set; } = new List<PaymentInstallment>();
}

/// <summary>
/// Μεμονωμένες δόσεις ενός σχεδίου πληρωμής
/// </summary>
public class PaymentInstallment
{
    public int Id { get; set; }
    public int PaymentPlanId { get; set; }
    public int InstallmentNumber { get; set; } // 1η δόση, 2η δόση κλπ
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public PaymentInstallmentStatus Status { get; set; } = PaymentInstallmentStatus.Pending;
    public int? PaymentId { get; set; } // Σύνδεση με το payment που έγινε
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual PaymentPlan PaymentPlan { get; set; } = null!;
    public virtual Payment? Payment { get; set; }
}

public enum PaymentPlanStatus
{
    Active,
    Completed,
    Cancelled,
    Suspended
}

public enum PaymentInstallmentStatus
{
    Pending,
    Paid,
    Overdue,
    Cancelled
}
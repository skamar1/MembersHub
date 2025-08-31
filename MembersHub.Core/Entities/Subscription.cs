using System;
using System.Collections.Generic;

namespace MembersHub.Core.Entities;

public class Subscription
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Member Member { get; set; } = null!;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum SubscriptionStatus
{
    Pending,
    Paid,
    Overdue
}
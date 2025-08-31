using System;
using System.Collections.Generic;

namespace MembersHub.Core.Entities;

public class Member
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int MembershipTypeId { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public MemberStatus Status { get; set; } = MemberStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    
    // Navigation properties
    public virtual MembershipType MembershipType { get; set; } = null!;
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum MemberStatus
{
    Active,
    Inactive,
    Suspended
}
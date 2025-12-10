using System;
using System.Collections.Generic;

namespace MembersHub.Core.Entities;

public class Member
{
    public int Id { get; set; }

    // Basic Information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }

    // Application Information
    public string? ApplicationNumber { get; set; }
    public int? DepartmentId { get; set; }

    // Parents Information
    public string? FatherFullName { get; set; }
    public string? MotherFullName { get; set; }

    // Address Information
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }

    // Official Documents
    public string? IdNumber { get; set; }        // Αριθμός ταυτότητας
    public string? SocialSecurityNumber { get; set; }  // ΑΜΚΑ
    public string? TaxNumber { get; set; }       // ΑΦΜ

    // Guardian Information (for minors)
    public string? GuardianFullName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianTaxNumber { get; set; }
    public string? GuardianEmail { get; set; }

    // Membership Information
    public int MembershipTypeId { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public MemberStatus Status { get; set; } = MemberStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    public bool IsMinor => DateOfBirth.HasValue && DateTime.Today.Year - DateOfBirth.Value.Year < 18;
    public string DisplayAddress => string.IsNullOrEmpty(Address) ? "" : $"{Address}, {PostalCode} {City}".Trim(' ', ',');

    // Navigation properties
    public virtual MembershipType MembershipType { get; set; } = null!;
    public virtual Department? Department { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum MemberStatus
{
    Active,
    Inactive,
    Suspended
}

public enum Gender
{
    Male,
    Female,
    Other
}
using System;
using MembersHub.Core.Entities;

namespace MembersHub.Application.DTOs;

public class MemberDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int MembershipTypeId { get; set; }
    public string MembershipTypeName { get; set; } = string.Empty;
    public decimal MonthlyFee { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public MemberStatus Status { get; set; }
    public decimal OutstandingBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateMemberDto
{
    // Basic Information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }

    // Application Information
    public string? ApplicationNumber { get; set; }
    public string? Department { get; set; }

    // Parents Information
    public string? FatherFullName { get; set; }
    public string? MotherFullName { get; set; }

    // Address Information
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }

    // Official Documents
    public string? IdNumber { get; set; }
    public string? SocialSecurityNumber { get; set; }
    public string? TaxNumber { get; set; }

    // Guardian Information (for minors)
    public string? GuardianFullName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianTaxNumber { get; set; }
    public string? GuardianEmail { get; set; }

    // Membership Information
    public int MembershipTypeId { get; set; }

    public Member ToEntity()
    {
        return new Member
        {
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Phone = Phone,
            DateOfBirth = DateOfBirth,
            Gender = Gender,
            ApplicationNumber = ApplicationNumber,
            Department = Department,
            FatherFullName = FatherFullName,
            MotherFullName = MotherFullName,
            Address = Address,
            PostalCode = PostalCode,
            City = City,
            IdNumber = IdNumber,
            SocialSecurityNumber = SocialSecurityNumber,
            TaxNumber = TaxNumber,
            GuardianFullName = GuardianFullName,
            GuardianPhone = GuardianPhone,
            GuardianTaxNumber = GuardianTaxNumber,
            GuardianEmail = GuardianEmail,
            MembershipTypeId = MembershipTypeId,
            Status = MemberStatus.Active
        };
    }
}

public class UpdateMemberDto
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
    public string? Department { get; set; }

    // Parents Information
    public string? FatherFullName { get; set; }
    public string? MotherFullName { get; set; }

    // Address Information
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }

    // Official Documents
    public string? IdNumber { get; set; }
    public string? SocialSecurityNumber { get; set; }
    public string? TaxNumber { get; set; }

    // Guardian Information (for minors)
    public string? GuardianFullName { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianTaxNumber { get; set; }
    public string? GuardianEmail { get; set; }

    // Membership Information
    public int MembershipTypeId { get; set; }
    public MemberStatus Status { get; set; }
}

public class MemberListDto
{
    public int Id { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string MembershipTypeName { get; set; } = string.Empty;
    public decimal MonthlyFee { get; set; }
    public MemberStatus Status { get; set; }
    public decimal OutstandingBalance { get; set; }
}
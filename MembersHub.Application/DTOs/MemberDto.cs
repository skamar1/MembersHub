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
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
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
            MembershipTypeId = MembershipTypeId,
            Status = MemberStatus.Active
        };
    }
}

public class UpdateMemberDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
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
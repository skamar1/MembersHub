using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MembersHub.Application.DTOs;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Application.Services;

public class MemberService : IMemberService
{
    private readonly MembersHubContext _context;

    public MemberService(MembersHubContext context)
    {
        _context = context;
    }

    public async Task<Member?> GetByIdAsync(int id)
    {
        return await _context.Members
            .Include(m => m.MembershipType)
            .Include(m => m.Subscriptions)
            .Include(m => m.Payments)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Member?> GetByMemberNumberAsync(string memberNumber)
    {
        return await _context.Members
            .Include(m => m.MembershipType)
            .FirstOrDefaultAsync(m => m.MemberNumber == memberNumber);
    }

    public async Task<IEnumerable<Member>> GetAllAsync()
    {
        return await _context.Members
            .Include(m => m.MembershipType)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Member>> GetAllActiveAsync()
    {
        return await _context.Members
            .Include(m => m.MembershipType)
            .Where(m => m.Status == MemberStatus.Active)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Member>> SearchAsync(string searchTerm)
    {
        var query = _context.Members
            .Include(m => m.MembershipType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(m =>
                m.FirstName.ToLower().Contains(searchTerm) ||
                m.LastName.ToLower().Contains(searchTerm) ||
                m.MemberNumber.ToLower().Contains(searchTerm) ||
                m.Phone.Contains(searchTerm) ||
                (m.Email != null && m.Email.ToLower().Contains(searchTerm))
            );
        }

        return await query.OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ToListAsync();
    }

    public async Task<Member> CreateAsync(Member member)
    {
        // Generate member number
        member.MemberNumber = await GenerateMemberNumberAsync(member.MembershipTypeId);
        member.CreatedAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;
        member.Status = MemberStatus.Active;

        _context.Members.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task UpdateAsync(Member member)
    {
        member.UpdatedAt = DateTime.UtcNow;
        _context.Members.Update(member);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var member = await _context.Members.FindAsync(id);
        if (member != null)
        {
            _context.Members.Remove(member);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<decimal> GetOutstandingBalanceAsync(int memberId)
    {
        var outstandingSubscriptions = await _context.Subscriptions
            .Where(s => s.MemberId == memberId && s.Status == SubscriptionStatus.Pending)
            .SumAsync(s => s.Amount);

        return outstandingSubscriptions;
    }

    public async Task<bool> ExistsAsync(string memberNumber)
    {
        return await _context.Members.AnyAsync(m => m.MemberNumber == memberNumber);
    }

    private async Task<string> GenerateMemberNumberAsync(int membershipTypeId)
    {
        var membershipType = await _context.MembershipTypes.FindAsync(membershipTypeId);
        var prefix = membershipType?.Name switch
        {
            "Ενήλικες" => "A",
            "Παιδιά" => "K",
            "Φοιτητές" => "F",
            _ => "M"
        };

        // Find the last member number with this prefix
        var lastMember = await _context.Members
            .Where(m => m.MemberNumber.StartsWith(prefix))
            .OrderByDescending(m => m.MemberNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastMember != null)
        {
            // Extract the numeric part
            var numericPart = lastMember.MemberNumber.Substring(1);
            if (int.TryParse(numericPart, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D3}"; // Format: A001, K001, F001, etc.
    }
}

// Extension methods for DTOs
public static class MemberExtensions
{
    public static MemberDto ToDto(this Member member)
    {
        return new MemberDto
        {
            Id = member.Id,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            Phone = member.Phone,
            DateOfBirth = member.DateOfBirth,
            MembershipTypeId = member.MembershipTypeId,
            MembershipTypeName = member.MembershipType?.Name ?? "",
            MonthlyFee = member.MembershipType?.MonthlyFee ?? 0,
            MemberNumber = member.MemberNumber,
            Status = member.Status,
            CreatedAt = member.CreatedAt,
            UpdatedAt = member.UpdatedAt
        };
    }

    public static MemberListDto ToListDto(this Member member, decimal outstandingBalance = 0)
    {
        return new MemberListDto
        {
            Id = member.Id,
            MemberNumber = member.MemberNumber,
            FullName = member.FullName,
            Phone = member.Phone,
            Email = member.Email,
            MembershipTypeName = member.MembershipType?.Name ?? "",
            MonthlyFee = member.MembershipType?.MonthlyFee ?? 0,
            Status = member.Status,
            OutstandingBalance = outstandingBalance
        };
    }

    public static Member ToEntity(this CreateMemberDto dto)
    {
        return new Member
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            DateOfBirth = dto.DateOfBirth,
            MembershipTypeId = dto.MembershipTypeId
        };
    }
}
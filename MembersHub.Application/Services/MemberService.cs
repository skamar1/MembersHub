using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Application.DTOs;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Application.Services;

public class MemberService : IMemberService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<MemberService> _logger;
    private readonly IAuditService? _auditService;

    public MemberService(MembersHubContext context, ILogger<MemberService> logger, IAuditService? auditService = null)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
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
        try
        {
            // Validation
            await ValidateCreateMemberAsync(member);

            // Generate member number
            member.MemberNumber = await GenerateMemberNumberAsync(member.MembershipTypeId);
            member.CreatedAt = DateTime.UtcNow;
            member.UpdatedAt = DateTime.UtcNow;
            member.Status = MemberStatus.Active;

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new member {MemberNumber}: {FullName}", member.MemberNumber, member.FullName);

            // Audit logging if available
            if (_auditService != null)
            {
                await _auditService.LogCreateAsync(member, 0, "System", "System", "127.0.0.1");
            }

            return member;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating member: {FullName}", member.FullName);
            throw;
        }
    }

    public async Task UpdateAsync(Member member)
    {
        try
        {
            var existingMember = await _context.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == member.Id);
            if (existingMember == null)
            {
                throw new InvalidOperationException($"Member with ID {member.Id} not found.");
            }

            // Validation
            await ValidateUpdateMemberAsync(member);

            member.UpdatedAt = DateTime.UtcNow;
            _context.Members.Update(member);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated member {MemberNumber}: {FullName}", member.MemberNumber, member.FullName);

            // Audit logging if available
            if (_auditService != null)
            {
                await _auditService.LogUpdateAsync(existingMember, member, 0, "System", "System", "127.0.0.1");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member {MemberId}", member.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                throw new InvalidOperationException($"Member with ID {id} not found.");
            }

            // Check for dependencies before deletion
            var hasPayments = await _context.Payments.AnyAsync(p => p.MemberId == id);
            var hasSubscriptions = await _context.Subscriptions.AnyAsync(s => s.MemberId == id);

            if (hasPayments || hasSubscriptions)
            {
                // Soft delete - just change status
                member.Status = MemberStatus.Inactive;
                member.UpdatedAt = DateTime.UtcNow;
                _context.Members.Update(member);

                _logger.LogInformation("Soft deleted member {MemberNumber} due to existing payments/subscriptions", member.MemberNumber);
            }
            else
            {
                // Hard delete
                _context.Members.Remove(member);
                _logger.LogInformation("Hard deleted member {MemberNumber}", member.MemberNumber);
            }

            // Audit logging if available
            if (_auditService != null)
            {
                await _auditService.LogDeleteAsync(member, 0, "System", "System", "127.0.0.1");
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting member {MemberId}", id);
            throw;
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

    private async Task ValidateCreateMemberAsync(Member member)
    {
        var errors = new List<string>();

        // Required fields validation
        if (string.IsNullOrWhiteSpace(member.FirstName))
            errors.Add("Το όνομα είναι υποχρεωτικό");

        if (string.IsNullOrWhiteSpace(member.LastName))
            errors.Add("Το επώνυμο είναι υποχρεωτικό");

        if (string.IsNullOrWhiteSpace(member.Phone))
            errors.Add("Το τηλέφωνο είναι υποχρεωτικό");

        // Phone validation
        if (!string.IsNullOrEmpty(member.Phone) && member.Phone.Length < 10)
            errors.Add("Το τηλέφωνο πρέπει να έχει τουλάχιστον 10 ψηφία");

        // Email validation (if provided)
        if (!string.IsNullOrEmpty(member.Email) && !IsValidEmail(member.Email))
            errors.Add("Μη έγκυρη διεύθυνση email");

        // Check for duplicate phone
        var existingByPhone = await _context.Members.AnyAsync(m => m.Phone == member.Phone);
        if (existingByPhone)
            errors.Add("Υπάρχει ήδη μέλος με αυτό το τηλέφωνο");

        // Check for duplicate email (if provided)
        if (!string.IsNullOrEmpty(member.Email))
        {
            var existingByEmail = await _context.Members.AnyAsync(m => m.Email == member.Email);
            if (existingByEmail)
                errors.Add("Υπάρχει ήδη μέλος με αυτό το email");
        }

        // Membership type validation
        var membershipTypeExists = await _context.MembershipTypes
            .AnyAsync(mt => mt.Id == member.MembershipTypeId && mt.IsActive);
        if (!membershipTypeExists)
            errors.Add("Μη έγκυρος τύπος συνδρομής");

        if (errors.Any())
        {
            throw new ArgumentException($"Validation errors: {string.Join(", ", errors)}");
        }
    }

    private async Task ValidateUpdateMemberAsync(Member member)
    {
        var errors = new List<string>();

        // Required fields validation
        if (string.IsNullOrWhiteSpace(member.FirstName))
            errors.Add("Το όνομα είναι υποχρεωτικό");

        if (string.IsNullOrWhiteSpace(member.LastName))
            errors.Add("Το επώνυμο είναι υποχρεωτικό");

        if (string.IsNullOrWhiteSpace(member.Phone))
            errors.Add("Το τηλέφωνο είναι υποχρεωτικό");

        // Phone validation
        if (!string.IsNullOrEmpty(member.Phone) && member.Phone.Length < 10)
            errors.Add("Το τηλέφωνο πρέπει να έχει τουλάχιστον 10 ψηφία");

        // Email validation (if provided)
        if (!string.IsNullOrEmpty(member.Email) && !IsValidEmail(member.Email))
            errors.Add("Μη έγκυρη διεύθυνση email");

        // Check for duplicate phone (excluding current member)
        var existingByPhone = await _context.Members.AnyAsync(m => m.Phone == member.Phone && m.Id != member.Id);
        if (existingByPhone)
            errors.Add("Υπάρχει ήδη μέλος με αυτό το τηλέφωνο");

        // Check for duplicate email (if provided, excluding current member)
        if (!string.IsNullOrEmpty(member.Email))
        {
            var existingByEmail = await _context.Members.AnyAsync(m => m.Email == member.Email && m.Id != member.Id);
            if (existingByEmail)
                errors.Add("Υπάρχει ήδη μέλος με αυτό το email");
        }

        // Membership type validation
        var membershipTypeExists = await _context.MembershipTypes
            .AnyAsync(mt => mt.Id == member.MembershipTypeId && mt.IsActive);
        if (!membershipTypeExists)
            errors.Add("Μη έγκυρος τύπος συνδρομής");

        if (errors.Any())
        {
            throw new ArgumentException($"Validation errors: {string.Join(", ", errors)}");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Member>> GetMembersByStatusAsync(MemberStatus status)
    {
        _logger.LogDebug("Getting members by status: {Status}", status);

        return await _context.Members
            .Include(m => m.MembershipType)
            .Where(m => m.Status == status)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Member>> GetMembersWithOverduePaymentsAsync()
    {
        _logger.LogDebug("Getting members with overdue payments");

        var overdueDate = DateTime.UtcNow.AddDays(-30); // 30 days overdue

        return await _context.Members
            .Include(m => m.MembershipType)
            .Include(m => m.Subscriptions)
            .Where(m => m.Subscriptions.Any(s =>
                s.Status == SubscriptionStatus.Overdue &&
                s.DueDate < overdueDate))
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync();
    }

    public async Task<int> GetTotalMembersCountAsync()
    {
        return await _context.Members.CountAsync();
    }

    public async Task<decimal> GetTotalMonthlyRevenueAsync()
    {
        var activeMembersRevenue = await _context.Members
            .Include(m => m.MembershipType)
            .Where(m => m.Status == MemberStatus.Active)
            .SumAsync(m => m.MembershipType.MonthlyFee);

        _logger.LogDebug("Total monthly revenue calculated: {Revenue:C}", activeMembersRevenue);
        return activeMembersRevenue;
    }

    public async Task ActivateMemberAsync(int memberId)
    {
        try
        {
            var member = await _context.Members.FindAsync(memberId);
            if (member == null)
            {
                throw new InvalidOperationException($"Member with ID {memberId} not found.");
            }

            if (member.Status == MemberStatus.Active)
            {
                _logger.LogWarning("Member {MemberNumber} is already active", member.MemberNumber);
                return;
            }

            member.Status = MemberStatus.Active;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Activated member {MemberNumber}", member.MemberNumber);

            // Audit logging if available
            if (_auditService != null)
            {
                await _auditService.LogUpdateAsync(member, member, 0, "System", "System", "127.0.0.1");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating member {MemberId}", memberId);
            throw;
        }
    }

    public async Task DeactivateMemberAsync(int memberId)
    {
        try
        {
            var member = await _context.Members.FindAsync(memberId);
            if (member == null)
            {
                throw new InvalidOperationException($"Member with ID {memberId} not found.");
            }

            if (member.Status == MemberStatus.Inactive)
            {
                _logger.LogWarning("Member {MemberNumber} is already inactive", member.MemberNumber);
                return;
            }

            member.Status = MemberStatus.Inactive;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deactivated member {MemberNumber}", member.MemberNumber);

            // Audit logging if available
            if (_auditService != null)
            {
                await _auditService.LogUpdateAsync(member, member, 0, "System", "System", "127.0.0.1");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating member {MemberId}", memberId);
            throw;
        }
    }

    public async Task SuspendMemberAsync(int memberId, string reason)
    {
        try
        {
            var member = await _context.Members.FindAsync(memberId);
            if (member == null)
            {
                throw new InvalidOperationException($"Member with ID {memberId} not found.");
            }

            if (member.Status == MemberStatus.Suspended)
            {
                _logger.LogWarning("Member {MemberNumber} is already suspended", member.MemberNumber);
                return;
            }

            member.Status = MemberStatus.Suspended;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Suspended member {MemberNumber} for reason: {Reason}", member.MemberNumber, reason);

            // Audit logging if available
            if (_auditService != null)
            {
                await _auditService.LogUpdateAsync(member, member, 0, "System", "System", "127.0.0.1");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending member {MemberId}", memberId);
            throw;
        }
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
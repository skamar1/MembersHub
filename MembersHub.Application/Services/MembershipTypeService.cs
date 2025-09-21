using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Application.Services;

public class MembershipTypeService : IMembershipTypeService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<MembershipTypeService> _logger;

    public MembershipTypeService(
        MembersHubContext context,
        ILogger<MembershipTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<MembershipType>> GetAllAsync()
    {
        return await _context.MembershipTypes
            .Include(mt => mt.Members)
            .OrderBy(mt => mt.Name)
            .ToListAsync();
    }

    public async Task<MembershipType?> GetByIdAsync(int id)
    {
        return await _context.MembershipTypes
            .Include(mt => mt.Members)
            .FirstOrDefaultAsync(mt => mt.Id == id);
    }

    public async Task<MembershipType> CreateAsync(MembershipType membershipType)
    {
        try
        {
            await ValidateMembershipTypeAsync(membershipType);

            _context.MembershipTypes.Add(membershipType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created membership type {Name} with fee {Fee}",
                membershipType.Name, membershipType.MonthlyFee);

            return membershipType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating membership type {Name}", membershipType.Name);
            throw;
        }
    }

    public async Task UpdateAsync(MembershipType membershipType)
    {
        try
        {
            var existingType = await _context.MembershipTypes.AsNoTracking()
                .FirstOrDefaultAsync(mt => mt.Id == membershipType.Id);

            if (existingType == null)
            {
                throw new InvalidOperationException($"Membership type with ID {membershipType.Id} not found.");
            }

            await ValidateMembershipTypeAsync(membershipType, isUpdate: true);

            _context.MembershipTypes.Update(membershipType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated membership type {Id}: {Name}", membershipType.Id, membershipType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating membership type {Id}", membershipType.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var membershipType = await _context.MembershipTypes
                .Include(mt => mt.Members)
                .FirstOrDefaultAsync(mt => mt.Id == id);

            if (membershipType == null)
            {
                throw new InvalidOperationException($"Membership type with ID {id} not found.");
            }

            if (membershipType.Members.Any())
            {
                throw new InvalidOperationException("Cannot delete membership type that has members assigned to it.");
            }

            _context.MembershipTypes.Remove(membershipType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted membership type {Id}: {Name}", id, membershipType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting membership type {Id}", id);
            throw;
        }
    }

    public async Task<bool> CanDeleteAsync(int id)
    {
        var memberCount = await _context.Members.CountAsync(m => m.MembershipTypeId == id);
        return memberCount == 0;
    }

    private async Task ValidateMembershipTypeAsync(MembershipType membershipType, bool isUpdate = false)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(membershipType.Name))
            errors.Add("Το όνομα είναι υποχρεωτικό");

        if (membershipType.MonthlyFee < 0)
            errors.Add("Το μηνιαίο τέλος δεν μπορεί να είναι αρνητικό");

        // Check for duplicate name
        var query = _context.MembershipTypes.Where(mt => mt.Name == membershipType.Name);
        if (isUpdate)
        {
            query = query.Where(mt => mt.Id != membershipType.Id);
        }

        var duplicateExists = await query.AnyAsync();
        if (duplicateExists)
            errors.Add($"Υπάρχει ήδη τύπος συνδρομής με όνομα '{membershipType.Name}'");

        if (errors.Any())
        {
            throw new ArgumentException($"Validation errors: {string.Join(", ", errors)}");
        }
    }
}
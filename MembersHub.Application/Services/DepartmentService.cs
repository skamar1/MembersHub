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

public class DepartmentService : IDepartmentService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(
        MembersHubContext context,
        ILogger<DepartmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Department>> GetAllAsync()
    {
        return await _context.Departments
            .Include(d => d.Members)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        return await _context.Departments
            .Include(d => d.Members)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Department> CreateAsync(Department department)
    {
        try
        {
            await ValidateDepartmentAsync(department);

            department.CreatedAt = DateTime.UtcNow;
            department.UpdatedAt = DateTime.UtcNow;

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created department {Name}", department.Name);

            return department;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department {Name}", department.Name);
            throw;
        }
    }

    public async Task UpdateAsync(Department department)
    {
        try
        {
            // Load the existing entity from the database
            var existingDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == department.Id);

            if (existingDepartment == null)
            {
                throw new InvalidOperationException($"Το τμήμα με ID {department.Id} δεν βρέθηκε.");
            }

            await ValidateDepartmentAsync(department, isUpdate: true);

            // Update only the properties we want to change
            existingDepartment.Name = department.Name;
            existingDepartment.IsActive = department.IsActive;
            existingDepartment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated department {Id}: {Name}", department.Id, department.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department {Id}", department.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            // Check if there are any members assigned first
            var memberCount = await _context.Members.CountAsync(m => m.DepartmentId == id);
            if (memberCount > 0)
            {
                throw new InvalidOperationException("Δεν μπορείτε να διαγράψετε τμήμα που έχει μέλη.");
            }

            // Load the entity fresh from the database
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                throw new InvalidOperationException($"Το τμήμα με ID {id} δεν βρέθηκε.");
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted department {Id}: {Name}", id, department.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department {Id}", id);
            throw;
        }
    }

    public async Task<bool> CanDeleteAsync(int id)
    {
        var memberCount = await _context.Members.CountAsync(m => m.DepartmentId == id);
        return memberCount == 0;
    }

    private async Task ValidateDepartmentAsync(Department department, bool isUpdate = false)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(department.Name))
            errors.Add("Το όνομα είναι υποχρεωτικό");

        if (department.Name?.Length > 100)
            errors.Add("Το όνομα δεν μπορεί να υπερβαίνει τους 100 χαρακτήρες");

        // Check for duplicate name
        var query = _context.Departments.Where(d => d.Name == department.Name);
        if (isUpdate)
        {
            query = query.Where(d => d.Id != department.Id);
        }

        var duplicateExists = await query.AnyAsync();
        if (duplicateExists)
            errors.Add($"Υπάρχει ήδη τμήμα με όνομα '{department.Name}'");

        if (errors.Any())
        {
            throw new ArgumentException($"Σφάλματα επικύρωσης: {string.Join(", ", errors)}");
        }
    }
}

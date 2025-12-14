using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MembersHub.Application.Services;

public class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<ExpenseCategoryService> _logger;
    private readonly IAuditService _auditService;

    public ExpenseCategoryService(
        MembersHubContext context,
        ILogger<ExpenseCategoryService> logger,
        IAuditService auditService)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<ExpenseCategory> CreateCategoryAsync(ExpenseCategory category)
    {
        try
        {
            ValidateCategory(category);

            category.CreatedAt = DateTime.UtcNow;
            category.IsActive = true;

            _context.ExpenseCategories.Add(category);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Create, "ExpenseCategory", category.Id.ToString(),
                category.Name, $"Δημιουργήθηκε κατηγορία εξόδου: {category.Name}");

            _logger.LogInformation("Δημιουργήθηκε κατηγορία εξόδου {CategoryName} με ID {CategoryId}",
                category.Name, category.Id);

            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά τη δημιουργία κατηγορίας εξόδου {CategoryName}", category.Name);
            throw;
        }
    }

    public async Task<ExpenseCategory> UpdateCategoryAsync(ExpenseCategory category)
    {
        try
        {
            var existing = await GetCategoryByIdAsync(category.Id);
            if (existing == null)
                throw new ArgumentException("Η κατηγορία δεν βρέθηκε");

            ValidateCategory(category);

            existing.Name = category.Name;
            existing.Description = category.Description;
            existing.IconName = category.IconName;
            existing.ColorCode = category.ColorCode;
            existing.DisplayOrder = category.DisplayOrder;
            existing.ParentCategoryId = category.ParentCategoryId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Update, "ExpenseCategory", category.Id.ToString(),
                category.Name, $"Ενημερώθηκε κατηγορία εξόδου: {category.Name}");

            _logger.LogInformation("Ενημερώθηκε κατηγορία εξόδου {CategoryName}", category.Name);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά την ενημέρωση κατηγορίας εξόδου {CategoryId}", category.Id);
            throw;
        }
    }

    public async Task<ExpenseCategory?> GetCategoryByIdAsync(int categoryId)
    {
        return await _context.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    public async Task<List<ExpenseCategory>> GetAllCategoriesAsync()
    {
        return await _context.ExpenseCategories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<ExpenseCategory>> GetActiveCategoriesAsync()
    {
        return await _context.ExpenseCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        try
        {
            var category = await GetCategoryByIdAsync(categoryId);
            if (category == null) return false;

            // Check if category is used by any expenses
            var expenseCount = await _context.Expenses
                .CountAsync(e => e.ExpenseCategoryId == categoryId);

            if (expenseCount > 0)
            {
                throw new InvalidOperationException(
                    $"Δεν μπορεί να διαγραφεί η κατηγορία '{category.Name}' επειδή χρησιμοποιείται από {expenseCount} έξοδα. " +
                    "Μπορείτε να την απενεργοποιήσετε αντί για διαγραφή.");
            }

            // Check if category has subcategories
            var subCategoryCount = await _context.ExpenseCategories
                .CountAsync(c => c.ParentCategoryId == categoryId);

            if (subCategoryCount > 0)
            {
                throw new InvalidOperationException(
                    $"Δεν μπορεί να διαγραφεί η κατηγορία '{category.Name}' επειδή έχει {subCategoryCount} υποκατηγορίες. " +
                    "Διαγράψτε πρώτα τις υποκατηγορίες ή μετακινήστε τις.");
            }

            _context.ExpenseCategories.Remove(category);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(AuditAction.Delete, "ExpenseCategory", categoryId.ToString(),
                category.Name, $"Διαγράφηκε κατηγορία εξόδου: {category.Name}");

            _logger.LogInformation("Διαγράφηκε κατηγορία εξόδου {CategoryName}", category.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά τη διαγραφή κατηγορίας εξόδου {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<bool> ToggleCategoryStatusAsync(int categoryId)
    {
        try
        {
            var category = await GetCategoryByIdAsync(categoryId);
            if (category == null) return false;

            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var status = category.IsActive ? "ενεργοποιήθηκε" : "απενεργοποιήθηκε";
            await _auditService.LogAsync(AuditAction.Update, "ExpenseCategory", categoryId.ToString(),
                category.Name, $"Η κατηγορία εξόδου '{category.Name}' {status}");

            _logger.LogInformation("Η κατηγορία εξόδου {CategoryName} {Status}",
                category.Name, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Σφάλμα κατά την αλλαγή κατάστασης κατηγορίας εξόδου {CategoryId}", categoryId);
            throw;
        }
    }

    private void ValidateCategory(ExpenseCategory category)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(category.Name))
            errors.Add("Το όνομα της κατηγορίας είναι υποχρεωτικό");

        if (category.Name.Length > 100)
            errors.Add("Το όνομα της κατηγορίας δεν μπορεί να υπερβαίνει τους 100 χαρακτήρες");

        if (errors.Any())
            throw new ArgumentException(string.Join(", ", errors));
    }

    // Subcategory methods

    public async Task<List<ExpenseCategory>> GetParentCategoriesAsync()
    {
        return await _context.ExpenseCategories
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<ExpenseCategory>> GetActiveParentCategoriesAsync()
    {
        return await _context.ExpenseCategories
            .Where(c => c.ParentCategoryId == null && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<ExpenseCategory>> GetSubCategoriesAsync(int parentCategoryId)
    {
        return await _context.ExpenseCategories
            .Where(c => c.ParentCategoryId == parentCategoryId)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<ExpenseCategory>> GetActiveSubCategoriesAsync(int parentCategoryId)
    {
        return await _context.ExpenseCategories
            .Where(c => c.ParentCategoryId == parentCategoryId && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<ExpenseCategory>> GetCategoriesWithSubCategoriesAsync()
    {
        return await _context.ExpenseCategories
            .Include(c => c.SubCategories.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name))
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }
}

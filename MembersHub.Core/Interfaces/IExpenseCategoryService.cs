using MembersHub.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MembersHub.Core.Interfaces;

/// <summary>
/// Υπηρεσία διαχείρισης κατηγοριών εξόδων
/// </summary>
public interface IExpenseCategoryService
{
    Task<ExpenseCategory> CreateCategoryAsync(ExpenseCategory category);
    Task<ExpenseCategory> UpdateCategoryAsync(ExpenseCategory category);
    Task<ExpenseCategory?> GetCategoryByIdAsync(int categoryId);
    Task<List<ExpenseCategory>> GetAllCategoriesAsync();
    Task<List<ExpenseCategory>> GetActiveCategoriesAsync();
    Task<bool> DeleteCategoryAsync(int categoryId);
    Task<bool> ToggleCategoryStatusAsync(int categoryId);

    // Subcategory methods
    Task<List<ExpenseCategory>> GetParentCategoriesAsync();
    Task<List<ExpenseCategory>> GetActiveParentCategoriesAsync();
    Task<List<ExpenseCategory>> GetSubCategoriesAsync(int parentCategoryId);
    Task<List<ExpenseCategory>> GetActiveSubCategoriesAsync(int parentCategoryId);
    Task<List<ExpenseCategory>> GetCategoriesWithSubCategoriesAsync();
}

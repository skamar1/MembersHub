using System;

namespace MembersHub.Core.Entities;

/// <summary>
/// Κατηγορία Εξόδου (Δυναμική - Διαχειρίσιμη από Owner/Admin)
/// </summary>
public class ExpenseCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // π.χ. "Γραφική Ύλη"
    public string? Description { get; set; }
    public string? IconName { get; set; } // MudBlazor icon name (optional)
    public string? ColorCode { get; set; } // Hex color for visual grouping (optional)
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0; // Για σειρά εμφάνισης
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Self-referencing for parent-child hierarchy (subcategories)
    public int? ParentCategoryId { get; set; }
    public virtual ExpenseCategory? ParentCategory { get; set; }
    public virtual ICollection<ExpenseCategory> SubCategories { get; set; } = new List<ExpenseCategory>();

    // Navigation properties
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}

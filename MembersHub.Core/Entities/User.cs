using System;
using System.Collections.Generic;

namespace MembersHub.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    // Password reset fields
    public DateTime? LastPasswordResetAt { get; set; }
    public int PasswordResetCount { get; set; } = 0;
    public bool IsPasswordResetRequired { get; set; } = false;
    
    // Navigation properties
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}

public enum UserRole
{
    Admin = 1,      // Διαχειριστής - Πλήρη δικαιώματα
    Owner = 2,      // Ιδιοκτήτης - Πλήρη δικαιώματα
    Treasurer = 3,  // Ταμίας - Διαχείριση πληρωμών και οικονομικών
    Secretary = 4,  // Γραμματέας - Διαχείριση μελών
    Staff = 5,      // Προσωπικό - Περιορισμένα δικαιώματα
    Viewer = 6      // Θεατής - Μόνο προβολή
}
using System;
using System.Collections.Generic;

namespace MembersHub.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    
    // Navigation properties
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}

public enum UserRole
{
    Admin,
    Secretary,
    Collector,
    Member
}
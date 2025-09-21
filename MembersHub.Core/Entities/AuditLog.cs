using System;

namespace MembersHub.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int? ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Navigation property
    public virtual User? User { get; set; }
}

public enum AuditAction
{
    // Authentication Actions
    Login = 1,
    Logout = 2,
    LoginFailed = 3,
    PasswordReset = 4,
    PasswordResetRequested = 5,
    PasswordResetCompleted = 6,
    
    // CRUD Actions
    Create = 10,
    Update = 11,
    Delete = 12,
    View = 13,
    
    // Specific Business Actions
    MemberCreate = 20,
    MemberUpdate = 21,
    MemberDelete = 22,
    MemberView = 23,
    
    UserCreate = 30,
    UserUpdate = 31,
    UserDelete = 32,
    UserView = 33,
    
    PaymentCreate = 40,
    PaymentUpdate = 41,
    PaymentDelete = 42,
    PaymentView = 43,
    
    ExpenseCreate = 50,
    ExpenseUpdate = 51,
    ExpenseDelete = 52,
    ExpenseView = 53,
    
    // System Actions
    Export = 60,
    Import = 61,
    Backup = 62,
    SystemAccess = 70,
    UnauthorizedAccess = 71,

    // Security Actions
    AccountLockout = 80,
    AccountUnlock = 81
}
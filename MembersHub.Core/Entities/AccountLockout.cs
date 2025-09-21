using System.ComponentModel.DataAnnotations;

namespace MembersHub.Core.Entities;

public class AccountLockout
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    public int FailedAttempts { get; set; }
    
    public DateTime? LockedUntil { get; set; }
    
    [MaxLength(45)]
    public string? LastAttemptIpAddress { get; set; }
    
    [MaxLength(500)]
    public string? LastAttemptUserAgent { get; set; }
    
    public DateTime LastAttemptAt { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(200)]
    public string? LockoutReason { get; set; }
    
    public virtual User User { get; set; } = null!;
    
    public bool IsLockedOut => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;
}
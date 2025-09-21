using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MembersHub.Core.Entities;

public class SecurityEvent
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty; // PasswordReset, Login, LoginFailure, etc.
    
    [Required]
    [StringLength(45)] // IPv6 max length
    public string IpAddress { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? Country { get; set; }
    
    [StringLength(100)]
    public string? City { get; set; }
    
    [StringLength(10)]
    public string? CountryCode { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    [StringLength(100)]
    public string? DeviceType { get; set; } // Mobile, Desktop, Tablet
    
    [StringLength(100)]
    public string? Browser { get; set; }
    
    [StringLength(100)]
    public string? OperatingSystem { get; set; }
    
    [StringLength(256)]
    public string? DeviceFingerprint { get; set; }
    
    public bool IsSuccessful { get; set; } = true;
    
    public bool IsSuspicious { get; set; } = false;
    
    [StringLength(500)]
    public string? SuspiciousReason { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? AdditionalData { get; set; } // JSON for extra security context
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}

public enum SecurityEventType
{
    PasswordResetRequested,
    PasswordResetCompleted,
    LoginAttempt,
    LoginSuccess,
    LoginFailure,
    AccountLocked,
    SuspiciousActivity,
    TokenValidation,
    RateLimitExceeded
}
using System;
using System.ComponentModel.DataAnnotations;

namespace MembersHub.Core.Entities;

public class PasswordResetRateLimit
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Identifier { get; set; } = string.Empty; // Email or IP address
    
    [Required]
    public RateLimitType Type { get; set; }
    
    [Required]
    public int AttemptCount { get; set; } = 0;
    
    [Required]
    public DateTime LastAttemptAt { get; set; }
    
    [Required]
    public DateTime WindowStartAt { get; set; }
    
    public DateTime? BlockedUntil { get; set; }
    
    public bool IsBlocked => BlockedUntil.HasValue && DateTime.UtcNow < BlockedUntil.Value;
    
    public bool IsWithinHourWindow => DateTime.UtcNow - WindowStartAt < TimeSpan.FromHours(1);
    
    public void ResetWindow()
    {
        AttemptCount = 0;
        WindowStartAt = DateTime.UtcNow;
        BlockedUntil = null;
    }
    
    public void IncrementAttempt()
    {
        if (!IsWithinHourWindow)
        {
            ResetWindow();
        }
        
        AttemptCount++;
        LastAttemptAt = DateTime.UtcNow;
        
        // Apply blocking rules based on type
        if (Type == RateLimitType.Email && AttemptCount >= 3)
        {
            BlockedUntil = DateTime.UtcNow.AddHours(1);
        }
        else if (Type == RateLimitType.IpAddress && AttemptCount >= 5)
        {
            BlockedUntil = DateTime.UtcNow.AddHours(1);
        }
    }
}

public enum RateLimitType
{
    Email = 1,
    IpAddress = 2
}
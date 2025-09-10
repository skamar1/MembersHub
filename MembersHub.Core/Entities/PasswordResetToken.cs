using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MembersHub.Core.Entities;

public class PasswordResetToken
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(256)]
    public string TokenHash { get; set; } = string.Empty;
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public bool IsUsed { get; set; } = false;
    
    [StringLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
    
    // Helper properties
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    [NotMapped]
    public bool IsValid => !IsUsed && !IsExpired;
}
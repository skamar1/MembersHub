using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MembersHub.Core.Entities;

public class UserDevice
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(256)]
    public string DeviceFingerprint { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? DeviceName { get; set; } // User-friendly name
    
    [StringLength(100)]
    public string? DeviceType { get; set; } // Mobile, Desktop, Tablet
    
    [StringLength(100)]
    public string? Browser { get; set; }
    
    [StringLength(50)]
    public string? BrowserVersion { get; set; }
    
    [StringLength(100)]
    public string? OperatingSystem { get; set; }
    
    [StringLength(50)]
    public string? OSVersion { get; set; }
    
    [StringLength(45)]
    public string? LastUsedIpAddress { get; set; }
    
    [StringLength(100)]
    public string? LastUsedLocation { get; set; }
    
    public bool IsTrusted { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    
    public int TotalLogins { get; set; } = 0;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
    
    // Helper properties
    [NotMapped]
    public bool IsRecentlyUsed => (DateTime.UtcNow - LastSeenAt).TotalDays <= 30;
    
    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(DeviceName) 
        ? DeviceName 
        : $"{Browser} on {OperatingSystem}";
}
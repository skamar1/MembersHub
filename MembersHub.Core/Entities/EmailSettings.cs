using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MembersHub.Core.Entities;

public class EmailSettings
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string SmtpHost { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 587;
    
    [Required]
    [StringLength(200)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string PasswordEncrypted { get; set; } = string.Empty; // Encrypted password
    
    [Required]
    [StringLength(200)]
    public string FromEmail { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string FromName { get; set; } = string.Empty;
    
    public bool EnableSsl { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string? CreatedBy { get; set; }
    
    [StringLength(100)]
    public string? UpdatedBy { get; set; }
    
    // Email template settings
    [StringLength(200)]
    public string? PasswordResetSubject { get; set; } = "Επαναφορά κωδικού πρόσβασης - MembersHub";
    
    [Column(TypeName = "nvarchar(max)")]
    public string? PasswordResetTemplate { get; set; }
    
    // Helper methods for password encryption/decryption
    [NotMapped]
    public string DecryptedPassword { get; set; } = string.Empty;
    
    // Validation
    [NotMapped]
    public bool IsValidConfiguration => 
        !string.IsNullOrWhiteSpace(SmtpHost) &&
        SmtpPort > 0 &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(FromEmail) &&
        IsValidEmail(FromEmail);
    
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
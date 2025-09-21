using System.ComponentModel.DataAnnotations;

namespace MembersHub.Core.Entities;

public class CompromisedPassword
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(64)]
    public string PasswordHashSHA1 { get; set; } = string.Empty;
    
    public int BreachCount { get; set; }
    
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
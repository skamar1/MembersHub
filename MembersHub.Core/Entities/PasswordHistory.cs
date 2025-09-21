using System.ComponentModel.DataAnnotations;

namespace MembersHub.Core.Entities;

public class PasswordHistory
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual User User { get; set; } = null!;
}
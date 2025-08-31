using System.Collections.Generic;

namespace MembersHub.Core.Entities;

public class MembershipType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // π.χ. "Ενήλικες", "Παιδιά", "Φοιτητές"
    public decimal MonthlyFee { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<Member> Members { get; set; } = new List<Member>();
}
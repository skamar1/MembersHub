namespace MembersHub.Core.Entities;

/// <summary>
/// Join table for many-to-many relationship between Users and Departments.
/// Defines which departments a user (especially Treasurers) can access.
/// </summary>
public class UserDepartment
{
    public int UserId { get; set; }
    public int DepartmentId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Department Department { get; set; } = null!;
}

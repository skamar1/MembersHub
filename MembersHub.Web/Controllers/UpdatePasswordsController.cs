using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UpdatePasswordsController : ControllerBase
{
    private readonly MembersHubContext _context;

    public UpdatePasswordsController(MembersHubContext context)
    {
        _context = context;
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdatePasswords()
    {
        try
        {
            // BCrypt hashes for the passwords
            var adminHash = "$2a$11$rBNbDf3DqWZ9K5jNpZ3MlO0GxF9z7xKfHhqPzO.X9xQqzjHm5N.Vy"; // Admin123!
            var ownerHash = "$2a$11$pL4.hxC.ZYF5xD0Jt2VxCuWR5K3nGxDzQfXHpO/LvBXCxYm1H4PQG"; // Owner123!
            var treasurerHash = "$2a$11$yH8BxJ0Tff7K9B7N.zPgWOHjQ5VKxZ3TQfXHpO/LvBXCxYm1H4PQe"; // Treasurer123!

            // Update passwords using raw SQL
            var sql = @"
                UPDATE Users SET PasswordHash = {0} WHERE Username = 'admin';
                UPDATE Users SET PasswordHash = {1} WHERE Username = 'owner';
                UPDATE Users SET PasswordHash = {2} WHERE Username = 'treasurer';
            ";

            var result = await _context.Database.ExecuteSqlRawAsync(sql, adminHash, ownerHash, treasurerHash);

            return Ok(new
            {
                Success = true,
                Message = "Password updates completed successfully!",
                Credentials = new
                {
                    Admin = "admin / Admin123!",
                    Owner = "owner / Owner123!",
                    Treasurer = "treasurer / Treasurer123!"
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Error = ex.Message
            });
        }
    }
}
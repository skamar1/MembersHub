using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MembersHub.Core.Interfaces;
using MembersHub.Core.Entities;
using MembersHub.Infrastructure.Data;

namespace MembersHub.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Owner")] // Only Admin and Owner can access user management
public class UsersController : ControllerBase
{
    private readonly MembersHubContext _context;
    private readonly IAuthenticationService _authService;
    private readonly IAuditService _auditService;
    private readonly ISessionService _sessionService;
    private readonly IHttpContextInfoService _httpContextInfo;

    public UsersController(
        MembersHubContext context,
        IAuthenticationService authService,
        IAuditService auditService,
        ISessionService sessionService,
        IHttpContextInfoService httpContextInfo)
    {
        _context = context;
        _authService = authService;
        _auditService = auditService;
        _sessionService = sessionService;
        _httpContextInfo = httpContextInfo;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => 
                    u.FullName.Contains(search) ||
                    u.Username.Contains(search) ||
                    (u.Email != null && u.Email.Contains(search)));
            }

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                data = users.Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    fullName = u.FullName,
                    email = u.Email,
                    phone = u.Phone,
                    role = u.Role.ToString(),
                    isActive = u.IsActive,
                    lastLoginAt = u.LastLoginAt,
                    createdAt = u.CreatedAt
                }),
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new { error = "Ο χρήστης δεν βρέθηκε" });
            }

            // Log view action
            var currentUser = await _sessionService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                await _auditService.LogViewAsync(user,
                    currentUser.Id, currentUser.Username, currentUser.FullName,
                    _httpContextInfo.GetIpAddress());
            }

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                firstName = user.FirstName,
                lastName = user.LastName,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role.ToString(),
                isActive = user.IsActive,
                lastLoginAt = user.LastLoginAt,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // Check if username already exists
            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == request.Username);

            if (existingUser)
            {
                return BadRequest(new { error = "Το όνομα χρήστη υπάρχει ήδη" });
            }

            var user = new User
            {
                Username = request.Username,
                PasswordHash = await _authService.HashPasswordAsync(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Role = request.Role,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Log create action
            var currentUser = await _sessionService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                await _auditService.LogCreateAsync(user,
                    currentUser.Id, currentUser.Username, currentUser.FullName,
                    _httpContextInfo.GetIpAddress());
            }

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
            {
                id = user.Id,
                username = user.Username,
                firstName = user.FirstName,
                lastName = user.LastName,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role.ToString(),
                isActive = user.IsActive,
                createdAt = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            // Get old values for audit (AsNoTracking για accurate snapshot)
            var oldUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null || oldUser == null)
            {
                return NotFound(new { error = "Ο χρήστης δεν βρέθηκε" });
            }

            // Update user properties
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.Phone = request.Phone;
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log update action
            var currentUser = await _sessionService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                await _auditService.LogUpdateAsync(oldUser, user,
                    currentUser.Id, currentUser.Username, currentUser.FullName,
                    _httpContextInfo.GetIpAddress());
            }

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                firstName = user.FirstName,
                lastName = user.LastName,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role.ToString(),
                isActive = user.IsActive,
                updatedAt = user.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new { error = "Ο χρήστης δεν βρέθηκε" });
            }

            // Prevent deleting yourself
            var currentUser = await _sessionService.GetCurrentUserAsync();
            if (currentUser != null && currentUser.Id == id)
            {
                return BadRequest(new { error = "Δεν μπορείτε να διαγράψετε τον δικό σας λογαριασμό" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Log delete action
            if (currentUser != null)
            {
                await _auditService.LogDeleteAsync(user,
                    currentUser.Id, currentUser.Username, currentUser.FullName,
                    _httpContextInfo.GetIpAddress());
            }

            return Ok(new { message = $"Ο χρήστης '{user.FullName}' διαγράφηκε επιτυχώς" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new { error = "Ο χρήστης δεν βρέθηκε" });
            }

            // Hash new password (API assumes password is provided by client)
            user.PasswordHash = await _authService.HashPasswordAsync(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log password reset action
            var currentUser = await _sessionService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                await _auditService.LogAsync(AuditAction.PasswordReset, "User", 
                    user.Id.ToString(), user.FullName,
                    $"Επαναφορά κωδικού για χρήστη: {user.FullName}",
                    currentUser.Id, currentUser.Username, currentUser.FullName,
                    _httpContextInfo.GetIpAddress());
            }

            return Ok(new { message = "Ο κωδικός πρόσβασης άλλαξε επιτυχώς" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
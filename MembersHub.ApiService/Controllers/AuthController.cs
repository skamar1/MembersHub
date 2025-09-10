using Microsoft.AspNetCore.Mvc;
using MembersHub.Core.Interfaces;
using MembersHub.Core.Entities;

namespace MembersHub.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IAuditService _auditService;
    private readonly IHttpContextInfoService _httpContextInfo;

    public AuthController(
        IAuthenticationService authService,
        IAuditService auditService,
        IHttpContextInfoService httpContextInfo)
    {
        _authService = authService;
        _auditService = auditService;
        _httpContextInfo = httpContextInfo;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.AuthenticateAsync(request.Username, request.Password);
            
            if (!result.IsSuccess)
            {
                // Log failed login attempt
                await _auditService.LogLoginFailedAsync(
                    request.Username,
                    _httpContextInfo.GetIpAddress(),
                    _httpContextInfo.GetUserAgent(),
                    result.ErrorMessage);
                    
                return Unauthorized(new { error = result.ErrorMessage });
            }

            // Log successful login
            if (result.User != null)
            {
                await _auditService.LogLoginAsync(
                    result.User.Id,
                    result.User.Username,
                    result.User.FullName,
                    _httpContextInfo.GetIpAddress(),
                    _httpContextInfo.GetUserAgent());
            }

            return Ok(new
            {
                token = result.Token,
                user = new
                {
                    id = result.User?.Id,
                    username = result.User?.Username,
                    fullName = result.User?.FullName,
                    role = result.User?.Role.ToString(),
                    email = result.User?.Email
                },
                expiresAt = result.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            await _auditService.LogLoginFailedAsync(
                request.Username,
                _httpContextInfo.GetIpAddress(),
                _httpContextInfo.GetUserAgent(),
                ex.Message);
                
            return StatusCode(500, new { error = "Σφάλμα κατά τη σύνδεση" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var token = HttpContext.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
            if (!string.IsNullOrEmpty(token))
            {
                await _authService.LogoutAsync(token);
            }
            
            return Ok(new { message = "Αποσυνδεθήκατε επιτυχώς" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
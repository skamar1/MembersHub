using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;
using System.Security.Claims;

namespace MembersHub.Application.Services;

public class ApiSessionService : ISessionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MembersHubContext _context;

    public ApiSessionService(IHttpContextAccessor httpContextAccessor, MembersHubContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public event Action? AuthenticationStateChanged;

    public async Task<bool> CreateSessionAsync(User user, string token)
    {
        // Not applicable for API - sessions are managed via JWT
        await Task.CompletedTask;
        return true;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return null;

        try
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request?.Headers?.Authorization.Count > 0)
        {
            var authHeader = httpContext.Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
        }

        await Task.CompletedTask;
        return null;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        await Task.CompletedTask;
        return httpContext?.User?.Identity?.IsAuthenticated == true;
    }

    public async Task LogoutAsync()
    {
        // Not applicable for API - logout is handled by token expiration
        await Task.CompletedTask;
        AuthenticationStateChanged?.Invoke();
    }

    public async Task<bool> ValidateSessionAsync()
    {
        var user = await GetCurrentUserAsync();
        return user != null;
    }
}
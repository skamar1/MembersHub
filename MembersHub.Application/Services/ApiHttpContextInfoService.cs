using Microsoft.AspNetCore.Http;
using MembersHub.Core.Interfaces;

namespace MembersHub.Application.Services;

public class ApiHttpContextInfoService : IHttpContextInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiHttpContextInfoService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAvailable => _httpContextAccessor.HttpContext != null;

    public string? GetIpAddress()
    {
        if (!IsAvailable) return null;

        var context = _httpContextAccessor.HttpContext!;
        
        // Check for forwarded IP (for reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Check for real IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString();
    }

    public string? GetUserAgent()
    {
        if (!IsAvailable) return null;
        return _httpContextAccessor.HttpContext!.Request.Headers["User-Agent"].FirstOrDefault();
    }

    public string? GetRequestPath()
    {
        if (!IsAvailable) return null;
        return _httpContextAccessor.HttpContext!.Request.Path.Value;
    }

    public string GetRequestMethod()
    {
        if (!IsAvailable) return "Unknown";
        return _httpContextAccessor.HttpContext!.Request.Method;
    }
}
using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(string username, string password);
    Task<AuthenticationResult> AuthenticateAsync(string username, string password, string ipAddress, string? userAgent = null);
    Task<string> GenerateJwtTokenAsync(User user);
    Task<bool> ValidatePasswordAsync(string password, string hashedPassword);
    Task<string> HashPasswordAsync(string password);
    Task LogoutAsync(string token);
    Task<SecurityRiskAssessment> AssessLoginRiskAsync(int userId, string ipAddress, string? userAgent = null);
    Task LogSecurityEventAsync(int userId, SecurityEventType eventType, string ipAddress, string? userAgent = null, bool isSuccessful = true);
    Task LogAnonymousSecurityEventAsync(SecurityEventType eventType, string ipAddress, string? userAgent = null, bool isSuccessful = true);
}

public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public User? User { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public SecurityRiskAssessment? RiskAssessment { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresDeviceVerification { get; set; }
    public bool IsAccountLocked { get; set; }
    public TimeSpan? LockoutTimeRemaining { get; set; }
}
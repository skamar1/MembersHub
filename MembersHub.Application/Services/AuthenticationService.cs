using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;
using BCrypt.Net;

namespace MembersHub.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly MembersHubContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ISecurityEventService _securityEventService;
    private readonly IAccountLockoutService _accountLockoutService;
    private readonly IDeviceTrackingService _deviceTrackingService;
    private readonly IPasswordSecurityService _passwordSecurityService;

    public AuthenticationService(
        MembersHubContext context,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger,
        ISecurityEventService securityEventService,
        IAccountLockoutService accountLockoutService,
        IDeviceTrackingService deviceTrackingService,
        IPasswordSecurityService passwordSecurityService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _securityEventService = securityEventService;
        _accountLockoutService = accountLockoutService;
        _deviceTrackingService = deviceTrackingService;
        _passwordSecurityService = passwordSecurityService;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
    {
        return await AuthenticateAsync(username, password, "127.0.0.1", null);
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, string ipAddress, string? userAgent = null)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Authentication failed: User '{Username}' not found or inactive from IP {IpAddress}", username, ipAddress);
                
                // Log failed security event for non-existent user
                await LogSecurityEventAsync(0, SecurityEventType.LoginFailure, ipAddress, userAgent, false);
                
                return new AuthenticationResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Λάθος όνομα χρήστη ή κωδικός πρόσβασης" 
                };
            }

            // Check account lockout
            var isLocked = await _accountLockoutService.IsAccountLockedOutAsync(user.Id);
            if (isLocked)
            {
                var remainingTime = await _accountLockoutService.GetRemainingLockoutTimeAsync(user.Id);
                await LogSecurityEventAsync(user.Id, SecurityEventType.LoginFailure, ipAddress, userAgent, false);
                
                _logger.LogWarning("Login attempt for locked account: User '{Username}' from IP {IpAddress}", username, ipAddress);
                
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    IsAccountLocked = true,
                    LockoutTimeRemaining = remainingTime,
                    ErrorMessage = $"Ο λογαριασμός είναι κλειδωμένος. Δοκιμάστε ξανά σε {remainingTime?.TotalMinutes:F0} λεπτά."
                };
            }

            if (!await ValidatePasswordAsync(password, user.PasswordHash))
            {
                _logger.LogWarning("Authentication failed: Invalid password for user '{Username}' from IP {IpAddress}", username, ipAddress);
                
                // Record failed attempt
                await _accountLockoutService.RecordFailedLoginAttemptAsync(user.Id, ipAddress, userAgent);
                await LogSecurityEventAsync(user.Id, SecurityEventType.LoginFailure, ipAddress, userAgent, false);
                
                return new AuthenticationResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Λάθος όνομα χρήστη ή κωδικός πρόσβασης" 
                };
            }

            // Assess login risk
            var riskAssessment = await _securityEventService.AssessLoginRiskAsync(user.Id, ipAddress, userAgent);

            // Track device
            var device = await _deviceTrackingService.GetOrCreateDeviceAsync(user.Id, userAgent ?? "", ipAddress);

            // Reset failed attempts on successful login
            await _accountLockoutService.ResetFailedAttemptsAsync(user.Id);

            // Log successful security event
            await LogSecurityEventAsync(user.Id, SecurityEventType.LoginSuccess, ipAddress, userAgent, true);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = await GenerateJwtTokenAsync(user);
            var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());

            _logger.LogInformation("User '{Username}' authenticated successfully from IP {IpAddress}", username, ipAddress);

            return new AuthenticationResult
            {
                IsSuccess = true,
                Token = token,
                User = user,
                ExpiresAt = expiresAt,
                RiskAssessment = riskAssessment,
                RequiresTwoFactor = riskAssessment.RequiresTwoFactor,
                RequiresDeviceVerification = riskAssessment.RequiresDeviceVerification
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user '{Username}' from IP {IpAddress}", username, ipAddress);
            return new AuthenticationResult 
            { 
                IsSuccess = false, 
                ErrorMessage = "Σφάλμα κατά την επαλήθευση" 
            };
        }
    }

    public async Task<string> GenerateJwtTokenAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(GetJwtSecret());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("FullName", user.FullName),
            new("Phone", user.Phone)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"] ?? "MembersHub",
            Audience = _configuration["Jwt:Audience"] ?? "MembersHub"
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<bool> ValidatePasswordAsync(string password, string hashedPassword)
    {
        await Task.CompletedTask; // Make it async for consistency
        
        // Try BCrypt first for new passwords
        try
        {
            if (hashedPassword.StartsWith("$2"))  // BCrypt hashes start with $2
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
        }
        catch { }
        
        // Fallback to SHA256 for existing passwords
        var hash = await HashPasswordWithSHA256Async(password);
        return hash == hashedPassword;
    }

    public async Task<string> HashPasswordAsync(string password)
    {
        await Task.CompletedTask; // Make it async for consistency
        // Use BCrypt for new passwords
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }
    
    private async Task<string> HashPasswordWithSHA256Async(string password)
    {
        await Task.CompletedTask;
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public async Task LogoutAsync(string token)
    {
        // For now, just log the logout
        // In production, you might want to blacklist tokens or use shorter expiration times
        _logger.LogInformation("User logged out with token: {TokenPrefix}...", token[..Math.Min(token.Length, 10)]);
        await Task.CompletedTask;
    }

    public async Task<SecurityRiskAssessment> AssessLoginRiskAsync(int userId, string ipAddress, string? userAgent = null)
    {
        return await _securityEventService.AssessLoginRiskAsync(userId, ipAddress, userAgent);
    }

    public async Task LogSecurityEventAsync(int userId, SecurityEventType eventType, string ipAddress, string? userAgent = null, bool isSuccessful = true)
    {
        var request = new SecurityEventRequest
        {
            UserId = userId,
            EventType = eventType,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuccessful = isSuccessful
        };

        await _securityEventService.LogSecurityEventAsync(request);
    }

    private string GetJwtSecret()
    {
        var secret = _configuration["Jwt:Secret"];
        if (string.IsNullOrEmpty(secret) || secret.Length < 32)
        {
            // Generate a default secret for demo purposes
            return "MembersHub-Super-Secret-Key-That-Is-At-Least-32-Characters-Long!";
        }
        return secret;
    }

    private int GetTokenExpirationHours()
    {
        if (int.TryParse(_configuration["Jwt:ExpirationHours"], out var hours))
        {
            return hours;
        }
        return 8; // Default 8 hours
    }
}
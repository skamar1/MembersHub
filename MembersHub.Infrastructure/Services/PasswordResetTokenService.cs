using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Infrastructure.Services;

public class PasswordResetTokenService : IPasswordResetTokenService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<PasswordResetTokenService> _logger;

    public PasswordResetTokenService(
        MembersHubContext context,
        ILogger<PasswordResetTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateTokenAsync(int userId, string ipAddress, string? userAgent = null)
    {
        // Generate a secure random token
        var tokenBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        
        var token = Convert.ToBase64String(tokenBytes);
        var tokenHash = ComputeTokenHash(token);

        // Invalidate any existing tokens for this user
        await InvalidateAllUserTokensAsync(userId);

        // Create new token
        var resetToken = new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour expiration
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsUsed = false
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset token generated for user {UserId} from IP {IpAddress}", userId, ipAddress);

        return token;
    }

    public async Task<PasswordResetToken?> GetValidTokenAsync(string token)
    {
        var tokenHash = ComputeTokenHash(token);
        var now = DateTime.UtcNow;
        
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && 
                                    !t.IsUsed && 
                                    t.ExpiresAt > now);

        return resetToken;
    }

    public async Task<bool> ValidateTokenAsync(string token, int userId)
    {
        var tokenHash = ComputeTokenHash(token);
        
        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UserId == userId);

        return resetToken?.IsValid == true;
    }

    public async Task MarkTokenAsUsedAsync(string token)
    {
        var tokenHash = ComputeTokenHash(token);
        
        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (resetToken != null)
        {
            resetToken.IsUsed = true;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Password reset token marked as used for user {UserId}", resetToken.UserId);
        }
    }

    public async Task InvalidateAllUserTokensAsync(int userId)
    {
        var userTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && !t.IsUsed)
            .ToListAsync();

        foreach (var token in userTokens)
        {
            token.IsUsed = true;
        }

        if (userTokens.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Invalidated {Count} tokens for user {UserId}", userTokens.Count, userId);
        }
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var expiredTokens = await _context.PasswordResetTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.PasswordResetTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} expired password reset tokens", expiredTokens.Count);
        }
    }

    private static string ComputeTokenHash(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}
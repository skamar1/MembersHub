using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IPasswordResetTokenService
{
    Task<string> GenerateTokenAsync(int userId, string ipAddress, string? userAgent = null);
    Task<PasswordResetToken?> GetValidTokenAsync(string tokenHash);
    Task<bool> ValidateTokenAsync(string token, int userId);
    Task MarkTokenAsUsedAsync(string tokenHash);
    Task InvalidateAllUserTokensAsync(int userId);
    Task CleanupExpiredTokensAsync();
}
using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IPasswordResetService
{
    Task<(bool Success, string Message)> RequestPasswordResetAsync(string email, string ipAddress, string? userAgent = null);
    Task<(bool Success, string Message)> ValidateResetTokenAsync(string token, string ipAddress);
    Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword, string ipAddress);
    Task CleanupExpiredTokensAsync();
}
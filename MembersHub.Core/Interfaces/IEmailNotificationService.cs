namespace MembersHub.Core.Interfaces;

public interface IEmailNotificationService
{
    Task<(bool Success, string Message)> SendPasswordResetEmailAsync(string email, string resetToken, string userName);
    Task<(bool Success, string Message)> SendPasswordResetConfirmationEmailAsync(string email, string userName);
}
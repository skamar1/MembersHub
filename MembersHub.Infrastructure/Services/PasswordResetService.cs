using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;
using BCrypt.Net;

namespace MembersHub.Infrastructure.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly MembersHubContext _context;
    private readonly IPasswordResetTokenService _tokenService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IEmailNotificationService _emailService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        MembersHubContext context,
        IPasswordResetTokenService tokenService,
        IRateLimitService rateLimitService,
        IEmailNotificationService emailService,
        IAuditService auditService,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _rateLimitService = rateLimitService;
        _emailService = emailService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(string email, string ipAddress, string? userAgent = null)
    {
        try
        {
            // Validate email format
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                return (false, "Παρακαλώ εισάγετε μια έγκυρη διεύθυνση email.");
            }

            // Check rate limits
            var rateLimitCheck = await _rateLimitService.CheckRateLimitAsync(email, ipAddress);
            if (!rateLimitCheck.IsAllowed)
            {
                _logger.LogWarning("Password reset rate limit exceeded for email {Email} from IP {IpAddress}", email, ipAddress);
                return (false, rateLimitCheck.Message);
            }

            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            // Record the attempt regardless of whether user exists (security best practice)
            await _rateLimitService.RecordAttemptAsync(email, ipAddress);

            if (user == null)
            {
                // Don't reveal that user doesn't exist
                _logger.LogInformation("Password reset requested for non-existent email {Email} from IP {IpAddress}", email, ipAddress);
                return (true, "Αν η διεύθυνση email υπάρχει στο σύστημα, θα λάβετε οδηγίες επαναφοράς κωδικού.");
            }

            // Check if user account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Password reset requested for inactive user {UserId} from IP {IpAddress}", user.Id, ipAddress);
                return (false, "Ο λογαριασμός δεν είναι ενεργός.");
            }

            // Generate reset token
            var resetToken = await _tokenService.GenerateTokenAsync(user.Id, ipAddress, userAgent);

            // Send reset email
            var emailResult = await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken, user.FullName);
            if (!emailResult.Success)
            {
                _logger.LogError("Failed to send password reset email to user {UserId}", user.Id);
                return (false, "Σφάλμα κατά την αποστολή του email. Παρακαλώ δοκιμάστε ξανά.");
            }

            // Log audit event
            await _auditService.LogEventAsync(
                eventType: "PasswordResetRequested",
                entityName: "User",
                entityId: user.Id.ToString(),
                details: $"Password reset requested for {user.Email}",
                ipAddress: ipAddress,
                userAgent: userAgent,
                userId: user.Id
            );

            _logger.LogInformation("Password reset requested successfully for user {UserId} from IP {IpAddress}", user.Id, ipAddress);
            return (true, "Οδηγίες επαναφοράς κωδικού έχουν σταλεί στο email σας.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset request for {Email} from IP {IpAddress}", email, ipAddress);
            return (false, "Παρουσιάστηκε σφάλμα. Παρακαλώ δοκιμάστε ξανά.");
        }
    }

    public async Task<(bool Success, string Message)> ValidateResetTokenAsync(string token, string ipAddress)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, "Μη έγκυρο token επαναφοράς.");
            }

            var resetToken = await _tokenService.GetValidTokenAsync(token);
            if (resetToken == null)
            {
                _logger.LogWarning("Invalid password reset token validation attempt from IP {IpAddress}", ipAddress);
                return (false, "Το token επαναφοράς δεν είναι έγκυρο ή έχει λήξει.");
            }

            _logger.LogInformation("Valid password reset token for user {UserId} from IP {IpAddress}", resetToken.UserId, ipAddress);
            return (true, "Το token είναι έγκυρο.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password reset token from IP {IpAddress}", ipAddress);
            return (false, "Παρουσιάστηκε σφάλμα κατά την επαλήθευση.");
        }
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword, string ipAddress)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, "Μη έγκυρο token επαναφοράς.");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return (false, "Ο κωδικός πρέπει να έχει τουλάχιστον 6 χαρακτήρες.");
            }

            // Get and validate token
            var resetToken = await _tokenService.GetValidTokenAsync(token);
            if (resetToken == null)
            {
                _logger.LogWarning("Invalid password reset token used from IP {IpAddress}", ipAddress);
                return (false, "Το token επαναφοράς δεν είναι έγκυρο ή έχει λήξει.");
            }

            // Get user
            var user = await _context.Users.FindAsync(resetToken.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogError("User not found or inactive for password reset token {TokenId}", resetToken.Id);
                return (false, "Ο χρήστης δεν βρέθηκε ή δεν είναι ενεργός.");
            }

            // Hash new password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // Update user password
            user.PasswordHash = hashedPassword;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = "System-PasswordReset";

            // Mark token as used
            await _tokenService.MarkTokenAsUsedAsync(token);

            // Save changes
            await _context.SaveChangesAsync();

            // Send confirmation email
            var emailResult = await _emailService.SendPasswordResetConfirmationEmailAsync(user.Email, user.FullName);
            if (!emailResult.Success)
            {
                _logger.LogWarning("Failed to send password reset confirmation email to user {UserId}", user.Id);
                // Don't fail the password reset if confirmation email fails
            }

            // Log audit event
            await _auditService.LogEventAsync(
                eventType: "PasswordResetCompleted",
                entityName: "User",
                entityId: user.Id.ToString(),
                details: $"Password successfully reset for {user.Email}",
                ipAddress: ipAddress,
                userId: user.Id
            );

            _logger.LogInformation("Password reset completed successfully for user {UserId} from IP {IpAddress}", user.Id, ipAddress);
            return (true, "Ο κωδικός πρόσβασης επαναφέρθηκε επιτυχώς.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password from IP {IpAddress}", ipAddress);
            return (false, "Παρουσιάστηκε σφάλμα κατά την επαναφορά του κωδικού.");
        }
    }

    public async Task CleanupExpiredTokensAsync()
    {
        try
        {
            await _tokenService.CleanupExpiredTokensAsync();
            await _rateLimitService.CleanupOldRecordsAsync();
            
            _logger.LogInformation("Password reset cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset cleanup");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Interfaces;

namespace MembersHub.Infrastructure.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly IEmailConfigurationService _emailConfigService;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IEmailConfigurationService emailConfigService,
        ILogger<EmailNotificationService> logger)
    {
        _emailConfigService = emailConfigService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        try
        {
            var emailSettings = await _emailConfigService.GetActiveEmailSettingsAsync();
            if (emailSettings == null)
            {
                return (false, "Δεν έχουν διαμορφωθεί ρυθμίσεις email.");
            }

            var resetLink = GenerateResetLink(resetToken);
            var subject = emailSettings.PasswordResetSubject ?? "Επαναφορά κωδικού πρόσβασης - MembersHub";
            var body = GeneratePasswordResetEmailBody(userName, resetLink, emailSettings.PasswordResetTemplate);

            var message = new MailMessage
            {
                From = new MailAddress(emailSettings.FromEmail, emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            
            message.To.Add(email);

            using var client = new SmtpClient(emailSettings.SmtpHost, emailSettings.SmtpPort)
            {
                EnableSsl = emailSettings.EnableSsl,
                Credentials = new System.Net.NetworkCredential(emailSettings.Username, emailSettings.DecryptedPassword)
            };

            await client.SendMailAsync(message);
            
            _logger.LogInformation("Password reset email sent successfully to {Email}", email);
            return (true, "Το email επαναφοράς κωδικού στάλθηκε επιτυχώς.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            return (false, "Σφάλμα κατά την αποστολή του email. Παρακαλώ δοκιμάστε ξανά.");
        }
    }

    public async Task<(bool Success, string Message)> SendPasswordResetConfirmationEmailAsync(string email, string userName)
    {
        try
        {
            var emailSettings = await _emailConfigService.GetActiveEmailSettingsAsync();
            if (emailSettings == null)
            {
                return (false, "Δεν έχουν διαμορφωθεί ρυθμίσεις email.");
            }

            var subject = "Επιβεβαίωση επαναφοράς κωδικού - MembersHub";
            var body = GeneratePasswordResetConfirmationBody(userName);

            var message = new MailMessage
            {
                From = new MailAddress(emailSettings.FromEmail, emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            
            message.To.Add(email);

            using var client = new SmtpClient(emailSettings.SmtpHost, emailSettings.SmtpPort)
            {
                EnableSsl = emailSettings.EnableSsl,
                Credentials = new System.Net.NetworkCredential(emailSettings.Username, emailSettings.DecryptedPassword)
            };

            await client.SendMailAsync(message);
            
            _logger.LogInformation("Password reset confirmation email sent successfully to {Email}", email);
            return (true, "Το email επιβεβαίωσης στάλθηκε επιτυχώς.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset confirmation email to {Email}", email);
            return (false, "Σφάλμα κατά την αποστολή του email επιβεβαίωσης.");
        }
    }

    private static string GenerateResetLink(string resetToken)
    {
        // TODO: This should use the actual base URL from configuration
        return $"https://localhost:7004/reset-password?token={resetToken}";
    }

    private static string GeneratePasswordResetEmailBody(string userName, string resetLink, string? customTemplate)
    {
        if (!string.IsNullOrEmpty(customTemplate))
        {
            return customTemplate
                .Replace("{UserName}", userName)
                .Replace("{ResetLink}", resetLink)
                .Replace("{ExpirationHours}", "1");
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>MembersHub - Επαναφορά Κωδικού</h2>
        </div>
        <div class='content'>
            <p>Γεια σας {userName},</p>
            
            <p>Λάβαμε αίτημα για επαναφορά του κωδικού πρόσβασης του λογαριασμού σας.</p>
            
            <p>Πατήστε στον παρακάτω σύνδεσμο για να επαναφέρετε τον κωδικό σας:</p>
            
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Επαναφορά Κωδικού</a>
            </p>
            
            <p><strong>Σημαντικό:</strong> Αυτός ο σύνδεσμος θα λήξει σε 1 ώρα για λόγους ασφαλείας.</p>
            
            <p>Αν δεν ζητήσατε την επαναφορά κωδικού, παρακαλώ αγνοήστε αυτό το email.</p>
            
            <p>Με εκτίμηση,<br>Η ομάδα του MembersHub</p>
        </div>
        <div class='footer'>
            <p>Αυτό είναι ένα αυτοματοποιημένο μήνυμα. Παρακαλώ μην απαντήσετε σε αυτό το email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GeneratePasswordResetConfirmationBody(string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .success {{ background-color: #d4edda; border: 1px solid #c3e6cb; color: #155724; padding: 12px; border-radius: 5px; margin: 10px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>MembersHub - Επιβεβαίωση Επαναφοράς</h2>
        </div>
        <div class='content'>
            <p>Γεια σας {userName},</p>
            
            <div class='success'>
                <strong>Επιτυχής επαναφορά κωδικού!</strong>
            </div>
            
            <p>Ο κωδικός πρόσβασης του λογαριασμού σας έχει επαναφερθεί επιτυχώς.</p>
            
            <p>Για λόγους ασφαλείας, συνιστούμε να:</p>
            <ul>
                <li>Συνδεθείτε με τον νέο σας κωδικό</li>
                <li>Αλλάξετε τον κωδικό σε έναν που μπορείτε να θυμάστε εύκολα</li>
                <li>Χρησιμοποιήσετε έναν ισχυρό και μοναδικό κωδικό</li>
            </ul>
            
            <p>Αν δεν κάνατε εσείς αυτή την αλλαγή, παρακαλώ επικοινωνήστε μαζί μας αμέσως.</p>
            
            <p>Με εκτίμηση,<br>Η ομάδα του MembersHub</p>
        </div>
        <div class='footer'>
            <p>Αυτό είναι ένα αυτοματοποιημένο μήνυμα. Παρακαλώ μην απαντήσετε σε αυτό το email.</p>
        </div>
    </div>
</body>
</html>";
    }
}
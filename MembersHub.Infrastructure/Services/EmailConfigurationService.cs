using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Infrastructure.Services;

public class EmailConfigurationService : IEmailConfigurationService
{
    private readonly MembersHubContext _context;
    private readonly IEmailEncryptionService _encryptionService;
    private readonly ILogger<EmailConfigurationService> _logger;

    public EmailConfigurationService(
        MembersHubContext context,
        IEmailEncryptionService encryptionService,
        ILogger<EmailConfigurationService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<EmailSettings?> GetActiveEmailSettingsAsync()
    {
        var settings = await _context.EmailSettings
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync();

        if (settings != null)
        {
            try
            {
                settings.DecryptedPassword = _encryptionService.DecryptPassword(settings.PasswordEncrypted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt email password for active settings");
                settings.DecryptedPassword = string.Empty;
            }
        }

        return settings;
    }

    public async Task<EmailSettings?> GetEmailSettingsByIdAsync(int id)
    {
        var settings = await _context.EmailSettings.FindAsync(id);
        
        if (settings != null)
        {
            try
            {
                settings.DecryptedPassword = _encryptionService.DecryptPassword(settings.PasswordEncrypted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt email password for settings ID {Id}", settings.Id);
                settings.DecryptedPassword = string.Empty;
            }
        }

        return settings;
    }

    public async Task<List<EmailSettings>> GetAllEmailSettingsAsync()
    {
        var settingsList = await _context.EmailSettings
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        foreach (var settings in settingsList)
        {
            try
            {
                settings.DecryptedPassword = _encryptionService.DecryptPassword(settings.PasswordEncrypted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt email password for settings ID {Id}", settings.Id);
                settings.DecryptedPassword = string.Empty;
            }
        }

        return settingsList;
    }

    public async Task<EmailSettings> CreateEmailSettingsAsync(EmailSettings settings, string currentUser)
    {
        settings.PasswordEncrypted = _encryptionService.EncryptPassword(settings.DecryptedPassword);
        settings.CreatedAt = DateTime.UtcNow;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.CreatedBy = currentUser;
        settings.UpdatedBy = currentUser;

        // If this is the first email settings or marked as active, deactivate others
        if (settings.IsActive)
        {
            await DeactivateAllEmailSettingsAsync();
        }

        _context.EmailSettings.Add(settings);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Email settings created by {User} with ID {Id}", currentUser, settings.Id);

        // Return with decrypted password for UI display
        settings.DecryptedPassword = _encryptionService.DecryptPassword(settings.PasswordEncrypted);
        return settings;
    }

    public async Task<EmailSettings> UpdateEmailSettingsAsync(EmailSettings settings, string currentUser)
    {
        var existingSettings = await _context.EmailSettings.FindAsync(settings.Id);
        if (existingSettings == null)
        {
            throw new ArgumentException($"Email settings with ID {settings.Id} not found");
        }

        // Update properties
        existingSettings.SmtpHost = settings.SmtpHost;
        existingSettings.SmtpPort = settings.SmtpPort;
        existingSettings.Username = settings.Username;
        existingSettings.PasswordEncrypted = _encryptionService.EncryptPassword(settings.DecryptedPassword);
        existingSettings.FromEmail = settings.FromEmail;
        existingSettings.FromName = settings.FromName;
        existingSettings.EnableSsl = settings.EnableSsl;
        existingSettings.PasswordResetSubject = settings.PasswordResetSubject;
        existingSettings.PasswordResetTemplate = settings.PasswordResetTemplate;
        existingSettings.UpdatedAt = DateTime.UtcNow;
        existingSettings.UpdatedBy = currentUser;

        // Handle active status change
        if (settings.IsActive && !existingSettings.IsActive)
        {
            await DeactivateAllEmailSettingsAsync();
            existingSettings.IsActive = true;
        }
        else if (!settings.IsActive && existingSettings.IsActive)
        {
            existingSettings.IsActive = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Email settings updated by {User} with ID {Id}", currentUser, settings.Id);

        // Return with decrypted password for UI display
        existingSettings.DecryptedPassword = _encryptionService.DecryptPassword(existingSettings.PasswordEncrypted);
        return existingSettings;
    }

    public async Task DeleteEmailSettingsAsync(int id)
    {
        var settings = await _context.EmailSettings.FindAsync(id);
        if (settings == null)
        {
            throw new ArgumentException($"Email settings with ID {id} not found");
        }

        _context.EmailSettings.Remove(settings);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Email settings deleted with ID {Id}", id);
    }

    public async Task SetActiveEmailSettingsAsync(int id, string currentUser)
    {
        var settings = await _context.EmailSettings.FindAsync(id);
        if (settings == null)
        {
            throw new ArgumentException($"Email settings with ID {id} not found");
        }

        // Deactivate all others
        await DeactivateAllEmailSettingsAsync();

        // Activate the selected one
        settings.IsActive = true;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedBy = currentUser;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Email settings activated by {User} with ID {Id}", currentUser, id);
    }

    public async Task<bool> TestEmailConnectionAsync(int settingsId)
    {
        var settings = await GetEmailSettingsByIdAsync(settingsId);
        if (settings == null) return false;

        return await TestEmailConnectionAsync(settings);
    }

    public async Task<bool> TestEmailConnectionAsync(EmailSettings settings)
    {
        try
        {
            // First, test basic connectivity to SMTP server
            using (var tcpClient = new TcpClient())
            {
                var connectTask = tcpClient.ConnectAsync(settings.SmtpHost, settings.SmtpPort);
                var timeoutTask = Task.Delay(5000); // 5 second timeout for connection
                
                if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                {
                    _logger.LogWarning("TCP connection timeout to {Host}:{Port}", settings.SmtpHost, settings.SmtpPort);
                    return false;
                }

                if (!tcpClient.Connected)
                {
                    _logger.LogWarning("Failed to establish TCP connection to {Host}:{Port}", settings.SmtpHost, settings.SmtpPort);
                    return false;
                }
            }

            // Now test SMTP authentication
            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                EnableSsl = settings.EnableSsl,
                Credentials = new System.Net.NetworkCredential(settings.Username, settings.DecryptedPassword),
                Timeout = 15000 // 15 seconds timeout
            };

            // Try to send a minimal test email to the same address
            // For Outlook, this should work with app-specific password
            var testMessage = new MailMessage(
                from: settings.FromEmail,
                to: settings.FromEmail,
                subject: "SMTP Connection Test",
                body: "This is an automatic test message to verify SMTP configuration."
            );

            try
            {
                await client.SendMailAsync(testMessage);
                _logger.LogInformation("Email connection test successful for {Host}:{Port}", settings.SmtpHost, settings.SmtpPort);
                return true;
            }
            catch (SmtpException ex) when (
                ex.StatusCode == SmtpStatusCode.MailboxBusy ||
                ex.StatusCode == SmtpStatusCode.InsufficientStorage ||
                ex.StatusCode == SmtpStatusCode.TransactionFailed ||
                ex.Message.Contains("recipient") ||
                ex.Message.Contains("mailbox"))
            {
                // Authentication succeeded, but there might be recipient or mailbox issues
                // For connection testing purposes, this is still a success
                _logger.LogInformation("Email connection test successful for {Host}:{Port} - Authentication OK, recipient validation may have failed (this is expected for testing)", settings.SmtpHost, settings.SmtpPort);
                return true;
            }
            catch (SmtpException ex) when (
                ex.StatusCode == SmtpStatusCode.GeneralFailure ||
                ex.StatusCode == SmtpStatusCode.SyntaxError ||
                ex.Message.Contains("authentication") ||
                ex.Message.Contains("unauthorized") ||
                ex.Message.Contains("5.7.139") ||
                ex.Message.Contains("5.7.3") ||
                ex.Message.Contains("535"))
            {
                // Clear authentication failure
                _logger.LogWarning(ex, "Email authentication failed for {Host}:{Port} - Check username/password", settings.SmtpHost, settings.SmtpPort);
                return false;
            }
            finally
            {
                testMessage?.Dispose();
            }
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Network connection failed to {Host}:{Port}", settings.SmtpHost, settings.SmtpPort);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email connection test failed for {Host}:{Port} - Error: {Message}", 
                settings.SmtpHost, settings.SmtpPort, ex.Message);
            return false;
        }
    }

    private async Task DeactivateAllEmailSettingsAsync()
    {
        await _context.EmailSettings
            .Where(x => x.IsActive)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsActive, false));
    }
}
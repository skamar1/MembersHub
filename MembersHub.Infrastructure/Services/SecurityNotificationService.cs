using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Infrastructure.Services;

public class SecurityNotificationService : ISecurityNotificationService
{
    private readonly MembersHubContext _context;
    private readonly IEmailNotificationService _emailService;
    private readonly IGeolocationService _geolocationService;
    private readonly ILogger<SecurityNotificationService> _logger;

    public SecurityNotificationService(
        MembersHubContext context,
        IEmailNotificationService emailService,
        IGeolocationService geolocationService,
        ILogger<SecurityNotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _geolocationService = geolocationService;
        _logger = logger;
    }

    public async Task CreateNotificationAsync(SecurityNotificationRequest request)
    {
        try
        {
            var notification = new SecurityNotification
            {
                UserId = request.UserId,
                NotificationType = request.NotificationType,
                Title = request.Title,
                Message = request.Message,
                Severity = request.Severity,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                Location = request.Location,
                AdditionalData = request.AdditionalData != null ? JsonSerializer.Serialize(request.AdditionalData) : null
            };

            _context.SecurityNotifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send email notification if requested and for high severity notifications
            if (request.SendEmail || request.Severity >= SecurityNotificationSeverity.High)
            {
                await SendEmailNotificationAsync(notification);
            }

            _logger.LogInformation("Security notification created for user {UserId}: {Type} - {Severity}",
                request.UserId, request.NotificationType, request.Severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating security notification for user {UserId}", request.UserId);
        }
    }

    public async Task<List<SecurityNotification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int limit = 50)
    {
        try
        {
            var query = _context.SecurityNotifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            return new List<SecurityNotification>();
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        try
        {
            return await _context.SecurityNotifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            return 0;
        }
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        try
        {
            var notification = await _context.SecurityNotifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        try
        {
            var unreadNotifications = await _context.SecurityNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            if (unreadNotifications.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
                    unreadNotifications.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
        }
    }

    public async Task<List<SecurityNotification>> GetCriticalNotificationsAsync(int hours = 24)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddHours(-hours);
            
            return await _context.SecurityNotifications
                .Include(n => n.User)
                .Where(n => n.Severity >= SecurityNotificationSeverity.Critical && n.CreatedAt >= fromDate)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting critical notifications");
            return new List<SecurityNotification>();
        }
    }

    public async Task DeleteOldNotificationsAsync(int daysToKeep = 90)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            
            var oldNotifications = await _context.SecurityNotifications
                .Where(n => n.CreatedAt < cutoffDate && n.Severity < SecurityNotificationSeverity.Critical)
                .ToListAsync();

            if (oldNotifications.Any())
            {
                _context.SecurityNotifications.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted {Count} old security notifications", oldNotifications.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old notifications");
        }
    }

    public async Task NotifyNewLoginAsync(int userId, string ipAddress, string? location, string? userAgent)
    {
        var request = new SecurityNotificationRequest
        {
            UserId = userId,
            NotificationType = "NewLogin",
            Title = "New login detected",
            Message = $"A new login was detected from {location ?? "unknown location"} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.",
            Severity = SecurityNotificationSeverity.Info,
            SendEmail = true,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Location = location
        };

        await CreateNotificationAsync(request);
    }

    public async Task NotifyPasswordChangeAsync(int userId, string ipAddress)
    {
        var location = await GetLocationString(ipAddress);
        
        var request = new SecurityNotificationRequest
        {
            UserId = userId,
            NotificationType = "PasswordChange",
            Title = "Password changed",
            Message = $"Your password was successfully changed from {location} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.",
            Severity = SecurityNotificationSeverity.Medium,
            SendEmail = true,
            IpAddress = ipAddress,
            Location = location
        };

        await CreateNotificationAsync(request);
    }

    public async Task NotifyAccountLockedAsync(int userId, string reason, TimeSpan lockoutDuration)
    {
        var request = new SecurityNotificationRequest
        {
            UserId = userId,
            NotificationType = "AccountLocked",
            Title = "Account temporarily locked",
            Message = $"Your account has been temporarily locked for {lockoutDuration.TotalMinutes:F0} minutes. Reason: {reason}",
            Severity = SecurityNotificationSeverity.High,
            SendEmail = true,
            AdditionalData = new Dictionary<string, object>
            {
                ["lockoutDuration"] = lockoutDuration.ToString(),
                ["reason"] = reason
            }
        };

        await CreateNotificationAsync(request);
    }

    public async Task NotifyUnusualActivityAsync(int userId, string activity, string ipAddress, string? location)
    {
        var request = new SecurityNotificationRequest
        {
            UserId = userId,
            NotificationType = "UnusualActivity",
            Title = "Unusual activity detected",
            Message = $"Unusual activity detected: {activity} from {location ?? "unknown location"} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.",
            Severity = SecurityNotificationSeverity.Medium,
            SendEmail = true,
            IpAddress = ipAddress,
            Location = location,
            AdditionalData = new Dictionary<string, object>
            {
                ["activity"] = activity
            }
        };

        await CreateNotificationAsync(request);
    }

    public async Task NotifyCompromisedPasswordAsync(int userId)
    {
        var request = new SecurityNotificationRequest
        {
            UserId = userId,
            NotificationType = "CompromisedPassword",
            Title = "Password security warning",
            Message = "We detected that your password may have been compromised in a data breach. Please change your password immediately.",
            Severity = SecurityNotificationSeverity.Critical,
            SendEmail = true
        };

        await CreateNotificationAsync(request);
    }

    public async Task NotifyNewDeviceAsync(int userId, string deviceInfo, string ipAddress, string? location)
    {
        var request = new SecurityNotificationRequest
        {
            UserId = userId,
            NotificationType = "NewDevice",
            Title = "New device login",
            Message = $"A new device ({deviceInfo}) was used to access your account from {location ?? "unknown location"} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.",
            Severity = SecurityNotificationSeverity.Medium,
            SendEmail = true,
            IpAddress = ipAddress,
            Location = location,
            AdditionalData = new Dictionary<string, object>
            {
                ["deviceInfo"] = deviceInfo
            }
        };

        await CreateNotificationAsync(request);
    }

    public async Task NotifySecuritySettingsChangeAsync(int userId, string settingChanged, string ipAddress)
    {
        var location = await GetLocationString(ipAddress);
        
        var request = new SecurityNotificationRequest
        {
            UserId = userId,
            NotificationType = "SecuritySettingsChange",
            Title = "Security settings changed",
            Message = $"Your security setting '{settingChanged}' was changed from {location} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.",
            Severity = SecurityNotificationSeverity.Medium,
            SendEmail = true,
            IpAddress = ipAddress,
            Location = location,
            AdditionalData = new Dictionary<string, object>
            {
                ["settingChanged"] = settingChanged
            }
        };

        await CreateNotificationAsync(request);
    }

    private async Task SendEmailNotificationAsync(SecurityNotification notification)
    {
        try
        {
            var user = await _context.Users.FindAsync(notification.UserId);
            if (user?.Email != null)
            {
                var subject = $"Security Alert: {notification.Title}";
                var body = $"""
                    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                        <h2 style="color: #d32f2f;">🔒 Security Notification</h2>
                        
                        <div style="background-color: #f5f5f5; padding: 20px; border-radius: 5px; margin: 20px 0;">
                            <h3 style="margin-top: 0; color: #333;">{notification.Title}</h3>
                            <p style="color: #555; line-height: 1.6;">{notification.Message}</p>
                            
                            {(notification.IpAddress != null ? $"<p><strong>IP Address:</strong> {notification.IpAddress}</p>" : "")}
                            {(notification.Location != null ? $"<p><strong>Location:</strong> {notification.Location}</p>" : "")}
                            <p><strong>Time:</strong> {notification.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
                        </div>
                        
                        <div style="background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0;">
                            <p style="margin: 0; color: #856404;">
                                <strong>What should you do?</strong><br>
                                • If this was you, no action is needed<br>
                                • If this wasn't you, please change your password immediately<br>
                                • Contact support if you need assistance
                            </p>
                        </div>
                        
                        <p style="color: #777; font-size: 12px; margin-top: 30px;">
                            This is an automated security notification from MembersHub.<br>
                            If you have questions, please contact our support team.
                        </p>
                    </div>
                    """;

                var result = await _emailService.SendEmailAsync(user.Email, subject, body);
                
                if (result.Success)
                {
                    notification.EmailSent = true;
                    notification.EmailSentAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogError("Failed to send security notification email: {Message}", result.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification for notification {NotificationId}", notification.Id);
        }
    }

    private async Task<string> GetLocationString(string ipAddress)
    {
        try
        {
            var location = await _geolocationService.GetLocationAsync(ipAddress);
            if (location != null)
            {
                return $"{location.City}, {location.Country}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting location for IP {IpAddress}", ipAddress);
        }
        
        return "unknown location";
    }
}
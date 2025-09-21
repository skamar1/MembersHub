using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface ISecurityNotificationService
{
    Task CreateNotificationAsync(SecurityNotificationRequest request);
    Task<List<SecurityNotification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int limit = 50);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
    Task<List<SecurityNotification>> GetCriticalNotificationsAsync(int hours = 24);
    Task DeleteOldNotificationsAsync(int daysToKeep = 90);
    
    // Specific notification types
    Task NotifyNewLoginAsync(int userId, string ipAddress, string? location, string? userAgent);
    Task NotifyPasswordChangeAsync(int userId, string ipAddress);
    Task NotifyAccountLockedAsync(int userId, string reason, TimeSpan lockoutDuration);
    Task NotifyUnusualActivityAsync(int userId, string activity, string ipAddress, string? location);
    Task NotifyCompromisedPasswordAsync(int userId);
    Task NotifyNewDeviceAsync(int userId, string deviceInfo, string ipAddress, string? location);
    Task NotifySecuritySettingsChangeAsync(int userId, string settingChanged, string ipAddress);
}

public class SecurityNotificationRequest
{
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public SecurityNotificationSeverity Severity { get; set; } = SecurityNotificationSeverity.Info;
    public bool SendEmail { get; set; } = false;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}
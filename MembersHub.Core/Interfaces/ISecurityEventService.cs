using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface ISecurityEventService
{
    Task LogSecurityEventAsync(SecurityEventRequest request);
    Task<List<SecurityEvent>> GetUserSecurityEventsAsync(int userId, int days = 30);
    Task<List<SecurityEvent>> GetSuspiciousEventsAsync(int days = 7);
    Task<SecurityRiskAssessment> AssessLoginRiskAsync(int userId, string ipAddress, string userAgent);
    Task MarkEventAsSuspiciousAsync(int eventId, string reason);
    Task<List<SecurityEvent>> GetRecentEventsAsync(int count = 50);
    Task CleanupOldEventsAsync(int daysToKeep = 365);
}

public class SecurityEventRequest
{
    public int? UserId { get; set; }
    public SecurityEventType EventType { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public Dictionary<string, object>? AdditionalData { get; set; }
}

public class SecurityRiskAssessment
{
    public RiskLevel RiskLevel { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresDeviceVerification { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
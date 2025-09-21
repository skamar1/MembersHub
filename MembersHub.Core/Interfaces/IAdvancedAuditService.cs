using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IAdvancedAuditService
{
    Task<List<AuditLog>> GetAuditTrailAsync(string entityType, string entityId, int limit = 100);
    Task<List<AuditLog>> GetUserActivityAsync(int userId, DateTime? from = null, DateTime? to = null, int limit = 100);
    Task<List<AuditLog>> GetSuspiciousActivitiesAsync(int days = 7);
    Task<List<AuditLog>> GetFailedOperationsAsync(int days = 1);
    Task<AuditStatistics> GetAuditStatisticsAsync(int days = 30);
    Task<List<AuditLog>> SearchAuditLogsAsync(AuditSearchCriteria criteria);
    Task ExportAuditLogsAsync(AuditExportRequest request);
    Task PerformSecurityAnalysisAsync();
    Task CleanupOldAuditLogsAsync(int daysToKeep = 365);
}

public class AuditSearchCriteria
{
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public AuditAction? Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? IpAddress { get; set; }
    public bool? HasError { get; set; }
    public string? SearchTerm { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 100;
    public string OrderBy { get; set; } = "Timestamp";
    public bool Descending { get; set; } = true;
}

public class AuditExportRequest
{
    public AuditSearchCriteria Criteria { get; set; } = new();
    public ExportFormat Format { get; set; } = ExportFormat.Csv;
    public string FilePath { get; set; } = string.Empty;
    public bool IncludeSensitiveData { get; set; } = false;
}

public class AuditStatistics
{
    public int TotalActions { get; set; }
    public int UniqueUsers { get; set; }
    public int FailedOperations { get; set; }
    public int SecurityEvents { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();
    public Dictionary<string, int> EntityCounts { get; set; } = new();
    public Dictionary<string, int> UserCounts { get; set; } = new();
    public Dictionary<DateTime, int> DailyActivity { get; set; } = new();
    public List<string> TopIpAddresses { get; set; } = new();
    public List<string> SuspiciousPatterns { get; set; } = new();
}

public enum ExportFormat
{
    Csv,
    Json,
    Excel
}
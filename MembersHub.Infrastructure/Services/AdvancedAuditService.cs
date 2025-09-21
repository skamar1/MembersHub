using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Infrastructure.Services;

public class AdvancedAuditService : IAdvancedAuditService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<AdvancedAuditService> _logger;
    private readonly ISecurityNotificationService _notificationService;

    public AdvancedAuditService(
        MembersHubContext context,
        ILogger<AdvancedAuditService> logger,
        ISecurityNotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<List<AuditLog>> GetAuditTrailAsync(string entityType, string entityId, int limit = 100)
    {
        try
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit trail for {EntityType} {EntityId}", entityType, entityId);
            return new List<AuditLog>();
        }
    }

    public async Task<List<AuditLog>> GetUserActivityAsync(int userId, DateTime? from = null, DateTime? to = null, int limit = 100)
    {
        try
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.UserId == userId);

            if (from.HasValue)
                query = query.Where(a => a.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.Timestamp <= to.Value);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity for user {UserId}", userId);
            return new List<AuditLog>();
        }
    }

    public async Task<List<AuditLog>> GetSuspiciousActivitiesAsync(int days = 7)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            
            // Execute all suspicious pattern queries sequentially to avoid DbContext concurrency issues
            var suspiciousActivities = new List<AuditLog>();

            // Multiple failed login attempts from same IP
            var failedLogins = await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.Timestamp >= fromDate && 
                           a.Action == AuditAction.Login && 
                           a.ErrorMessage != null)
                .GroupBy(a => a.IpAddress)
                .Where(g => g.Count() >= 5)
                .SelectMany(g => g)
                .OrderByDescending(a => a.Timestamp)
                .Take(100)
                .ToListAsync();
            suspiciousActivities.AddRange(failedLogins);

            // Rapid successive operations from same user
            var rapidOperations = await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.Timestamp >= fromDate)
                .GroupBy(a => new { a.UserId, Hour = a.Timestamp.Hour })
                .Where(g => g.Count() >= 50)
                .SelectMany(g => g)
                .OrderByDescending(a => a.Timestamp)
                .Take(100)
                .ToListAsync();
            suspiciousActivities.AddRange(rapidOperations);

            // Operations outside normal hours (2 AM - 6 AM)
            var offHourOperations = await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.Timestamp >= fromDate && 
                           a.Timestamp.Hour >= 2 && a.Timestamp.Hour <= 6)
                .OrderByDescending(a => a.Timestamp)
                .Take(100)
                .ToListAsync();
            suspiciousActivities.AddRange(offHourOperations);

            return suspiciousActivities
                .DistinctBy(a => a.Id)
                .OrderByDescending(a => a.Timestamp)
                .Take(500)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suspicious activities");
            return new List<AuditLog>();
        }
    }

    public async Task<List<AuditLog>> GetFailedOperationsAsync(int days = 1)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            
            return await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.Timestamp >= fromDate && a.ErrorMessage != null)
                .OrderByDescending(a => a.Timestamp)
                .Take(1000)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failed operations");
            return new List<AuditLog>();
        }
    }

    public async Task<AuditStatistics> GetAuditStatisticsAsync(int days = 30)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            var statistics = new AuditStatistics();

            var auditLogs = await _context.AuditLogs
                .Where(a => a.Timestamp >= fromDate)
                .ToListAsync();

            statistics.TotalActions = auditLogs.Count;
            statistics.UniqueUsers = auditLogs.Where(a => a.UserId.HasValue).Select(a => a.UserId).Distinct().Count();
            statistics.FailedOperations = auditLogs.Count(a => a.ErrorMessage != null);
            statistics.SecurityEvents = auditLogs.Count(a => 
                a.Action == AuditAction.Login || 
                a.Action == AuditAction.Logout || 
                a.EntityType.Contains("Security"));

            // Action counts
            statistics.ActionCounts = auditLogs
                .GroupBy(a => a.Action.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Entity counts
            statistics.EntityCounts = auditLogs
                .GroupBy(a => a.EntityType)
                .ToDictionary(g => g.Key, g => g.Count());

            // User activity counts
            statistics.UserCounts = auditLogs
                .Where(a => a.UserId.HasValue)
                .GroupBy(a => a.Username)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            // Daily activity
            statistics.DailyActivity = auditLogs
                .GroupBy(a => a.Timestamp.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            // Top IP addresses
            statistics.TopIpAddresses = auditLogs
                .Where(a => !string.IsNullOrEmpty(a.IpAddress))
                .GroupBy(a => a.IpAddress)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            // Suspicious patterns detection
            statistics.SuspiciousPatterns = await DetectSuspiciousPatternsAsync(auditLogs);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit statistics");
            return new AuditStatistics();
        }
    }

    public async Task<List<AuditLog>> SearchAuditLogsAsync(AuditSearchCriteria criteria)
    {
        try
        {
            var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

            if (criteria.UserId.HasValue)
                query = query.Where(a => a.UserId == criteria.UserId.Value);

            if (!string.IsNullOrEmpty(criteria.Username))
                query = query.Where(a => a.Username.Contains(criteria.Username));

            if (criteria.Action.HasValue)
                query = query.Where(a => a.Action == criteria.Action.Value);

            if (!string.IsNullOrEmpty(criteria.EntityType))
                query = query.Where(a => a.EntityType.Contains(criteria.EntityType));

            if (!string.IsNullOrEmpty(criteria.EntityId))
                query = query.Where(a => a.EntityId == criteria.EntityId);

            if (criteria.FromDate.HasValue)
                query = query.Where(a => a.Timestamp >= criteria.FromDate.Value);

            if (criteria.ToDate.HasValue)
                query = query.Where(a => a.Timestamp <= criteria.ToDate.Value);

            if (!string.IsNullOrEmpty(criteria.IpAddress))
                query = query.Where(a => a.IpAddress == criteria.IpAddress);

            if (criteria.HasError.HasValue)
            {
                if (criteria.HasError.Value)
                    query = query.Where(a => a.ErrorMessage != null);
                else
                    query = query.Where(a => a.ErrorMessage == null);
            }

            if (!string.IsNullOrEmpty(criteria.SearchTerm))
            {
                query = query.Where(a => 
                    a.Description.Contains(criteria.SearchTerm) ||
                    a.EntityName.Contains(criteria.SearchTerm) ||
                    a.OldValues.Contains(criteria.SearchTerm) ||
                    a.NewValues.Contains(criteria.SearchTerm));
            }

            // Apply ordering
            query = criteria.OrderBy.ToLower() switch
            {
                "username" => criteria.Descending ? query.OrderByDescending(a => a.Username) : query.OrderBy(a => a.Username),
                "action" => criteria.Descending ? query.OrderByDescending(a => a.Action) : query.OrderBy(a => a.Action),
                "entitytype" => criteria.Descending ? query.OrderByDescending(a => a.EntityType) : query.OrderBy(a => a.EntityType),
                _ => criteria.Descending ? query.OrderByDescending(a => a.Timestamp) : query.OrderBy(a => a.Timestamp)
            };

            return await query
                .Skip(criteria.Skip)
                .Take(criteria.Take)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit logs");
            return new List<AuditLog>();
        }
    }

    public async Task ExportAuditLogsAsync(AuditExportRequest request)
    {
        try
        {
            var logs = await SearchAuditLogsAsync(request.Criteria);
            
            switch (request.Format)
            {
                case ExportFormat.Csv:
                    await ExportToCsvAsync(logs, request.FilePath, request.IncludeSensitiveData);
                    break;
                case ExportFormat.Json:
                    await ExportToJsonAsync(logs, request.FilePath, request.IncludeSensitiveData);
                    break;
                case ExportFormat.Excel:
                    throw new NotImplementedException("Excel export not yet implemented");
                default:
                    throw new ArgumentException("Unsupported export format");
            }

            _logger.LogInformation("Exported {Count} audit logs to {FilePath} in {Format} format", 
                logs.Count, request.FilePath, request.Format);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            throw;
        }
    }

    public async Task PerformSecurityAnalysisAsync()
    {
        try
        {
            var suspiciousActivities = await GetSuspiciousActivitiesAsync(7);
            var failedOperations = await GetFailedOperationsAsync(1);

            // Analyze patterns and create notifications
            var suspiciousIps = suspiciousActivities
                .Where(a => !string.IsNullOrEmpty(a.IpAddress))
                .GroupBy(a => a.IpAddress)
                .Where(g => g.Count() >= 10)
                .Select(g => g.Key)
                .ToList();

            foreach (var ip in suspiciousIps)
            {
                var activities = suspiciousActivities.Where(a => a.IpAddress == ip).ToList();
                var userIds = activities.Where(a => a.UserId.HasValue).Select(a => a.UserId!.Value).Distinct();

                foreach (var userId in userIds)
                {
                    await _notificationService.NotifyUnusualActivityAsync(
                        userId,
                        $"Multiple suspicious activities detected from IP {ip}",
                        ip,
                        null);
                }
            }

            // Detect unusual login patterns
            var recentLogins = suspiciousActivities
                .Where(a => a.Action == AuditAction.Login)
                .GroupBy(a => a.UserId)
                .Where(g => g.Count() >= 5)
                .ToList();

            foreach (var userLogins in recentLogins)
            {
                if (userLogins.Key.HasValue)
                {
                    var locations = userLogins.Where(l => !string.IsNullOrEmpty(l.IpAddress))
                        .Select(l => l.IpAddress).Distinct().Count();
                    
                    if (locations >= 3)
                    {
                        await _notificationService.NotifyUnusualActivityAsync(
                            userLogins.Key.Value,
                            "Multiple login locations detected",
                            userLogins.First().IpAddress ?? "",
                            null);
                    }
                }
            }

            _logger.LogInformation("Security analysis completed. Found {SuspiciousCount} suspicious activities and {FailedCount} failed operations",
                suspiciousActivities.Count, failedOperations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing security analysis");
        }
    }

    public async Task CleanupOldAuditLogsAsync(int daysToKeep = 365)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            
            // Keep critical security events longer
            var oldLogs = await _context.AuditLogs
                .Where(a => a.Timestamp < cutoffDate && 
                           a.Action != AuditAction.Login &&
                           !a.EntityType.Contains("Security") &&
                           a.ErrorMessage == null)
                .Take(10000) // Process in batches
                .ToListAsync();

            if (oldLogs.Any())
            {
                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cleaned up {Count} old audit logs", oldLogs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old audit logs");
        }
    }

    private async Task<List<string>> DetectSuspiciousPatternsAsync(List<AuditLog> auditLogs)
    {
        var patterns = new List<string>();

        try
        {
            // Pattern 1: Rapid successive operations
            var rapidOps = auditLogs
                .GroupBy(a => new { a.UserId, a.IpAddress, Hour = a.Timestamp.Hour })
                .Where(g => g.Count() >= 20)
                .ToList();

            if (rapidOps.Any())
                patterns.Add($"Detected {rapidOps.Count} instances of rapid successive operations");

            // Pattern 2: Failed login concentration
            var failedLogins = auditLogs
                .Where(a => a.Action == AuditAction.Login && a.ErrorMessage != null)
                .GroupBy(a => a.IpAddress)
                .Where(g => g.Count() >= 5)
                .ToList();

            if (failedLogins.Any())
                patterns.Add($"Detected {failedLogins.Count} IP addresses with multiple failed login attempts");

            // Pattern 3: Off-hours activity
            var offHours = auditLogs
                .Where(a => a.Timestamp.Hour >= 2 && a.Timestamp.Hour <= 6)
                .Count();

            if (offHours > 50)
                patterns.Add($"Detected {offHours} operations during off-hours (2 AM - 6 AM)");

            // Pattern 4: Multiple locations for same user
            var multiLocation = auditLogs
                .Where(a => a.UserId.HasValue && !string.IsNullOrEmpty(a.IpAddress))
                .GroupBy(a => a.UserId)
                .Where(g => g.Select(l => l.IpAddress).Distinct().Count() >= 5)
                .Count();

            if (multiLocation > 0)
                patterns.Add($"Detected {multiLocation} users with activity from multiple locations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting suspicious patterns");
            patterns.Add("Error occurred during pattern detection");
        }

        return patterns;
    }

    private async Task ExportToCsvAsync(List<AuditLog> logs, string filePath, bool includeSensitiveData)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,Username,Action,EntityType,EntityId,EntityName,Description,IpAddress,UserAgent,HasError");

        foreach (var log in logs)
        {
            var line = $"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
                      $"\"{log.Username}\"," +
                      $"\"{log.Action}\"," +
                      $"\"{log.EntityType}\"," +
                      $"\"{log.EntityId}\"," +
                      $"\"{log.EntityName}\"," +
                      $"\"{log.Description}\"," +
                      $"\"{log.IpAddress}\"," +
                      $"\"{(includeSensitiveData ? log.UserAgent : "***")}\"," +
                      $"\"{!string.IsNullOrEmpty(log.ErrorMessage)}\"";
            csv.AppendLine(line);
        }

        await File.WriteAllTextAsync(filePath, csv.ToString());
    }

    private async Task ExportToJsonAsync(List<AuditLog> logs, string filePath, bool includeSensitiveData)
    {
        var exportData = logs.Select(log => new
        {
            log.Id,
            log.Timestamp,
            log.Username,
            Action = log.Action.ToString(),
            log.EntityType,
            log.EntityId,
            log.EntityName,
            log.Description,
            log.IpAddress,
            UserAgent = includeSensitiveData ? log.UserAgent : "***",
            HasError = !string.IsNullOrEmpty(log.ErrorMessage),
            ErrorMessage = includeSensitiveData ? log.ErrorMessage : null
        });

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json);
    }
}
using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IAuditService
{
    // Basic audit logging
    Task LogAsync(AuditAction action, string entityType, string? entityId = null, 
        string? entityName = null, string? description = null, 
        int? userId = null, string? username = null, string? fullName = null,
        string? ipAddress = null, string? userAgent = null,
        string? oldValues = null, string? newValues = null, string? errorMessage = null);
    
    // Convenience methods for common operations
    Task LogLoginAsync(int userId, string username, string fullName, string? ipAddress = null, string? userAgent = null);
    Task LogLoginFailedAsync(string username, string? ipAddress = null, string? userAgent = null, string? errorMessage = null);
    Task LogLogoutAsync(int userId, string username, string fullName, string? ipAddress = null);
    
    Task LogCreateAsync<T>(T entity, int userId, string username, string fullName, string? ipAddress = null) where T : class;
    Task LogUpdateAsync<T>(T oldEntity, T newEntity, int userId, string username, string fullName, string? ipAddress = null) where T : class;
    Task LogDeleteAsync<T>(T entity, int userId, string username, string fullName, string? ipAddress = null) where T : class;
    Task LogViewAsync<T>(T entity, int userId, string username, string fullName, string? ipAddress = null) where T : class;
    
    Task LogUnauthorizedAccessAsync(string resource, int? userId = null, string? username = null, 
        string? ipAddress = null, string? userAgent = null);
    
    // Query methods
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int? userId = null, AuditAction? action = null, 
        string? entityType = null, DateTime? fromDate = null, DateTime? toDate = null, 
        int skip = 0, int take = 100);
    
    Task<int> GetAuditLogsCountAsync(int? userId = null, AuditAction? action = null, 
        string? entityType = null, DateTime? fromDate = null, DateTime? toDate = null);
}
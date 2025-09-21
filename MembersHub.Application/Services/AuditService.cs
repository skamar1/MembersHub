using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace MembersHub.Application.Services;

public class AuditService : IAuditService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(MembersHubContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(AuditAction action, string entityType, string? entityId = null,
        string? entityName = null, string? description = null,
        int? userId = null, string? username = null, string? fullName = null,
        string? ipAddress = null, string? userAgent = null,
        string? oldValues = null, string? newValues = null, string? errorMessage = null)
    {
        try
        {
            // If userId is provided, verify that the user exists in the database
            if (userId.HasValue)
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId.Value);
                if (!userExists)
                {
                    _logger.LogWarning("Attempted to log audit entry for non-existent user ID {UserId}. Setting UserId to null.", userId);
                    userId = null;
                }
            }

            var auditLog = new AuditLog
            {
                UserId = userId,
                Username = username ?? "Άγνωστος",
                FullName = fullName ?? "Άγνωστος Χρήστης",
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description ?? GenerateDescription(action, entityType, entityName),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                OldValues = oldValues,
                NewValues = newValues,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry for action {Action} on {EntityType}", action, entityType);
        }
    }

    public async Task LogLoginAsync(int userId, string username, string fullName, string? ipAddress = null, string? userAgent = null)
    {
        await LogAsync(AuditAction.Login, "Authentication", userId.ToString(), fullName,
            $"Ο χρήστης {fullName} συνδέθηκε στην εφαρμογή", userId, username, fullName, ipAddress, userAgent);
    }

    public async Task LogLoginFailedAsync(string username, string? ipAddress = null, string? userAgent = null, string? errorMessage = null)
    {
        await LogAsync(AuditAction.LoginFailed, "Authentication", null, username,
            $"Αποτυχημένη προσπάθεια σύνδεσης για τον χρήστη {username}", null, username, username, ipAddress, userAgent, null, null, errorMessage);
    }

    public async Task LogLogoutAsync(int userId, string username, string fullName, string? ipAddress = null)
    {
        await LogAsync(AuditAction.Logout, "Authentication", userId.ToString(), fullName,
            $"Ο χρήστης {fullName} αποσυνδέθηκε από την εφαρμογή", userId, username, fullName, ipAddress);
    }

    public async Task LogCreateAsync<T>(T entity, int userId, string username, string fullName, string? ipAddress = null) where T : class
    {
        var entityType = typeof(T).Name;
        var entityName = GetEntityName(entity);
        var entityId = GetEntityId(entity);
        var newValues = SerializeEntity(entity);

        var action = GetActionForEntity<T>(AuditAction.Create);

        await LogAsync(action, entityType, entityId, entityName,
            $"Δημιουργία νέου {GetGreekEntityType<T>()}: {entityName}", 
            userId, username, fullName, ipAddress, null, null, newValues);
    }

    public async Task LogUpdateAsync<T>(T oldEntity, T newEntity, int userId, string username, string fullName, string? ipAddress = null) where T : class
    {
        var entityType = typeof(T).Name;
        var entityName = GetEntityName(newEntity);
        var entityId = GetEntityId(newEntity);
        var oldValues = SerializeEntity(oldEntity);
        var newValues = SerializeEntity(newEntity);
        var changes = GetChanges(oldEntity, newEntity);

        var action = GetActionForEntity<T>(AuditAction.Update);

        await LogAsync(action, entityType, entityId, entityName,
            $"Ενημέρωση {GetGreekEntityType<T>()}: {entityName}{(changes.Any() ? $" - Αλλαγές: {string.Join(", ", changes)}" : "")}",
            userId, username, fullName, ipAddress, null, oldValues, newValues);
    }

    public async Task LogDeleteAsync<T>(T entity, int userId, string username, string fullName, string? ipAddress = null) where T : class
    {
        var entityType = typeof(T).Name;
        var entityName = GetEntityName(entity);
        var entityId = GetEntityId(entity);
        var oldValues = SerializeEntity(entity);

        var action = GetActionForEntity<T>(AuditAction.Delete);

        await LogAsync(action, entityType, entityId, entityName,
            $"Διαγραφή {GetGreekEntityType<T>()}: {entityName}", 
            userId, username, fullName, ipAddress, null, oldValues);
    }

    public async Task LogViewAsync<T>(T entity, int userId, string username, string fullName, string? ipAddress = null) where T : class
    {
        var entityType = typeof(T).Name;
        var entityName = GetEntityName(entity);
        var entityId = GetEntityId(entity);

        var action = GetActionForEntity<T>(AuditAction.View);

        await LogAsync(action, entityType, entityId, entityName,
            $"Προβολή {GetGreekEntityType<T>()}: {entityName}", 
            userId, username, fullName, ipAddress);
    }

    public async Task LogUnauthorizedAccessAsync(string resource, int? userId = null, string? username = null,
        string? ipAddress = null, string? userAgent = null)
    {
        await LogAsync(AuditAction.UnauthorizedAccess, "Security", null, resource,
            $"Μη εξουσιοδοτημένη πρόσβαση στον πόρο: {resource}",
            userId, username, username, ipAddress, userAgent);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int? userId = null, AuditAction? action = null,
        string? entityType = null, DateTime? fromDate = null, DateTime? toDate = null,
        int skip = 0, int take = 100)
    {
        var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (action.HasValue)
            query = query.Where(a => a.Action == action.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (fromDate.HasValue)
            query = query.Where(a => a.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.Timestamp <= toDate.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetAuditLogsCountAsync(int? userId = null, AuditAction? action = null,
        string? entityType = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (action.HasValue)
            query = query.Where(a => a.Action == action.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (fromDate.HasValue)
            query = query.Where(a => a.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.Timestamp <= toDate.Value);

        return await query.CountAsync();
    }

    private static string GenerateDescription(AuditAction action, string entityType, string? entityName)
    {
        var greekEntityType = GetGreekEntityType(entityType);
        var entityDisplay = !string.IsNullOrEmpty(entityName) ? $": {entityName}" : "";

        return action switch
        {
            AuditAction.Create => $"Δημιουργία {greekEntityType}{entityDisplay}",
            AuditAction.Update => $"Ενημέρωση {greekEntityType}{entityDisplay}",
            AuditAction.Delete => $"Διαγραφή {greekEntityType}{entityDisplay}",
            AuditAction.View => $"Προβολή {greekEntityType}{entityDisplay}",
            AuditAction.Login => "Σύνδεση στην εφαρμογή",
            AuditAction.Logout => "Αποσύνδεση από την εφαρμογή",
            _ => $"{action} - {greekEntityType}{entityDisplay}"
        };
    }

    private static string GetGreekEntityType<T>() => GetGreekEntityType(typeof(T).Name);

    private static string GetGreekEntityType(string entityType) => entityType switch
    {
        "Member" => "μέλους",
        "User" => "χρήστη",
        "Payment" => "πληρωμής",
        "Expense" => "εξόδου",
        "Subscription" => "συνδρομής",
        _ => entityType.ToLower()
    };

    private static AuditAction GetActionForEntity<T>(AuditAction baseAction) => typeof(T).Name switch
    {
        "Member" => baseAction switch
        {
            AuditAction.Create => AuditAction.MemberCreate,
            AuditAction.Update => AuditAction.MemberUpdate,
            AuditAction.Delete => AuditAction.MemberDelete,
            AuditAction.View => AuditAction.MemberView,
            _ => baseAction
        },
        "User" => baseAction switch
        {
            AuditAction.Create => AuditAction.UserCreate,
            AuditAction.Update => AuditAction.UserUpdate,
            AuditAction.Delete => AuditAction.UserDelete,
            AuditAction.View => AuditAction.UserView,
            _ => baseAction
        },
        "Payment" => baseAction switch
        {
            AuditAction.Create => AuditAction.PaymentCreate,
            AuditAction.Update => AuditAction.PaymentUpdate,
            AuditAction.Delete => AuditAction.PaymentDelete,
            AuditAction.View => AuditAction.PaymentView,
            _ => baseAction
        },
        "Expense" => baseAction switch
        {
            AuditAction.Create => AuditAction.ExpenseCreate,
            AuditAction.Update => AuditAction.ExpenseUpdate,
            AuditAction.Delete => AuditAction.ExpenseDelete,
            AuditAction.View => AuditAction.ExpenseView,
            _ => baseAction
        },
        _ => baseAction
    };

    private static string GetEntityName<T>(T entity) where T : class
    {
        return entity switch
        {
            Member member => member.FullName,
            User user => user.FullName,
            Payment payment => $"Πληρωμή #{payment.Id}",
            Expense expense => expense.Description,
            _ => entity.ToString() ?? "Unknown"
        };
    }

    private static string GetEntityId<T>(T entity) where T : class
    {
        var idProperty = typeof(T).GetProperty("Id");
        return idProperty?.GetValue(entity)?.ToString() ?? "";
    }

    private static string SerializeEntity<T>(T entity) where T : class
    {
        try
        {
            return JsonSerializer.Serialize(entity, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            System.Diagnostics.Debug.WriteLine($"Failed to serialize entity {typeof(T).Name}: {ex.Message}");
            return entity.ToString() ?? "";
        }
    }

    private static List<string> GetChanges<T>(T oldEntity, T newEntity) where T : class
    {
        var changes = new List<string>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !p.PropertyType.IsClass || p.PropertyType == typeof(string));

        foreach (var property in properties)
        {
            var oldValue = property.GetValue(oldEntity);
            var newValue = property.GetValue(newEntity);

            if (!Equals(oldValue, newValue))
            {
                changes.Add($"{property.Name}: '{oldValue}' → '{newValue}'");
            }
        }

        return changes;
    }
}
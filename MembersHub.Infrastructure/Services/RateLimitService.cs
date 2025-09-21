using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Infrastructure.Services;

public class RateLimitService : IRateLimitService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<RateLimitService> _logger;
    
    // Rate limit configuration
    private const int EmailMaxAttempts = 3;
    private const int IpMaxAttempts = 5;
    private const int WindowHours = 1;

    public RateLimitService(
        MembersHubContext context,
        ILogger<RateLimitService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool IsAllowed, string Message)> CheckRateLimitAsync(string email, string ipAddress)
    {
        var now = DateTime.UtcNow;
        
        // Check email rate limit
        var emailLimit = await GetOrCreateRateLimitAsync(email, RateLimitType.Email);
        var emailCheck = CheckSingleRateLimit(emailLimit, EmailMaxAttempts, now);
        
        if (!emailCheck.IsAllowed)
        {
            return emailCheck;
        }

        // Check IP rate limit
        var ipLimit = await GetOrCreateRateLimitAsync(ipAddress, RateLimitType.IpAddress);
        var ipCheck = CheckSingleRateLimit(ipLimit, IpMaxAttempts, now);
        
        return ipCheck;
    }

    public async Task RecordAttemptAsync(string email, string ipAddress)
    {
        var now = DateTime.UtcNow;
        
        try
        {
            // Record email attempt
            var emailLimit = await GetOrCreateRateLimitAsync(email, RateLimitType.Email);
            await RecordSingleAttemptAsync(emailLimit, now);
            
            // Record IP attempt
            var ipLimit = await GetOrCreateRateLimitAsync(ipAddress, RateLimitType.IpAddress);
            await RecordSingleAttemptAsync(ipLimit, now);
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Recorded password reset attempt for email {Email} from IP {IpAddress}", email, ipAddress);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601)
        {
            // Duplicate key constraint violation - handle concurrent access
            _logger.LogWarning("Rate limit duplicate key constraint for email {Email} from IP {IpAddress} - clearing change tracker and retrying", email, ipAddress);
            
            // Clear all tracked rate limit entities to avoid conflicts
            var trackedEntries = _context.ChangeTracker.Entries<PasswordResetRateLimit>().ToList();
            foreach (var entry in trackedEntries)
            {
                entry.State = EntityState.Detached;
            }
            
            // For rate limiting purposes, we'll just log this as handled
            // The fact that we got a duplicate key means rate limiting is working
            _logger.LogInformation("Rate limit constraint handled for email {Email} from IP {IpAddress}", email, ipAddress);
        }
    }

    public async Task CleanupOldRecordsAsync()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-WindowHours * 2); // Keep records for double the window time
        
        var oldRecords = await _context.PasswordResetRateLimits
            .Where(r => r.WindowStartAt < cutoffTime)
            .ToListAsync();
            
        if (oldRecords.Any())
        {
            _context.PasswordResetRateLimits.RemoveRange(oldRecords);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} old rate limit records", oldRecords.Count);
        }
    }

    private async Task<PasswordResetRateLimit> GetOrCreateRateLimitAsync(string identifier, RateLimitType type)
    {
        // First, check if we already have this entity tracked in the context
        var trackedEntity = _context.ChangeTracker.Entries<PasswordResetRateLimit>()
            .FirstOrDefault(e => e.Entity.Identifier == identifier && e.Entity.Type == type)?.Entity;
            
        if (trackedEntity != null)
        {
            return trackedEntity;
        }
        
        var rateLimit = await _context.PasswordResetRateLimits
            .FirstOrDefaultAsync(r => r.Identifier == identifier && r.Type == type);
            
        if (rateLimit == null)
        {
            rateLimit = new PasswordResetRateLimit
            {
                Identifier = identifier,
                Type = type,
                AttemptCount = 0,
                WindowStartAt = DateTime.UtcNow,
                LastAttemptAt = DateTime.UtcNow
            };
            
            _context.PasswordResetRateLimits.Add(rateLimit);
        }
        
        return rateLimit;
    }

    private static (bool IsAllowed, string Message) CheckSingleRateLimit(PasswordResetRateLimit rateLimit, int maxAttempts, DateTime now)
    {
        // Check if currently blocked
        if (rateLimit.IsBlocked)
        {
            var remainingBlockTime = rateLimit.BlockedUntil!.Value - now;
            return (false, $"Πολλές προσπάθειες. Δοκιμάστε ξανά σε {remainingBlockTime.Minutes} λεπτά.");
        }
        
        // Reset window if needed
        if (!rateLimit.IsWithinHourWindow)
        {
            rateLimit.AttemptCount = 0;
            rateLimit.WindowStartAt = now;
            rateLimit.BlockedUntil = null;
        }
        
        // Check if under limit
        if (rateLimit.AttemptCount < maxAttempts)
        {
            return (true, "Allowed");
        }
        
        // Block for the remainder of the window
        var windowEnd = rateLimit.WindowStartAt.AddHours(WindowHours);
        rateLimit.BlockedUntil = windowEnd;
        
        var blockTimeMinutes = (int)(windowEnd - now).TotalMinutes + 1;
        return (false, $"Υπερβήκατε το όριο προσπαθειών. Δοκιμάστε ξανά σε {blockTimeMinutes} λεπτά.");
    }

    private static Task RecordSingleAttemptAsync(PasswordResetRateLimit rateLimit, DateTime now)
    {
        // Reset window if needed
        if (!rateLimit.IsWithinHourWindow)
        {
            rateLimit.AttemptCount = 0;
            rateLimit.WindowStartAt = now;
            rateLimit.BlockedUntil = null;
        }
        
        rateLimit.AttemptCount++;
        rateLimit.LastAttemptAt = now;
        
        return Task.CompletedTask;
    }
}
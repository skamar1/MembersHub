using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Infrastructure.Services;

public class AccountLockoutService : IAccountLockoutService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<AccountLockoutService> _logger;
    private readonly IConfiguration _configuration;
    
    // Configuration defaults
    private readonly int _maxFailedAttempts;
    private readonly TimeSpan _lockoutDuration;
    private readonly TimeSpan _failedAttemptWindow;
    private readonly bool _enableAccountLockout;

    public AccountLockoutService(
        MembersHubContext context,
        ILogger<AccountLockoutService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        
        // Load configuration with defaults
        _maxFailedAttempts = _configuration.GetValue("Security:AccountLockout:MaxFailedAttempts", 5);
        _lockoutDuration = TimeSpan.FromMinutes(_configuration.GetValue("Security:AccountLockout:LockoutDurationMinutes", 15));
        _failedAttemptWindow = TimeSpan.FromMinutes(_configuration.GetValue("Security:AccountLockout:FailedAttemptWindowMinutes", 15));
        _enableAccountLockout = _configuration.GetValue("Security:AccountLockout:Enabled", true);
    }

    public async Task<bool> IsAccountLockedOutAsync(int userId)
    {
        if (!_enableAccountLockout)
            return false;

        try
        {
            var lockout = await GetAccountLockoutAsync(userId);
            return lockout?.IsLockedOut ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking account lockout status for user {UserId}", userId);
            return false; // Fail open for availability
        }
    }

    public async Task<TimeSpan?> GetRemainingLockoutTimeAsync(int userId)
    {
        if (!_enableAccountLockout)
            return null;

        try
        {
            var lockout = await GetAccountLockoutAsync(userId);
            if (lockout?.LockedUntil.HasValue == true && lockout.LockedUntil > DateTime.UtcNow)
            {
                return lockout.LockedUntil.Value - DateTime.UtcNow;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remaining lockout time for user {UserId}", userId);
            return null;
        }
    }

    public async Task RecordFailedLoginAttemptAsync(int userId, string ipAddress, string? userAgent = null)
    {
        if (!_enableAccountLockout)
            return;

        try
        {
            var lockout = await GetOrCreateAccountLockoutAsync(userId);
            
            // Reset failed attempts if the last attempt was outside the window
            if (DateTime.UtcNow - lockout.LastAttemptAt > _failedAttemptWindow)
            {
                lockout.FailedAttempts = 0;
            }

            lockout.FailedAttempts++;
            lockout.LastAttemptAt = DateTime.UtcNow;
            lockout.LastAttemptIpAddress = ipAddress;
            lockout.LastAttemptUserAgent = userAgent;
            lockout.UpdatedAt = DateTime.UtcNow;

            // Check if account should be locked out
            if (lockout.FailedAttempts >= _maxFailedAttempts && !lockout.IsLockedOut)
            {
                lockout.LockedUntil = DateTime.UtcNow.Add(_lockoutDuration);
                lockout.LockoutReason = $"Account locked after {lockout.FailedAttempts} failed login attempts";
                
                _logger.LogWarning("Account locked for user {UserId} after {FailedAttempts} failed attempts from IP {IpAddress}",
                    userId, lockout.FailedAttempts, ipAddress);
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Recorded failed login attempt for user {UserId} from IP {IpAddress}. Total attempts: {FailedAttempts}",
                userId, ipAddress, lockout.FailedAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording failed login attempt for user {UserId}", userId);
        }
    }

    public async Task ResetFailedAttemptsAsync(int userId)
    {
        if (!_enableAccountLockout)
            return;

        try
        {
            var lockout = await GetAccountLockoutAsync(userId);
            if (lockout != null)
            {
                lockout.FailedAttempts = 0;
                lockout.LockedUntil = null;
                lockout.LockoutReason = null;
                lockout.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Reset failed login attempts for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting failed attempts for user {UserId}", userId);
        }
    }

    public async Task LockoutAccountAsync(int userId, TimeSpan lockoutDuration, string reason)
    {
        try
        {
            var lockout = await GetOrCreateAccountLockoutAsync(userId);
            lockout.LockedUntil = DateTime.UtcNow.Add(lockoutDuration);
            lockout.LockoutReason = reason;
            lockout.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogWarning("Account manually locked for user {UserId} until {LockedUntil}. Reason: {Reason}",
                userId, lockout.LockedUntil, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking account for user {UserId}", userId);
        }
    }

    public async Task UnlockAccountAsync(int userId)
    {
        try
        {
            var lockout = await GetAccountLockoutAsync(userId);
            if (lockout != null)
            {
                lockout.FailedAttempts = 0;
                lockout.LockedUntil = null;
                lockout.LockoutReason = null;
                lockout.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Account manually unlocked for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account for user {UserId}", userId);
        }
    }

    public async Task<List<AccountLockout>> GetRecentLockoutsAsync(int days = 7)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            
            return await _context.AccountLockouts
                .Include(l => l.User)
                .Where(l => (l.LockedUntil.HasValue && l.LockedUntil >= fromDate) || 
                           l.UpdatedAt >= fromDate)
                .OrderByDescending(l => l.UpdatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent lockouts");
            return new List<AccountLockout>();
        }
    }

    public async Task<AccountLockoutStatus> GetAccountStatusAsync(int userId)
    {
        var status = new AccountLockoutStatus
        {
            MaxAllowedAttempts = _maxFailedAttempts
        };

        if (!_enableAccountLockout)
        {
            status.RemainingAttempts = _maxFailedAttempts;
            return status;
        }

        try
        {
            var lockout = await GetAccountLockoutAsync(userId);
            if (lockout != null)
            {
                // Reset failed attempts if outside the window
                if (DateTime.UtcNow - lockout.LastAttemptAt > _failedAttemptWindow)
                {
                    lockout.FailedAttempts = 0;
                }

                status.FailedAttempts = lockout.FailedAttempts;
                status.IsLockedOut = lockout.IsLockedOut;
                status.LockedUntil = lockout.LockedUntil;
                status.LockoutReason = lockout.LockoutReason;
                status.LastAttemptAt = lockout.LastAttemptAt;
                
                if (lockout.IsLockedOut)
                {
                    status.RemainingLockoutTime = lockout.LockedUntil - DateTime.UtcNow;
                }
            }

            status.RemainingAttempts = Math.Max(0, _maxFailedAttempts - status.FailedAttempts);
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account status for user {UserId}", userId);
            status.RemainingAttempts = _maxFailedAttempts;
            return status;
        }
    }

    public async Task CleanupExpiredLockoutsAsync()
    {
        try
        {
            var expiredLockouts = await _context.AccountLockouts
                .Where(l => l.LockedUntil.HasValue && l.LockedUntil < DateTime.UtcNow)
                .ToListAsync();

            foreach (var lockout in expiredLockouts)
            {
                lockout.FailedAttempts = 0;
                lockout.LockedUntil = null;
                lockout.LockoutReason = null;
                lockout.UpdatedAt = DateTime.UtcNow;
            }

            if (expiredLockouts.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired account lockouts", expiredLockouts.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired lockouts");
        }
    }

    private async Task<AccountLockout?> GetAccountLockoutAsync(int userId)
    {
        return await _context.AccountLockouts
            .FirstOrDefaultAsync(l => l.UserId == userId);
    }

    private async Task<AccountLockout> GetOrCreateAccountLockoutAsync(int userId)
    {
        var lockout = await GetAccountLockoutAsync(userId);
        if (lockout == null)
        {
            lockout = new AccountLockout
            {
                UserId = userId
            };
            _context.AccountLockouts.Add(lockout);
        }
        return lockout;
    }
}
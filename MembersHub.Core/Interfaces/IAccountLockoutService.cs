using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IAccountLockoutService
{
    Task<bool> IsAccountLockedOutAsync(int userId);
    Task<TimeSpan?> GetRemainingLockoutTimeAsync(int userId);
    Task RecordFailedLoginAttemptAsync(int userId, string ipAddress, string? userAgent = null);
    Task ResetFailedAttemptsAsync(int userId);
    Task LockoutAccountAsync(int userId, TimeSpan lockoutDuration, string reason);
    Task UnlockAccountAsync(int userId);
    Task<List<AccountLockout>> GetRecentLockoutsAsync(int days = 7);
    Task<AccountLockoutStatus> GetAccountStatusAsync(int userId);
    Task CleanupExpiredLockoutsAsync();
}

public class AccountLockoutStatus
{
    public bool IsLockedOut { get; set; }
    public int FailedAttempts { get; set; }
    public int MaxAllowedAttempts { get; set; }
    public int RemainingAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public TimeSpan? RemainingLockoutTime { get; set; }
    public string? LockoutReason { get; set; }
    public DateTime? LastAttemptAt { get; set; }
}
using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IRateLimitService
{
    Task<(bool IsAllowed, string Message)> CheckRateLimitAsync(string email, string ipAddress);
    Task RecordAttemptAsync(string email, string ipAddress);
    Task CleanupOldRecordsAsync();
}
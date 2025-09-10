using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface ISessionService
{
    Task<bool> CreateSessionAsync(User user, string token);
    Task<User?> GetCurrentUserAsync();
    Task<string?> GetTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task LogoutAsync();
    Task<bool> ValidateSessionAsync();
    event Action? AuthenticationStateChanged;
}
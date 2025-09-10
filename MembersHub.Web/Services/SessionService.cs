using Microsoft.JSInterop;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using System.Text.Json;

namespace MembersHub.Web.Services;

public class SessionService : ISessionService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IAuthenticationService _authService;
    private User? _currentUser;
    private string? _token;
    
    public event Action? AuthenticationStateChanged;

    public SessionService(IJSRuntime jsRuntime, IAuthenticationService authService)
    {
        _jsRuntime = jsRuntime;
        _authService = authService;
    }

    public async Task<bool> CreateSessionAsync(User user, string token)
    {
        try
        {
            _currentUser = user;
            _token = token;
            
            // Store in session storage
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "authToken", token);
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "currentUser", JsonSerializer.Serialize(user));
            
            AuthenticationStateChanged?.Invoke();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        if (_currentUser != null)
            return _currentUser;

        try
        {
            var userJson = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "currentUser");
            if (!string.IsNullOrEmpty(userJson))
            {
                _currentUser = JsonSerializer.Deserialize<User>(userJson);
            }
        }
        catch
        {
            // Session storage not available or error reading
        }

        return _currentUser;
    }

    public async Task<string?> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_token))
            return _token;

        try
        {
            _token = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "authToken");
        }
        catch
        {
            // Session storage not available or error reading
        }

        return _token;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        _token = null;
        
        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "currentUser");
        }
        catch
        {
            // Session storage not available
        }
        
        AuthenticationStateChanged?.Invoke();
    }

    public async Task<bool> ValidateSessionAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            return false;

        // TODO: Validate token expiration
        // For now, just check if token exists
        return true;
    }
}
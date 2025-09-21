using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Infrastructure.Services;

public partial class DeviceTrackingService : IDeviceTrackingService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<DeviceTrackingService> _logger;

    public DeviceTrackingService(MembersHubContext context, ILogger<DeviceTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserDevice> GetOrCreateDeviceAsync(int userId, string userAgent, string ipAddress)
    {
        var fingerprint = await GenerateDeviceFingerprintAsync(userAgent, ipAddress);
        var deviceInfo = await ParseDeviceInfoAsync(userAgent);

        var existingDevice = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceFingerprint == fingerprint);

        if (existingDevice != null)
        {
            // Update last seen info
            existingDevice.LastSeenAt = DateTime.UtcNow;
            existingDevice.LastUsedIpAddress = ipAddress;
            existingDevice.TotalLogins++;
            await _context.SaveChangesAsync();
            return existingDevice;
        }

        // Create new device
        var newDevice = new UserDevice
        {
            UserId = userId,
            DeviceFingerprint = fingerprint,
            DeviceType = deviceInfo.DeviceType,
            Browser = deviceInfo.Browser,
            BrowserVersion = deviceInfo.BrowserVersion,
            OperatingSystem = deviceInfo.OperatingSystem,
            OSVersion = deviceInfo.OSVersion,
            LastUsedIpAddress = ipAddress,
            TotalLogins = 1
        };

        _context.UserDevices.Add(newDevice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New device registered for user {UserId}: {DeviceType} - {Browser}", 
            userId, deviceInfo.DeviceType, deviceInfo.Browser);

        return newDevice;
    }

    public async Task<List<UserDevice>> GetUserDevicesAsync(int userId)
    {
        return await _context.UserDevices
            .Where(d => d.UserId == userId && d.IsActive)
            .OrderByDescending(d => d.LastSeenAt)
            .ToListAsync();
    }

    public Task<DeviceInfo> ParseDeviceInfoAsync(string userAgent)
    {
        var deviceInfo = new DeviceInfo();

        if (string.IsNullOrEmpty(userAgent))
            return Task.FromResult(deviceInfo);

        var ua = userAgent.ToLower();

        // Detect device type
        if (MobileRegex().IsMatch(ua))
            deviceInfo.DeviceType = "Mobile";
        else if (TabletRegex().IsMatch(ua))
            deviceInfo.DeviceType = "Tablet";
        else
            deviceInfo.DeviceType = "Desktop";

        // Detect browser
        if (ua.Contains("edg/"))
        {
            deviceInfo.Browser = "Microsoft Edge";
            var match = EdgeRegex().Match(userAgent);
            if (match.Success)
                deviceInfo.BrowserVersion = match.Groups[1].Value;
        }
        else if (ua.Contains("chrome/"))
        {
            deviceInfo.Browser = "Google Chrome";
            var match = ChromeRegex().Match(userAgent);
            if (match.Success)
                deviceInfo.BrowserVersion = match.Groups[1].Value;
        }
        else if (ua.Contains("firefox/"))
        {
            deviceInfo.Browser = "Mozilla Firefox";
            var match = FirefoxRegex().Match(userAgent);
            if (match.Success)
                deviceInfo.BrowserVersion = match.Groups[1].Value;
        }
        else if (ua.Contains("safari/") && !ua.Contains("chrome"))
        {
            deviceInfo.Browser = "Safari";
            var match = SafariRegex().Match(userAgent);
            if (match.Success)
                deviceInfo.BrowserVersion = match.Groups[1].Value;
        }
        else
        {
            deviceInfo.Browser = "Unknown";
        }

        // Detect operating system
        if (ua.Contains("windows nt"))
        {
            deviceInfo.OperatingSystem = "Windows";
            if (ua.Contains("windows nt 10.0"))
                deviceInfo.OSVersion = "10/11";
            else if (ua.Contains("windows nt 6.3"))
                deviceInfo.OSVersion = "8.1";
            else if (ua.Contains("windows nt 6.2"))
                deviceInfo.OSVersion = "8";
            else if (ua.Contains("windows nt 6.1"))
                deviceInfo.OSVersion = "7";
        }
        else if (ua.Contains("mac os x"))
        {
            deviceInfo.OperatingSystem = "macOS";
            var match = MacOSRegex().Match(userAgent);
            if (match.Success)
                deviceInfo.OSVersion = match.Groups[1].Value.Replace('_', '.');
        }
        else if (ua.Contains("linux"))
        {
            deviceInfo.OperatingSystem = "Linux";
        }
        else if (ua.Contains("android"))
        {
            deviceInfo.OperatingSystem = "Android";
            var match = AndroidRegex().Match(userAgent);
            if (match.Success)
                deviceInfo.OSVersion = match.Groups[1].Value;
        }
        else if (ua.Contains("ios") || ua.Contains("iphone") || ua.Contains("ipad"))
        {
            deviceInfo.OperatingSystem = "iOS";
            var match = iOSRegex().Match(userAgent);
            if (match.Success)
                deviceInfo.OSVersion = match.Groups[1].Value.Replace('_', '.');
        }
        else
        {
            deviceInfo.OperatingSystem = "Unknown";
        }

        return Task.FromResult(deviceInfo);
    }

    public Task<string> GenerateDeviceFingerprintAsync(string userAgent, string ipAddress)
    {
        // Create a stable fingerprint based on user agent characteristics
        // Note: This is a simplified version. In production, you'd want more sophisticated fingerprinting
        var combined = $"{userAgent}|{GetIPNetwork(ipAddress)}";
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Task.FromResult(Convert.ToBase64String(hashBytes));
    }

    public async Task<bool> IsTrustedDeviceAsync(int userId, string deviceFingerprint)
    {
        return await _context.UserDevices
            .AnyAsync(d => d.UserId == userId && 
                          d.DeviceFingerprint == deviceFingerprint && 
                          d.IsTrusted && 
                          d.IsActive);
    }

    public async Task MarkDeviceAsTrustedAsync(int userId, string deviceFingerprint)
    {
        var device = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceFingerprint == deviceFingerprint);

        if (device != null)
        {
            device.IsTrusted = true;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Device marked as trusted for user {UserId}: {DeviceFingerprint}", 
                userId, deviceFingerprint);
        }
    }

    public async Task RevokeDeviceAsync(int deviceId)
    {
        var device = await _context.UserDevices.FindAsync(deviceId);
        if (device != null)
        {
            device.IsActive = false;
            device.IsTrusted = false;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Device revoked: {DeviceId} for user {UserId}", deviceId, device.UserId);
        }
    }

    public async Task CleanupInactiveDevicesAsync(int daysInactive = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysInactive);
        
        var inactiveDevices = await _context.UserDevices
            .Where(d => d.LastSeenAt < cutoffDate && !d.IsTrusted)
            .ToListAsync();

        if (inactiveDevices.Any())
        {
            _context.UserDevices.RemoveRange(inactiveDevices);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} inactive devices", inactiveDevices.Count);
        }
    }

    private static string GetIPNetwork(string ipAddress)
    {
        // Return the first 3 octets for IPv4 to group by network
        if (System.Net.IPAddress.TryParse(ipAddress, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.x";
        }
        return "unknown";
    }

    // Compiled regex patterns for better performance
    [GeneratedRegex(@"mobile|android|iphone|ipad|ipod|blackberry|iemobile|opera mobile")]
    private static partial Regex MobileRegex();
    
    [GeneratedRegex(@"tablet|ipad")]
    private static partial Regex TabletRegex();
    
    [GeneratedRegex(@"edg/(\d+\.\d+)")]
    private static partial Regex EdgeRegex();
    
    [GeneratedRegex(@"chrome/(\d+\.\d+)")]
    private static partial Regex ChromeRegex();
    
    [GeneratedRegex(@"firefox/(\d+\.\d+)")]
    private static partial Regex FirefoxRegex();
    
    [GeneratedRegex(@"version/(\d+\.\d+).*safari")]
    private static partial Regex SafariRegex();
    
    [GeneratedRegex(@"mac os x (\d+[._]\d+)")]
    private static partial Regex MacOSRegex();
    
    [GeneratedRegex(@"android (\d+\.\d+)")]
    private static partial Regex AndroidRegex();
    
    [GeneratedRegex(@"os (\d+[._]\d+)")]
    private static partial Regex iOSRegex();
}
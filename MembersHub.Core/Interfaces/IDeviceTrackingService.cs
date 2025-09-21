using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IDeviceTrackingService
{
    Task<UserDevice> GetOrCreateDeviceAsync(int userId, string userAgent, string ipAddress);
    Task<List<UserDevice>> GetUserDevicesAsync(int userId);
    Task<DeviceInfo> ParseDeviceInfoAsync(string userAgent);
    Task<string> GenerateDeviceFingerprintAsync(string userAgent, string ipAddress);
    Task<bool> IsTrustedDeviceAsync(int userId, string deviceFingerprint);
    Task MarkDeviceAsTrustedAsync(int userId, string deviceFingerprint);
    Task RevokeDeviceAsync(int deviceId);
    Task CleanupInactiveDevicesAsync(int daysInactive = 90);
}

public class DeviceInfo
{
    public string DeviceType { get; set; } = string.Empty; // Mobile, Desktop, Tablet
    public string Browser { get; set; } = string.Empty;
    public string BrowserVersion { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public bool IsMobile => DeviceType.Equals("Mobile", StringComparison.OrdinalIgnoreCase);
    public bool IsTablet => DeviceType.Equals("Tablet", StringComparison.OrdinalIgnoreCase);
    public bool IsDesktop => DeviceType.Equals("Desktop", StringComparison.OrdinalIgnoreCase);
}
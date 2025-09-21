using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Interfaces;

namespace MembersHub.Infrastructure.Services;

public class GeolocationService : IGeolocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeolocationService> _logger;
    private readonly Dictionary<string, string> _countryFlags;

    public GeolocationService(HttpClient httpClient, ILogger<GeolocationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _countryFlags = InitializeCountryFlags();
    }

    public async Task<GeolocationInfo?> GetLocationAsync(string ipAddress)
    {
        if (!IsValidIpAddress(ipAddress) || IsLocalOrPrivateIP(ipAddress))
        {
            return new GeolocationInfo
            {
                Country = "Local Network",
                CountryCode = "LO",
                City = "localhost",
                Region = "Local"
            };
        }

        try
        {
            // Using ip-api.com (free service with 1000 requests/month limit)
            var response = await _httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                if (data.GetProperty("status").GetString() == "success")
                {
                    return new GeolocationInfo
                    {
                        Country = data.TryGetProperty("country", out var country) ? country.GetString() ?? "" : "",
                        CountryCode = data.TryGetProperty("countryCode", out var countryCode) ? countryCode.GetString() ?? "" : "",
                        City = data.TryGetProperty("city", out var city) ? city.GetString() ?? "" : "",
                        Region = data.TryGetProperty("regionName", out var region) ? region.GetString() ?? "" : "",
                        Latitude = data.TryGetProperty("lat", out var lat) ? lat.GetDouble() : null,
                        Longitude = data.TryGetProperty("lon", out var lon) ? lon.GetDouble() : null,
                        TimeZone = data.TryGetProperty("timezone", out var tz) ? tz.GetString() ?? "" : "",
                        ISP = data.TryGetProperty("isp", out var isp) ? isp.GetString() ?? "" : "",
                        IsProxy = data.TryGetProperty("proxy", out var proxy) && proxy.GetBoolean(),
                        // Note: Free ip-api doesn't provide VPN/Tor detection, would need premium service
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get geolocation for IP {IpAddress}", ipAddress);
        }

        return null;
    }

    public bool IsValidIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }

    public string GetCountryFlag(string countryCode)
    {
        return _countryFlags.TryGetValue(countryCode.ToUpper(), out var flag) ? flag : "🌐";
    }

    private static bool IsLocalOrPrivateIP(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip))
            return false;

        // Check for localhost
        if (IPAddress.IsLoopback(ip))
            return true;

        // Check for private IP ranges
        var bytes = ip.GetAddressBytes();
        if (bytes.Length == 4) // IPv4
        {
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;
            
            // 172.16.0.0/12
            if (bytes[0] == 172 && (bytes[1] >= 16 && bytes[1] <= 31))
                return true;
            
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;
        }

        return false;
    }

    private static Dictionary<string, string> InitializeCountryFlags()
    {
        return new Dictionary<string, string>
        {
            ["GR"] = "🇬🇷", ["US"] = "🇺🇸", ["GB"] = "🇬🇧", ["DE"] = "🇩🇪", 
            ["FR"] = "🇫🇷", ["IT"] = "🇮🇹", ["ES"] = "🇪🇸", ["CA"] = "🇨🇦",
            ["AU"] = "🇦🇺", ["JP"] = "🇯🇵", ["CN"] = "🇨🇳", ["IN"] = "🇮🇳",
            ["BR"] = "🇧🇷", ["RU"] = "🇷🇺", ["NL"] = "🇳🇱", ["SE"] = "🇸🇪",
            ["CH"] = "🇨🇭", ["AT"] = "🇦🇹", ["BE"] = "🇧🇪", ["DK"] = "🇩🇰",
            ["FI"] = "🇫🇮", ["NO"] = "🇳🇴", ["PL"] = "🇵🇱", ["CZ"] = "🇨🇿",
            ["LO"] = "🏠" // Local network
        };
    }
}
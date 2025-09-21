namespace MembersHub.Core.Interfaces;

public interface IGeolocationService
{
    Task<GeolocationInfo?> GetLocationAsync(string ipAddress);
    bool IsValidIpAddress(string ipAddress);
    string GetCountryFlag(string countryCode);
}

public class GeolocationInfo
{
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public string ISP { get; set; } = string.Empty;
    public bool IsVPN { get; set; }
    public bool IsTor { get; set; }
    public bool IsProxy { get; set; }
    public bool IsSuspicious => IsVPN || IsTor || IsProxy;
}
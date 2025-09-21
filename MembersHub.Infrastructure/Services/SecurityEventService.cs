using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Infrastructure.Services;

public class SecurityEventService : ISecurityEventService
{
    private readonly MembersHubContext _context;
    private readonly IGeolocationService _geolocationService;
    private readonly IDeviceTrackingService _deviceTrackingService;
    private readonly ILogger<SecurityEventService> _logger;

    public SecurityEventService(
        MembersHubContext context,
        IGeolocationService geolocationService,
        IDeviceTrackingService deviceTrackingService,
        ILogger<SecurityEventService> logger)
    {
        _context = context;
        _geolocationService = geolocationService;
        _deviceTrackingService = deviceTrackingService;
        _logger = logger;
    }

    public async Task LogSecurityEventAsync(SecurityEventRequest request)
    {
        try
        {
            var locationInfo = await _geolocationService.GetLocationAsync(request.IpAddress);
            var deviceInfo = !string.IsNullOrEmpty(request.UserAgent) 
                ? await _deviceTrackingService.ParseDeviceInfoAsync(request.UserAgent) 
                : null;

            var securityEvent = new SecurityEvent
            {
                UserId = request.UserId,
                EventType = request.EventType.ToString(),
                IpAddress = request.IpAddress,
                Country = locationInfo?.Country,
                City = locationInfo?.City,
                CountryCode = locationInfo?.CountryCode,
                UserAgent = request.UserAgent,
                DeviceType = deviceInfo?.DeviceType,
                Browser = deviceInfo?.Browser,
                OperatingSystem = deviceInfo?.OperatingSystem,
                IsSuccessful = request.IsSuccessful,
                AdditionalData = request.AdditionalData != null ? JsonSerializer.Serialize(request.AdditionalData) : null
            };

            // Check for suspicious patterns
            await AnalyzeSuspiciousActivity(securityEvent);

            _context.SecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Security event logged: {EventType} for user {UserId} from {Country}, {City}",
                request.EventType, request.UserId, locationInfo?.Country ?? "Unknown", locationInfo?.City ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event for user {UserId}", request.UserId);
        }
    }

    public async Task<List<SecurityEvent>> GetUserSecurityEventsAsync(int userId, int days = 30)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        return await _context.SecurityEvents
            .Where(e => e.UserId == userId && e.CreatedAt >= fromDate)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SecurityEvent>> GetSuspiciousEventsAsync(int days = 7)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        return await _context.SecurityEvents
            .Include(e => e.User)
            .Where(e => e.IsSuspicious && e.CreatedAt >= fromDate)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<SecurityRiskAssessment> AssessLoginRiskAsync(int userId, string ipAddress, string userAgent)
    {
        var assessment = new SecurityRiskAssessment
        {
            RiskLevel = RiskLevel.Low,
            RiskFactors = new List<string>()
        };

        try
        {
            // Check recent login patterns
            var recentEvents = await GetUserSecurityEventsAsync(userId, 7);
            var loginEvents = recentEvents.Where(e => e.EventType.Contains("Login")).ToList();

            // Risk factor 1: New location
            var locationInfo = await _geolocationService.GetLocationAsync(ipAddress);
            if (locationInfo != null)
            {
                var hasLoginFromCountry = loginEvents.Any(e => e.CountryCode == locationInfo.CountryCode);
                if (!hasLoginFromCountry && loginEvents.Any())
                {
                    assessment.RiskFactors.Add($"New location: {locationInfo.City}, {locationInfo.Country}");
                    assessment.RiskLevel = RiskLevel.Medium;
                }

                if (locationInfo.IsSuspicious)
                {
                    assessment.RiskFactors.Add("Suspicious IP (VPN/Proxy/Tor detected)");
                    assessment.RiskLevel = RiskLevel.High;
                }
            }

            // Risk factor 2: New device
            if (!string.IsNullOrEmpty(userAgent))
            {
                var fingerprint = await _deviceTrackingService.GenerateDeviceFingerprintAsync(userAgent, ipAddress);
                var isTrustedDevice = await _deviceTrackingService.IsTrustedDeviceAsync(userId, fingerprint);
                
                if (!isTrustedDevice)
                {
                    var deviceInfo = await _deviceTrackingService.ParseDeviceInfoAsync(userAgent);
                    assessment.RiskFactors.Add($"New device: {deviceInfo.Browser} on {deviceInfo.OperatingSystem}");
                    
                    if (assessment.RiskLevel == RiskLevel.Low)
                        assessment.RiskLevel = RiskLevel.Medium;
                    
                    assessment.RequiresDeviceVerification = true;
                }
            }

            // Risk factor 3: Unusual time patterns
            var userTimeZone = GetMostCommonTimeZone(loginEvents);
            var currentHour = DateTime.UtcNow.Hour;
            if (IsUnusualLoginTime(currentHour, userTimeZone))
            {
                assessment.RiskFactors.Add("Unusual login time");
                if (assessment.RiskLevel == RiskLevel.Low)
                    assessment.RiskLevel = RiskLevel.Medium;
            }

            // Risk factor 4: Multiple failed attempts recently
            var recentFailures = recentEvents.Count(e => e.EventType == "LoginFailure" && !e.IsSuccessful);
            if (recentFailures >= 3)
            {
                assessment.RiskFactors.Add($"{recentFailures} recent failed login attempts");
                assessment.RiskLevel = RiskLevel.High;
            }

            // Risk factor 5: Account age and activity
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                var accountAge = DateTime.UtcNow - user.CreatedAt;
                if (accountAge.TotalDays < 7) // New account
                {
                    assessment.RiskFactors.Add("New account (less than 7 days old)");
                    if (assessment.RiskLevel == RiskLevel.Low)
                        assessment.RiskLevel = RiskLevel.Medium;
                }
            }

            // Determine required actions
            assessment.RequiresTwoFactor = assessment.RiskLevel >= RiskLevel.Medium;
            
            assessment.RecommendedAction = assessment.RiskLevel switch
            {
                RiskLevel.Low => "Allow login",
                RiskLevel.Medium => "Require additional verification",
                RiskLevel.High => "Require multi-factor authentication",
                RiskLevel.Critical => "Block login and require manual review",
                _ => "Allow login"
            };

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assess login risk for user {UserId}", userId);
            assessment.RiskLevel = RiskLevel.Medium;
            assessment.RecommendedAction = "Error in risk assessment - require verification";
            return assessment;
        }
    }

    public async Task MarkEventAsSuspiciousAsync(int eventId, string reason)
    {
        var securityEvent = await _context.SecurityEvents.FindAsync(eventId);
        if (securityEvent != null)
        {
            securityEvent.IsSuspicious = true;
            securityEvent.SuspiciousReason = reason;
            await _context.SaveChangesAsync();

            _logger.LogWarning("Security event {EventId} marked as suspicious: {Reason}", eventId, reason);
        }
    }

    public async Task<List<SecurityEvent>> GetRecentEventsAsync(int count = 50)
    {
        return await _context.SecurityEvents
            .Include(e => e.User)
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task CleanupOldEventsAsync(int daysToKeep = 365)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var oldEvents = await _context.SecurityEvents
            .Where(e => e.CreatedAt < cutoffDate && !e.IsSuspicious)
            .ToListAsync();

        if (oldEvents.Any())
        {
            _context.SecurityEvents.RemoveRange(oldEvents);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} old security events", oldEvents.Count);
        }
    }

    private async Task AnalyzeSuspiciousActivity(SecurityEvent securityEvent)
    {
        var suspiciousReasons = new List<string>();

        // Check for rapid successive attempts
        var recentEvents = await _context.SecurityEvents
            .Where(e => e.UserId == securityEvent.UserId && 
                       e.IpAddress == securityEvent.IpAddress &&
                       e.CreatedAt >= DateTime.UtcNow.AddMinutes(-5))
            .CountAsync();

        if (recentEvents >= 5)
        {
            suspiciousReasons.Add("Multiple rapid attempts from same IP");
        }

        // Check for geographically impossible travel
        var lastEvent = await _context.SecurityEvents
            .Where(e => e.UserId == securityEvent.UserId && 
                       e.Country != null && 
                       e.Country != securityEvent.Country)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastEvent != null && 
            (securityEvent.CreatedAt - lastEvent.CreatedAt).TotalHours < 2 && 
            securityEvent.Country != lastEvent.Country && 
            !IsNeighboringCountry(securityEvent.CountryCode, lastEvent.CountryCode))
        {
            suspiciousReasons.Add($"Impossible travel: {lastEvent.Country} to {securityEvent.Country} in {(securityEvent.CreatedAt - lastEvent.CreatedAt).TotalMinutes:F0} minutes");
        }

        // Check for known bad patterns
        if (securityEvent.UserAgent?.Contains("bot", StringComparison.OrdinalIgnoreCase) == true ||
            securityEvent.UserAgent?.Contains("crawler", StringComparison.OrdinalIgnoreCase) == true)
        {
            suspiciousReasons.Add("Bot or crawler detected in user agent");
        }

        if (suspiciousReasons.Any())
        {
            securityEvent.IsSuspicious = true;
            securityEvent.SuspiciousReason = string.Join("; ", suspiciousReasons);
        }
    }

    private static string? GetMostCommonTimeZone(List<SecurityEvent> events)
    {
        // Simplified - in a real implementation, you'd analyze the time patterns
        return events.FirstOrDefault()?.Country switch
        {
            "Greece" => "Europe/Athens",
            "United States" => "America/New_York",
            _ => "UTC"
        };
    }

    private static bool IsUnusualLoginTime(int currentHour, string? timeZone)
    {
        // Simplified check - consider 2 AM to 6 AM as unusual login times
        return currentHour >= 2 && currentHour <= 6;
    }

    private static bool IsNeighboringCountry(string? country1, string? country2)
    {
        if (string.IsNullOrEmpty(country1) || string.IsNullOrEmpty(country2))
            return false;

        // Simplified neighboring country check
        var neighbors = new Dictionary<string, string[]>
        {
            ["GR"] = ["BG", "AL", "MK", "TR"], // Greece neighbors
            ["US"] = ["CA", "MX"], // US neighbors
            ["DE"] = ["FR", "AT", "CH", "PL", "CZ", "NL", "BE", "DK"], // Germany neighbors
            // Add more as needed
        };

        return neighbors.TryGetValue(country1, out var neighborList) && 
               neighborList.Contains(country2);
    }
}
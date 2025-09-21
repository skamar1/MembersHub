using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;

namespace MembersHub.Infrastructure.Services;

public partial class PasswordSecurityService : IPasswordSecurityService
{
    private readonly MembersHubContext _context;
    private readonly ILogger<PasswordSecurityService> _logger;
    private readonly HttpClient _httpClient;
    
    private static readonly string[] CommonPasswords = {
        "password", "123456", "password123", "admin", "qwerty", "letmein", "welcome",
        "monkey", "1234567890", "abc123", "111111", "123123", "password1", "1234567",
        "12345678", "12345", "iloveyou", "adobe123", "123456789", "sunshine", "1234567890"
    };

    public PasswordSecurityService(
        MembersHubContext context, 
        ILogger<PasswordSecurityService> logger,
        HttpClient httpClient)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> IsPasswordCompromisedAsync(string password)
    {
        try
        {
            // Use SHA-1 hash for HaveIBeenPwned API compatibility
            var sha1Hash = ComputeSHA1Hash(password);
            var prefix = sha1Hash[..5]; // First 5 characters
            var suffix = sha1Hash[5..]; // Remaining characters

            // Check local database first for performance
            var localResult = await _context.CompromisedPasswords
                .AnyAsync(cp => cp.PasswordHashSHA1 == sha1Hash);
            
            if (localResult)
            {
                _logger.LogWarning("Password found in local compromise database");
                return true;
            }

            // Check HaveIBeenPwned API (k-anonymity model)
            var response = await _httpClient.GetAsync($"https://api.pwnedpasswords.com/range/{prefix}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var lines = content.Split('\n');
                
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2 && parts[0].Trim().Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        var count = int.Parse(parts[1].Trim());
                        
                        // Store in local database for future quick lookups
                        await StoreCompromisedPasswordAsync(sha1Hash, count);
                        
                        _logger.LogWarning("Password found in HaveIBeenPwned database with {Count} breaches", count);
                        return true;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Unable to check HaveIBeenPwned API: {StatusCode}", response.StatusCode);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if password is compromised");
            return false; // Fail open for availability
        }
    }

    public async Task<bool> HasPasswordBeenUsedBeforeAsync(int userId, string password)
    {
        try
        {
            var passwordHashes = await _context.PasswordHistories
                .Where(ph => ph.UserId == userId)
                .Select(ph => ph.PasswordHash)
                .ToListAsync();

            foreach (var storedHash in passwordHashes)
            {
                if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking password history for user {UserId}", userId);
            return false;
        }
    }

    public async Task AddPasswordToHistoryAsync(int userId, string passwordHash)
    {
        try
        {
            var passwordHistory = new PasswordHistory
            {
                UserId = userId,
                PasswordHash = passwordHash
            };

            _context.PasswordHistories.Add(passwordHistory);
            await _context.SaveChangesAsync();

            // Keep only the last 10 passwords
            var oldPasswords = await _context.PasswordHistories
                .Where(ph => ph.UserId == userId)
                .OrderByDescending(ph => ph.CreatedAt)
                .Skip(10)
                .ToListAsync();

            if (oldPasswords.Any())
            {
                _context.PasswordHistories.RemoveRange(oldPasswords);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Added password to history for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding password to history for user {UserId}", userId);
        }
    }

    public async Task<List<DateTime>> GetPasswordHistoryAsync(int userId, int limit = 10)
    {
        try
        {
            return await _context.PasswordHistories
                .Where(ph => ph.UserId == userId)
                .OrderByDescending(ph => ph.CreatedAt)
                .Take(limit)
                .Select(ph => ph.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password history for user {UserId}", userId);
            return new List<DateTime>();
        }
    }

    public async Task CleanupOldPasswordHistoryAsync(int daysToKeep = 365)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            
            var oldPasswords = await _context.PasswordHistories
                .Where(ph => ph.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldPasswords.Any())
            {
                _context.PasswordHistories.RemoveRange(oldPasswords);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cleaned up {Count} old password history records", oldPasswords.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old password history");
        }
    }

    public Task<PasswordStrengthResult> AnalyzePasswordStrengthAsync(string password)
    {
        var result = new PasswordStrengthResult();
        var score = 0;
        
        if (string.IsNullOrEmpty(password))
        {
            result.Level = PasswordStrengthLevel.VeryWeak;
            result.Warnings.Add("Password cannot be empty");
            return Task.FromResult(result);
        }

        // Length checks
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Length >= 16) score++;
        else if (password.Length < 8)
        {
            result.Warnings.Add("Password should be at least 8 characters long");
        }

        // Character variety checks
        if (ContainsLowercase(password)) score++;
        else result.Suggestions.Add("Add lowercase letters");
        
        if (ContainsUppercase(password)) score++;
        else result.Suggestions.Add("Add uppercase letters");
        
        if (ContainsNumbers(password)) score++;
        else result.Suggestions.Add("Add numbers");
        
        if (ContainsSpecialChars(password)) score++;
        else result.Suggestions.Add("Add special characters (!@#$%^&*)");

        // Penalty checks
        if (HasRepeatingChars(password))
        {
            score = Math.Max(0, score - 2);
            result.Warnings.Add("Avoid repeating characters");
        }

        if (HasSequentialChars(password))
        {
            score = Math.Max(0, score - 1);
            result.Warnings.Add("Avoid sequential characters (abc, 123)");
        }

        if (IsCommonPassword(password))
        {
            score = Math.Max(0, score - 3);
            result.Warnings.Add("This is a commonly used password");
        }

        if (ContainsPersonalInfo(password))
        {
            score = Math.Max(0, score - 2);
            result.Warnings.Add("Avoid using personal information");
        }

        // Calculate final score and level
        result.Score = Math.Min(score, 10); // Cap at 10
        result.Level = score switch
        {
            >= 7 => PasswordStrengthLevel.VeryStrong,
            >= 5 => PasswordStrengthLevel.Strong,
            >= 3 => PasswordStrengthLevel.Fair,
            >= 1 => PasswordStrengthLevel.Weak,
            _ => PasswordStrengthLevel.VeryWeak
        };

        // Minimum requirements check
        result.MeetsMinimumRequirements = password.Length >= 8 && 
                                        ContainsLowercase(password) && 
                                        ContainsUppercase(password) && 
                                        ContainsNumbers(password) &&
                                        !IsCommonPassword(password);

        return Task.FromResult(result);
    }

    private async Task StoreCompromisedPasswordAsync(string sha1Hash, int breachCount)
    {
        try
        {
            var existing = await _context.CompromisedPasswords
                .FirstOrDefaultAsync(cp => cp.PasswordHashSHA1 == sha1Hash);

            if (existing != null)
            {
                existing.BreachCount = Math.Max(existing.BreachCount, breachCount);
                existing.LastSeenAt = DateTime.UtcNow;
            }
            else
            {
                var compromisedPassword = new CompromisedPassword
                {
                    PasswordHashSHA1 = sha1Hash,
                    BreachCount = breachCount
                };
                _context.CompromisedPasswords.Add(compromisedPassword);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing compromised password hash");
        }
    }

    private static string ComputeSHA1Hash(string input)
    {
        using var sha1 = SHA1.Create();
        var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes);
    }

    // Regex patterns for password analysis
    [GeneratedRegex(@"[a-z]")]
    private static partial Regex LowercaseRegex();
    
    [GeneratedRegex(@"[A-Z]")]
    private static partial Regex UppercaseRegex();
    
    [GeneratedRegex(@"\d")]
    private static partial Regex NumberRegex();
    
    [GeneratedRegex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")]
    private static partial Regex SpecialCharRegex();
    
    [GeneratedRegex(@"(.)\1{2,}")]
    private static partial Regex RepeatingCharRegex();
    
    [GeneratedRegex(@"(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz|123|234|345|456|567|678|789|890)", RegexOptions.IgnoreCase)]
    private static partial Regex SequentialRegex();
    
    [GeneratedRegex(@"(admin|user|test|demo|guest|root|password|login|welcome)", RegexOptions.IgnoreCase)]
    private static partial Regex PersonalInfoRegex();

    private static bool ContainsLowercase(string password) => LowercaseRegex().IsMatch(password);
    private static bool ContainsUppercase(string password) => UppercaseRegex().IsMatch(password);
    private static bool ContainsNumbers(string password) => NumberRegex().IsMatch(password);
    private static bool ContainsSpecialChars(string password) => SpecialCharRegex().IsMatch(password);
    private static bool HasRepeatingChars(string password) => RepeatingCharRegex().IsMatch(password);
    private static bool HasSequentialChars(string password) => SequentialRegex().IsMatch(password);
    private static bool ContainsPersonalInfo(string password) => PersonalInfoRegex().IsMatch(password);

    private static bool IsCommonPassword(string password)
    {
        return CommonPasswords.Contains(password.ToLower()) ||
               password.ToLower().Contains("password") ||
               password == "123456789" ||
               password == "qwertyuiop";
    }
}
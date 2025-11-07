using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MembersHub.Core.Entities;
using MembersHub.Infrastructure.Data;
using MembersHub.Infrastructure.Utilities;

namespace MembersHub.Infrastructure.Services;

public class DatabaseSeeder
{
    private readonly MembersHubContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(MembersHubContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> SeedAdminUserIfNeededAsync()
    {
        try
        {
            // Check if any users exist in the database
            var userCount = await _context.Users.CountAsync();

            if (userCount > 0)
            {
                _logger.LogInformation("Database already contains {Count} user(s). Skipping admin user creation.", userCount);
                return null;
            }

            _logger.LogWarning("========================================");
            _logger.LogWarning("No users found in database. Creating default admin user...");
            _logger.LogWarning("========================================");

            // Generate a secure random password
            var generatedPassword = PasswordGenerator.GenerateSecurePassword(16);

            // Hash the password using BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(generatedPassword);

            // Create the admin user
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@membershub.local",
                FullName = "System Administrator",
                PasswordHash = passwordHash,
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            _logger.LogWarning("========================================");
            _logger.LogWarning("ADMIN USER CREATED SUCCESSFULLY!");
            _logger.LogWarning("========================================");
            _logger.LogWarning("Username: admin");
            _logger.LogWarning("Password: {Password}", generatedPassword);
            _logger.LogWarning("========================================");
            _logger.LogWarning("IMPORTANT: Please save this password and change it after first login!");
            _logger.LogWarning("========================================");

            return generatedPassword;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding admin user");
            return null;
        }
    }
}

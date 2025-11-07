using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;
using MembersHub.Infrastructure.Repositories;
using MembersHub.Infrastructure.Services;

namespace MembersHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Check if DbContextFactory is already registered
        var factoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDbContextFactory<MembersHubContext>));

        if (factoryDescriptor == null)
        {
            // Factory not registered - register it
            // Try DefaultConnection first (Fly.io), fallback to membershubdb (Aspire local dev)
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? configuration.GetConnectionString("membershubdb");

            services.AddDbContextFactory<MembersHubContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(MembersHubContext).Assembly.FullName);
                });
            });
        }

        // Check if scoped DbContext is already registered (e.g., by Aspire)
        var dbContextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MembersHubContext));

        if (dbContextDescriptor == null)
        {
            // Add scoped DbContext for services that need it (using the factory)
            services.AddScoped<MembersHubContext>(provider =>
            {
                var factory = provider.GetRequiredService<IDbContextFactory<MembersHubContext>>();
                return factory.CreateDbContext();
            });
        }

        // Register generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register email services
        services.AddScoped<IEmailEncryptionService, EmailEncryptionService>();
        services.AddScoped<IEmailConfigurationService, EmailConfigurationService>();
        
        // Register password reset services
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
        services.AddScoped<IRateLimitService, RateLimitService>();
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        
        // Register security services
        services.AddHttpClient<IGeolocationService, GeolocationService>();
        services.AddScoped<IDeviceTrackingService, DeviceTrackingService>();
        services.AddScoped<ISecurityEventService, SecurityEventService>();
        services.AddHttpClient<IPasswordSecurityService, PasswordSecurityService>();
        services.AddScoped<IAccountLockoutService, AccountLockoutService>();
        services.AddScoped<ISecurityNotificationService, SecurityNotificationService>();
        services.AddScoped<IAdvancedAuditService, AdvancedAuditService>();

        // Register financial services
        services.AddScoped<IFinancialService, FinancialService>();

        return services;
    }
}
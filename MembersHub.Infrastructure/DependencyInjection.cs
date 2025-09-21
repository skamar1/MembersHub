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
        // Get connection string
        var connectionString = configuration.GetConnectionString("membershubdb")
            ?? "Server=100.113.99.32\\EXP2022;Database=membershubdb;User Id=sa;Password=admin8*;TrustServerCertificate=true;Encrypt=false;";

        // Add DbContext Factory for Blazor thread safety - this handles both factory and scoped contexts
        services.AddDbContextFactory<MembersHubContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly(typeof(MembersHubContext).Assembly.FullName);
            });
        });

        // Add scoped DbContext for services that need it (using the factory)
        services.AddScoped<MembersHubContext>(provider =>
        {
            var factory = provider.GetRequiredService<IDbContextFactory<MembersHubContext>>();
            return factory.CreateDbContext();
        });

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
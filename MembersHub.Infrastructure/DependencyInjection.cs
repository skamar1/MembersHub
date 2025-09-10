using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;
using MembersHub.Infrastructure.Repositories;

namespace MembersHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext with SQL Server
        services.AddDbContext<MembersHubContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("membershubdb") 
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? "Server=100.113.99.32,1433;Database=membershubdb;User Id=sa;Password=admin8*;TrustServerCertificate=true;Encrypt=false;";
            
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly(typeof(MembersHubContext).Assembly.FullName);
            });
        });

        // Register generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}
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
        // Add DbContext with PostgreSQL
        services.AddDbContext<MembersHubContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("membershubdb") 
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=membershubdb;Username=postgres;Password=postgres";
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(MembersHubContext).Assembly.FullName);
            });
        });

        // Register generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}
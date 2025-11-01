using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MembersHub.Infrastructure.Data;

public class MembersHubContextFactory : IDesignTimeDbContextFactory<MembersHubContext>
{
    public MembersHubContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MembersHubContext>();

        // Default PostgreSQL connection string for design-time operations (migrations)
        // This will be overridden by Aspire at runtime
        var connectionString = "Host=localhost;Port=5432;Database=membershubdb;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(MembersHubContext).Assembly.FullName);
        });

        return new MembersHubContext(optionsBuilder.Options);
    }
}
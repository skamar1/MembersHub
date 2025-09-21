using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MembersHub.Infrastructure.Data;

public class MembersHubContextFactory : IDesignTimeDbContextFactory<MembersHubContext>
{
    public MembersHubContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MembersHubContext>();

        // Simple default connection string για development
        var connectionString = "Server=100.113.99.32,1433;Database=membershubdb;User Id=sa;Password=admin8*;TrustServerCertificate=true;Encrypt=false;";

        optionsBuilder.UseSqlServer(connectionString);

        return new MembersHubContext(optionsBuilder.Options);
    }
}
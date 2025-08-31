using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MembersHub.Infrastructure.Data;

public class MembersHubContextFactory : IDesignTimeDbContextFactory<MembersHubContext>
{
    public MembersHubContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MembersHubContext>();
        
        // Simple default connection string για development
        var connectionString = "Host=localhost;Database=membershubdb;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(connectionString);
        
        return new MembersHubContext(optionsBuilder.Options);
    }
}
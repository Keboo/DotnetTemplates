using BlazorApp.Core;
using BlazorApp.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BlazorApp.AppHost;

#if DEBUG
public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // This is only used when adding migrations and updating the database from the cmd line.
        // It shouldn't ever be used in code where it might end up running in production.
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.EnableSensitiveDataLogging();
        ConfigurationBuilder config = new();
        config.AddEnvironmentVariables();
        config.AddCommandLine(args);
        IConfiguration configuration = config.Build();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString(ConnectionStrings.DatabaseKey));
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
#endif

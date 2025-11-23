using BlazorApp.Core;
using BlazorApp.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorApp.AppHost;

#if DEBUG
public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // This is only used when adding migrations and updating the database from the cmd line.
        // It shouldn't ever be used in code where it might end up running in production.
        ConfigurationBuilder config = new();
        config.AddEnvironmentVariables();
        config.AddCommandLine(args);
        IConfiguration configuration = config.Build();

        IServiceCollection services = null!;
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        })
        ;

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString(ConnectionStrings.DatabaseKey));
        optionsBuilder.
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
#endif

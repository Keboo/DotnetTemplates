using AspireApp.Core;
using AspireApp.Data;

using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspireApp.AppHost;

public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        var factory = new DefaultServiceProviderFactory(new ServiceProviderOptions()
        {
            ValidateOnBuild = false,
            ValidateScopes = false
        });
        builder.ConfigureContainer(factory);
        builder.AddDatabase();

        var host = builder.Build();

        return host.Services.GetRequiredService<ApplicationDbContext>();
    }
}


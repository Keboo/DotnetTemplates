using BlazorApp.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlazorApp.Core;

public static class DependencyInjection
{
    public static TBuilder AddDatabase<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionStrings.DatabaseKey) 
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStrings.DatabaseKey}' not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseAzureSql(connectionString);
        });
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        return builder;
    }
}

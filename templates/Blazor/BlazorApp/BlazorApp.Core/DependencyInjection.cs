using BlazorApp.Core.Services;
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

        void BuildDbOptions(DbContextOptionsBuilder options)
        {
            options.UseAzureSql(connectionString);
        }
        builder.Services.AddDbContextFactory<ApplicationDbContext>(BuildDbOptions);
        builder.Services.AddDbContextPool<ApplicationDbContext>(BuildDbOptions);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        }

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

    public static TBuilder AddTicketing<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddScoped<ITicketQueueService, TicketQueueService>();

        return builder;
    }

}

using ReactApp.Core.QA;
using ReactApp.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ReactApp.Core;

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

        // Only run migrations on startup when explicitly enabled (e.g., during: azd up)
        // Applying migrations on startup is not recommended for production scenarios.
        // See: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli&WT.mc_id=DT-MVP-5003472
        if (builder.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
        {
            builder.Services.AddHostedService<DatabaseMigrationService>();
        }

        return builder;
    }

    public static TBuilder AddQAServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddScoped<IRoomService, RoomService>();
        builder.Services.AddScoped<IQuestionService, QuestionService>();

        return builder;
    }

}
using ReactApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ReactApp.Core;

/// <summary>
/// Background service that applies EF Core migrations on application startup.
/// This ensures the database schema is up-to-date before the application begins serving requests.
/// </summary>
internal sealed class DatabaseMigrationService(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<DatabaseMigrationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the application to be fully started
        await Task.Yield();

        try
        {
            logger.LogInformation("Applying database migrations...");

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await dbContext.Database.MigrateAsync(stoppingToken);

            logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations");
            
            // Stop the application if migrations fail
            lifetime.StopApplication();
            throw;
        }
    }
}

using System.Diagnostics;

using BlazorApp.AppHost;
using BlazorApp.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorApp.AppHost;

public static class Resources
{
    public static IResourceBuilder<SqlServerServerResource> AddSqlServer(this IDistributedApplicationBuilder builder)
    {
        return builder
            .AddSqlServer("blazorapp-sql")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithContainerName("blazorapp-sql")
            .WithDataVolume("blazorapp-database")
            //Give the SQL server container a fixed port number. This can be useful if people want to use external tools 
            // with re-usable port numbers to connect to the database. The expected range is between 1024-49151.
            //.WithHostPort(00000)
            ;
    }

    public static IResourceBuilder<TResource> AddDotnetToolRestoreCommand<TResource>(this IResourceBuilder<TResource> resource)
        where TResource : IResource
    {
        resource.WithCommand("RestoreTools", "Restore Tools", async ctx =>
        {
            bool processResult = await RestoreDotnetToolsAsync(resource.Resource, ctx.ServiceProvider);
            return processResult ? CommandResults.Success() : CommandResults.Failure("Failed to restore tools");
        }, new CommandOptions()
        {
            IconName = "Toolbox"
        });
        return resource;
    }


    public static IResourceBuilder<SqlServerDatabaseResource> AddSqlDatabase(this IResourceBuilder<SqlServerServerResource> sql)
    {
        var database = sql.AddDatabase("blazorapp-db");

        database.OnResourceReady(async (resource, e, cancellationToken) =>
        {
            if (!await ApplyDatabaseMigrationsAsync(resource, e.Services, cancellationToken))
            {
                throw new Exception("Failed to apply database migrations to the database");
            }
        });
        database.AddDotnetToolRestoreCommand();

        database.WithCommand("CreateMigration", "Create Migration", async ctx =>
        {
            string? connectionString = await database.Resource.ConnectionStringExpression.GetValueAsync(ctx.CancellationToken);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return CommandResults.Failure("No connection string to the database");
            }
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var interactionService = ctx.ServiceProvider.GetRequiredService<IInteractionService>();
            var migrationNameResult = await interactionService.PromptInputAsync("Migration Name", "Enter the name for the migration", "Name", "", cancellationToken: ctx.CancellationToken);
            if (migrationNameResult.Canceled || string.IsNullOrWhiteSpace(migrationNameResult.Data.Value))
            {
                return CommandResults.Canceled();
            }
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            ProcessStartInfo psi = new()
            {
                FileName = "dotnet",
                ArgumentList = {
                    "ef",
                    "migrations",
                    "--startup-project",
                    "./BlazorApp.AppHost",
                    "--project",
                    "./BlazorApp.Data",
                    "--no-build",
                    "add",
                    migrationNameResult.Data.Value
                },
                WorkingDirectory = "..",
                EnvironmentVariables =
                {
                    { $"ConnectionStrings__{ConnectionStrings.DatabaseKey}", connectionString }
                }
            };
            bool processResult = await database.ExecuteProcessAsync(ctx, psi);
            return processResult ? CommandResults.Success() : CommandResults.Failure("Failed to create a migration");
        }, new CommandOptions()
        {
            IconName = "TableAdd"
        });

        database.WithCommand("RemoveMigration", "Remove Migration", async ctx =>
        {
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var interactionService = ctx.ServiceProvider.GetRequiredService<IInteractionService>();
            var confirmationResult = await interactionService.PromptConfirmationAsync("Remove Migration", "This will remove the most recent compiled migration. Continue?",
                options: new()
                {
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    Intent = MessageIntent.Warning
                },
                cancellationToken: ctx.CancellationToken);
            if (confirmationResult.Canceled || !confirmationResult.Data)
            {
                return CommandResults.Canceled();
            }
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            string? connectionString = await database.Resource.ConnectionStringExpression.GetValueAsync(ctx.CancellationToken);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return CommandResults.Canceled();
            }
            ProcessStartInfo psi = new()
            {
                FileName = "dotnet",
                ArgumentList = {
                    "ef",
                    "migrations",
                    "--startup-project",
                    "./BlazorApp.AppHost",
                    "--project",
                    "./BlazorApp.Data",
                    "--no-build",
                    "remove"
                },
                WorkingDirectory = "..",
                EnvironmentVariables =
                {
                    { $"ConnectionStrings__{ConnectionStrings.DatabaseKey}", connectionString }
                }
            };

            bool processResult = await database.ExecuteProcessAsync(ctx, psi);
            return processResult ? CommandResults.Success() : CommandResults.Failure("Failed to remove a migration");
        }, new CommandOptions()
        {
            IconName = "TableDismiss",
        });

        database.WithCommand("ApplyMigrations", "Apply Database Migrations",
            async ctx => await ApplyDatabaseMigrationsAsync(database.Resource, ctx.ServiceProvider, ctx.CancellationToken)
                ? CommandResults.Success() : CommandResults.Failure("Failed to apply migrations"), new CommandOptions()
                {
                    IconName = "DatabaseLightning"
                });

        return database;
    }

    private static async Task<bool> RestoreDotnetToolsAsync(IResource resource, IServiceProvider services)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "dotnet",
            ArgumentList = {
                    "tool",
                    "restore"
                },
            WorkingDirectory = "..",
        };

        return await resource.ExecuteProcessAsync(services, psi);
    }

    private static async Task<bool> ApplyDatabaseMigrationsAsync(SqlServerDatabaseResource database, IServiceProvider services, CancellationToken cancellationToken)
    {
        string? connectionString = await database.ConnectionStringExpression.GetValueAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Connection string for database not available");

        ILogger logger = services.GetResourceLogger(database);

        logger.LogInformation("Applying any pending migrations to the database");

        bool processResult = await ApplyMigrationsAsync();

        if (!processResult && await RestoreDotnetToolsAsync(database, services))
        {
            processResult = await ApplyMigrationsAsync();
        }

        if (processResult)
        {
            logger.LogInformation("Applied migrations to the database");
            return true;
        }
        else
        {
            logger.LogError("Failed to apply migrations to the database");
            return false;
        }

        Task<bool> ApplyMigrationsAsync()
        {
            ProcessStartInfo psi = new()
            {
                FileName = "dotnet",
                ArgumentList = {
                    "ef",
                    "database",
                    "update",
                    "--no-build",
                    "--startup-project",
                    "./BlazorApp.AppHost",
                    "--project",
                    "./BlazorApp.Data",
                },
                WorkingDirectory = "..",
                EnvironmentVariables =
                {
                    { $"ConnectionStrings__{ConnectionStrings.DatabaseKey}", connectionString }
                }
            };
            return database.ExecuteProcessAsync(services, psi, cancellationToken);
        }
    }
}
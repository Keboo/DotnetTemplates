using System.Diagnostics;
using System.Text;

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

    public static IResourceBuilder<SqlServerDatabaseResource> AddSqlDatabase(this IResourceBuilder<SqlServerServerResource> sql)
    {
        var database = sql.AddDatabase("blazorapp-db");

        database.WithCommand("CreateMigration", "Create Migration", async ctx =>
        {
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var interactionService = ctx.ServiceProvider.GetRequiredService<IInteractionService>();
            var migrationNameResult = await interactionService.PromptInputAsync("Migration Name", "Enter the name for the migration", "Name", "", cancellationToken: ctx.CancellationToken);
            if (migrationNameResult.Canceled || string.IsNullOrWhiteSpace(migrationNameResult.Data.Value))
            {
                return CommandResults.Canceled();
            }
            string? connectionString = await database.Resource.ConnectionStringExpression.GetValueAsync(ctx.CancellationToken);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return CommandResults.Canceled();
            }
            var logger = ctx.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
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
                    "add",
                    migrationNameResult.Data.Value
                },
                WorkingDirectory = "..",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                EnvironmentVariables =
                {
                    { $"ConnectionStrings__{ConnectionStrings.DatabaseKey}", connectionString }
                }
            };
            if (Process.Start(psi) is { } process)
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        logger.LogInformation(e.Data);
                    }
                };
                
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        logger.LogError(e.Data);
                    }
                };
                
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                await process.WaitForExitAsync(ctx.CancellationToken);
                if (process.ExitCode != 0)
                {
                    return CommandResults.Failure("Failed to create a migration");
                }
                return CommandResults.Success();
            }
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            else
            {
                return CommandResults.Failure("Failed to run dotnet");
            }
        }, new CommandOptions()
        {
            IconName = "TableAdd"
        });

        database.WithCommand("RemoveMigration", "Remove Migration", async ctx =>
        {
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var interactionService = ctx.ServiceProvider.GetRequiredService<IInteractionService>();
            var confirmationResult = await interactionService.PromptConfirmationAsync("Remove Migration", "This will remove the most recent migration. Continue?", 
                options: new()
                {
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    Intent = MessageIntent.Confirmation
                },
                cancellationToken: ctx.CancellationToken);
            if (confirmationResult.Canceled || !confirmationResult.Data)
            {
                return CommandResults.Canceled();
            }
            string? connectionString = await database.Resource.ConnectionStringExpression.GetValueAsync(ctx.CancellationToken);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return CommandResults.Canceled();
            }
            var logger = ctx.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
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
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                EnvironmentVariables =
                {
                    { $"ConnectionStrings__{ConnectionStrings.DatabaseKey}", connectionString }
                }
            };

            if (Process.Start(psi) is { } process)
            {
                StringBuilder sb = new();
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        sb.AppendLine(e.Data);
                        logger.LogInformation(e.Data);
                    }
                };
                
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        sb.AppendLine(e.Data);
                        logger.LogError(e.Data);
                    }
                };
                
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                await process.WaitForExitAsync(ctx.CancellationToken);
                string output = sb.ToString();
                if (process.ExitCode != 0)
                {
                    return CommandResults.Failure("Failed to remove a migration");
                }
                return CommandResults.Success();
            }
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            else
            {
                return CommandResults.Failure("Failed to run dotnet");
            }
        }, new CommandOptions()
        {
            IconName = "TableDismiss"
        });

        return database;
    }
}
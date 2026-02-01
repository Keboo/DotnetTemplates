using System.Diagnostics;

using Aspire.Hosting.Azure;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using ReactApp.AppHost;
using ReactApp.Core;

namespace ReactApp.AppHost;

public static class Resources
{
    public const string ContainerSuffixKey = "ReactApp:ContainerSuffix";
    public const string Frontend = "ReactApp-frontend";
    public const string SqlServer = "ReactApp-sql";


    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<AzureSqlServerResource> AddAzureSqlServer()
        {
            return builder.AddAzureSqlServer("ReactApp-sql");
        }

        public IResourceBuilder<SqlServerServerResource> AddSqlServer()
        {
            string? containerSuffix = builder.Configuration[ContainerSuffixKey];
            string? containerName = string.IsNullOrWhiteSpace(containerSuffix) ? "ReactApp-sql" : $"ReactApp-{containerSuffix}-sql";
            
            return builder
                .AddSqlServer(SqlServer)
                .WithLifetime(ContainerLifetime.Persistent)
                .WithContainerName(containerName)
                .WithDataVolume("ReactApp-database")

                // This pairs with the usage of .UseAzureSql() which has a compatibility level of 170.
                .WithImageTag("2025-latest")
                .PublishAsConnectionString()
                //Give the SQL server container a fixed port number. This can be useful if people want to use external tools 
                // with re-usable port numbers to connect to the database. The expected range is between 1024-49151.
                //.WithHostPort(00000)
                ;
        }

        public IResourceBuilder<ExternalServiceResource> AddAspireDocs()
        {
            IResourceBuilder<ExternalServiceResource> rv = builder.AddExternalService("aspire-docs", "https://aspire.dev/docs/")
                .ExcludeFromManifest()
                .ExcludeFromMcp();

            rv.WithCommand("ShowAspireCLIVersion", "Show CLI Version", async ctx =>
            {
                RefreshPathVariable();
                return await ShowCLIVersionAsync(rv, ctx)
                    ? CommandResults.Success() : CommandResults.Failure("Aspire CLI is not installed");
            },
            new CommandOptions()
            {
                IconName = "BookToolbox"
            });

            rv.WithCommand("InstallAspireCLI", "Install CLI", async ctx =>
            {
#pragma warning disable ASPIREINTERACTION001
                var interactionService = ctx.ServiceProvider.GetRequiredService<IInteractionService>();
                var confirmationResult = await interactionService.PromptConfirmationAsync(
                    "Install Aspire CLI",
                    "This will download and install the Aspire CLI from https://aspire.dev/get-started/install-cli/\n\nDo you want to continue?",
                    options: new()
                    {
                        PrimaryButtonText = "Install",
                        SecondaryButtonText = "Cancel",
                        Intent = MessageIntent.Information,
                        EnableMessageMarkdown = true
                    },
                    cancellationToken: ctx.CancellationToken);

                if (confirmationResult.Canceled || !confirmationResult.Data)
                {
                    return CommandResults.Canceled();
                }
#pragma warning restore ASPIREINTERACTION001

                bool isWindows = OperatingSystem.IsWindows();

                ProcessStartInfo psi = isWindows
                    ? new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        ArgumentList =
                        {
                            "-NonInteractive",
                            "-ExecutionPolicy",
                            "Bypass",
                            "-Command",
                            "irm https://aspire.dev/install.ps1 | iex"
                        }
                    }
                    : new ProcessStartInfo
                    {
                        FileName = "bash",
                        ArgumentList =
                        {
                            "-c",
                            "curl -sSL https://aspire.dev/install.sh | bash"
                        }
                    };

                bool processResult = await rv.ExecuteProcessAsync(ctx, psi);

                if (processResult)
                {
                    // Refresh PATH environment variable to pick up the newly installed aspire CLI
                    RefreshPathVariable();

                    await ShowCLIVersionAsync(rv, ctx);
                }

                return processResult ? CommandResults.Success() : CommandResults.Failure("Failed to install Aspire CLI");
            },
            new CommandOptions()
            {
                IconName = "DesktopToolbox"
            });

            return rv;

            static async Task<bool> ShowCLIVersionAsync(IResourceBuilder<ExternalServiceResource> builder, ExecuteCommandContext ctx)
            {
                ProcessStartInfo getAspireVersion = new()
                {
                    FileName = "aspire",
                    ArgumentList =
                {
                    "--version"
                }
                };
                return await builder.ExecuteProcessAsync(ctx, getAspireVersion);
            }
        }

        public IResourceBuilder<LogicalGroupResource> AddLogicalGroup(string name)
        {
            var resource = new LogicalGroupResource(name);
            var resourceBuilder = builder.AddResource(resource)
                .ExcludeFromManifest()
                .ExcludeFromMcp();

            // Add a lifecycle hook to dynamically update state based on children
            resourceBuilder.WithAnnotation(new ResourceSnapshotAnnotation(
                new CustomResourceSnapshot
                {
                    ResourceType = "LogicalGroupResource",
                    State = new ResourceStateSnapshot("Starting", null),
                    Properties = []
                }
            ));

            // Add a callback that runs when the app starts to monitor children
            builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) =>
            {
                // Start a background task to monitor children and update parent state
                _ = Task.Run(async () => await MonitorChildrenStateAsync(e.Services, resource), ct);

                return Task.CompletedTask;
            });

            return resourceBuilder;
        }
    }

    extension<TResource>(IResourceBuilder<TResource> builder) where TResource : IResource
    {
        public IResourceBuilder<TResource> WithDotnetToolRestoreCommand()
        {
            builder.WithCommand("RestoreTools", "Restore Tools", async ctx =>
            {
                bool processResult = await RestoreDotnetToolsAsync(builder.Resource, ctx.ServiceProvider);
                return processResult ? CommandResults.Success() : CommandResults.Failure("Failed to restore tools");
            }, new CommandOptions()
            {
                IconName = "Toolbox"
            });
            return builder;
        }

        public IResourceBuilder<TResource> WithParentRelationship(
            IResourceBuilder<LogicalGroupResource> parent)
        {
            // Track the child in the parent's children list
            parent.Resource.Children.Add(builder.Resource);

            // Add the parent relationship annotation using the same approach as Aspire
            builder.WithAnnotation(new ResourceRelationshipAnnotation(parent.Resource, "Parent"));

            // Copy all endpoint annotations from child to parent for aggregated view
            var childEndpointAnnotations = builder.Resource.Annotations.OfType<EndpointAnnotation>().ToList();
            foreach (var endpoint in childEndpointAnnotations)
            {
                // Clone the endpoint to the parent with the child's name as prefix
                var parentEndpoint = new EndpointAnnotation(
                    endpoint.Protocol,
                    name: $"{builder.Resource.Name}-{endpoint.Name}",
                    port: endpoint.Port,
                    targetPort: endpoint.TargetPort,
                    uriScheme: endpoint.UriScheme,
                    transport: endpoint.Transport,
                    isProxied: endpoint.IsProxied
                );

                parent.WithAnnotation(parentEndpoint);
            }

            return builder;
        }

        public IResourceBuilder<TResource> WithUITests()
        {
            builder.WithCommand("RunUITests", "Run UI Tests", async ctx =>
            {
                if (!builder.Resource.TryGetEndpoints(out var endpoints) || endpoints.FirstOrDefault() is not { } endpoint)
                {
                    return CommandResults.Failure("No external HTTP endpoint available for UI tests");
                }
                string baseUrl = $"{endpoint.UriScheme}://{endpoint.TargetHost}:{endpoint.Port}";
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var interactionService = ctx.ServiceProvider.GetRequiredService<IInteractionService>();
                InteractionInput headlessInput = new()
                {
                    Name = "Headless?",
                    InputType = InputType.Boolean,
                    Value = bool.TrueString
                };
                InteractionInput baseUrlInput = new()
                {
                    Name = "Base URL",
                    InputType = InputType.Text,
                    Value = baseUrl
                };
                var result = await interactionService.PromptInputsAsync("Testing", "Run the UI tests", [
                    headlessInput,
                    baseUrlInput
                ]);
                if (result.Canceled) return CommandResults.Canceled();
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                ProcessStartInfo psi = new()
                {
                    FileName = "dotnet",
                    ArgumentList = {
                        "run",
                        "--no-build",
                        "--project",
                        "ReactApp.UITests\\ReactApp.UITests.csproj"
                    },
                    WorkingDirectory = GetSolutionDirectory()?.FullName,
                    EnvironmentVariables =
                    {
                        { "TEST_BASE_URL", baseUrlInput.Value },
                        { "HEADED", headlessInput.Value }
                    }
                };

                bool processResult = await builder.Resource.ExecuteProcessAsync(ctx.ServiceProvider, psi);
                return processResult ? CommandResults.Success() : CommandResults.Failure("UI Tests did not complete successfully");
            }, new CommandOptions()
            {
                IconName = "ChevronDoubleRight",
                UpdateState = ctx =>
                    ctx.ResourceSnapshot.HealthStatus == HealthStatus.Healthy
                        ? ResourceCommandState.Enabled : ResourceCommandState.Disabled
            });
            return builder;
        }
    }

    extension(IResourceBuilder<SqlServerServerResource> sql)
    {
        public IResourceBuilder<SqlServerDatabaseResource> AddSqlDatabase()
        {
            var database = sql.AddDatabase("ReactApp-db");

            database.OnResourceReady(async (resource, e, cancellationToken) =>
            {
                if (!await ApplyDatabaseMigrationsAsync(resource, e.Services, cancellationToken))
                {
                    throw new Exception("Failed to apply database migrations to the database");
                }
            });
            database.WithDotnetToolRestoreCommand();

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
                        "./ReactApp.AppHost",
                        "--project",
                        "./ReactApp.Data",
                        "--no-build",
                        "add",
                        migrationNameResult.Data.Value
                    },
                    WorkingDirectory = GetSolutionDirectory()?.FullName,
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
                        "./ReactApp.AppHost",
                        "--project",
                        "./ReactApp.Data",
                        "--no-build",
                        "remove"
                    },
                    WorkingDirectory = GetSolutionDirectory()?.FullName,
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
            WorkingDirectory = GetSolutionDirectory()?.FullName,
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
                    "./ReactApp.AppHost",
                    "--project",
                    "./ReactApp.Data",
                },
                WorkingDirectory = GetSolutionDirectory()?.FullName,
                EnvironmentVariables =
                {
                    { $"ConnectionStrings__{ConnectionStrings.DatabaseKey}", connectionString }
                }
            };
            return database.ExecuteProcessAsync(services, psi, cancellationToken);
        }
    }

    private static void RefreshPathVariable()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        // Reload PATH from registry to pick up changes made by installers
        // This combines Machine and User PATH variables
        string? machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
        string? userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

        string newPath = string.Join(Path.PathSeparator,
            new[] { userPath, machinePath }.Where(p => !string.IsNullOrWhiteSpace(p)));

        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Process);
    }

    private static DirectoryInfo? GetSolutionDirectory()
    {
        for (DirectoryInfo dir = new(Directory.GetCurrentDirectory());
            dir.Parent is not null;
            dir = dir.Parent)
        {
            if (dir.EnumerateFiles("*.sln?").Any())
            { 
                return dir;
            }
        }
        return null;
    }

    private static async Task MonitorChildrenStateAsync(IServiceProvider services, LogicalGroupResource resource)
    {
        var resourceNotificationService = services.GetService<ResourceNotificationService>();
        if (resourceNotificationService is null)
        {
            return;
        }

        var resourceLoggerService = services.GetService<ResourceLoggerService>();
        var logger = resourceLoggerService?.GetLogger(resource);

        // Keep track of child states
        var childStates = new Dictionary<string, (string State, HealthStatus Health)>();

        await foreach (var resourceEvent in resourceNotificationService.WatchAsync())
        {
            // Check if any of the events relate to our children
            if (resource.Children.Any(c => c.Name == resourceEvent.Resource.Name))
            {
                // Update the state for this child
                var state = resourceEvent.Snapshot.State?.Text ?? "Unknown";
                var health = resourceEvent.Snapshot.HealthStatus ?? HealthStatus.Healthy;
                childStates[resourceEvent.Resource.Name] = (state, health);

                // Aggregate state from all children
                var hasUnhealthy = false;
                var hasStarting = false;
                var allRunning = true;

                foreach (var (childState, childHealth) in childStates.Values)
                {
                    if (childState != "Running")
                    {
                        allRunning = false;
                        if (childState == "Starting" || childState == "FailedToStart")
                        {
                            hasStarting = true;
                        }
                    }

                    if (childHealth == HealthStatus.Unhealthy)
                    {
                        hasUnhealthy = true;
                    }
                }

                // Determine parent state
                string parentState = hasUnhealthy ? "Unhealthy" : !allRunning ? hasStarting ? "Starting" : "Degraded" : "Running";

                // Update the parent's state
                await resourceNotificationService.PublishUpdateAsync(resource,
                    snapshot => snapshot with
                    {
                        State = new ResourceStateSnapshot(parentState, null),
                        Properties = [new("ChildCount", resource.Children.Count)]
                    });

                if (logger?.IsEnabled(LogLevel.Information) == true)
                {
                    logger.LogInformation("Updated logical group '{Name}' state to {State}",
                        resource.Name, parentState);
                }
            }
        }
    }
}
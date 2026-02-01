using ReactApp.AppHost;
using ReactApp.Core;

using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("ReactApp-cae");

var docsGroup = builder.AddLogicalGroup("docs");
builder.AddAspireDocs().WithParentRelationship(docsGroup);

IResourceBuilder<IResourceWithConnectionString> db;

if (builder.ExecutionContext.IsPublishMode)
{
    db = builder.AddAzureSqlServer().AddDatabase("ReactApp-db");
}
else
{
    var sql = builder.AddSqlServer();
    db = sql.AddSqlDatabase();

    //DBGate is a database viewer
    var dbGate = builder.AddContainer("dbgate", "dbgate/dbgate")
        .ExcludeFromManifest()
        .ExcludeFromMcp()
        .WithExplicitStart()
        .WithLifetime(ContainerLifetime.Persistent)
        .WithContainerName("ReactApp-db-gate")
        .WithHttpEndpoint(targetPort: 3000)
        .WaitFor(sql)
        .WithEnvironment("CONNECTIONS", "mssql")
        .WithEnvironment("LABEL_mssql", "MS SQL")
        .WithEnvironment("SERVER_mssql", "host.docker.internal")
        .WithEnvironment("PORT_mssql", () => $"{sql.Resource.PrimaryEndpoint.Port}")
        .WithEnvironment("USER_mssql", "sa")
        .WithEnvironment("PASSWORD_mssql", sql.Resource.PasswordParameter)
        .WithEnvironment("ENGINE_mssql", "mssql@dbgate-plugin-mssql")
        .WithParentRelationship(sql)
        .WithHttpHealthCheck("/")
        ;
}

var backend = builder.AddProject<Projects.ReactApp>("ReactApp-backend")
    .WithDependency(db, ConnectionStrings.DatabaseKey)
    .WithUITests()
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) => app.Template.Scale.MaxReplicas = 1);

var frontendApp = builder.AddJavaScriptApp(Resources.Frontend, "../ReactApp.Web", "dev")
    .WithNpm(install: true)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithDependency(backend);

if (builder.ExecutionContext.IsPublishMode)
{
    // Enable migrations on startup for Azure deployments
    // Applying migrations on startup is not recommended for production scenarios.
    // See: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli&WT.mc_id=DT-MVP-5003472
    backend.WithEnvironment("RunMigrationsOnStartup", "true");
}

builder.Build().Run();

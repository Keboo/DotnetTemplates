using ReactApp.AppHost;
using ReactApp.Core;

using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("ReactApp-cae");

var docsGroup = builder.AddLogicalGroup("docs");
builder.AddAspireDocs().WithParentRelationship(docsGroup);
builder.AddMUIDocs().WithParentRelationship(docsGroup);

IResourceBuilder<IResourceWithConnectionString> db;

if (builder.ExecutionContext.IsPublishMode)
{
    db = builder.AddAzureSqlServer().AddDatabase("ReactApp-db");
}
else
{
    var sql = builder.AddSqlServer()
        .WithDbGate(dbGate => dbGate.WithExplicitStart());
    db = sql.AddSqlDatabase();
}

var backend = builder.AddProject<Projects.ReactApp>("ReactApp-backend")
    .WithDependency(db, ConnectionStrings.DatabaseKey)
    .WithUITests()
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) => app.Template.Scale.MaxReplicas = 1);

var frontendApp = builder.AddJavaScriptApp(Resources.Frontend, "../__PROJECT_NAME__.Web", "dev")
    .WithNpm(install: true)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithDependency(backend)
    .WithEnvironment("REACTAPP_BACKEND_HTTP", backend.GetEndpoint("http"))
    .WithEnvironment("REACTAPP_BACKEND_HTTPS", backend.GetEndpoint("https"));

if (builder.ExecutionContext.IsPublishMode)
{
    // Enable migrations on startup for Azure deployments
    // Applying migrations on startup is not recommended for production scenarios.
    // See: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli&WT.mc_id=DT-MVP-5003472
    backend.WithEnvironment("RunMigrationsOnStartup", "true");
}

builder.Build().Run();

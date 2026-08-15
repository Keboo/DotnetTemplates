using AspireApp.AppHost;
using AspireApp.Core;

using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment(Resources.ContainerAppEnvironment);

var docsGroup = builder.AddLogicalGroup("docs");
builder.AddAspireDocs().WithParentRelationship(docsGroup);
builder.AddMUIDocs().WithParentRelationship(docsGroup);

IResourceBuilder<IResourceWithConnectionString> db;

if (builder.ExecutionContext.IsPublishMode)
{
    db = builder.AddAzureSqlServer().AddDatabase(Resources.Database);
}
else
{
    var sql = builder.AddSqlServer()
        .WithDbGate(dbGate => dbGate.WithExplicitStart());
    db = sql.AddSqlDatabase();
}

var backend = builder.AddProject<Projects.__PROJECT_SAFE_NAME__>(Resources.Backend)
    .WithReference(db, ConnectionStrings.DatabaseKey)
    .WithUITests()
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) => app.Template.Scale.MaxReplicas = 1);

var dbMigrations = backend.AddEFMigrations(Resources.Migrations, "AspireApp.Data.ApplicationDbContext")
    .WithMigrationsProject<Projects.__PROJECT_SAFE_NAME___Data>()
    .WaitFor(db)
    .RunDatabaseUpdateOnStart();

dbMigrations.PublishAsMigrationBundle(publishContainer: true)
    .PublishAsAzureContainerAppJob();

backend.WaitForCompletion(dbMigrations);

#pragma warning disable ASPIREBROWSERLOGS001
var frontendApp = builder.AddJavaScriptApp(Resources.Frontend, "../__PROJECT_NAME__.Web", "dev")
    .WithNpm(install: true)
    .WithHttpEndpoint(env: "PORT")
    .WithBrowserLogs()
    .WithExternalHttpEndpoints()
    .WithDependency(backend)
    .WithEnvironment("APP_BACKEND_HTTP", backend.GetEndpoint("http"))
    .WithEnvironment("APP_BACKEND_HTTPS", backend.GetEndpoint("https"));
#pragma warning restore ASPIREBROWSERLOGS001

builder.Build().Run();

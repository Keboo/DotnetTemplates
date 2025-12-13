using BlazorApp.AppHost;
using BlazorApp.Core;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer();
var db = sql.AddSqlDatabase();

//DBGate is a database viewer
var dbGate = builder.AddContainer("dbgate", "dbgate/dbgate")
    .WithExplicitStart()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("blazorapp-db-gate")
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

builder.AddProject<Projects.BlazorApp>("blazorapp")
    .WaitFor(db)
    .WithReference(db, ConnectionStrings.DatabaseKey);

builder.Build().Run();

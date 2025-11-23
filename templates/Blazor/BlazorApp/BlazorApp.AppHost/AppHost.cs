var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BlazorApp>("blazorapp");

builder.Build().Run();

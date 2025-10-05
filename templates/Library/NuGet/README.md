# NuGet package template
This template creates a solution for producing a NuGet package, along with unit tests.


## Template
Create a new app in your current directory by running.

```cli
> dotnet new keboo.nuget
```

### Parameters
[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--pipeline` | CI/CD provider to use. Options: `github`, `azuredevops` | `github` |

**Example with Azure DevOps:**
```cli
> dotnet new keboo.nuget --pipeline azuredevops
```

## Updating .NET Version

This template uses a `global.json` file to specify the required .NET SDK version. To update the .NET SDK version:

1. Update the `global.json` file in the solution root
2. Update the `<TargetFramework>` in the `csproj` files.

## Key Features

### Build Customization
[Docs](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory?view=vs-2022&WT.mc_id=DT-MVP-5003472)

### Centralized Package Management
[Docs](https://learn.microsoft.com/nuget/consume-packages/Central-Package-Management?WT.mc_id=DT-MVP-5003472)

### NuGet package source mapping
[Docs](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping?WT.mc_id=DT-MVP-5003472)

### GitHub Actions / Azure DevOps Pipeline
Build, test, pack, and deploy to NuGet.org included. Use `--pipeline` parameter to choose between GitHub Actions (default) or Azure DevOps Pipelines.
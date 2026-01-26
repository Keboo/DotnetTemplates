# Aspire React App template
This template creates a [React Web App](https://react.dev/) solution with .NET Aspire orchestration, Identity authentication, and unit tests.


## Template
Create a new app in your current directory by running.

```cli
> dotnet new keboo.blazor
```

### Parameters
[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--pipeline` | CI/CD provider to use. Options: `github`, `azuredevops`, `none` | `github` |
| `--sln` | Use legacy .sln format instead of .slnx format | `false` |
| `--tests` | Testing framework to use. Options: `xunit`, `mstest`, `tunit`, `none` | `xunit` |

**Example with Azure DevOps:**
```cli
> dotnet new keboo.blazor --pipeline azuredevops
```

**Example with no CI/CD pipeline:**
```cli
> dotnet new keboo.blazor --pipeline none
```

**Example with legacy .sln format:**
```cli
> dotnet new keboo.blazor --sln true
```

**Example with MSTest:**
```cli
> dotnet new keboo.blazor --tests mstest
```

**Example with no tests:**
```cli
> dotnet new keboo.blazor --tests none
```

## Updating .NET Version

This template uses a `global.json` file to specify the required .NET SDK version. To update the .NET SDK version:

1. Update the `global.json` file in the solution root
2. Update the `<TargetFramework>` in the `csproj` files.

## Key Features

### Progressive Web App (PWA) Support
Both the ReactApp.Web includes full PWA support with:
- Service worker for offline functionality
- Web app manifest for install-to-homescreen capability
- Caching strategies for improved performance
- App icons (192x192 and 512x512)

**React/Vite PWA:**
The React frontend uses `vite-plugin-pwa` with Workbox for advanced caching strategies. 

Features include:
- Automatic service worker registration and updates
- Static asset precaching with Workbox
- Runtime caching for images and Google Fonts
- App Shell pattern for SPA navigation
- Customizable manifest configuration in `vite.config.ts`

Note: Service workers only work in production builds and over HTTPS (or localhost).

### Build Customization
[Docs](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory?view=vs-2022&WT.mc_id=DT-MVP-5003472)

### Centralized Package Management
[Docs](https://learn.microsoft.com/nuget/consume-packages/Central-Package-Management?WT.mc_id=DT-MVP-5003472)

### NuGet package source mapping
[Docs](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping?WT.mc_id=DT-MVP-5003472)

### GitHub Actions / Azure DevOps Pipeline
Build, test, and code coverage reporting included. Use `--pipeline` parameter to choose between GitHub Actions (default) or Azure DevOps Pipelines.

### Solution File Format (slnx)
By default, this template uses the new `.slnx` (XML-based solution) format introduced in .NET 9. This modern format is more maintainable and easier to version control compared to the legacy `.sln` format.

[Blog: Introducing slnx support in the dotnet CLI](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/?WT.mc_id=DT-MVP-5003472)  
[Docs: dotnet sln command](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln?WT.mc_id=DT-MVP-5003472)

If you need to use the legacy `.sln` format, use the `--sln true` parameter when creating the template.

## Deployment
Deployment is handled with the [Azure Development CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/?WT.mc_id=DT-MVP-5003472).

This can be [installed](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd?tabs=winget-windows%2Cbrew-mac%2Cscript-linux&pivots=os-windows&WT.mc_id=DT-MVP-5003472) with `winget install microsoft.azd` 

If you don't already have it installed, you will also need to install bicep as this is what 

You will first need to login using `azd auth login` to authenticate with the Azure account that will be used for deployment.

On your first time, you will need to run `azd init` and scan the current directory. It will prompt you to provide a unique name for the app. This information will be stored in a `.azure` directory. It will also generate an `azure.yaml` file as well as a `next-steps.md` file outlining how to continue with publishing.





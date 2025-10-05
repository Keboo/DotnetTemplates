# WPF app template
This template creates a full WPF application, along with unit tests.

## Template
Create a new app in your current directory by running.

```cli
> dotnet new keboo.wpf
```

### Parameters
[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--pipeline` | CI/CD provider to use. Options: `github`, `azuredevops` | `github` |
| `--sln` | Use legacy .sln format instead of .slnx format | `false` |

**Example with Azure DevOps:**
```cli
> dotnet new keboo.wpf --pipeline azuredevops
```

**Example with legacy .sln format:**
```cli
> dotnet new keboo.wpf --sln true
```

## Updating .NET Version

This template uses a `global.json` file to specify the required .NET SDK version. To update the .NET SDK version:

1. Update the `global.json` file in the solution root
2. Update the `<TargetFramework>` in the `csproj` files.

## Key Features

### Generic Host Dependency Injection
[Docs](https://learn.microsoft.com/dotnet/core/extensions/generic-host?tabs=appbuilder&WT.mc_id=DT-MVP-5003472)

### Centralized Package Management
[Docs](https://learn.microsoft.com/nuget/consume-packages/Central-Package-Management?WT.mc_id=DT-MVP-5003472)

### Build Customization
[Docs](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory?view=vs-2022&WT.mc_id=DT-MVP-5003472)

### CommunityToolkit MVVM
[Docs](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/?WT.mc_id=DT-MVP-5003472)

### Material Design in XAML
[Repo](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)

### .editorconfig formatting
[Docs](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/code-style-rule-options?WT.mc_id=DT-MVP-5003472)

### Testing with Moq.AutoMocker
[Repo](https://github.com/moq/Moq.AutoMocker)

### NuGet package source mapping
[Docs](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping?WT.mc_id=DT-MVP-5003472)

### Dependabot auto updating of dependencies
[Docs](https://docs.github.com/code-security/dependabot/dependabot-version-updates)
Auto merging of these PRs done with [fastify/github-action-merge-dependabot](https://github.com/fastify/github-action-merge-dependabot).

### GitHub Actions workflow with code coverage reporting
[Docs](https://docs.github.com/actions).
Code coverage provided by [coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet).
Code coverage report provided by [danielpalme/ReportGenerator-GitHub-Action](https://github.com/danielpalme/ReportGenerator-GitHub-Action).
The coverage reports are posted as "stciky" PR comments provided by [marocchino/sticky-pull-request-comment](https://github.com/marocchino/sticky-pull-request-comment)

### Azure DevOps Pipeline support
Alternative to GitHub Actions. Set `--pipeline azuredevops` when creating the template.
Code coverage provided by [coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet).
Uses built-in Azure DevOps code coverage reporting.

### Solution File Format (slnx)
By default, this template uses the new `.slnx` (XML-based solution) format introduced in .NET 9. This modern format is more maintainable and easier to version control compared to the legacy `.sln` format.

[Blog: Introducing slnx support in the dotnet CLI](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/?WT.mc_id=DT-MVP-5003472)  
[Docs: dotnet sln command](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln?WT.mc_id=DT-MVP-5003472)

If you need to use the legacy `.sln` format, use the `--sln true` parameter when creating the template.
# WPF app template
This template creates a full WPF application, along with unit tests.

## Template
Create a new app in your current directory by running.

```cli
> dotnet new keboo.wpf
```

### Parameters
[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

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
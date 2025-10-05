# Dotnet Templates

The repository contains a set of opinionated [dotnet new templates](https://learn.microsoft.com/dotnet/core/tools/custom-templates). I am happy to receive critique/feedback on the existing templates, so feel free to open issues.

## Installing
Use [dotnet new install](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-install) to install the templates.

```cli
dotnet new install Keboo.Dotnet.Templates
```

## Updating
If you have previously installed the templates and want to install the latest version, you can use [dotnet new update](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-update) to update your installed templates.
```cli
dotnet new update
```

# Uninstalling
```cli
dotnet new uninstall Keboo.Dotnet.Templates
```

## Included Templates 
- [Avalonia Solution](./templates/Avalonia/AvaloniaSolution/README.md)
- [WPF Solution](./templates/WPF/WpfApp/README.md)
- [NuGet Package Solution](./templates/Library/NuGet/README.md)
- [System.CommandLine Solution](./templates/Console/ConsoleApp/README.md)

Many templates support both GitHub Actions and Azure DevOps Pipelines. Use the `--pipeline` parameter to choose:
- `--pipeline github` (default) - Includes `.github` folder with GitHub Actions workflows
- `--pipeline azuredevops` - Includes `.devops` folder with Azure DevOps pipelines

## Updating .NET Version

The repository and each template use a `global.json` file to specify the required .NET SDK version. This ensures consistency across development environments and CI/CD pipelines.

To update the .NET SDK version:

1. Update the `global.json` file in the root directory or template directory
2. Update the corresponding GitHub Actions workflow file to match (if applicable)
Example `global.json`:
```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestFeature"
  }
}
```

The `rollForward` policy allows using newer feature versions (e.g., 8.0.200) while ensuring the minimum version requirement is met.

# Local testing 
Build the template package:
```cli
dotnet pack --configuration Release -o .
```

Install the locally built template package
```cli
dotnet new install . --force
```

You can now test the template by running:
```cli
dotnet new keboo.wpf
dotnet build
dotent test --no-build
dotnet publish --no-build
```

When done, you can remove the local install of the template package by running:
```cli
dotnet new uninstall .
```

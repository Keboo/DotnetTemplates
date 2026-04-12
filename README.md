# Dotnet Templates

The repository contains a set of opinionated [dotnet new templates](https://learn.microsoft.com/dotnet/core/tools/custom-templates). I am happy to receive critique and feedback on the existing templates, so feel free to open issues.

## Installing

Use [dotnet new install](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-install) to install the templates.

```cli
dotnet new install Keboo.Dotnet.Templates
```

## Updating

If you have previously installed the templates and want the latest version, you can use [dotnet new update](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-update).

```cli
dotnet new update
```

## Uninstalling

```cli
dotnet new uninstall Keboo.Dotnet.Templates
```

## Included templates

- [Avalonia Solution](./templates/Avalonia/AvaloniaSolution/README.md)
- [WPF Solution](./templates/WPF/WpfApp/README.md)
- [NuGet Package Solution](./templates/Library/NuGet/README.md)
- [System.CommandLine Solution](./templates/Console/ConsoleApp/README.md) - supports `--mcp` to scaffold a NuGet-hosted MCP server variant.

## Local testing

Build the template package:

```cli
dotnet pack --configuration Release -o .
```

Install the locally built template package:

```cli
dotnet new install . --force
```

You can now test a template by running:

```cli
dotnet new keboo.wpf
dotnet build
dotnet test --no-build
dotnet publish --no-build
```

You can also exercise the new console MCP option with:

```cli
dotnet new keboo.console --mcp
dotnet test
dotnet pack -c Release
```

When done, you can remove the local install of the template package:

```cli
dotnet new uninstall .
```

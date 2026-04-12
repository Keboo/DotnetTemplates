# Command line app template

This template creates a [System.CommandLine](https://github.com/dotnet/command-line-api) solution with unit tests and CI/CD scaffolding.

When you pass `--mcp`, the template instead scaffolds a stdio [Model Context Protocol](https://modelcontextprotocol.io/introduction) server that is ready to pack and publish as a NuGet-hosted MCP tool.

## Template

Create a new app in your current directory by running:

```cli
dotnet new keboo.console
```

### Parameters

[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--mcp` | Configure the app as a NuGet-hosted MCP server instead of a System.CommandLine CLI | `false` |
| `--pipeline` | CI/CD provider to use. Options: `github`, `azuredevops`, `none` | `github` |
| `--sln` | Use legacy `.sln` format instead of `.slnx` format | `false` |
| `--tests` | Testing framework to use. Options: `xunit`, `mstest`, `tunit`, `none` | `tunit` |

### Examples

**Default CLI app**

```cli
dotnet new keboo.console
```

**Azure DevOps pipeline**

```cli
dotnet new keboo.console --pipeline azuredevops
```

**No CI/CD pipeline**

```cli
dotnet new keboo.console --pipeline none
```

**Legacy solution format**

```cli
dotnet new keboo.console --sln true
```

**MSTest**

```cli
dotnet new keboo.console --tests mstest
```

**No tests**

```cli
dotnet new keboo.console --tests none
```

**NuGet-hosted MCP server**

```cli
dotnet new keboo.console --mcp
```

**NuGet-hosted MCP server with xUnit**

```cli
dotnet new keboo.console --mcp --tests xunit
```

## MCP option

When `--mcp` is enabled the template:

1. Replaces the `System.CommandLine` sample with a stdio MCP server built on the [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).
2. Configures the project as a .NET tool package with `PackAsTool`, `PackageType` `McpServer`, self-contained publish defaults, and common runtime identifiers.
3. Includes a `.mcp/server.json` file so NuGet.org and MCP clients can discover the package metadata.
4. Regenerates the packaged `.mcp/server.json` during `dotnet pack` so its version entries stay aligned with the project/package version.

Before publishing, review and customize:

1. The package metadata generated from your project name, especially `PackageId` and the tool command name.
2. The placeholder repository values in `.mcp/server.json`.
3. The sample `MathTools` class and its tool descriptions, arguments, and any environment variables you want to expose.

For background and publishing guidance, see:

1. [MCP servers in NuGet packages](https://learn.microsoft.com/nuget/concepts/nuget-mcp)
2. [Create a minimal MCP server using C# and publish to NuGet](https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)
3. [Generic `server.json` schema reference](https://github.com/modelcontextprotocol/registry/blob/main/docs/reference/server-json/generic-server-json.md)

## Updating .NET version

This template uses a `global.json` file to specify the required .NET SDK version. To update the .NET SDK version:

1. Update the `global.json` file in the solution root.
2. Update the `<TargetFramework>` in the `csproj` files.

## Key features

### Build customization

[Docs](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory?view=vs-2022&WT.mc_id=DT-MVP-5003472)

### Centralized package management

[Docs](https://learn.microsoft.com/nuget/consume-packages/Central-Package-Management?WT.mc_id=DT-MVP-5003472)

### NuGet package source mapping

[Docs](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping?WT.mc_id=DT-MVP-5003472)

### GitHub Actions / Azure DevOps pipeline

Build, test, and code coverage reporting are included. Use `--pipeline` to choose between GitHub Actions (default), Azure DevOps Pipelines, or no CI/CD files.

### Solution file format (`.slnx`)

By default, this template uses the new `.slnx` (XML-based solution) format introduced in .NET 9. This modern format is more maintainable and easier to version control than the legacy `.sln` format.

1. [Blog: Introducing slnx support in the dotnet CLI](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/?WT.mc_id=DT-MVP-5003472)
2. [Docs: `dotnet sln` command](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln?WT.mc_id=DT-MVP-5003472)

If you need the legacy `.sln` format, use `--sln true` when creating the template.

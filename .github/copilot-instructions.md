# Keboo's .NET Templates - AI Coding Agent Instructions

This repository provides opinionated .NET project templates packaged as a NuGet Template Package. Templates are designed to create complete, production-ready project solutions with modern tooling and CI/CD.

## Template System Architecture

**Core Components:**
- `Templates.csproj` - Template package definition with `PackageType=Template`
- `templates/` - Contains 4 template categories: Avalonia, Console, Library, WPF
- Each template has `.template.config/template.json` defining parameters, symbols, and conditional file inclusion

**Template Parameters Pattern:**
All templates share common parameters:
- `no-sln` - Exclude solution files
- `tests` - Choose testing framework: "xunit" (default), "mstest", "tunit", or "none"
- `sln` - Use legacy .sln instead of modern .slnx format
- `pipeline` - Choice between "github" or "azuredevops" CI/CD
- Auto-generated symbols: `user_secrets_id` (GUID), `createdDate` (year)

## Development Workflows

**Local Testing:**
```powershell
# Use TestLocal.ps1 for iterative development
dotnet pack -o .                    # Build template package
dotnet new install . --force        # Install locally
dotnet new keboo.console            # Test template
dotnet new uninstall .              # Clean up
```

**Template Creation Commands:**
- `keboo.wpf` - WPF application with MVVM
- `keboo.console` - Console app with System.CommandLine
- `keboo.avalonia` - Cross-platform Avalonia UI
- `keboo.nuget` - NuGet library package

## Project Structure Conventions

**Shared Build Configuration:**
- `Directory.Build.props` - Shared MSBuild properties (ImplicitUsings, Nullable, LangVersion 12)
- `Directory.Build.targets` - Shared MSBuild targets
- `Directory.Packages.props` - Central Package Management (CPM)
- `global.json` - .NET SDK version pinning

**Template File Organization:**
- Root: Solution files (.sln/.slnx), global configs, README
- Source projects in subdirectories matching template name
- Test projects with `.Tests` suffix
- CI/CD configs: `.github/workflows/` or `.devops/`

## Conditional Content System

Templates use MSBuild-style conditions in `template.json`:
```json
"condition": "(noTests)",
"exclude": ["ProjectName.Tests/**"]
```

**Test Framework Selection:**
- `useXunit` computed symbol includes xUnit v3 tests
- `useMstest` computed symbol includes MSTest tests
- `useTunit` computed symbol includes TUnit tests
- `noTests` computed symbol excludes all test projects
- Each framework has separate test files with `.xunit.cs`, `.mstest.cs`, or `.tunit.cs` extensions that get renamed during template instantiation

**CI/CD Pipeline Selection:**
- `useGitHub` computed symbol excludes `.devops/**`
- `useAzureDevOps` computed symbol excludes `.github/**`
- Default GitHub Actions includes code coverage reporting

## Key Files to Understand

**Template Definition:** `templates/{category}/{name}/.template.config/template.json`
**Build Configuration:** `Directory.Build.props` - Modern C# features enabled
**Package Metadata:** `Templates.csproj` - NuGet package settings
**Test Workflow:** `TestLocal.ps1` - Local development loop

## Template Modification Guidelines

When adding/modifying templates:
1. Update `template.json` with proper symbols and conditions
2. Use `sourceName` replacement for project names
3. Include both .sln and .slnx files with conditional exclusion
4. Add CI/CD configs for both GitHub and Azure DevOps
5. Follow the shared build configuration pattern
6. Test with `TestLocal.ps1` before committing

Templates are solution-level (`type: "solution"`) and prefer name directories for clean organization.
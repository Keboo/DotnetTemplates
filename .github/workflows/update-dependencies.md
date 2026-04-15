---
name: Update Dependencies
description: Weekly automated dependency updates for NuGet packages, GitHub Actions, and npm packages
on:
  schedule: weekly
  workflow_dispatch:
timeout-minutes: 45
permissions:
  contents: read
  actions: read
  issues: read
  pull-requests: read
engine:
  id: copilot
imports:
  - copilot-setup-steps.yml
steps:
  - name: Setup .NET SDK
    uses: actions/setup-dotnet@v5
    with:
      dotnet-version: "10.x"
      dotnet-quality: "ga"
  - name: Setup Node.js
    uses: actions/setup-node@v4
    with:
      node-version: "22"
tools:
  edit:
  bash: true
  web-fetch:
  github:
    toolsets: [default, actions]
network:
  allowed:
    - defaults
    - dotnet
    - node
    - github
    - terraform
safe-outputs:
  create-pull-request:
    title-prefix: "[bot] "
    labels: [dependencies, automated]
    draft: false
    preserve-branch-name: true
    if-no-changes: "ignore"
---

# Update All Dependencies

Update all dependencies across this repository to their latest stable versions. Use the branch name `bot/automated-updates` for the pull request.

## Instructions

Follow the update-dependencies prompt file at `.github/prompts/update-dependencies.prompt.md` for detailed instructions on what to update and how. Below is a summary of the key areas:

### 1. NuGet Packages (Central Package Management)

Use NuGet.org package metadata, the NuGet API, or repository-local tooling such as `dotnet-outdated` to check for the latest stable versions of all packages. Update the `Version` attribute on every `<PackageVersion>` element in these `Directory.Packages.props` files:

- `templates/Aspire/ReactApp/Directory.Packages.props`
- `templates/Avalonia/AvaloniaSolution/Directory.Packages.props`
- `templates/Console/ConsoleApp/Directory.Packages.props`
- `templates/Library/NuGet/Directory.Packages.props`
- `templates/WPF/WpfApp/Directory.Packages.props`

**Important version grouping rules:**
- Aspire packages (`Aspire.Hosting.*`) should all use the same version.
- Avalonia packages should all use the same version.
- Test framework packages should be consistent across templates.
- `Microsoft.Extensions.*` packages within each template should use matching versions.
- If a project already uses a prerelease version, update to the latest prerelease instead of stable.

### 2. .NET Local Tools

Search the entire repository for `.config/dotnet-tools.json` files and update every tool to its latest stable version. Currently the known file is:

- `templates/Aspire/ReactApp/.config/dotnet-tools.json`

For each tool, use NuGet.org metadata or `dotnet tool search <tool-name>` to find the latest version, then update the `version` field.

**Important alignment rules:**
- `dotnet-ef` version should match `Microsoft.EntityFrameworkCore.*` NuGet package versions.
- `aspire.cli` version should match `Aspire.Hosting.*` NuGet package versions.
- If a new `.config/dotnet-tools.json` file has been added elsewhere in the repo, update it too.

### 3. npm Packages

Update npm dependencies in `templates/Aspire/ReactApp/ReactApp.Web/package.json`:

```bash
cd templates/Aspire/ReactApp/ReactApp.Web
npx npm-check-updates -u
npm install
```

**Version grouping rules:**
- `@mui/material` and `@mui/icons-material` should use the same version.
- `react` and `react-dom` should always match.
- `workbox-*` packages should all share the same version.

### 4. GitHub Actions

Update all GitHub Action references in workflow files to their latest versions. Check the latest release tags for each action.

**Workflow files to update:**
- `.github/workflows/build.yml`
- `templates/Aspire/ReactApp/.github/workflows/build-and-deploy.yml`
- `templates/Aspire/ReactApp/.github/workflows/deploy-infrastructure.yml`
- `templates/Console/ConsoleApp/.github/workflows/build-and-deploy.yml`
- `templates/Console/ConsoleApp/.github/workflows/pr-code-coverage-comment.yml`
- `templates/Library/NuGet/.github/workflows/build-and-deploy.yml`
- `templates/Library/NuGet/.github/workflows/pr-code-coverage-comment.yml`
- `templates/WPF/WpfApp/.github/workflows/build_app.yml`
- `templates/WPF/WpfApp/.github/workflows/code_coverage_comment.yml`

Use the major version tag format (e.g., `@v6`) when the action follows semver. Use exact version tags when the action does not publish major version tags. Ensure the same action uses the same version across all workflow files.

### 5. Terraform Providers (Aspire Template Only)

Update Terraform provider versions in `templates/Aspire/ReactApp/Infra/providers.tf` for `hashicorp/azuread`, `hashicorp/azurerm`, and `hashicorp/random`.

### 6. Verification

After all updates, verify the changes build:

```bash
# Build each template to verify NuGet packages resolve
for dir in templates/Console/ConsoleApp templates/Library/NuGet templates/Avalonia/AvaloniaSolution templates/WPF/WpfApp templates/Aspire/ReactApp; do
  echo "Building $dir..."
  cd "$dir" && dotnet build && cd -
done

# Verify npm packages
cd templates/Aspire/ReactApp/ReactApp.Web && npm ci && cd -
```

### 7. Create Pull Request

After making all changes, create a pull request with:
- **Branch name**: `bot/automated-updates`
- **Title**: `Update dependencies`
- **Body**: A summary of all dependency updates organized by category (NuGet, npm, GitHub Actions, Terraform). Include a table or list showing old version → new version for each updated dependency.

If no dependencies need updating, do nothing.

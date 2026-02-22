<#
.SYNOPSIS
    Interactive setup script that configures Azure AD App Registrations, 
    Terraform backend storage, and GitHub Actions OIDC authentication.

.DESCRIPTION
    This script combines the Setup-GitHubOIDC and Create-Infra workflows into
    a single interactive onboarding experience. It:
    1. Prompts for project configuration with sensible defaults
    2. Creates Azure AD App Registrations with federated credentials
    3. Creates a Service Principal for each app
    4. Assigns required Azure roles
    5. Optionally creates an "Infra" app with Microsoft Graph permissions
    6. Creates the Terraform backend storage account
    7. Generates Terraform files for service principal references
    8. Configures GitHub repository secrets

    The script is idempotent and can be safely run multiple times.

.PARAMETER NonInteractive
    If specified, skips interactive prompts and uses parameter values or defaults.

.EXAMPLE
    .\Setup.ps1

.EXAMPLE
    .\Setup.ps1 -NonInteractive -ProjectName "MyApp" -GitHubOwner "myorg" -GitHubRepo "myrepo"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectName,

    [Parameter(Mandatory = $false)]
    [string]$GitHubOwner,

    [Parameter(Mandatory = $false)]
    [string]$GitHubRepo,

    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,

    [Parameter(Mandatory = $false)]
    [string]$Environment = "production",

    [Parameter(Mandatory = $false)]
    [string]$Branch = "main",

    [Parameter(Mandatory = $false)]
    [string]$Location = "westus3",

    [Parameter(Mandatory = $false)]
    [string]$TerraformResourceGroupName,

    [Parameter(Mandatory = $false)]
    [string]$TerraformStorageAccountName,

    [Parameter(Mandatory = $false)]
    [string]$TerraformContainerName = "terraform",

    [Parameter(Mandatory = $false)]
    [switch]$CreateInfraApp = $true,

    [Parameter(Mandatory = $false)]
    [switch]$NonInteractive
)

$ErrorActionPreference = "Stop"

# ============================================================
# Helper Functions
# ============================================================

function Prompt-WithDefault {
    param(
        [string]$Message,
        [string]$Default
    )

    if ($Default) {
        $input = Read-Host "$Message [$Default]"
        if ([string]::IsNullOrWhiteSpace($input)) { return $Default }
        return $input
    }
    else {
        do {
            $input = Read-Host "$Message"
        } while ([string]::IsNullOrWhiteSpace($input))
        return $input
    }
}

function Prompt-YesNo {
    param(
        [string]$Message,
        [bool]$Default = $true
    )

    $suffix = if ($Default) { "[Y/n]" } else { "[y/N]" }
    $input = Read-Host "$Message $suffix"

    if ([string]::IsNullOrWhiteSpace($input)) { return $Default }
    return $input -match '^[Yy]'
}

function Get-GitHubRemoteInfo {
    try {
        $remoteUrl = git remote get-url origin 2>$null
        if ($remoteUrl) {
            # Handle SSH format: git@github.com:owner/repo.git
            if ($remoteUrl -match 'git@github\.com:([^/]+)/([^/.]+)(\.git)?$') {
                return @{ Owner = $Matches[1]; Repo = $Matches[2] }
            }
            # Handle HTTPS format: https://github.com/owner/repo.git
            if ($remoteUrl -match 'github\.com/([^/]+)/([^/.]+)(\.git)?$') {
                return @{ Owner = $Matches[1]; Repo = $Matches[2] }
            }
        }
    }
    catch { }
    return $null
}

function Create-AppRegistrationWithRoles {
    param(
        [string]$Name,
        [string]$SubscriptionId,
        [string]$GitHubOwner,
        [string]$GitHubRepo,
        [string]$Environment,
        [string]$Branch,
        [string[]]$Roles,
        [switch]$IncludeGraphPermissions
    )

    # Check if app registration already exists by name
    Write-Host "`n  Checking for existing App Registration '$Name'..." -ForegroundColor Yellow
    $existingApp = az ad app list --display-name $Name --query "[0]" 2>$null | ConvertFrom-Json

    if ($existingApp) {
        Write-Host "  Found existing App Registration '$Name' (Client ID: $($existingApp.appId))" -ForegroundColor Green
        $ClientId = $existingApp.appId
        $AppObjectId = $existingApp.id
    }
    else {
        Write-Host "  Creating new App Registration '$Name'..." -ForegroundColor Yellow
        $app = az ad app create --display-name $Name | ConvertFrom-Json
        $ClientId = $app.appId
        $AppObjectId = $app.id
        Write-Host "  Created App Registration (Client ID: $ClientId)" -ForegroundColor Green
    }

    # Check if Service Principal exists
    Write-Host "  Checking for existing Service Principal..." -ForegroundColor Yellow
    $existingSp = az ad sp list --filter "appId eq '$ClientId'" --query "[0]" 2>$null | ConvertFrom-Json

    if ($existingSp) {
        Write-Host "  Service Principal already exists" -ForegroundColor Green
        $SpObjectId = $existingSp.id
    }
    else {
        Write-Host "  Creating Service Principal..." -ForegroundColor Yellow
        $sp = az ad sp create --id $ClientId | ConvertFrom-Json
        $SpObjectId = $sp.id
        Write-Host "  Created Service Principal" -ForegroundColor Green
    }

    # Assign roles
    Write-Host "  Checking role assignments..." -ForegroundColor Yellow
    foreach ($role in $Roles) {
        $existingRole = az role assignment list --assignee $ClientId --role $role --scope "/subscriptions/$SubscriptionId" 2>$null | ConvertFrom-Json

        if ($existingRole -and $existingRole.Count -gt 0) {
            Write-Host "  $role role already assigned" -ForegroundColor Green
        }
        else {
            Write-Host "  Assigning $role role..." -ForegroundColor Yellow
            az role assignment create `
                --assignee $ClientId `
                --role $role `
                --scope "/subscriptions/$SubscriptionId" | Out-Null
            Write-Host "  Assigned $role role" -ForegroundColor Green
        }
    }

    # Configure Microsoft Graph permissions if requested
    if ($IncludeGraphPermissions) {
        Write-Host "  Configuring Microsoft Graph API permissions..." -ForegroundColor Yellow

        $graphSp = az ad sp list --filter "appId eq '00000003-0000-0000-c000-000000000000'" --query "[0]" | ConvertFrom-Json

        $directoryReadWriteAllPermission = $graphSp.appRoles | Where-Object { $_.value -eq "Directory.ReadWrite.All" } | Select-Object -First 1
        $roleManagementPermission = $graphSp.appRoles | Where-Object { $_.value -eq "RoleManagement.ReadWrite.Directory" } | Select-Object -First 1

        if ($directoryReadWriteAllPermission -and $roleManagementPermission) {
            $requiredResourceAccess = @{
                resourceAppId  = "00000003-0000-0000-c000-000000000000"
                resourceAccess = @(
                    @{ id = $directoryReadWriteAllPermission.id; type = "Role" },
                    @{ id = $roleManagementPermission.id; type = "Role" }
                )
            }

            $requiredResourceAccessJson = "[$($requiredResourceAccess | ConvertTo-Json -Depth 10 -Compress)]"
            $requiredResourceAccessJson | az ad app update --id $AppObjectId --required-resource-accesses "@-" | Out-Null
            Write-Host "  Added Microsoft Graph API permissions" -ForegroundColor Green
            Write-Host "  IMPORTANT: Admin consent required for these permissions." -ForegroundColor Magenta
            Write-Host "  Grant consent at: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/CallAnAPI/appId/$ClientId" -ForegroundColor Magenta
        }
    }

    # Configure federated credentials
    Write-Host "  Configuring federated credentials..." -ForegroundColor Yellow

    # Environment credential
    $envCredentialName = "github-actions-$Environment"
    $envSubject = "repo:${GitHubOwner}/${GitHubRepo}:environment:${Environment}"
    $existingEnvCred = az ad app federated-credential list --id $AppObjectId --query "[?name=='$envCredentialName']" 2>$null | ConvertFrom-Json

    if ($existingEnvCred -and $existingEnvCred.Count -gt 0) {
        Write-Host "  Federated credential for environment '$Environment' already exists" -ForegroundColor Green
    }
    else {
        $envCredential = @{
            name        = $envCredentialName
            issuer      = "https://token.actions.githubusercontent.com"
            subject     = $envSubject
            audiences   = @("api://AzureADTokenExchange")
            description = "GitHub Actions federated credential for $Environment environment"
        } | ConvertTo-Json -Compress

        $envCredential | az ad app federated-credential create --id $AppObjectId --parameters "@-" | Out-Null
        Write-Host "  Created federated credential for environment '$Environment'" -ForegroundColor Green
    }

    # Branch credential
    $branchCredentialName = "github-actions-branch-$Branch"
    $branchSubject = "repo:${GitHubOwner}/${GitHubRepo}:ref:refs/heads/${Branch}"
    $existingBranchCred = az ad app federated-credential list --id $AppObjectId --query "[?name=='$branchCredentialName']" 2>$null | ConvertFrom-Json

    if ($existingBranchCred -and $existingBranchCred.Count -gt 0) {
        Write-Host "  Federated credential for branch '$Branch' already exists" -ForegroundColor Green
    }
    else {
        $branchCredential = @{
            name        = $branchCredentialName
            issuer      = "https://token.actions.githubusercontent.com"
            subject     = $branchSubject
            audiences   = @("api://AzureADTokenExchange")
            description = "GitHub Actions federated credential for $Branch branch"
        } | ConvertTo-Json -Compress

        $branchCredential | az ad app federated-credential create --id $AppObjectId --parameters "@-" | Out-Null
        Write-Host "  Created federated credential for branch '$Branch'" -ForegroundColor Green
    }

    # Pull request credential
    $prCredentialName = "github-actions-pull-request"
    $prSubject = "repo:${GitHubOwner}/${GitHubRepo}:pull_request"
    $existingPrCred = az ad app federated-credential list --id $AppObjectId --query "[?name=='$prCredentialName']" 2>$null | ConvertFrom-Json

    if ($existingPrCred -and $existingPrCred.Count -gt 0) {
        Write-Host "  Federated credential for pull requests already exists" -ForegroundColor Green
    }
    else {
        $prCredential = @{
            name        = $prCredentialName
            issuer      = "https://token.actions.githubusercontent.com"
            subject     = $prSubject
            audiences   = @("api://AzureADTokenExchange")
            description = "GitHub Actions federated credential for pull requests"
        } | ConvertTo-Json -Compress

        $prCredential | az ad app federated-credential create --id $AppObjectId --parameters "@-" | Out-Null
        Write-Host "  Created federated credential for pull requests" -ForegroundColor Green
    }

    return @{
        ClientId    = $ClientId
        DisplayName = $Name
        AppObjectId = $AppObjectId
        SpObjectId  = $SpObjectId
    }
}

function Create-TerraformBackendStorage {
    param(
        [string]$ResourceGroupName,
        [string]$StorageAccountName,
        [string]$ContainerName,
        [string]$Location,
        [string]$AppRegistrationName
    )

    Write-Host "`n  Checking resource group '$ResourceGroupName'..." -ForegroundColor Yellow
    $rgExists = az group exists --name $ResourceGroupName

    if ($rgExists -eq "true") {
        Write-Host "  Resource group already exists" -ForegroundColor Green
    }
    else {
        Write-Host "  Creating resource group '$ResourceGroupName'..." -ForegroundColor Yellow
        az group create --name $ResourceGroupName --location $Location --output none
        if ($LASTEXITCODE -ne 0) { throw "Failed to create resource group." }
        Write-Host "  Resource group created" -ForegroundColor Green
    }

    # Create Storage Account
    Write-Host "  Checking storage account '$StorageAccountName'..." -ForegroundColor Yellow
    $ErrorActionPreference = "SilentlyContinue"
    $storageAccountCheck = az storage account show --name $StorageAccountName --resource-group $ResourceGroupName 2>$null
    $ErrorActionPreference = "Stop"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Storage account already exists" -ForegroundColor Green
    }
    else {
        Write-Host "  Creating storage account '$StorageAccountName' (this may take a few minutes)..." -ForegroundColor Yellow
        az storage account create `
            --name $StorageAccountName `
            --resource-group $ResourceGroupName `
            --location $Location `
            --sku Standard_LRS `
            --kind StorageV2 `
            --min-tls-version TLS1_2 `
            --allow-blob-public-access false `
            --https-only true `
            --output none

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create storage account. The name '$StorageAccountName' may not be globally unique. Try a different name."
        }
        Write-Host "  Storage account created" -ForegroundColor Green
    }

    # Create Blob Container
    Write-Host "  Checking blob container '$ContainerName'..." -ForegroundColor Yellow
    $ErrorActionPreference = "SilentlyContinue"
    az storage container show --name $ContainerName --account-name $StorageAccountName --auth-mode login --output none 2>$null
    $ErrorActionPreference = "Stop"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Blob container already exists" -ForegroundColor Green
    }
    else {
        Write-Host "  Creating blob container '$ContainerName'..." -ForegroundColor Yellow
        az storage container create --name $ContainerName --account-name $StorageAccountName --auth-mode login --output none
        if ($LASTEXITCODE -ne 0) { throw "Failed to create blob container." }
        Write-Host "  Blob container created" -ForegroundColor Green
    }

    # Get storage account ID for role assignments
    $storageAccountId = az storage account show --name $StorageAccountName --resource-group $ResourceGroupName --query id -o tsv

    # Assign roles to app registrations if specified
    if ($AppRegistrationName) {
        # Main app → Storage Blob Data Reader
        Write-Host "  Looking up App Registration '$AppRegistrationName'..." -ForegroundColor Yellow
        $appRegistration = az ad app list --display-name $AppRegistrationName 2>&1 | ConvertFrom-Json

        if ($appRegistration -and $appRegistration.Count -gt 0) {
            $appId = $appRegistration[0].appId
            $servicePrincipal = az ad sp list --filter "appId eq '$appId'" 2>&1 | ConvertFrom-Json

            if ($servicePrincipal -and $servicePrincipal.Count -gt 0) {
                $servicePrincipalId = $servicePrincipal[0].id
                Write-Host "  Assigning 'Storage Blob Data Reader' to '$AppRegistrationName'..." -ForegroundColor Yellow
                az role assignment create --role "Storage Blob Data Reader" --assignee $servicePrincipalId --scope $storageAccountId --output none 2>$null
                Write-Host "  Role assigned" -ForegroundColor Green
            }
            else {
                Write-Host "  Service Principal not found for '$AppRegistrationName'" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "  App Registration '$AppRegistrationName' not found" -ForegroundColor Yellow
        }

        # Infra app → Storage Blob Data Contributor
        $infraAppName = "${AppRegistrationName}Infra"
        Write-Host "  Looking up App Registration '$infraAppName'..." -ForegroundColor Yellow
        $infraAppRegistration = az ad app list --display-name $infraAppName 2>&1 | ConvertFrom-Json

        if ($infraAppRegistration -and $infraAppRegistration.Count -gt 0) {
            $infraAppId = $infraAppRegistration[0].appId
            $infraServicePrincipal = az ad sp list --filter "appId eq '$infraAppId'" 2>&1 | ConvertFrom-Json

            if ($infraServicePrincipal -and $infraServicePrincipal.Count -gt 0) {
                $infraServicePrincipalId = $infraServicePrincipal[0].id
                Write-Host "  Assigning 'Storage Blob Data Contributor' to '$infraAppName'..." -ForegroundColor Yellow
                az role assignment create --role "Storage Blob Data Contributor" --assignee $infraServicePrincipalId --scope $storageAccountId --output none 2>$null
                Write-Host "  Role assigned" -ForegroundColor Green
            }
            else {
                Write-Host "  Service Principal not found for '$infraAppName'" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "  App Registration '$infraAppName' not found" -ForegroundColor Yellow
        }
    }

    # Assign Storage Blob Data Contributor to current user
    Write-Host "  Assigning 'Storage Blob Data Contributor' to current user..." -ForegroundColor Yellow
    $currentUserId = az ad signed-in-user show --query id -o tsv 2>$null
    if ($currentUserId) {
        az role assignment create --role "Storage Blob Data Contributor" --assignee $currentUserId --scope $storageAccountId --output none 2>$null
        Write-Host "  Role assigned" -ForegroundColor Green
    }
    else {
        Write-Host "  Could not determine current user. You may need to manually assign the role." -ForegroundColor Yellow
    }
}

function Generate-ServicePrincipalsTerraform {
    param(
        [string]$OutputPath,
        [string]$AppDisplayName,
        [string]$InfraDisplayName
    )

    $tfContent = @"
# =============================================================================
# Service Principal References
# =============================================================================
# Auto-generated by Setup.ps1 - These reference the Azure AD App Registrations
# created during project setup. They are added as members of the admins group
# to allow CI/CD pipelines to manage infrastructure resources.
#
# App Registration:   $AppDisplayName
# Infra Registration: $InfraDisplayName
# =============================================================================

data "azuread_service_principal" "app_sp" {
  display_name = "$AppDisplayName"
}

data "azuread_service_principal" "infra_sp" {
  display_name = "$InfraDisplayName"
}

resource "azuread_group_member" "app_sp" {
  group_object_id  = azuread_group.admins_group.object_id
  member_object_id = data.azuread_service_principal.app_sp.object_id
}

resource "azuread_group_member" "infra_sp" {
  group_object_id  = azuread_group.admins_group.object_id
  member_object_id = data.azuread_service_principal.infra_sp.object_id
}
"@

    $directory = Split-Path -Parent $OutputPath
    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Set-Content -Path $OutputPath -Value $tfContent -Encoding UTF8
    Write-Host "  Generated $OutputPath" -ForegroundColor Green
}

# ============================================================
# Main Script
# ============================================================

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  Project Setup - Azure OIDC & Infrastructure" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  This script will:" -ForegroundColor White
Write-Host "    1. Create Azure AD App Registrations for CI/CD" -ForegroundColor White
Write-Host "    2. Configure GitHub Actions OIDC authentication" -ForegroundColor White
Write-Host "    3. Create Terraform backend storage" -ForegroundColor White
Write-Host "    4. Generate Terraform service principal references" -ForegroundColor White
Write-Host "    5. Set GitHub repository secrets" -ForegroundColor White
Write-Host ""

# ============================================================
# Step 1: Check Prerequisites
# ============================================================

Write-Host "Checking prerequisites..." -ForegroundColor Yellow

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI is not installed. Install from https://docs.microsoft.com/cli/azure/install-azure-cli"
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI is not installed. Install from https://cli.github.com/"
}

# Check Azure CLI login
$azAccount = az account show 2>$null | ConvertFrom-Json
if (-not $azAccount) {
    Write-Host "Not logged into Azure CLI. Please log in..." -ForegroundColor Yellow
    az login
    $azAccount = az account show | ConvertFrom-Json
}
Write-Host "  Azure: Logged in as $($azAccount.user.name)" -ForegroundColor Green

# Check GitHub CLI login
$ghAuth = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Not logged into GitHub CLI. Please log in..." -ForegroundColor Yellow
    gh auth login
}
Write-Host "  GitHub: Authenticated" -ForegroundColor Green

# ============================================================
# Step 2: Gather Configuration (Interactive)
# ============================================================

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Configuration" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host ""

# Detect defaults from git remote
$gitInfo = Get-GitHubRemoteInfo
$defaultOwner = if ($GitHubOwner) { $GitHubOwner } elseif ($gitInfo) { $gitInfo.Owner } else { "" }
$defaultRepo = if ($GitHubRepo) { $GitHubRepo } elseif ($gitInfo) { $gitInfo.Repo } else { "" }
$defaultProject = if ($ProjectName) { $ProjectName } elseif ($defaultRepo) { $defaultRepo } else { "" }

if ($NonInteractive) {
    if (-not $ProjectName) { throw "ProjectName is required in non-interactive mode" }
    if (-not $GitHubOwner) { $GitHubOwner = $defaultOwner }
    if (-not $GitHubRepo) { $GitHubRepo = $defaultRepo }
    if (-not $GitHubOwner -or -not $GitHubRepo) { throw "GitHubOwner and GitHubRepo are required in non-interactive mode" }
}
else {
    $ProjectName = Prompt-WithDefault -Message "Project name" -Default $defaultProject
    $GitHubOwner = Prompt-WithDefault -Message "GitHub owner (org or user)" -Default $defaultOwner
    $GitHubRepo = Prompt-WithDefault -Message "GitHub repository name" -Default $defaultRepo

    if (-not $SubscriptionId) {
        $useCurrentSub = Prompt-YesNo -Message "Use current Azure subscription '$($azAccount.name)' ($($azAccount.id))?" -Default $true
        if (-not $useCurrentSub) {
            $SubscriptionId = Prompt-WithDefault -Message "Azure Subscription ID" -Default ""
        }
    }

    $Environment = Prompt-WithDefault -Message "GitHub environment name" -Default $Environment
    $Branch = Prompt-WithDefault -Message "Default branch" -Default $Branch
    $Location = Prompt-WithDefault -Message "Azure region" -Default $Location
    $CreateInfraApp = Prompt-YesNo -Message "Create separate Infra app registration (with Graph permissions for AAD role management)?" -Default $true
}

# Set subscription
if ($SubscriptionId) {
    az account set --subscription $SubscriptionId
    $azAccount = az account show | ConvertFrom-Json
}

$SubscriptionId = $azAccount.id
$TenantId = $azAccount.tenantId

# Derive names from project name
$AppName = "${ProjectName}-GitHubActions"
$projectNameLower = $ProjectName.ToLower() -replace '[^a-z0-9]', ''

if (-not $TerraformResourceGroupName) {
    $TerraformResourceGroupName = "${projectNameLower}-terraform-rg"
}
if (-not $TerraformStorageAccountName) {
    $TerraformStorageAccountName = "${projectNameLower}infra"
}

if (-not $NonInteractive) {
    $TerraformResourceGroupName = Prompt-WithDefault -Message "Terraform resource group name" -Default $TerraformResourceGroupName
    $TerraformStorageAccountName = Prompt-WithDefault -Message "Terraform storage account name (must be globally unique)" -Default $TerraformStorageAccountName
}

# Display configuration summary
Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Configuration Summary" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Project Name:          $ProjectName" -ForegroundColor White
Write-Host "  GitHub:                $GitHubOwner/$GitHubRepo" -ForegroundColor White
Write-Host "  Azure Subscription:    $($azAccount.name) ($SubscriptionId)" -ForegroundColor White
Write-Host "  Tenant ID:             $TenantId" -ForegroundColor White
Write-Host "  App Registration:      $AppName" -ForegroundColor White
if ($CreateInfraApp) {
    Write-Host "  Infra App Registration: ${AppName}Infra" -ForegroundColor White
}
Write-Host "  Environment:           $Environment" -ForegroundColor White
Write-Host "  Branch:                $Branch" -ForegroundColor White
Write-Host "  Location:              $Location" -ForegroundColor White
Write-Host "  TF Resource Group:     $TerraformResourceGroupName" -ForegroundColor White
Write-Host "  TF Storage Account:    $TerraformStorageAccountName" -ForegroundColor White
Write-Host ""

if (-not $NonInteractive) {
    $proceed = Prompt-YesNo -Message "Proceed with this configuration?" -Default $true
    if (-not $proceed) {
        Write-Host "Setup cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# ============================================================
# Step 3: Create App Registrations
# ============================================================

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Step 1/5: Creating App Registrations" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan

$mainApp = Create-AppRegistrationWithRoles `
    -Name $AppName `
    -SubscriptionId $SubscriptionId `
    -GitHubOwner $GitHubOwner `
    -GitHubRepo $GitHubRepo `
    -Environment $Environment `
    -Branch $Branch `
    -Roles @("Contributor", "User Access Administrator")

$infraApp = $null
if ($CreateInfraApp) {
    Write-Host ""
    Write-Host "  Creating Infrastructure App Registration..." -ForegroundColor Cyan

    $infraAppName = "${AppName}Infra"
    $infraApp = Create-AppRegistrationWithRoles `
        -Name $infraAppName `
        -SubscriptionId $SubscriptionId `
        -GitHubOwner $GitHubOwner `
        -GitHubRepo $GitHubRepo `
        -Environment $Environment `
        -Branch $Branch `
        -Roles @("Contributor", "User Access Administrator") `
        -IncludeGraphPermissions
}

# ============================================================
# Step 4: Create Terraform Backend Storage
# ============================================================

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Step 2/5: Creating Terraform Backend Storage" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan

Create-TerraformBackendStorage `
    -ResourceGroupName $TerraformResourceGroupName `
    -StorageAccountName $TerraformStorageAccountName `
    -ContainerName $TerraformContainerName `
    -Location $Location `
    -AppRegistrationName $AppName

# ============================================================
# Step 5: Generate Terraform Files
# ============================================================

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Step 3/5: Generating Terraform Files" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan

$infraDir = Join-Path $PSScriptRoot "Infra"

# Generate service_principals.tf in prod/
$spTfPath = Join-Path $infraDir "prod" "service_principals.tf"
$infraDisplayName = if ($infraApp) { $infraApp.DisplayName } else { "${AppName}Infra" }

Generate-ServicePrincipalsTerraform `
    -OutputPath $spTfPath `
    -AppDisplayName $mainApp.DisplayName `
    -InfraDisplayName $infraDisplayName

# Generate azure.auto.tfvars
$tfvarsPath = Join-Path $infraDir "azure.auto.tfvars"
$tfvarsContent = @"
SUBSCRIPTION_ID = "$SubscriptionId"
"@
Set-Content -Path $tfvarsPath -Value $tfvarsContent -Encoding UTF8
Write-Host "  Generated $tfvarsPath" -ForegroundColor Green

# Update providers.tf backend config with actual storage account values
Write-Host "  Updating Infra/providers.tf backend configuration..." -ForegroundColor Yellow
$providersPath = Join-Path $infraDir "providers.tf"
if (Test-Path $providersPath) {
    $providersContent = Get-Content $providersPath -Raw

    # Update resource_group_name
    $providersContent = $providersContent -replace '(resource_group_name\s*=\s*)"[^"]*"', "`$1`"$TerraformResourceGroupName`""
    # Update storage_account_name
    $providersContent = $providersContent -replace '(storage_account_name\s*=\s*)"[^"]*"', "`$1`"$TerraformStorageAccountName`""

    Set-Content -Path $providersPath -Value $providersContent -Encoding UTF8 -NoNewline
    Write-Host "  Updated providers.tf backend configuration" -ForegroundColor Green
}

# ============================================================
# Step 6: Configure GitHub Secrets
# ============================================================

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Step 4/5: Configuring GitHub Secrets" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan

Write-Host "  Setting ARM_CLIENT_ID..." -ForegroundColor Yellow
gh secret set ARM_CLIENT_ID --repo "$GitHubOwner/$GitHubRepo" --body $mainApp.ClientId

Write-Host "  Setting ARM_TENANT_ID..." -ForegroundColor Yellow
gh secret set ARM_TENANT_ID --repo "$GitHubOwner/$GitHubRepo" --body $TenantId

Write-Host "  Setting ARM_SUBSCRIPTION_ID..." -ForegroundColor Yellow
gh secret set ARM_SUBSCRIPTION_ID --repo "$GitHubOwner/$GitHubRepo" --body $SubscriptionId

if ($infraApp) {
    Write-Host "  Setting ARM_CLIENT_ID_INFRA..." -ForegroundColor Yellow
    gh secret set ARM_CLIENT_ID_INFRA --repo "$GitHubOwner/$GitHubRepo" --body $infraApp.ClientId

    Write-Host "  Setting ARM_TENANT_ID_INFRA..." -ForegroundColor Yellow
    gh secret set ARM_TENANT_ID_INFRA --repo "$GitHubOwner/$GitHubRepo" --body $TenantId

    Write-Host "  Setting ARM_SUBSCRIPTION_ID_INFRA..." -ForegroundColor Yellow
    gh secret set ARM_SUBSCRIPTION_ID_INFRA --repo "$GitHubOwner/$GitHubRepo" --body $SubscriptionId
}

Write-Host "  GitHub secrets configured" -ForegroundColor Green

# ============================================================
# Step 7: Summary
# ============================================================

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  Step 5/5: Summary" -ForegroundColor Cyan
Write-Host "------------------------------------------------------" -ForegroundColor Cyan

Write-Host ""
Write-Host "======================================================" -ForegroundColor Green
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Green

Write-Host ""
Write-Host "  App Registrations:" -ForegroundColor Yellow
Write-Host "    Main:  $AppName (Client ID: $($mainApp.ClientId))" -ForegroundColor White
if ($infraApp) {
    Write-Host "    Infra: $($infraApp.DisplayName) (Client ID: $($infraApp.ClientId))" -ForegroundColor White
}

Write-Host ""
Write-Host "  Terraform Backend:" -ForegroundColor Yellow
Write-Host "    Resource Group:  $TerraformResourceGroupName" -ForegroundColor White
Write-Host "    Storage Account: $TerraformStorageAccountName" -ForegroundColor White
Write-Host "    Container:       $TerraformContainerName" -ForegroundColor White

Write-Host ""
Write-Host "  Generated Files:" -ForegroundColor Yellow
Write-Host "    $spTfPath" -ForegroundColor White
Write-Host "    $tfvarsPath" -ForegroundColor White

Write-Host ""
Write-Host "  GitHub Secrets:" -ForegroundColor Yellow
Write-Host "    ARM_CLIENT_ID, ARM_TENANT_ID, ARM_SUBSCRIPTION_ID" -ForegroundColor White
if ($infraApp) {
    Write-Host "    ARM_CLIENT_ID_INFRA, ARM_TENANT_ID_INFRA, ARM_SUBSCRIPTION_ID_INFRA" -ForegroundColor White
}

Write-Host ""
Write-Host "  Federated Credentials:" -ForegroundColor Yellow
Write-Host "    Environment: $Environment" -ForegroundColor White
Write-Host "    Branch:      $Branch" -ForegroundColor White
Write-Host "    Pull Requests" -ForegroundColor White

Write-Host ""
Write-Host "  Next Steps:" -ForegroundColor Yellow
Write-Host "    1. Ensure the GitHub environment '$Environment' exists:" -ForegroundColor White
Write-Host "       https://github.com/$GitHubOwner/$GitHubRepo/settings/environments" -ForegroundColor Cyan
if ($infraApp) {
    Write-Host "    2. Grant admin consent for the Infra app's Graph permissions:" -ForegroundColor White
    Write-Host "       https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/CallAnAPI/appId/$($infraApp.ClientId)" -ForegroundColor Cyan
    Write-Host "    3. Wait a few minutes for permissions to propagate, then run:" -ForegroundColor White
}
else {
    Write-Host "    2. Wait a few minutes for permissions to propagate, then run:" -ForegroundColor White
}
Write-Host "       cd Infra && terraform init && terraform plan" -ForegroundColor Cyan
Write-Host ""

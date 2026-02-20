<#
.SYNOPSIS
    Creates an Azure AD App Registration with federated credentials for GitHub Actions OIDC authentication
    and configures the required secrets in the GitHub repository.

.DESCRIPTION
    This script is idempotent and can be safely run multiple times. It:
    1. Checks Azure AD for existing app registrations by name
    2. Creates an Azure AD App Registration (or uses existing one)
    3. Creates a Service Principal for the app
    4. Assigns Contributor and User Access Administrator roles to the subscription
    5. Configures federated credentials for GitHub Actions
    6. Adds/updates the required secrets in the GitHub repository
    7. Optionally creates a second "Infra" app registration with Microsoft Graph permissions for AAD role management

    The script uses Azure AD as the source of truth. If an app registration with the specified name
    already exists, it will be reused and its configuration will be updated as needed.

.PARAMETER AppName
    The name of the Azure AD App Registration to create.

.PARAMETER GitHubOrg
    The GitHub organization or username.

.PARAMETER GitHubRepo
    The GitHub repository name.

.PARAMETER SubscriptionId
    The Azure subscription ID. If not provided, uses the current subscription.

.PARAMETER Environment
    The GitHub environment name for federated credentials. Default is 'production'.

.PARAMETER Branch
    The branch name for federated credentials. Default is 'main'.

.PARAMETER CreateInfraApp
    If specified, creates a second app registration with "Infra" appended to the name.
    This app will have additional Microsoft Graph permissions (Directory.Read.All and RoleManagement.ReadWrite.Directory)
    for managing AAD roles. Secrets will be created with _INFRA suffix.

.EXAMPLE
    .\Setup-GitHubOIDC.ps1 -AppName "ReactApp-GitHubActions" -GitHubOrg "myorg" -GitHubRepo "myrepo"

.EXAMPLE
    .\Setup-GitHubOIDC.ps1 -AppName "ReactApp-GitHubActions" -GitHubOrg "myorg" -GitHubRepo "myrepo" -CreateInfraApp

.EXAMPLE
    .\Setup-GitHubOIDC.ps1 -AppName "ReactApp-GitHubActions" -GitHubOrg "myorg" -GitHubRepo "myrepo" -SubscriptionId "12345678-1234-1234-1234-123456789012"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AppName,

    [Parameter(Mandatory = $true)]
    [string]$GitHubOrg,

    [Parameter(Mandatory = $true)]
    [string]$GitHubRepo,

    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,

    [Parameter(Mandatory = $false)]
    [string]$Environment = "production",

    [Parameter(Mandatory = $false)]
    [string]$Branch = "main",

    [Parameter(Mandatory = $false)]
    [switch]$CreateInfraApp = $true
)

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "GitHub Actions OIDC Setup Script" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

# Check prerequisites
Write-Host "`nChecking prerequisites..." -ForegroundColor Yellow

# Check Azure CLI
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli"
}

# Check GitHub CLI
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI is not installed. Please install it from https://cli.github.com/"
}

# Check Azure CLI login
$azAccount = az account show 2>$null | ConvertFrom-Json
if (-not $azAccount) {
    Write-Host "Not logged into Azure CLI. Please log in..." -ForegroundColor Yellow
    az login
    $azAccount = az account show | ConvertFrom-Json
}

# Check GitHub CLI login
$ghAuth = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Not logged into GitHub CLI. Please log in..." -ForegroundColor Yellow
    gh auth login
}

# Set subscription
if ($SubscriptionId) {
    Write-Host "`nSetting subscription to $SubscriptionId..." -ForegroundColor Yellow
    az account set --subscription $SubscriptionId
    $azAccount = az account show | ConvertFrom-Json
}

$SubscriptionId = $azAccount.id
$TenantId = $azAccount.tenantId

Write-Host "`nUsing Azure subscription: $($azAccount.name) ($SubscriptionId)" -ForegroundColor Green
Write-Host "Tenant ID: $TenantId" -ForegroundColor Green

function Create-AppRegistrationWithRoles {
    param(
        [string]$Name,
        [string]$SubscriptionId,
        [string]$GitHubOrg,
        [string]$GitHubRepo,
        [string]$Environment,
        [string]$Branch,
        [string[]]$Roles,
        [switch]$IncludeGraphPermissions
    )

    # Check if app registration already exists by name
    Write-Host "`nChecking for existing App Registration '$Name'..." -ForegroundColor Yellow
    $existingApp = az ad app list --display-name $Name --query "[0]" 2>$null | ConvertFrom-Json

    if ($existingApp) {
        Write-Host "Found existing App Registration '$Name' with Client ID: $($existingApp.appId)" -ForegroundColor Green
        $ClientId = $existingApp.appId
        $AppObjectId = $existingApp.id
    } else {
        # Create App Registration
        Write-Host "`nCreating new App Registration '$Name'..." -ForegroundColor Yellow
        $app = az ad app create --display-name $Name | ConvertFrom-Json
        $ClientId = $app.appId
        $AppObjectId = $app.id
        Write-Host "Created App Registration with Client ID: $ClientId" -ForegroundColor Green
    }

    # Check if Service Principal exists
    Write-Host "`nChecking for existing Service Principal..." -ForegroundColor Yellow
    $existingSp = az ad sp list --filter "appId eq '$ClientId'" --query "[0]" 2>$null | ConvertFrom-Json

    if ($existingSp) {
        Write-Host "Service Principal already exists" -ForegroundColor Yellow
        $SpObjectId = $existingSp.id
    } else {
        # Create Service Principal
        Write-Host "`nCreating Service Principal..." -ForegroundColor Yellow
        $sp = az ad sp create --id $ClientId | ConvertFrom-Json
        $SpObjectId = $sp.id
        Write-Host "Created Service Principal" -ForegroundColor Green
    }

    # Assign roles
    Write-Host "`nChecking role assignments..." -ForegroundColor Yellow
    foreach ($role in $Roles) {
        $existingRole = az role assignment list --assignee $ClientId --role $role --scope "/subscriptions/$SubscriptionId" 2>$null | ConvertFrom-Json

        if ($existingRole -and $existingRole.Count -gt 0) {
            Write-Host "$role role already assigned" -ForegroundColor Yellow
        } else {
            Write-Host "`nAssigning $role role to subscription..." -ForegroundColor Yellow
            az role assignment create `
                --assignee $ClientId `
                --role $role `
                --scope "/subscriptions/$SubscriptionId" | Out-Null
            Write-Host "Assigned $role role" -ForegroundColor Green
        }
    }

    # Configure Microsoft Graph permissions if requested
    if ($IncludeGraphPermissions) {
        Write-Host "`nConfiguring Microsoft Graph API permissions for AAD role management..." -ForegroundColor Yellow
        
        # Get Microsoft Graph Service Principal
        $graphSp = az ad sp list --filter "appId eq '00000003-0000-0000-c000-000000000000'" --query "[0]" | ConvertFrom-Json
        
        # Find the required permissions
        $directoryReadWriteAllPermission = $graphSp.appRoles | Where-Object { $_.value -eq "Directory.ReadWrite.All" } | Select-Object -First 1
        $roleManagementPermission = $graphSp.appRoles | Where-Object { $_.value -eq "RoleManagement.ReadWrite.Directory" } | Select-Object -First 1
        
        if ($directoryReadWriteAllPermission -and $roleManagementPermission) {
            # Add the required resource accesses
            $requiredResourceAccess = @{
                resourceAppId = "00000003-0000-0000-c000-000000000000"
                resourceAccess = @(
                    @{
                        id = $directoryReadWriteAllPermission.id
                        type = "Role"
                    },
                    @{
                        id = $roleManagementPermission.id
                        type = "Role"
                    }
                )
            }
            
            $requiredResourceAccessJson = "[$($requiredResourceAccess | ConvertTo-Json -Depth 10 -Compress)]"
            
            # Use the @- pattern to pass JSON via stdin to avoid PowerShell quoting issues
            $requiredResourceAccessJson | az ad app update --id $AppObjectId --required-resource-accesses "@-" | Out-Null
            Write-Host "Added Microsoft Graph API permissions" -ForegroundColor Green
            Write-Host "`nIMPORTANT: Admin consent is required for these permissions." -ForegroundColor Magenta
            Write-Host "Please grant admin consent at: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/CallAnAPI/appId/$ClientId" -ForegroundColor Magenta
        }
    }

    # Configure federated credentials
    Write-Host "`nConfiguring federated credentials..." -ForegroundColor Yellow

    # Federated credential for environment
    $envCredentialName = "github-actions-$Environment"
    $envSubject = "repo:${GitHubOrg}/${GitHubRepo}:environment:${Environment}"

    $existingEnvCred = az ad app federated-credential list --id $AppObjectId --query "[?name=='$envCredentialName']" 2>$null | ConvertFrom-Json

    if ($existingEnvCred -and $existingEnvCred.Count -gt 0) {
        Write-Host "Federated credential for environment '$Environment' already exists" -ForegroundColor Yellow
    } else {
        Write-Host "Creating federated credential for environment '$Environment'..." -ForegroundColor Yellow
        
        $envCredential = @{
            name = $envCredentialName
            issuer = "https://token.actions.githubusercontent.com"
            subject = $envSubject
            audiences = @("api://AzureADTokenExchange")
            description = "GitHub Actions federated credential for $Environment environment"
        } | ConvertTo-Json -Compress

        $envCredential | az ad app federated-credential create --id $AppObjectId --parameters "@-" | Out-Null
        Write-Host "Created federated credential for environment '$Environment'" -ForegroundColor Green
    }

    # Federated credential for branch
    $branchCredentialName = "github-actions-branch-$Branch"
    $branchSubject = "repo:${GitHubOrg}/${GitHubRepo}:ref:refs/heads/${Branch}"

    $existingBranchCred = az ad app federated-credential list --id $AppObjectId --query "[?name=='$branchCredentialName']" 2>$null | ConvertFrom-Json

    if ($existingBranchCred -and $existingBranchCred.Count -gt 0) {
        Write-Host "Federated credential for branch '$Branch' already exists" -ForegroundColor Yellow
    } else {
        Write-Host "Creating federated credential for branch '$Branch'..." -ForegroundColor Yellow
        
        $branchCredential = @{
            name = $branchCredentialName
            issuer = "https://token.actions.githubusercontent.com"
            subject = $branchSubject
            audiences = @("api://AzureADTokenExchange")
            description = "GitHub Actions federated credential for $Branch branch"
        } | ConvertTo-Json -Compress

        $branchCredential | az ad app federated-credential create --id $AppObjectId --parameters "@-" | Out-Null
        Write-Host "Created federated credential for branch '$Branch'" -ForegroundColor Green
    }

    # Federated credential for pull requests
    $prCredentialName = "github-actions-pull-request"
    $prSubject = "repo:${GitHubOrg}/${GitHubRepo}:pull_request"

    $existingPrCred = az ad app federated-credential list --id $AppObjectId --query "[?name=='$prCredentialName']" 2>$null | ConvertFrom-Json

    if ($existingPrCred -and $existingPrCred.Count -gt 0) {
        Write-Host "Federated credential for pull requests already exists" -ForegroundColor Yellow
    } else {
        Write-Host "Creating federated credential for pull requests..." -ForegroundColor Yellow
        
        $prCredential = @{
            name = $prCredentialName
            issuer = "https://token.actions.githubusercontent.com"
            subject = $prSubject
            audiences = @("api://AzureADTokenExchange")
            description = "GitHub Actions federated credential for pull requests"
        } | ConvertTo-Json -Compress

        $prCredential | az ad app federated-credential create --id $AppObjectId --parameters "@-" | Out-Null
        Write-Host "Created federated credential for pull requests" -ForegroundColor Green
    }

    return @{
        ClientId = $ClientId
        AppObjectId = $AppObjectId
        SpObjectId = $SpObjectId
    }
}

# Create main app registration
Write-Host "`n======================================" -ForegroundColor Cyan
Write-Host "Creating Main App Registration" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$mainApp = Create-AppRegistrationWithRoles `
    -Name $AppName `
    -SubscriptionId $SubscriptionId `
    -GitHubOrg $GitHubOrg `
    -GitHubRepo $GitHubRepo `
    -Environment $Environment `
    -Branch $Branch `
    -Roles @("Contributor", "User Access Administrator")

$ClientId = $mainApp.ClientId

# Create infrastructure app registration if requested
$infraApp = $null
if ($CreateInfraApp) {
    Write-Host "`n======================================" -ForegroundColor Cyan
    Write-Host "Creating Infrastructure App Registration" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan

    $infraAppName = "${AppName}Infra"
    $infraApp = Create-AppRegistrationWithRoles `
        -Name $infraAppName `
        -SubscriptionId $SubscriptionId `
        -GitHubOrg $GitHubOrg `
        -GitHubRepo $GitHubRepo `
        -Environment $Environment `
        -Branch $Branch `
        -Roles @("Contributor", "User Access Administrator") `
        -IncludeGraphPermissions
}

# Add secrets to GitHub repository
Write-Host "\n======================================" -ForegroundColor Cyan
Write-Host "Adding Secrets to GitHub Repository" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

Write-Host "\nAdding main app secrets to GitHub repository '$GitHubOrg/$GitHubRepo'..." -ForegroundColor Yellow

Write-Host "Setting ARM_CLIENT_ID..." -ForegroundColor Yellow
gh secret set ARM_CLIENT_ID --repo "$GitHubOrg/$GitHubRepo" --body $mainApp.ClientId

Write-Host "Setting ARM_TENANT_ID..." -ForegroundColor Yellow
gh secret set ARM_TENANT_ID --repo "$GitHubOrg/$GitHubRepo" --body $TenantId

Write-Host "Setting ARM_SUBSCRIPTION_ID..." -ForegroundColor Yellow
gh secret set ARM_SUBSCRIPTION_ID --repo "$GitHubOrg/$GitHubRepo" --body $SubscriptionId

if ($infraApp) {
    Write-Host "\nAdding infrastructure app secrets to GitHub repository '$GitHubOrg/$GitHubRepo'..." -ForegroundColor Yellow

    Write-Host "Setting ARM_CLIENT_ID_INFRA..." -ForegroundColor Yellow
    gh secret set ARM_CLIENT_ID_INFRA --repo "$GitHubOrg/$GitHubRepo" --body $infraApp.ClientId

    Write-Host "Setting ARM_TENANT_ID_INFRA..." -ForegroundColor Yellow
    gh secret set ARM_TENANT_ID_INFRA --repo "$GitHubOrg/$GitHubRepo" --body $TenantId

    Write-Host "Setting ARM_SUBSCRIPTION_ID_INFRA..." -ForegroundColor Yellow
    gh secret set ARM_SUBSCRIPTION_ID_INFRA --repo "$GitHubOrg/$GitHubRepo" --body $SubscriptionId
}

Write-Host "`n======================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan

Write-Host "`nMain App Registration Summary:" -ForegroundColor Yellow
Write-Host "  App Registration: $AppName" -ForegroundColor White
Write-Host "  Client ID: $($mainApp.ClientId)" -ForegroundColor White
Write-Host "  Tenant ID: $TenantId" -ForegroundColor White
Write-Host "  Subscription ID: $SubscriptionId" -ForegroundColor White
Write-Host "  Roles: Contributor, User Access Administrator" -ForegroundColor White

if ($infraApp) {
    Write-Host "`nInfrastructure App Registration Summary:" -ForegroundColor Yellow
    Write-Host "  App Registration: ${AppName}Infra" -ForegroundColor White
    Write-Host "  Client ID: $($infraApp.ClientId)" -ForegroundColor White
    Write-Host "  Tenant ID: $TenantId" -ForegroundColor White
    Write-Host "  Subscription ID: $SubscriptionId" -ForegroundColor White
    Write-Host "  Roles: Contributor, User Access Administrator" -ForegroundColor White
    Write-Host "  Graph Permissions: Directory.ReadWrite.All, RoleManagement.ReadWrite.Directory" -ForegroundColor White
}

Write-Host "`nFederated Credentials configured for:" -ForegroundColor Yellow
Write-Host "  - Environment: $Environment" -ForegroundColor White
Write-Host "  - Branch: $Branch" -ForegroundColor White
Write-Host "  - Pull Requests" -ForegroundColor White

Write-Host "`nGitHub Secrets added:" -ForegroundColor Yellow
Write-Host "  - ARM_CLIENT_ID" -ForegroundColor White
Write-Host "  - ARM_TENANT_ID" -ForegroundColor White
Write-Host "  - ARM_SUBSCRIPTION_ID" -ForegroundColor White

if ($infraApp) {
    Write-Host "  - ARM_CLIENT_ID_INFRA" -ForegroundColor White
    Write-Host "  - ARM_TENANT_ID_INFRA" -ForegroundColor White
    Write-Host "  - ARM_SUBSCRIPTION_ID_INFRA" -ForegroundColor White
}

Write-Host "`nNote: Make sure the GitHub environment '$Environment' exists in your repository settings." -ForegroundColor Magenta
Write-Host "You can create it at: https://github.com/$GitHubOrg/$GitHubRepo/settings/environments" -ForegroundColor Magenta

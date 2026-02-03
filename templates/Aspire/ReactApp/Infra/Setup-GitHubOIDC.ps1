<#
.SYNOPSIS
    Creates an Azure AD App Registration with federated credentials for GitHub Actions OIDC authentication
    and configures the required secrets in the GitHub repository.

.DESCRIPTION
    This script:
    1. Creates an Azure AD App Registration (or uses existing one)
    2. Creates a Service Principal for the app
    3. Assigns Contributor role to the subscription
    4. Configures federated credentials for GitHub Actions
    5. Adds the required secrets to the GitHub repository

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

.EXAMPLE
    .\Setup-GitHubOIDC.ps1 -AppName "ReactApp-GitHubActions" -GitHubOrg "myorg" -GitHubRepo "myrepo"

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
    [string]$Branch = "main"
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

# Check if app registration already exists
Write-Host "`nChecking for existing App Registration '$AppName'..." -ForegroundColor Yellow
$existingApp = az ad app list --display-name $AppName --query "[0]" 2>$null | ConvertFrom-Json

if ($existingApp) {
    Write-Host "App Registration '$AppName' already exists with Client ID: $($existingApp.appId)" -ForegroundColor Yellow
    $ClientId = $existingApp.appId
    $AppObjectId = $existingApp.id
} else {
    # Create App Registration
    Write-Host "`nCreating App Registration '$AppName'..." -ForegroundColor Yellow
    $app = az ad app create --display-name $AppName | ConvertFrom-Json
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

# Check if role assignment exists
Write-Host "`nChecking role assignments..." -ForegroundColor Yellow
$existingRole = az role assignment list --assignee $ClientId --role "Contributor" --scope "/subscriptions/$SubscriptionId" 2>$null | ConvertFrom-Json

if ($existingRole -and $existingRole.Count -gt 0) {
    Write-Host "Contributor role already assigned" -ForegroundColor Yellow
} else {
    # Assign Contributor role
    Write-Host "`nAssigning Contributor role to subscription..." -ForegroundColor Yellow
    az role assignment create `
        --assignee $ClientId `
        --role "Contributor" `
        --scope "/subscriptions/$SubscriptionId" | Out-Null
    Write-Host "Assigned Contributor role" -ForegroundColor Green
}

# Check if User Access Administrator role exists (needed for role assignments in Terraform)
$existingUaaRole = az role assignment list --assignee $ClientId --role "User Access Administrator" --scope "/subscriptions/$SubscriptionId" 2>$null | ConvertFrom-Json

if ($existingUaaRole -and $existingUaaRole.Count -gt 0) {
    Write-Host "User Access Administrator role already assigned" -ForegroundColor Yellow
} else {
    Write-Host "`nAssigning User Access Administrator role to subscription..." -ForegroundColor Yellow
    az role assignment create `
        --assignee $ClientId `
        --role "User Access Administrator" `
        --scope "/subscriptions/$SubscriptionId" | Out-Null
    Write-Host "Assigned User Access Administrator role" -ForegroundColor Green
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

# Add secrets to GitHub repository
Write-Host "`nAdding secrets to GitHub repository '$GitHubOrg/$GitHubRepo'..." -ForegroundColor Yellow

Write-Host "Setting ARM_CLIENT_ID..." -ForegroundColor Yellow
gh secret set ARM_CLIENT_ID --repo "$GitHubOrg/$GitHubRepo" --body $ClientId

Write-Host "Setting ARM_TENANT_ID..." -ForegroundColor Yellow
gh secret set ARM_TENANT_ID --repo "$GitHubOrg/$GitHubRepo" --body $TenantId

Write-Host "Setting ARM_SUBSCRIPTION_ID..." -ForegroundColor Yellow
gh secret set ARM_SUBSCRIPTION_ID --repo "$GitHubOrg/$GitHubRepo" --body $SubscriptionId

Write-Host "`n======================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan

Write-Host "`nSummary:" -ForegroundColor Yellow
Write-Host "  App Registration: $AppName" -ForegroundColor White
Write-Host "  Client ID: $ClientId" -ForegroundColor White
Write-Host "  Tenant ID: $TenantId" -ForegroundColor White
Write-Host "  Subscription ID: $SubscriptionId" -ForegroundColor White
Write-Host "`nFederated Credentials configured for:" -ForegroundColor Yellow
Write-Host "  - Environment: $Environment" -ForegroundColor White
Write-Host "  - Branch: $Branch" -ForegroundColor White
Write-Host "  - Pull Requests" -ForegroundColor White
Write-Host "`nGitHub Secrets added:" -ForegroundColor Yellow
Write-Host "  - ARM_CLIENT_ID" -ForegroundColor White
Write-Host "  - ARM_TENANT_ID" -ForegroundColor White
Write-Host "  - ARM_SUBSCRIPTION_ID" -ForegroundColor White

Write-Host "`nNote: Make sure the GitHub environment '$Environment' exists in your repository settings." -ForegroundColor Magenta
Write-Host "You can create it at: https://github.com/$GitHubOrg/$GitHubRepo/settings/environments" -ForegroundColor Magenta

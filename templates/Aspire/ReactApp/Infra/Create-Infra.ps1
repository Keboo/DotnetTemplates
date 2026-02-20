<#
.SYNOPSIS
    Creates the Azure Storage Account for Terraform backend state.

.DESCRIPTION
    This script creates the resource group, storage account, and blob container
    required for storing Terraform state as defined in providers.tf.

.PARAMETER ResourceGroupName
    The name of the resource group. Default: reactapp-terraform-rg

.PARAMETER StorageAccountName
    The name of the storage account (must be globally unique). Default: reactappterraform

.PARAMETER ContainerName
    The name of the blob container. Default: terraform

.PARAMETER Location
    The Azure region for the resources. Default: westus3

.PARAMETER AppRegistrationName
    The name of the App registration to grant Storage Blob Data Reader access.
    An additional App registration with 'Infra' appended will be granted Storage Blob Data Contributor.
    If not provided, only the current user will be granted access.

.EXAMPLE
    .\Create-TerraformBackend.ps1

.EXAMPLE
    .\Create-TerraformBackend.ps1 -StorageAccountName "myuniquestorageacct" -Location "westus2"

.EXAMPLE
    .\Create-TerraformBackend.ps1 -AppRegistrationName "MyGitHubOIDC"
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$ResourceGroupName = "reactapp-terraform-rg",

    [Parameter()]
    [string]$StorageAccountName = "reactappinfra",

    [Parameter()]
    [string]$ContainerName = "terraform",

    [Parameter()]
    [string]$Location = "westus3",

    [Parameter()]
    [string]$AppRegistrationName
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Terraform Backend Storage Account Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if logged in to Azure
Write-Host "Checking Azure CLI login status..." -ForegroundColor Yellow
try {
    $account = az account show 2>&1 | ConvertFrom-Json
    Write-Host "Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "Subscription: $($account.name) ($($account.id))" -ForegroundColor Green
}
catch {
    Write-Host "Not logged in to Azure CLI. Please run 'az login' first." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Resource Group:   $ResourceGroupName"
Write-Host "  Storage Account:  $StorageAccountName"
Write-Host "  Container:        $ContainerName"
Write-Host "  Location:         $Location"
Write-Host ""

# Create Resource Group
Write-Host "Checking resource group '$ResourceGroupName'..." -ForegroundColor Yellow
$rgExists = az group exists --name $ResourceGroupName

if ($rgExists -eq "true") {
    Write-Host "Resource group already exists." -ForegroundColor Green
}
else {
    Write-Host "Creating resource group '$ResourceGroupName'..." -ForegroundColor Yellow
    az group create `
        --name $ResourceGroupName `
        --location $Location `
        --output none

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to create resource group." -ForegroundColor Red
        exit 1
    }
    Write-Host "Resource group created successfully." -ForegroundColor Green
}

# Create Storage Account
Write-Host "Checking storage account '$StorageAccountName'..." -ForegroundColor Yellow
$storageAccountCheck = az storage account show `
    --name $StorageAccountName `
    --resource-group $ResourceGroupName `
    2>$null

if ($LASTEXITCODE -eq 0) {
    Write-Host "Storage account already exists." -ForegroundColor Green
}
else {
    Write-Host "Creating storage account '$StorageAccountName'..." -ForegroundColor Yellow
    Write-Host "  (This may take a few minutes)" -ForegroundColor Gray

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
        Write-Host "Failed to create storage account. The name '$StorageAccountName' may not be globally unique." -ForegroundColor Red
        Write-Host "Try a different storage account name using: -StorageAccountName 'youruniquename'" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "Storage account created successfully." -ForegroundColor Green
}

# Create Blob Container
Write-Host "Checking blob container '$ContainerName'..." -ForegroundColor Yellow

az storage container show `
    --name $ContainerName `
    --account-name $StorageAccountName `
    --auth-mode login `
    --output none 2>$null

if ($LASTEXITCODE -eq 0) {
    Write-Host "Blob container already exists." -ForegroundColor Green
}
else {
    Write-Host "Creating blob container '$ContainerName'..." -ForegroundColor Yellow

    az storage container create `
        --name $ContainerName `
        --account-name $StorageAccountName `
        --auth-mode login `
        --output none

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to create blob container." -ForegroundColor Red
        exit 1
    }
    Write-Host "Blob container created successfully." -ForegroundColor Green
}

# Get storage account ID for role assignments
$storageAccountId = az storage account show `
    --name $StorageAccountName `
    --resource-group $ResourceGroupName `
    --query id -o tsv

# Assign Storage Blob Data roles to App Registrations if specified
if ($AppRegistrationName) {
    # Grant Storage Blob Data Reader to the main app registration
    Write-Host "Looking up App Registration '$AppRegistrationName'..." -ForegroundColor Yellow
    
    $appRegistration = az ad app list --display-name $AppRegistrationName 2>&1 | ConvertFrom-Json
    
    if ($appRegistration -and $appRegistration.Count -gt 0) {
        if ($appRegistration.Count -gt 1) {
            Write-Host "Warning: Multiple app registrations found with name '$AppRegistrationName'. Using the first one." -ForegroundColor Yellow
        }
        
        $appId = $appRegistration[0].appId
        
        # Get the service principal for the app registration
        Write-Host "Looking up Service Principal for App Registration..." -ForegroundColor Yellow
        $servicePrincipal = az ad sp list --filter "appId eq '$appId'" 2>&1 | ConvertFrom-Json
        
        if ($servicePrincipal -and $servicePrincipal.Count -gt 0) {
            $servicePrincipalId = $servicePrincipal[0].id
            
            Write-Host "Assigning 'Storage Blob Data Reader' role to '$AppRegistrationName'..." -ForegroundColor Yellow
            
            az role assignment create `
                --role "Storage Blob Data Reader" `
                --assignee $servicePrincipalId `
                --scope $storageAccountId `
                --output none 2>$null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Role assignment created successfully for '$AppRegistrationName'." -ForegroundColor Green
            }
            else {
                Write-Host "Role assignment may already exist or requires elevated permissions." -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "Service Principal not found for App Registration '$AppRegistrationName'." -ForegroundColor Red
            Write-Host "You may need to create a Service Principal for this App Registration first." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "App Registration '$AppRegistrationName' not found." -ForegroundColor Red
        Write-Host "Please verify the name and try again." -ForegroundColor Yellow
    }

    # Grant Storage Blob Data Contributor to the Infra app registration
    $infraAppRegistrationName = "${AppRegistrationName}Infra"
    Write-Host "Looking up App Registration '$infraAppRegistrationName'..." -ForegroundColor Yellow
    
    $infraAppRegistration = az ad app list --display-name $infraAppRegistrationName 2>&1 | ConvertFrom-Json
    
    if ($infraAppRegistration -and $infraAppRegistration.Count -gt 0) {
        if ($infraAppRegistration.Count -gt 1) {
            Write-Host "Warning: Multiple app registrations found with name '$infraAppRegistrationName'. Using the first one." -ForegroundColor Yellow
        }
        
        $infraAppId = $infraAppRegistration[0].appId
        
        # Get the service principal for the infra app registration
        Write-Host "Looking up Service Principal for Infra App Registration..." -ForegroundColor Yellow
        $infraServicePrincipal = az ad sp list --filter "appId eq '$infraAppId'" 2>&1 | ConvertFrom-Json
        
        if ($infraServicePrincipal -and $infraServicePrincipal.Count -gt 0) {
            $infraServicePrincipalId = $infraServicePrincipal[0].id
            
            Write-Host "Assigning 'Storage Blob Data Contributor' role to '$infraAppRegistrationName'..." -ForegroundColor Yellow
            
            az role assignment create `
                --role "Storage Blob Data Contributor" `
                --assignee $infraServicePrincipalId `
                --scope $storageAccountId `
                --output none 2>$null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Role assignment created successfully for '$infraAppRegistrationName'." -ForegroundColor Green
            }
            else {
                Write-Host "Role assignment may already exist or requires elevated permissions." -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "Service Principal not found for App Registration '$infraAppRegistrationName'." -ForegroundColor Red
            Write-Host "You may need to create a Service Principal for this App Registration first." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "App Registration '$infraAppRegistrationName' not found." -ForegroundColor Red
        Write-Host "Please verify the name and try again." -ForegroundColor Yellow
    }
}

# Assign Storage Blob Data Contributor role to current user for OIDC auth
Write-Host "Assigning 'Storage Blob Data Contributor' role to current user..." -ForegroundColor Yellow

$currentUserId = az ad signed-in-user show --query id -o tsv 2>$null
if ($currentUserId) {
    az role assignment create `
        --role "Storage Blob Data Contributor" `
        --assignee $currentUserId `
        --scope $storageAccountId `
        --output none 2>$null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Role assignment created successfully." -ForegroundColor Green
    }
    else {
        Write-Host "Role assignment may already exist or requires elevated permissions." -ForegroundColor Yellow
    }
}
else {
    Write-Host "Could not determine current user ID. You may need to manually assign the 'Storage Blob Data Contributor' role." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Terraform Backend Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Backend configuration for providers.tf:" -ForegroundColor Cyan
Write-Host @"
  backend "azurerm" {
    resource_group_name  = "$ResourceGroupName"
    storage_account_name = "$StorageAccountName"
    container_name       = "$ContainerName"
    key                  = "state"
    use_oidc             = true
    use_azuread_auth     = true
  }
"@
Write-Host ""
Write-Host "This may take a few minutes for the new permissions to be applied." -ForegroundColor Yellow
Write-Host "You can now run 'terraform init' to initialize the backend." -ForegroundColor Yellow

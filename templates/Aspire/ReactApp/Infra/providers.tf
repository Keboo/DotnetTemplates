terraform {
  required_providers {
    azuread = {
      source  = "hashicorp/azuread"
      version = "3.6.0"
    }
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.54.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "3.7.2"
    }
  }

  backend "azurerm" {
    resource_group_name  = "reactapp-terraform-rg"
    storage_account_name = "reactappinfra" # This name must be made globally unique
    container_name       = "terraform"
    key                  = "state"
    use_oidc             = true
    use_azuread_auth     = true
  }

  # Add configuration for better performance and stability
  required_version = ">= 1.0"
}

provider "azuread" {
  use_oidc = true

  client_id = var.CLIENT_ID
  tenant_id = var.TENANT_ID
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
    key_vault {
      purge_soft_delete_on_destroy    = true
      recover_soft_deleted_key_vaults = true
    }
  }

  use_oidc = true

  client_id       = var.CLIENT_ID
  subscription_id = var.SUBSCRIPTION_ID
  tenant_id       = var.TENANT_ID
}

provider "random" {
}
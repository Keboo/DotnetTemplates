locals {
  environment = "Prod"
  tags = merge(var.tags,
    {
      "Environment" = local.environment
  })
}

resource "azurerm_resource_group" "resource_group" {
  name     = "reactapp-${lower(local.environment)}-rg"
  location = var.location

  tags = local.tags
}

resource "azurerm_user_assigned_identity" "app_identity" {
  name                = "reactapp-${lower(local.environment)}-mi"
  location            = azurerm_resource_group.resource_group.location
  resource_group_name = azurerm_resource_group.resource_group.name

  tags = local.tags
}

module "container_app_environment" {
  source = "../modules/container_app_environment"

  container_app_environment_name = "reactapp-${lower(local.environment)}-cae"
  resource_group                 = azurerm_resource_group.resource_group
  identity_id                    = azurerm_user_assigned_identity.app_identity.principal_id
}

module "backend_container_app" {
  source = "../modules/container_app"

  name                         = "reactapp-${lower(local.environment)}-backend"
  container_app_environment_id = module.container_app_environment.container_app_environment_id
  resource_group_name          = azurerm_resource_group.resource_group.name
  identity_id                  = azurerm_user_assigned_identity.app_identity.principal_id
  registry_server              = var.acr_login_server
  env_vars                     = {}
}

module "sql" {
  source = "../modules/sql"

  resource_group = azurerm_resource_group.resource_group

  server_name   = "reactapp-${lower(local.environment)}-sqlserver"
  database_name = "reactapp-${lower(local.environment)}-db"
  sku = {
    name        = "S0"
    max_size_gb = "16"
  }
  tags                 = local.tags
  users                = []
  sql_admin_group_name = "sql-admins"
}
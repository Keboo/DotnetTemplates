locals {
  environment = var.environment
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

data "azuread_client_config" "current" {}

resource "azuread_group" "admins_group" {
  display_name     = "ReactApp-${local.environment}-admins"
  security_enabled = true
}

resource "azuread_group_member" "current_user_admin" {
  group_object_id  = azuread_group.admins_group.object_id
  member_object_id = data.azuread_client_config.current.object_id
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

  name                            = "reactapp-${lower(local.environment)}-backend"
  container_app_environment_id    = module.container_app_environment.container_app_environment_id
  resource_group_name             = azurerm_resource_group.resource_group.name
  identity_id                     = azurerm_user_assigned_identity.app_identity.id
  container_registry_login_server = var.acr_login_server

  env_vars = {
    AZURE_CLIENT_ID = azurerm_user_assigned_identity.app_identity.client_id
    # Aspire uses ConnectionStrings__<key> naming convention
    ConnectionStrings__Database = module.sql.connection_string
    # CORS: Allow the Static Web App origin
    AllowedOrigins__0 = "https://${module.static_web_app.default_host_name}"
  }

  depends_on = [module.sql, module.static_web_app]
}

module "static_web_app" {
  source = "../modules/static_web_app"

  name           = "reactapp-${lower(local.environment)}-swa"
  resource_group = azurerm_resource_group.resource_group
  sku = {
    tier = "Free"
    size = "Free"
  }

  tags = local.tags
}

module "sql" {
  source = "../modules/sql"

  resource_group = azurerm_resource_group.resource_group

  server_name   = "reactapp-${lower(local.environment)}-sqlserver"
  database_name = "reactapp-${lower(local.environment)}-db"

  tags            = local.tags
  users           = [azurerm_user_assigned_identity.app_identity.name]
  sql_admin_group = azuread_group.admins_group
}
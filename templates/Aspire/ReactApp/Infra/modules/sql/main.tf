locals {
  connection_string = "Server=tcp:${azurerm_mssql_server.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.database.name};Encrypt=True;TrustServerCertificate=False;Connection Timeout=120;Authentication=\"Active Directory Default\";"

  db_permissions = [
    "db_datareader",
    "db_datawriter",
    "db_ddladmin"
  ]

  database_users = concat(var.users, [data.azuread_group.sql_admins_group.display_name])

  server_version             = "12.0"
  server_minimum_tls_version = "1.2"
}

data "azurerm_client_config" "current" {}

resource "azurerm_mssql_server" "sql_server" {
  name                = var.server_name
  resource_group_name = var.resource_group.name
  location            = var.resource_group.location
  version             = local.server_version
  minimum_tls_version = local.server_minimum_tls_version

  public_network_access_enabled = true

  azuread_administrator {
    login_username              = data.azuread_group.sql_admins_group.display_name
    object_id                   = data.azuread_group.sql_admins_group.object_id
    azuread_authentication_only = true
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.sql_admin.id]
  }
  primary_user_assigned_identity_id = azurerm_user_assigned_identity.sql_admin.id

  tags = var.tags
}

resource "azurerm_user_assigned_identity" "sql_admin" {
  name                = "${var.server_name}-sqladmin"
  resource_group_name = var.resource_group.name
  location            = var.resource_group.location
}

data "azuread_group" "sql_admins_group" {
  display_name     = var.sql_admin_group_name
  security_enabled = true
}

resource "azuread_group_member" "mi_admin_assignment" {
  group_object_id  = data.azuread_group.sql_admins_group.object_id
  member_object_id = azurerm_user_assigned_identity.sql_admin.principal_id
}

resource "azuread_directory_role" "directory_readers_role" {
  display_name = "Directory Readers"
}

resource "azuread_directory_role_assignment" "sql_admin_to_directory_readers" {
  role_id             = azuread_directory_role.directory_readers_role.template_id
  principal_object_id = azurerm_user_assigned_identity.sql_admin.principal_id
}

resource "azurerm_mssql_database" "database" {
  name        = var.database_name
  server_id   = azurerm_mssql_server.sql_server.id
  collation   = "SQL_Latin1_General_CP1_CI_AS"
  sku_name    = var.sku.name
  max_size_gb = var.sku.max_size_gb

  tags = var.tags

  # Consider enabling to prevent the possibility of accidental data loss
  # lifecycle {
  #   prevent_destroy = true
  # }

  timeouts {
    create = "10m"
    update = "10m"
    delete = "10m"
  }
}

resource "terraform_data" "setup_users" {
  depends_on = [azuread_directory_role_assignment.sql_admin_to_directory_readers]
  for_each   = toset(local.database_users)

  triggers_replace = [
    azurerm_mssql_database.database.id,
    join(",", local.db_permissions),
    "v1" # Increment this version when permissions change
  ]

  provisioner "local-exec" {
    command = <<-EOT
    $sql = @"
    -- Create user if they don't exist
      IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = '${each.key}')
    BEGIN
        CREATE USER [${each.key}] FROM EXTERNAL PROVIDER;
    END;

    -- Enforce dbo as the default schema
    ALTER USER [${each.key}] WITH DEFAULT_SCHEMA = [dbo];

    -- Add user to database roles
      ${join("\n", [for role in local.db_permissions : "ALTER ROLE ${role} ADD MEMBER [${each.key}];"])}

    -- Grant execute permissions for stored procedures
    GRANT EXECUTE TO [${each.key}];
    "@

    Invoke-Sqlcmd -ConnectionString '${local.connection_string}' -Query $sql
  EOT

    interpreter = ["pwsh", "-Command"]

    # https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-authentication?view=sql-server-ver16&tabs=go&WT.mc_id=DT-MVP-5003472#activedirectorydefault
    environment = {
      "AZURE_CLIENT_ID" = "${data.azurerm_client_config.current.client_id}"
      "AZURE_TENANT_ID" = "${data.azurerm_client_config.current.tenant_id}"
    }
  }
}
locals {
  tags = merge(var.tags,
    {
      "Environment" = "Prod"
  })
}

resource "azurerm_resource_group" "resource_group" {
  name     = "reactapp-shared-rg"
  location = var.location

  tags = local.tags
}

module "container_registry" {
  source = "../modules/container_registry"

  name           = "reactappacr${lower(var.environment)}"
  resource_group = azurerm_resource_group.resource_group
  tags           = local.tags

  pull_identity_ids = values(var.app_identities)
  push_identity_ids = []
}


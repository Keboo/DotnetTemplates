resource "azurerm_container_app_environment" "container_app_environment" {
  name                = var.container_app_environment_name
  location            = var.resource_group.location
  resource_group_name = var.resource_group.name

  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
  }

  tags = var.tags

  lifecycle {
    ignore_changes = [
      tags
    ]
  }
}

resource "azurerm_role_assignment" "container_app_environment_contributor" {
  scope = azurerm_container_app_environment.container_app_environment.id
  // https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#acrpush
  role_definition_name = "Contributor"
  principal_id         = var.identity_id
}
data "azurerm_client_config" "current" {}

resource "azurerm_container_registry" "acr" {
  name                = var.name
  resource_group_name = var.resource_group.name
  location            = var.resource_group.location
  sku                 = var.sku
  admin_enabled       = true

  tags = var.tags
}

resource "azurerm_role_assignment" "azure_acr_pull_user" {
  count = length(var.pull_identity_ids)

  scope = azurerm_container_registry.acr.id
  // https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#acrpull
  role_definition_name = "AcrPull"
  principal_id         = var.pull_identity_ids[count.index]
}

resource "azurerm_role_assignment" "azure_acr_push_user" {
  count = length(var.push_identity_ids)

  scope = azurerm_container_registry.acr.id
  // https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#acrpush
  role_definition_name = "AcrPush"
  principal_id         = var.push_identity_ids[count.index]
}

// Import hello world image for initial container app/job deployments.
resource "terraform_data" "import_helloworld_image" {
  provisioner "local-exec" {
    command     = <<-EOT
    az acr import `
      --name ${azurerm_container_registry.acr.name} `
      --source docker.io/crccheck/hello-world:latest `
      --image crccheck/hello-world:latest `
      --subscription ${data.azurerm_client_config.current.subscription_id}
    EOT
    interpreter = ["pwsh", "-Command"]
  }

  triggers_replace = {
    acr_id  = azurerm_container_registry.acr.id
    version = 1
  }

  depends_on = [azurerm_container_registry.acr]
}
// Azure Static Web App resource
// Deployment of app content is handled separately via CI/CD pipeline

resource "azurerm_static_web_app" "app" {
  name                = var.name
  resource_group_name = var.resource_group.name
  location            = var.resource_group.location
  sku_tier            = var.sku.tier
  sku_size            = var.sku.size

  app_settings = var.app_settings

  tags = var.tags
}

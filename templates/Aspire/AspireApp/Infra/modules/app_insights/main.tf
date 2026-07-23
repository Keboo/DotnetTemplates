resource "azurerm_application_insights" "application_insights" {
  name                = "reactapp-${lower(var.environment)}-appinsights"
  location            = var.resource_group.location
  resource_group_name = var.resource_group.name
  application_type    = "web"

  tags = var.tags
}

resource "azurerm_role_assignment" "app_insights_reader" {
  for_each             = var.reader_ids
  scope                = azurerm_application_insights.application_insights.id
  role_definition_name = "Reader"
  principal_id         = each.value
}

output "id" {
  description = "The ID of the container app"
  value       = azurerm_container_app.app.id
}

output "name" {
  description = "The name of the container app"
  value       = azurerm_container_app.app.name
}

output "fqdn" {
  description = "The fully qualified domain name of the container app"
  value       = azurerm_container_app.app.ingress[0].fqdn
}

output "latest_revision_fqdn" {
  description = "The FQDN of the latest revision"
  value       = azurerm_container_app.app.latest_revision_fqdn
}

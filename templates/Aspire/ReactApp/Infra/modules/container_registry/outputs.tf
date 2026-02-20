output "container_registry_id" {
  description = "The ID of the container registry."
  value       = azurerm_container_registry.acr.id
}

output "login_server" {
  description = "The login server URL of the container registry."
  value       = azurerm_container_registry.acr.login_server
}

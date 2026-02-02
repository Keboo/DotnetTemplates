output "id" {
  description = "The ID of the static web app"
  value       = azurerm_static_web_app.app.id
}

output "name" {
  description = "The name of the static web app"
  value       = azurerm_static_web_app.app.name
}

output "default_host_name" {
  description = "The default hostname of the static web app"
  value       = azurerm_static_web_app.app.default_host_name
}

output "api_key" {
  description = "The API key for deployment (sensitive)"
  value       = azurerm_static_web_app.app.api_key
  sensitive   = true
}

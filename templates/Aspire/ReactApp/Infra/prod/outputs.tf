output "app_identity" {
  value = azurerm_user_assigned_identity.app_identity
}

output "backend_container_app_name" {
  description = "The name of the backend container app"
  value       = module.backend_container_app.name
}

output "resource_group_name" {
  description = "The name of the resource group"
  value       = azurerm_resource_group.resource_group.name
}

output "database_connection_string" {
  description = "The connection string for the SQL database"
  value       = module.sql.connection_string
}

output "static_web_app_name" {
  description = "The name of the static web app"
  value       = module.static_web_app.name
}

output "static_web_app_api_key" {
  description = "The API key for the static web app deployment"
  value       = module.static_web_app.api_key
  sensitive   = true
}

output "static_web_app_url" {
  description = "The URL of the deployed static web app"
  value       = "https://${module.static_web_app.default_host_name}"
}

output "backend_url" {
  description = "The URL of the backend API"
  value       = "https://${module.backend_container_app.fqdn}"
}
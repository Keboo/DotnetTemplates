output "container_app_environment_id" {
  description = "The ID of the Container App Environment"
  value       = azurerm_container_app_environment.container_app_environment.id
}

output "container_app_environment_name" {
  description = "The name of the Container App Environment"
  value       = azurerm_container_app_environment.container_app_environment.name
}

output "default_domain" {
  description = "The default domain of the Container App Environment"
  value       = azurerm_container_app_environment.container_app_environment.default_domain
}

output "custom_domain_verification_id" {
  description = "The ID of the Custom Domain Verification for this Container App Environment"
  value       = azurerm_container_app_environment.container_app_environment.custom_domain_verification_id
}
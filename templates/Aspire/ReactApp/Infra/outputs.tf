output "acr_login_server" {
  description = "The login server for the Azure Container Registry"
  value       = module.shared.acr_login_server
}

output "backend_container_app_name" {
  description = "The name of the backend container app"
  value       = module.prod.backend_container_app_name
}

output "resource_group_name" {
  description = "The name of the resource group"
  value       = module.prod.resource_group_name
}

output "static_web_app_name" {
  description = "The name of the static web app"
  value       = module.prod.static_web_app_name
}

output "static_web_app_api_key" {
  description = "The API key for the static web app deployment"
  value       = module.prod.static_web_app_api_key
  sensitive   = true
}

output "static_web_app_url" {
  description = "The URL of the deployed static web app"
  value       = module.prod.static_web_app_url
}

output "backend_url" {
  description = "The URL of the backend API"
  value       = module.prod.backend_url
}

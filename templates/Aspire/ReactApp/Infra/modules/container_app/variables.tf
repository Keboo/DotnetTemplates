variable "name" {
  description = "Name of the container app"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "container_app_environment_id" {
  description = "ID of the Container App Environment"
  type        = string
}

variable "env_vars" {
  description = "Environment variables for the container"
  type        = map(string)
  default     = {}
}

variable "secret_env_vars" {
  description = "Secret environment variables for the container (stored as secrets)"
  type        = map(string)
  default     = {}
  sensitive   = true
}

variable "identity_id" {
  description = "Resource ID of the User Assigned Identity"
  type        = string
}

variable "registry_server" {
  description = "Container registry server"
  type        = string
}

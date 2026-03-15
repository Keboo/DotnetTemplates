variable "name" {
  description = "Name of the container app"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}
variable "identity_id" {
  description = "The resource ID of the user-assigned managed identity."
  type        = string
}

variable "container_app_environment_id" {
  description = "ID of the Container App Environment"
  type        = string
}

variable "container_registry_login_server" {
  description = "Container registry server"
  type        = string
}

variable "cpu" {
  description = "CPU cores allocated to the container (e.g. 0.5, 1.0)."
  type        = number
  default     = 0.5
}

variable "memory" {
  description = "Memory allocated to the container (e.g. 1Gi)."
  type        = string
  default     = "1Gi"
}

variable "min_replicas" {
  description = "Minimum number of replicas."
  type        = number
  default     = 0
}

variable "max_replicas" {
  description = "Maximum number of replicas."
  type        = number
  default     = 1
}

variable "env_vars" {
  description = "Map of environment variables for the container."
  type        = map(string)
  default     = {}
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}

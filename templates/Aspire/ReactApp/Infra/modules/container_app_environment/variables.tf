variable "resource_group" {
  description = "Complex object for resource group configuration"
  type = object({
    name     = string
    location = string
  })
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}

variable "container_app_environment_name" {
  description = "Environment variables for the container app"
  type        = string
}

variable "identity_id" {
  description = "ID of the managed identity"
  type        = string
}
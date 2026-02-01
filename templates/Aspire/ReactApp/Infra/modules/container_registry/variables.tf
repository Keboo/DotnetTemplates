variable "resource_group" {
  description = "Complex object for resource group configuration"
  type = object({
    name     = string
    location = string
  })
}

variable "name" {
  description = "Name of the container registry"
  type        = string
}

variable "pull_identity_ids" {
  description = "IDs of the identity that should be granted pull permissions"
  type        = list(string)
}

variable "push_identity_ids" {
  description = "IDs of the identity that should be granted push permissions"
  type        = list(string)
}

variable "sku" {
  description = "The SKU of the container registry"
  type        = string
  default     = "Basic"
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}
variable "name" {
  description = "Name of the static web app"
  type        = string
}

variable "resource_group" {
  description = "Resource group configuration"
  type = object({
    name     = string
    location = string
  })
}

variable "sku" {
  description = "SKU configuration for the static web app"
  type = object({
    tier = string
    size = string
  })
  default = {
    tier = "Free"
    size = "Free"
  }

  validation {
    condition     = contains(["Free", "Standard"], var.sku.tier)
    error_message = "sku.tier must be either 'Free' or 'Standard'."
  }

  validation {
    condition     = contains(["Free", "Standard"], var.sku.size)
    error_message = "sku.size must be either 'Free' or 'Standard'."
  }
}

variable "app_settings" {
  description = "Application settings (environment variables) for the static web app"
  type        = map(string)
  default     = {}
}

variable "tags" {
  description = "Tags to apply to the resource"
  type        = map(string)
  default     = {}
}

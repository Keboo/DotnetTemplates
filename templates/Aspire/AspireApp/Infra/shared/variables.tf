variable "location" {
  description = "Azure region for the resources"
  type        = string
}

variable "environment" {
  description = "The deployment environment (e.g., Dev, Prod)"
  type        = string
}

variable "app_identities" {
  description = "Map of application identities"
  type        = map(string)
  default     = {}
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}
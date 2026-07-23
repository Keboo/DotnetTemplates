variable "environment" {
  description = "The deployment environment (e.g., Dev, QA, Stage, Prod)"
  type        = string
}

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

variable "reader_ids" {
  description = "Map of principal IDs to assign Reader role. Keys should be unique names."
  type        = map(string)
  default     = {}
}

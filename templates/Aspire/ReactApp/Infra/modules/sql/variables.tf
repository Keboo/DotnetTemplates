variable "resource_group" {
  description = "Complex object for resource group configuration"
  type = object({
    name     = string
    location = string
  })
}

variable "server_name" {
  description = "The name of the Azure SQL server"
  type        = string
}

variable "database_name" {
  description = "The name of the database"
  type        = string
}

variable "sku" {
  description = "The SKU of the SQL database"
  type = object({
    name        = string
    max_size_gb = string
  })
  default = {
    name        = "Basic"
    max_size_gb = "2"
  }
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}

variable "users" {
  description = "A list of users to enable external provider access to on the database. These users will be given read/write access"
  type        = list(string)
  default     = []
}

variable "sql_admin_group" {
  description = "The admin group for the SQL server"
  type = object({
    display_name = string,
    object_id    = string
  })
}
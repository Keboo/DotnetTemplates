variable "CLIENT_ID" {
  description = "Value of the client id of the service principal"
  type        = string
  default     = ""
}

variable "TENANT_ID" {
  type        = string
  description = "Value of the tenant id of the service principal"
  default     = ""
}

variable "SUBSCRIPTION_ID" {
  type        = string
  description = "Value of the subscription id to use"
  default     = ""
}

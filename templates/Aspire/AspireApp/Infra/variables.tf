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

variable "backend_custom_domain" {
  description = "Custom domain URL for the backend API (e.g. https://api.example.com). When set, overrides the auto-generated Container App FQDN as the backend_url output."
  type        = string
  default     = ""
}

variable "frontend_custom_domain" {
  description = "Custom domain URL for the frontend Static Web App (e.g. https://app.example.com). When set, overrides the auto-generated Static Web App hostname as the static_web_app_url output, and is added as an additional allowed CORS origin on the backend."
  type        = string
  default     = ""
}

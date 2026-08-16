variable "acr_login_server" {
  description = "The login server of the container registry."
  type        = string
}

variable "environment" {
  description = "The deployment environment (e.g., Dev, Prod)"
  type        = string
}

variable "location" {
  description = "Azure region for the resources"
  type        = string
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
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

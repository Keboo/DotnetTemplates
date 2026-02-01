locals {
  default_tags = {
    "app" = "ReactApp"
  }

  # TODO: Review and adjust locations as needed
  location = "westus2"
}

module "shared" {
  source = "./shared"

  location = local.location
  tags     = local.default_tags

  app_identities = {
    "prod" = module.prod.app_identity.principal_id
  }
}

module "prod" {
  source = "./prod"

  acr_login_server = module.shared.acr_login_server
  location = local.location
  tags     = local.default_tags
}

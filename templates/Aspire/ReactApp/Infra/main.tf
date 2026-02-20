locals {
  default_tags = {
    "app" = "ReactApp"
  }

  # TODO: Review and adjust variables as needed for your project
  location    = "westus2"
  environment = random_pet.environment.id
}


#TODO: For a real project, consider using a more robust naming convention
resource "random_pet" "environment" {
  length = 1
}

module "shared" {
  source = "./shared"

  environment = local.environment

  location = local.location
  tags     = local.default_tags

  app_identities = {
    "prod" = module.prod.app_identity.principal_id
  }
}

module "prod" {
  source = "./prod"

  environment = local.environment

  acr_login_server = module.shared.acr_login_server
  location         = local.location
  tags             = local.default_tags
}


// TODO (future - not currently available):
// Automate assignment of custom domain and SSL cert
// https://github.com/microsoft/azure-container-apps/issues/796#issuecomment-2515167794 
// https://github.com/hashicorp/terraform-provider-azurerm/pull/31137 

resource "azurerm_container_app" "app" {
  name                         = var.name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name = var.name
      // NOTE: Initial container image will be replaced by azure pipelines deploy
      image  = "${var.registry_server}/crccheck/hello-world:latest"
      cpu    = "0.5"
      memory = "1Gi"

      env {
        # Port for crccheck/hello-world to listen on
        name  = "PORT"
        value = "8080"
      }

      env {
        name  = "ASPNETCORE_HTTP_PORTS"
        value = "8080"
      }

      dynamic "env" {
        for_each = var.env_vars
        content {
          name  = env.key
          value = env.value
        }
      }
    }
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [var.identity_id]
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  registry {
    server   = var.registry_server
    identity = var.identity_id
  }

  lifecycle {
    ignore_changes = [
      template[0].container[0].image
    ]
  }
}

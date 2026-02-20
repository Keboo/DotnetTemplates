
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
  tags                         = var.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [var.identity_id]
  }

  registry {
    server   = var.container_registry_login_server
    identity = var.identity_id
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = var.name
      image  = "${var.container_registry_login_server}/crccheck/hello-world:latest"
      cpu    = var.cpu
      memory = var.memory

      env {
        name  = "PORT"
        value = "8080"
      }

      env {
        name  = "ASPNETCORE_HTTP_PORTS"
        value = "8080;8081"
      }

      env {
        name  = "HEALTH_PORT"
        value = "8081"
      }

      dynamic "env" {
        for_each = var.env_vars
        content {
          name  = env.key
          value = env.value
        }
      }

      liveness_probe {
        path             = "/alive"
        port             = 8081
        transport        = "HTTP"
        initial_delay    = 10
        interval_seconds = 30
      }

      readiness_probe {
        path             = "/alive"
        port             = 8081
        transport        = "HTTP"
        interval_seconds = 10
      }

      startup_probe {
        path                    = "/health"
        port                    = 8081
        transport               = "HTTP"
        interval_seconds        = 5
        failure_count_threshold = 15 # 5 * 15 = 75 seconds
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
    ]
  }
}

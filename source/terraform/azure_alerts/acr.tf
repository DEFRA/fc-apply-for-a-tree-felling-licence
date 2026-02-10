locals {
  acr_id = data.terraform_remote_state.platform.outputs.acr.id
}

resource "azurerm_monitor_activity_log_alert" "acr_delete" {
  name                = "acr-delete"
  location            = "global"
  resource_group_name = local.rg_name
  scopes              = [local.acr_id]
  description         = "ACR delete activity detected."
  enabled             = true

  criteria {
    category       = "Administrative"
    operation_name = "Microsoft.ContainerRegistry/registries/delete"
  }

  action {
    action_group_id = local.action_group_id.nonprod
  }
}

# resource "azurerm_monitor_activity_log_alert" "acr_update" {
#   name                = "acr-update"
#   location            = "global"
#   resource_group_name = local.rg_name
#   scopes              = [local.acr_id]
#   description         = "ACR update activity detected (config/network/admin user changes)."
#   enabled             = true

#   criteria {
#     category       = "Administrative"
#     operation_name = "Microsoft.ContainerRegistry/registries/write"
#   }

#   action {
#     action_group_id = local.action_group_id.nonprod
#   }
# }


resource "azurerm_monitor_action_group" "prod" {
  name                = "flo-prod-action-group"
  resource_group_name = azurerm_resource_group.fs_flov2.name
  short_name          = "FLOPROD"

  webhook_receiver {
    name        = "opsgenie"
    service_uri = module.shared.opsgenie_prod_webhook_url
  }
}

resource "azurerm_monitor_action_group" "nonprod" {
  name                = "flo-nonprod-action-group"
  resource_group_name = azurerm_resource_group.fs_flov2.name
  short_name          = "FLONP"

  webhook_receiver {
    name        = "opsgenie"
    service_uri = module.shared.opsgenie_nonprod_webhook_url
  }
}

output "action_group_prod_id"   { value = azurerm_monitor_action_group.prod.id }
output "action_group_nonprod_id" { value = azurerm_monitor_action_group.nonprod.id }

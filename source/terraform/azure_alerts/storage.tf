locals {
  storage_accounts_all = data.terraform_remote_state.platform.outputs.storage_accounts

  # only stage/migrate/live (as you said)
  storage_accounts = {
    stage   = { id = local.storage_accounts_all.stage.id,   env = "nonprod" }
    migrate = { id = local.storage_accounts_all.migrate.id, env = "nonprod" }
    live    = { id = local.storage_accounts_all.live.id,    env = "prod" }
  }
}

# # Storage used capacity (bytes) - tune thresholds per account
# resource "azurerm_monitor_metric_alert" "sa_used_capacity_high" {
#   for_each            = local.storage_accounts
#   name                = "sa-${each.key}-used-capacity-high"
#   resource_group_name = local.rg_name
#   scopes              = [each.value.id]
#   severity            = each.value.env == "prod" ? 1 : 2
#   description         = "Storage account ${each.key}: used capacity high"
#   enabled             = true

#   frequency   = "PT5M"
#   window_size = "PT1H"

#   criteria {
#     metric_namespace = "Microsoft.Storage/storageAccounts"
#     metric_name      = "UsedCapacity"
#     aggregation      = "Average"
#     operator         = "GreaterThan"

#     # NOTE: your threshold example is almost certainly wrong for real storage accounts.
#     # Replace with a real byte threshold, or use a percentage approach via Log Analytics.
#     threshold = 1000000000000 # example 1TB
#   }

#   action {
#     action_group_id = local.action_group_id[each.value.env]
#   }

#   tags = { service = "storage", env = each.value.env }
# }

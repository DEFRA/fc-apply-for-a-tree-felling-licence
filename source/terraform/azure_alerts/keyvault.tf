
locals {
  # you said KV is "platform" and you’re not bothering with dev/test
  kv_scope = data.terraform_remote_state.azure_keyvault.outputs.key_vault.id
}

# ---------- Metrics: Throttling (429) ----------
resource "azurerm_monitor_metric_alert" "kv_throttling" {
  name                = "kv-throttling-429"
  resource_group_name = local.rg_name
  scopes              = [local.kv_scope]
  description         = "Key Vault is throttling requests (HTTP 429)."
  severity            = 1
  enabled             = true

  frequency   = "PT5M"
  window_size = "PT15M"

  criteria {
    metric_namespace = "Microsoft.KeyVault/vaults"
    metric_name      = "ServiceApiResult"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 50

    dimension {
      name     = "StatusCode"
      operator = "Include"
      values   = ["429"]
    }
  }

  action {
    action_group_id = local.action_group_id.prod
  }

  tags = { service = "keyvault", env = "platform" }
}

# ---------- Metrics: Auth failures (401/403) ----------
# resource "azurerm_monitor_metric_alert" "kv_auth_fail" {
#   name                = "kv-auth-fail-401-403"
#   resource_group_name = local.rg_name
#   scopes              = [local.kv_scope]
#   description         = "Key Vault authentication failures (401/403)."
#   severity            = 2
#   enabled             = true

#   frequency   = "PT5M"
#   window_size = "PT15M"

#   criteria {
#     metric_namespace = "Microsoft.KeyVault/vaults"
#     metric_name      = "ServiceApiResult"
#     aggregation      = "Total"
#     operator         = "GreaterThan"
#     threshold        = 20

#     dimension {
#       name     = "StatusCode"
#       operator = "Include"
#       values   = ["401", "403"]
#     }
#   }

#   action {
#     action_group_id = local.action_group_id.prod
#   }

#   tags = { service = "keyvault", env = "platform" }
# }

# ---------- Activity Log: Vault deleted / purge attempted ----------
resource "azurerm_monitor_activity_log_alert" "kv_delete_purge" {
  name                = "kv-delete-or-purge"
  location            = "Global"
  resource_group_name = local.rg_name
  scopes              = [local.kv_scope]
  description         = "Key Vault delete/purge activity detected."
  enabled             = true

  criteria {
    category = "Administrative"
    operation_name = "Microsoft.KeyVault/vaults/delete"
  }

  action {
    action_group_id = local.action_group_id.prod
  }
}

resource "azurerm_monitor_activity_log_alert" "kv_purge" {
  name                = "kv-purge"
  location            = "Global"
  resource_group_name = local.rg_name
  scopes              = [local.kv_scope]
  description         = "Key Vault purge activity detected."
  enabled             = true

  criteria {
    category = "Administrative"
    operation_name = "Microsoft.KeyVault/locations/deletedVaults/purge/action"
  }

  action {
    action_group_id = local.action_group_id.prod
  }
}

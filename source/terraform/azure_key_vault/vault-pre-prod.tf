# # Do not delete this Key Vault without retrieving the secrets
# resource "azurerm_key_vault" "pre_prod_kv" {
#   name                        = "flov2-uks-kv-pre-prod"
#   location                    = "UK South"
#   resource_group_name         = module.shared.rg_name
#   tenant_id                   = data.azurerm_client_config.current.tenant_id
#   sku_name                    = "standard"
#   purge_protection_enabled    = true
#   soft_delete_retention_days  = 7

#   lifecycle {
#     prevent_destroy = true
#     ignore_changes = [ 
#       access_policy
#      ]
#   }

#   access_policy {
#     tenant_id = data.azurerm_client_config.current.tenant_id
#     object_id = data.azurerm_client_config.current.object_id

#     secret_permissions = [
#       "Get", "List", "Set", "Delete", "Recover", "Backup", "Restore"
#     ]
#   }

#   access_policy {
#     tenant_id = data.azurerm_client_config.current.tenant_id
#     object_id = module.shared.tc_client_id

#     secret_permissions = [
#       "Get",
#       "List"
#     ]
#   }

#   tags = {
#     environment = "platform"
#     team        = "devops"
#   }
# }

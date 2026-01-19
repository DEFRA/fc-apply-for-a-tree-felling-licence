# resource "azurerm_storage_account" "stress" {
#   count = var.enable_stress_testing_environment ? 1 : 0

#   name                             = "stressflo"
#   resource_group_name              = azurerm_resource_group.fs_flov2.name
#   location                         = module.shared.azure_location
#   account_tier                     = "Standard"
#   account_replication_type         = "GRS"
#   cross_tenant_replication_enabled = true

#   tags = local.stress_tags
# }

# resource "azurerm_storage_container" "stress" {
#   count = var.enable_stress_testing_environment ? 1 : 0

#   name = "stressflo"
#   #storage_account_name  = azurerm_storage_account.stress.name
#   storage_account_id    = azurerm_storage_account.stress.id
#   container_access_type = "blob"
# }
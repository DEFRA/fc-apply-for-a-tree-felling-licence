data "azurerm_key_vault" "flov2_kv" {
  provider            = azurerm
  name                = "forestry-uksouth-kv"
  resource_group_name = "fs_flov2"
}
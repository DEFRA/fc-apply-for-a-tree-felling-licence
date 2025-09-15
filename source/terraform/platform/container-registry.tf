# Create Azure Container Registry
resource "azurerm_container_registry" "fs_flov2" {
  name                = "${module.shared.prefix}flo"
  resource_group_name = azurerm_resource_group.fs_flov2.name
  location            = module.shared.azure_location
  sku                 = "Premium"
  admin_enabled       = true
}

# Give TeamCity service principal permissions
resource "azurerm_role_assignment" "tc_acrpull" {
  principal_id                     = "14fb02a8-2e33-4ab1-bb4b-10b1295e1b80"
  role_definition_name             = "AcrPull"
  scope                            = azurerm_container_registry.fs_flov2.id
  skip_service_principal_aad_check = true
}

resource "azurerm_role_assignment" "tc_acrpush" {
  principal_id                     = "14fb02a8-2e33-4ab1-bb4b-10b1295e1b80"
  role_definition_name             = "AcrPush"
  scope                            = azurerm_container_registry.fs_flov2.id
  skip_service_principal_aad_check = true
}
# Create Resource Group for FLOv2 Azure Resoruces
resource "azurerm_resource_group" "fs_flov2" {
  name     = module.shared.rg_name
  location = module.shared.azure_location
  tags     = module.shared.azure_tags
}
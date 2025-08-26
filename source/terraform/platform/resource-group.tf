# Create Resource Group for FLOv2 Azure Resoruces
resource "azurerm_resource_group" "fs_flov2" {
  name     = "fs_flov2"
  location = var.azure_location
  tags     = var.azure_tags
}
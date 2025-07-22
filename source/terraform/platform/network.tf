resource "azurerm_virtual_network" "fs_flov2" {
  name                = "${var.prefix}-network"
  location            = var.azure_location
  resource_group_name = azurerm_resource_group.fs_flov2.name
  address_space       = ["192.168.52.0/22"]
}

resource "azurerm_subnet" "default" {
  name                              = "default"
  virtual_network_name              = azurerm_virtual_network.fs_flov2.name
  resource_group_name               = azurerm_resource_group.fs_flov2.name
  address_prefixes                  = ["192.168.52.0/23"]
  private_endpoint_network_policies = "Enabled"

  depends_on = [
    azurerm_virtual_network.fs_flov2
  ]
}

resource "azurerm_subnet" "database" {
  name                              = "database"
  virtual_network_name              = azurerm_virtual_network.fs_flov2.name
  resource_group_name               = azurerm_resource_group.fs_flov2.name
  address_prefixes                  = ["192.168.54.0/24"]
  service_endpoints                 = ["Microsoft.Storage"]
  private_endpoint_network_policies = "Enabled"

  delegation {
    name = "fs"
    service_delegation {
      name    = "Microsoft.DBforPostgreSQL/flexibleServers"
      actions = ["Microsoft.Network/virtualNetworks/subnets/join/action"]
    }
  }
  depends_on = [
    azurerm_virtual_network.fs_flov2
  ]
}
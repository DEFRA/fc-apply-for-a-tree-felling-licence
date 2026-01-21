resource "azurerm_virtual_network" "fs_flov2_ukw" {
  name                = "${module.shared.prefix}-network-ukw"
  location            = "ukwest"
  resource_group_name = azurerm_resource_group.fs_flov2.name
  address_space       = ["192.168.56.0/22"]
}

resource "azurerm_subnet" "default_ukw" {
  name                              = "default"
  virtual_network_name              = azurerm_virtual_network.fs_flov2_ukw.name
  resource_group_name               = azurerm_resource_group.fs_flov2.name
  address_prefixes                  = ["192.168.56.0/23"]
  private_endpoint_network_policies = "Enabled"
}

resource "azurerm_subnet" "database_ukw" {
  name                              = "database"
  virtual_network_name              = azurerm_virtual_network.fs_flov2_ukw.name
  resource_group_name               = azurerm_resource_group.fs_flov2.name
  address_prefixes                  = ["192.168.58.0/24"]
  private_endpoint_network_policies = "Enabled"

  delegation {
    name = "fs"
    service_delegation {
      name    = "Microsoft.DBforPostgreSQL/flexibleServers"
      actions = ["Microsoft.Network/virtualNetworks/subnets/join/action"]
    }
  }
}

# VNet Peering: ukwest → uksouth  
resource "azurerm_virtual_network_peering" "uks_to_ukw" {
  name                      = "peer-uks-to-ukw"
  resource_group_name       = azurerm_resource_group.fs_flov2.name
  virtual_network_name      = azurerm_virtual_network.fs_flov2.name
  remote_virtual_network_id = azurerm_virtual_network.fs_flov2_ukw.id

  allow_virtual_network_access = true
  allow_gateway_transit        = false
  use_remote_gateways          = false
  allow_forwarded_traffic      = false
}

# VNet Peering: uksouth → ukwest
resource "azurerm_virtual_network_peering" "ukw_to_uks" {
  name                      = "peer-ukw-to-uks"
  resource_group_name       = azurerm_resource_group.fs_flov2.name
  virtual_network_name      = azurerm_virtual_network.fs_flov2_ukw.name
  remote_virtual_network_id = azurerm_virtual_network.fs_flov2.id

  allow_virtual_network_access = true
  allow_gateway_transit        = false
  use_remote_gateways          = false
  allow_forwarded_traffic      = false
}
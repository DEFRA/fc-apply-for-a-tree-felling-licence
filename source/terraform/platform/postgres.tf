resource "azurerm_private_dns_zone" "database" {
  name                = "flo-databases.postgres.database.azure.com"
  resource_group_name = azurerm_resource_group.fs_flov2.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "database" {
  name                  = "database"
  private_dns_zone_name = azurerm_private_dns_zone.database.name
  virtual_network_id    = azurerm_virtual_network.fs_flov2.id
  resource_group_name   = azurerm_resource_group.fs_flov2.name
}

data "azurerm_virtual_network" "ms_production" {
  name                = "ms_production"
  resource_group_name = "ms_production"
  provider            = azurerm.managedservices
}

resource "azurerm_private_dns_zone_virtual_network_link" "production-dns-link" {
  name                  = "production-dns-link"
  private_dns_zone_name = azurerm_private_dns_zone.database.name
  virtual_network_id    = data.azurerm_virtual_network.ms_production.id
  resource_group_name   = azurerm_resource_group.fs_flov2.name
}

resource "azurerm_postgresql_flexible_server" "staging" {
  name                          = "staging-flo"
  resource_group_name           = azurerm_resource_group.fs_flov2.name
  location                      = var.azure_location
  version                       = "13"
  delegated_subnet_id           = azurerm_subnet.database.id
  private_dns_zone_id           = azurerm_private_dns_zone.database.id
  geo_redundant_backup_enabled  = true
  administrator_login           = "qxlva"
  administrator_password        = "H*VsS^$&mBHywkKXt$yrVQF5"
  zone                          = "1"
  public_network_access_enabled = false

  storage_mb = 32768

  sku_name   = "GP_Standard_D2ds_v4"
  depends_on = [azurerm_private_dns_zone_virtual_network_link.database]
}

resource "azurerm_postgresql_flexible_server_configuration" "staging" {
  name      = "azure.extensions"
  server_id = azurerm_postgresql_flexible_server.staging.id
  value     = "POSTGIS,UUID-OSSP"
}

resource "azurerm_postgresql_flexible_server" "live" {
  name                          = "live-flo"
  resource_group_name           = azurerm_resource_group.fs_flov2.name
  location                      = var.azure_location
  version                       = "13"
  delegated_subnet_id           = azurerm_subnet.database.id
  private_dns_zone_id           = azurerm_private_dns_zone.database.id
  geo_redundant_backup_enabled  = true
  administrator_login           = "qxlva"
  administrator_password        = "#yk*gXsc!ocZ6teChg"
  zone                          = "1"
  public_network_access_enabled = false

  storage_mb = 32768

  sku_name   = "GP_Standard_D2ds_v4"
  depends_on = [azurerm_private_dns_zone_virtual_network_link.database]
}

resource "azurerm_postgresql_flexible_server_configuration" "live" {
  name      = "azure.extensions"
  server_id = azurerm_postgresql_flexible_server.live.id
  value     = "POSTGIS,UUID-OSSP"
}

resource "azurerm_postgresql_flexible_server" "migrate" {
  name                          = "migrate-flo"
  resource_group_name           = azurerm_resource_group.fs_flov2.name
  location                      = var.azure_location
  version                       = "13"
  delegated_subnet_id           = azurerm_subnet.database.id
  private_dns_zone_id           = azurerm_private_dns_zone.database.id
  geo_redundant_backup_enabled  = true
  administrator_login           = "qxlva"
  administrator_password        = "2GgNq596p9VRgv7fWK"
  zone                          = "1"
  public_network_access_enabled = false

  storage_mb = 32768

  sku_name   = "GP_Standard_D2ds_v5"
  depends_on = [azurerm_private_dns_zone_virtual_network_link.database]
}

resource "azurerm_postgresql_flexible_server_configuration" "migrate" {
  name      = "azure.extensions"
  server_id = azurerm_postgresql_flexible_server.migrate.id
  value     = "POSTGIS,UUID-OSSP"
}
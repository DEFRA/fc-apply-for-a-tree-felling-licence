resource "azurerm_private_dns_zone_virtual_network_link" "database_ukw" {
  name                  = "database-ukw"
  private_dns_zone_name = azurerm_private_dns_zone.database.name
  virtual_network_id    = data.terraform_remote_state.platform.outputs.virtual_network_ukw_id
  resource_group_name   = data.terraform_remote_state.platform.outputs.resource_group_name
}

resource "azurerm_postgresql_flexible_server" "live_replica" {
  name                = "live-replica-flo"
  resource_group_name = data.terraform_remote_state.platform.outputs.resource_group_name
  location            = "ukwest"

  create_mode      = "Replica"
  source_server_id = azurerm_postgresql_flexible_server.live.id

  delegated_subnet_id           = data.terraform_remote_state.platform.outputs.subnet_database_ukw_id
  private_dns_zone_id           = azurerm_private_dns_zone.database.id
  public_network_access_enabled = false

  storage_mb = 32768
  sku_name   = "GP_Standard_D2ds_v4"

  depends_on = [
    azurerm_private_dns_zone_virtual_network_link.database,
    azurerm_private_dns_zone_virtual_network_link.database_ukw,
    azurerm_postgresql_flexible_server.live
  ]

  lifecycle {
    ignore_changes = [zone]
  }
}
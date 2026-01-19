data "azurerm_private_dns_zone" "database" {
  name                = "flo-databases.postgres.database.azure.com"
  resource_group_name = data.terraform_remote_state.platform.outputs.resource_group_name
}

resource "azurerm_postgresql_flexible_server" "stress_testing" {
  count = var.enable_stress_testing_environment ? 1 : 0

  name                = "stress-testing-flo"
  resource_group_name = data.terraform_remote_state.platform.outputs.resource_group_name
  location            = module.shared.azure_location
  version             = "13"

  delegated_subnet_id = data.terraform_remote_state.platform.outputs.subnet_database_id
  private_dns_zone_id = data.azurerm_private_dns_zone.database.id

  geo_redundant_backup_enabled  = false
  administrator_login           = "qxlva"
  administrator_password        = module.shared.pg_staging_pass
  zone                          = "1"
  public_network_access_enabled = false

  storage_mb = 32768
  sku_name   = "GP_Standard_D2ds_v4"

  tags = local.stress_tags
}

resource "azurerm_postgresql_flexible_server_configuration" "stress_testing_extensions" {
  count = var.enable_stress_testing_environment ? 1 : 0

  name      = "azure.extensions"
  server_id = azurerm_postgresql_flexible_server.stress_testing[0].id
  value     = "POSTGIS,UUID-OSSP"
}

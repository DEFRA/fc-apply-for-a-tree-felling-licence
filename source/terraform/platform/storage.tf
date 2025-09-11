resource "azurerm_storage_account" "dev" {
  name                             = "devflo"
  resource_group_name              = azurerm_resource_group.fs_flov2.name
  location                         = module.shared.azure_location
  account_tier                     = "Standard"
  account_replication_type         = "GRS"
  cross_tenant_replication_enabled = true

  tags = {
    environment = "development"
  }
}

resource "azurerm_storage_container" "dev" {
  name = "devflo"
  # storage_account_name  = azurerm_storage_account.dev.name
  storage_account_id    = azurerm_storage_account.dev.id
  container_access_type = "blob"
}

resource "azurerm_storage_account" "test" {
  name                             = "testingflo"
  resource_group_name              = azurerm_resource_group.fs_flov2.name
  location                         = module.shared.azure_location
  account_tier                     = "Standard"
  account_replication_type         = "GRS"
  cross_tenant_replication_enabled = true

  tags = {
    environment = "testing"
  }
}

resource "azurerm_storage_container" "test" {
  name = "testingflo"
  #storage_account_name  = azurerm_storage_account.test.name
  storage_account_id    = azurerm_storage_account.test.id
  container_access_type = "blob"
}

resource "azurerm_storage_account" "stage" {
  name                             = "stageflo"
  resource_group_name              = azurerm_resource_group.fs_flov2.name
  location                         = module.shared.azure_location
  account_tier                     = "Standard"
  account_replication_type         = "GRS"
  cross_tenant_replication_enabled = true

  tags = {
    environment = "staging"
  }
}

resource "azurerm_storage_container" "stage" {
  name = "stageflo"
  #storage_account_name  = azurerm_storage_account.stage.name
  storage_account_id    = azurerm_storage_account.stage.id
  container_access_type = "blob"
}

resource "azurerm_storage_account" "live" {
  name                             = "liveflo"
  resource_group_name              = azurerm_resource_group.fs_flov2.name
  location                         = module.shared.azure_location
  account_tier                     = "Standard"
  account_replication_type         = "GRS"
  cross_tenant_replication_enabled = true

  tags = {
    environment = "live"
  }
}

resource "azurerm_storage_container" "live" {
  name = "liveflo"
  #storage_account_name  = azurerm_storage_account.live.name
  storage_account_id    = azurerm_storage_account.live.id
  container_access_type = "blob"
}

resource "azurerm_storage_account" "migrate" {
  name                             = "migrateflo"
  resource_group_name              = azurerm_resource_group.fs_flov2.name
  location                         = module.shared.azure_location
  account_tier                     = "Standard"
  account_replication_type         = "GRS"
  cross_tenant_replication_enabled = true

  tags = {
    environment = "migrate"
  }
}

resource "azurerm_storage_container" "migrate" {
  name = "migrateflo"
  #storage_account_name  = azurerm_storage_account.migrate.name
  storage_account_id    = azurerm_storage_account.migrate.id
  container_access_type = "blob"
}
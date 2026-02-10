output "resource_group_name" {
  value = azurerm_resource_group.fs_flov2.name
}

output "virtual_network_id" {
  value = azurerm_virtual_network.fs_flov2.id
}

output "subnet_database_id" {
  value = azurerm_subnet.database.id
}

output "azurerm_subnet_id" {
  value = azurerm_subnet.default.id
}

output "azurerm_container_registry_fs_flov2_id" {
  value = azurerm_container_registry.fs_flov2.id
}

output "virtual_network_ukw_id" {
  value = azurerm_virtual_network.fs_flov2_ukw.id
}

output "subnet_database_ukw_id" {
  value = azurerm_subnet.database_ukw.id
}

output "acr" {
  description = "ACR details"
  value = {
    id   = azurerm_container_registry.fs_flov2.id
    name = azurerm_container_registry.fs_flov2.name
  }
}

output "storage_accounts" {
  description = "Storage accounts"
  value = {
    dev = {
      id   = azurerm_storage_account.dev.id
      name = azurerm_storage_account.dev.name
    }
    test = {
      id   = azurerm_storage_account.test.id
      name = azurerm_storage_account.test.name
    }
    stage = {
      id   = azurerm_storage_account.stage.id
      name = azurerm_storage_account.stage.name
    }
    migrate = {
      id   = azurerm_storage_account.migrate.id
      name = azurerm_storage_account.migrate.name
    }
    live = {
      id   = azurerm_storage_account.live.id
      name = azurerm_storage_account.live.name
    }
  }
}

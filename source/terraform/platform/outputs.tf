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


output "stress_db_name" {
  value       = try(azurerm_postgresql_flexible_server.stress_testing[0].name, null)
  description = "Name of the stress testing PostgreSQL server"
}

output "stress_db_fqdn" {
  value       = try(azurerm_postgresql_flexible_server.stress_testing[0].fqdn, null)
  description = "FQDN of the stress testing PostgreSQL server"
}

output "stress_db_admin_user" {
  value       = try(azurerm_postgresql_flexible_server.stress_testing[0].administrator_login, null)
  description = "Admin username for the stress testing PostgreSQL server"
}

output "stress_db_private_dns_zone" {
  value       = data.azurerm_private_dns_zone.database.name
  description = "Private DNS zone used by the stress testing DB"
}

output "stress_app_node_pool" {
  value = var.enable_stress_testing_environment ? {
    name       = azurerm_kubernetes_cluster_node_pool.perf_app[0].name
    vm_size    = azurerm_kubernetes_cluster_node_pool.perf_app[0].vm_size
    node_count = azurerm_kubernetes_cluster_node_pool.perf_app[0].node_count
    labels     = azurerm_kubernetes_cluster_node_pool.perf_app[0].node_labels
    taints     = azurerm_kubernetes_cluster_node_pool.perf_app[0].node_taints
  } : null

  description = "Details of the stress testing application node pool"
}

output "stress_load_node_pool" {
  value = var.enable_stress_testing_environment ? {
    name       = azurerm_kubernetes_cluster_node_pool.perf_load[0].name
    vm_size    = azurerm_kubernetes_cluster_node_pool.perf_load[0].vm_size
    node_count = azurerm_kubernetes_cluster_node_pool.perf_load[0].node_count
    labels     = azurerm_kubernetes_cluster_node_pool.perf_load[0].node_labels
    taints     = azurerm_kubernetes_cluster_node_pool.perf_load[0].node_taints
  } : null

  description = "Details of the stress testing load generation node pool"
}

output "stress_environment_summary" {
  value = {
    enabled        = var.enable_stress_testing_environment
    db_fqdn        = try(azurerm_postgresql_flexible_server.stress_testing[0].fqdn, null)
    app_node_pool  = try(azurerm_kubernetes_cluster_node_pool.perf_app[0].name, null)
    load_node_pool = try(azurerm_kubernetes_cluster_node_pool.perf_load[0].name, null)
  }
  description = "Quick summary of stress testing components"
}

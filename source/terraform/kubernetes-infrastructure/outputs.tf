output "aks_id" {
  description = "PostgreSQL flexible servers used by FLOv2"
  value = azurerm_kubernetes_cluster.default_system.id
}

# Attach Container Registry to Cluster
resource "azurerm_role_assignment" "acrpull" {
  principal_id                     = azurerm_kubernetes_cluster.default_system.kubelet_identity[0].object_id
  role_definition_name             = "AcrPull"
  scope                            = data.terraform_remote_state.platform.outputs.azurerm_container_registry_fs_flov2_id
  
  lifecycle {
    create_before_destroy = true
  }
}
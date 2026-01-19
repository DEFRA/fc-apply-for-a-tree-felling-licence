# Existing AKS cluster created in FLOv2-kubernetes workspace
data "azurerm_kubernetes_cluster" "default_system" {
  name                = module.shared.cluster_name
  resource_group_name = module.shared.rg_name
}

# Perf App Node Pool – for flov2-perf workloads
resource "azurerm_kubernetes_cluster_node_pool" "perf_app" {
  count = var.enable_stress_testing_environment ? 1 : 0

  name                  = var.stress_app_pool_name
  kubernetes_cluster_id = data.azurerm_kubernetes_cluster.default_system.id

  vm_size         = var.stress_app_vm_size
  node_count      = var.stress_app_node_count
  zones           = var.stress_app_zones
  os_disk_type    = "Ephemeral"
  os_disk_size_gb = var.stress_app_os_disk_size_gb
  max_pods        = var.stress_app_max_pods

  node_labels = var.stress_app_node_labels
  node_taints = [
    "perf-app=true:NoSchedule"
  ]
  vnet_subnet_id = data.terraform_remote_state.platform.outputs.azurerm_subnet_id

  orchestrator_version    = "1.33.3"
  auto_scaling_enabled    = false
  node_public_ip_enabled  = false
  host_encryption_enabled = false
  fips_enabled            = false

  upgrade_settings {
    max_surge                     = "10%"
    drain_timeout_in_minutes      = 30
    node_soak_duration_in_minutes = 0
  }

  tags = local.stress_tags
}

# Perf Load Node Pool – JMeter / load generator
resource "azurerm_kubernetes_cluster_node_pool" "perf_load" {
  count = var.enable_stress_testing_environment ? 1 : 0

  name                  = var.stress_load_pool_name
  kubernetes_cluster_id = data.azurerm_kubernetes_cluster.default_system.id

  vm_size         = var.stress_load_vm_size
  node_count      = var.stress_load_node_count
  zones           = var.stress_load_zones
  os_disk_type    = "Ephemeral"
  os_disk_size_gb = var.stress_load_os_disk_size_gb
  max_pods        = var.stress_load_max_pods

  node_labels = var.stress_load_node_labels
  node_taints = [
    "perf-load=true:NoSchedule"
  ]
  vnet_subnet_id = data.terraform_remote_state.platform.outputs.azurerm_subnet_id

  orchestrator_version    = "1.33.3"
  auto_scaling_enabled    = false
  node_public_ip_enabled  = false
  host_encryption_enabled = false
  fips_enabled            = false

  upgrade_settings {
    max_surge                     = "10%"
    drain_timeout_in_minutes      = 30
    node_soak_duration_in_minutes = 0
  }

  tags = local.stress_tags
}


# Kubernetes provider
provider "kubernetes" {
  host                   = data.azurerm_kubernetes_cluster.default_system.kube_config[0].host
  client_certificate     = base64decode(data.azurerm_kubernetes_cluster.default_system.kube_config[0].client_certificate)
  client_key             = base64decode(data.azurerm_kubernetes_cluster.default_system.kube_config[0].client_key)
  cluster_ca_certificate = base64decode(data.azurerm_kubernetes_cluster.default_system.kube_config[0].cluster_ca_certificate)
}

# Kubectl provider
provider "kubectl" {
  host                   = data.azurerm_kubernetes_cluster.default_system.kube_config[0].host
  client_certificate     = base64decode(data.azurerm_kubernetes_cluster.default_system.kube_config[0].client_certificate)
  client_key             = base64decode(data.azurerm_kubernetes_cluster.default_system.kube_config[0].client_key)
  cluster_ca_certificate = base64decode(data.azurerm_kubernetes_cluster.default_system.kube_config[0].cluster_ca_certificate)
}

# Helm provider
provider "helm" {
  kubernetes = {
    host                   = data.azurerm_kubernetes_cluster.default_system.kube_config[0].host
    client_certificate     = base64decode(data.azurerm_kubernetes_cluster.default_system.kube_config[0].client_certificate)
    client_key             = base64decode(data.azurerm_kubernetes_cluster.default_system.kube_config[0].client_key)
    cluster_ca_certificate = base64decode(data.azurerm_kubernetes_cluster.default_system.kube_config[0].cluster_ca_certificate)
  }
}

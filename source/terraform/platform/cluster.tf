# Create ASK Cluster and System Node Pool
resource "azurerm_kubernetes_cluster" "default_system" {
  name                = var.cluster_name
  location            = var.azure_location
  resource_group_name = azurerm_resource_group.fs_flov2.name
  dns_prefix          = "${var.prefix}-flov2"

  # api_server_access_profile {
  #   authorized_ip_ranges = ["217.171.109.128/28"]
  # }

  node_os_upgrade_channel = "NodeImage"

  default_node_pool {
    name                         = "system"
    node_count                   = var.cluster_system_node_count
    node_labels                  = var.cluster_system_node_labels
    zones                        = var.cluster_system_zones
    vm_size                      = var.cluster_system_vm_size
    max_pods                     = var.cluster_system_max_pods
    os_disk_size_gb              = var.cluster_system_os_disk_size_gb
    os_disk_type                 = "Ephemeral"
    vnet_subnet_id               = azurerm_subnet.default.id
    orchestrator_version         = "1.32.4"
    only_critical_addons_enabled = false
    node_public_ip_enabled       = false
    host_encryption_enabled      = false
    fips_enabled                 = false
    auto_scaling_enabled         = false

    upgrade_settings {
      max_surge                     = "10%"
      drain_timeout_in_minutes      = 30
      node_soak_duration_in_minutes = 0
    }
  }

  network_profile {
    network_plugin = "azure"
    service_cidr   = "192.168.220.0/24"
    dns_service_ip = "192.168.220.10"
    load_balancer_profile {
      managed_outbound_ip_count = 1
      backend_pool_type         = "NodeIPConfiguration"
      idle_timeout_in_minutes   = 30
    }
  }

  identity {
    type = "SystemAssigned"
  }

  azure_policy_enabled             = false
  http_application_routing_enabled = false
  image_cleaner_enabled            = false
  image_cleaner_interval_hours     = 48
  local_account_disabled           = false
  oidc_issuer_enabled              = false
  open_service_mesh_enabled        = false

  tags = var.azure_tags
}

# Create Dev Node Pool
resource "azurerm_kubernetes_cluster_node_pool" "dev" {
  name                  = var.cluster_dev_name
  kubernetes_cluster_id = azurerm_kubernetes_cluster.default_system.id
  vm_size               = var.cluster_dev_vm_size
  node_count            = var.cluster_dev_node_count
  zones                 = var.cluster_dev_zones
  node_labels           = var.cluster_dev_node_labels
  os_disk_type          = "Ephemeral"
  os_disk_size_gb       = var.cluster_dev_os_disk_size_gb
  vnet_subnet_id        = azurerm_subnet.default.id

  orchestrator_version = "1.32.4"
  max_pods             = var.cluster_dev_max_pods

  auto_scaling_enabled    = false
  node_public_ip_enabled  = false
  host_encryption_enabled = false
  fips_enabled            = false

  upgrade_settings {
    max_surge                     = "10%"
    drain_timeout_in_minutes      = 30
    node_soak_duration_in_minutes = 0
  }

  tags = var.azure_tags
}

# Create Production Node Pool
resource "azurerm_kubernetes_cluster_node_pool" "production" {
  name                  = var.cluster_prod_name
  kubernetes_cluster_id = azurerm_kubernetes_cluster.default_system.id
  vm_size               = var.cluster_prod_vm_size
  node_count            = var.cluster_prod_node_count
  zones                 = var.cluster_prod_zones
  node_labels           = var.cluster_prod_node_labels
  os_disk_type          = "Ephemeral"
  os_disk_size_gb       = var.cluster_prod_os_disk_size_gb
  vnet_subnet_id        = azurerm_subnet.default.id

  orchestrator_version = "1.32.4"
  max_pods             = var.cluster_dev_max_pods

  auto_scaling_enabled    = false
  node_public_ip_enabled  = false
  host_encryption_enabled = false
  fips_enabled            = false

  upgrade_settings {
    max_surge                     = "10%"
    drain_timeout_in_minutes      = 30
    node_soak_duration_in_minutes = 0
  }

  tags = var.azure_tags
}

#Load and connect kubectl
provider "kubernetes" {
  host                   = azurerm_kubernetes_cluster.default_system.kube_config.0.host
  client_certificate     = base64decode(azurerm_kubernetes_cluster.default_system.kube_config.0.client_certificate)
  client_key             = base64decode(azurerm_kubernetes_cluster.default_system.kube_config.0.client_key)
  cluster_ca_certificate = base64decode(azurerm_kubernetes_cluster.default_system.kube_config.0.cluster_ca_certificate)
}

# Load and connect to kubectl
provider "kubectl" {
  host                   = azurerm_kubernetes_cluster.default_system.kube_config.0.host
  client_certificate     = base64decode(azurerm_kubernetes_cluster.default_system.kube_config.0.client_certificate)
  client_key             = base64decode(azurerm_kubernetes_cluster.default_system.kube_config.0.client_key)
  cluster_ca_certificate = base64decode(azurerm_kubernetes_cluster.default_system.kube_config.0.cluster_ca_certificate)
}

# Load and connect to helm
provider "helm" {
  kubernetes = {
    host                   = azurerm_kubernetes_cluster.default_system.kube_config[0].host
    client_certificate     = base64decode(azurerm_kubernetes_cluster.default_system.kube_config[0].client_certificate)
    client_key             = base64decode(azurerm_kubernetes_cluster.default_system.kube_config[0].client_key)
    cluster_ca_certificate = base64decode(azurerm_kubernetes_cluster.default_system.kube_config[0].cluster_ca_certificate)
  }
}
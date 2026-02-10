# Create ASK Cluster and System Node Pool
resource "azurerm_kubernetes_cluster" "default_system" {
  name                = module.shared.cluster_name
  location            = module.shared.azure_location
  resource_group_name = module.shared.rg_name
  dns_prefix          = "${module.shared.prefix}-flov2"

  # api_server_access_profile {
  #   authorized_ip_ranges = ["217.171.109.128/28"]
  # }

  node_os_upgrade_channel = "NodeImage"

  upgrade_override {
    force_upgrade_enabled = false
  }

  default_node_pool {
    name                         = "system"
    node_count                   = module.shared.cluster_system_node_count
    node_labels                  = module.shared.cluster_system_node_labels
    zones                        = module.shared.cluster_system_zones
    vm_size                      = module.shared.cluster_system_vm_size
    max_pods                     = module.shared.cluster_system_max_pods
    os_disk_size_gb              = module.shared.cluster_system_os_disk_size_gb
    os_disk_type                 = "Ephemeral"
    vnet_subnet_id               = data.terraform_remote_state.platform.outputs.azurerm_subnet_id
    orchestrator_version         = "1.33.3"
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

  tags = module.shared.azure_tags
}

# Create Dev Node Pool
resource "azurerm_kubernetes_cluster_node_pool" "dev" {
  name                  = module.shared.cluster_dev_name
  kubernetes_cluster_id = azurerm_kubernetes_cluster.default_system.id
  vm_size               = module.shared.cluster_dev_vm_size
  node_count            = module.shared.cluster_dev_node_count
  zones                 = module.shared.cluster_dev_zones
  node_labels           = module.shared.cluster_dev_node_labels
  os_disk_type          = "Ephemeral"
  os_disk_size_gb       = module.shared.cluster_dev_os_disk_size_gb
  vnet_subnet_id        = data.terraform_remote_state.platform.outputs.azurerm_subnet_id

  orchestrator_version = "1.33.3"
  max_pods             = module.shared.cluster_dev_max_pods

  auto_scaling_enabled    = false
  node_public_ip_enabled  = false
  host_encryption_enabled = false
  fips_enabled            = false

  upgrade_settings {
    max_surge                     = "10%"
    drain_timeout_in_minutes      = 30
    node_soak_duration_in_minutes = 0
  }

  tags = module.shared.azure_tags
}

# Create Production Node Pool
resource "azurerm_kubernetes_cluster_node_pool" "production" {
  name                  = module.shared.cluster_prod_name
  kubernetes_cluster_id = azurerm_kubernetes_cluster.default_system.id
  vm_size               = module.shared.cluster_prod_vm_size
  node_count            = module.shared.cluster_prod_node_count
  zones                 = module.shared.cluster_prod_zones
  node_labels           = module.shared.cluster_prod_node_labels
  os_disk_type          = "Ephemeral"
  os_disk_size_gb       = module.shared.cluster_prod_os_disk_size_gb
  vnet_subnet_id        = data.terraform_remote_state.platform.outputs.azurerm_subnet_id

  orchestrator_version = "1.33.3"
  max_pods             = module.shared.cluster_dev_max_pods

  auto_scaling_enabled    = false
  node_public_ip_enabled  = false
  host_encryption_enabled = false
  fips_enabled            = false

  upgrade_settings {
    max_surge                     = "10%"
    drain_timeout_in_minutes      = 30
    node_soak_duration_in_minutes = 0
  }

  tags = module.shared.azure_tags
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
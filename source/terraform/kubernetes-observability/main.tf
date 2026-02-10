terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 3.9.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = ">= 2.11.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = ">= 2.6.0"
    }
    kubectl = {
      source  = "alekc/kubectl"
      version = "~> 2.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = ">= 2.7.0"
    }
    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = "~> 4.40"
    }
  }

  cloud {
    organization = "Quicksilva"

    workspaces {
      name = "FLOv2-kubernetes-observerability"
    }
  }
}

provider "cloudflare" {
  api_token = module.shared.flov2_cloudflare_zone_settings_api
}

# Configure the Microsoft Azure Provider
provider "azurerm" {
  alias = "cli"

  features {}
  use_cli         = true
  subscription_id = "305d2b06-c358-4814-9905-ff2d46fadfbe"
}

provider "azurerm" {
  #alias = "flov2"
  features {}

  client_id       = module.shared.tf_client_id
  client_secret   = module.shared.azure_sp_token
  subscription_id = module.shared.tf_sub_id
  tenant_id       = module.shared.tf_tenant_id
}

provider "azurerm" {
  alias = "managedservices"
  features {}

  client_id       = module.shared.tf_client_id
  client_secret   = module.shared.azure_sp_token
  subscription_id = module.shared.tf_m_sub_id
  tenant_id       = module.shared.tf_tenant_id
}

module "shared" {
  source = "../modules/shared"

  providers = {
    azurerm = azurerm.cli
  }
}

module "kube_observability" {
  source = "../modules/kubernetes/observability"

  providers = {
    azurerm = azurerm.cli
  }
}

data "terraform_remote_state" "platform" {
  backend = "remote"

  config = {
    organization = "Quicksilva"
    workspaces = {
      name = "FLOv2-Platform"
    }
  }
}

data "terraform_remote_state" "kubernetes_infra" {
  backend = "remote"

  config = {
    organization = "Quicksilva"
    workspaces = {
      name = "FLOv2-kubernetes-infra"
    }
  }
}

data "azurerm_kubernetes_cluster" "aks" {
  name                = module.shared.cluster_name
  resource_group_name = module.shared.rg_name
}

provider "kubernetes" {
  host                   = data.azurerm_kubernetes_cluster.aks.kube_config[0].host
  client_certificate     = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config[0].client_certificate)
  client_key             = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config[0].client_key)
  cluster_ca_certificate = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config[0].cluster_ca_certificate)
}

provider "helm" {
  kubernetes = {
    host                   = data.azurerm_kubernetes_cluster.aks.kube_config[0].host
    client_certificate     = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config[0].client_certificate)
    client_key             = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config[0].client_key)
    cluster_ca_certificate = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config[0].cluster_ca_certificate)
  }
}

provider "kubectl" {
  host                   = data.azurerm_kubernetes_cluster.aks.kube_config[0].host
  client_certificate     = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config[0].client_certificate)
  client_key             = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config[0].client_key)
  cluster_ca_certificate = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config[0].cluster_ca_certificate)
  load_config_file       = false
}
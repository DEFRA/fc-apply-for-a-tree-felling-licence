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
      name = "FLOv2-kubernetes"
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

provider "azuread" {
  alias     = "dev"
  tenant_id = "2a89548a-7e9e-4ea6-99b6-74b88fd981c2"
}

provider "azuread" {
  alias     = "test"
  tenant_id = "2a89548a-7e9e-4ea6-99b6-74b88fd981c2"
}

provider "azuread" {
  alias     = "staging"
  tenant_id = "2a89548a-7e9e-4ea6-99b6-74b88fd981c2"
}

provider "azuread" {
  alias     = "live"
  tenant_id = "2a89548a-7e9e-4ea6-99b6-74b88fd981c2"
}

provider "azuread" {
  alias     = "migrate"
  tenant_id = "2a89548a-7e9e-4ea6-99b6-74b88fd981c2"
}

module "shared" {
  source = "../modules/shared"

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

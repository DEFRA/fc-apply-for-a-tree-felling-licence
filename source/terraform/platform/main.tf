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
      source  = "gavinbunney/kubectl"
      version = ">= 1.14.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = ">= 2.7.0"
    }
  }

  cloud {
    organization = "Quicksilva"

    workspaces {
      name = "FLOv2"
    }
  }
}

# Configure the Microsoft Azure Provider
provider "azurerm" {
  features {}

  client_id       = "b4330e38-3476-48a6-8ae1-217f9c8f9ba3"
  client_secret   = var.azure_token
  subscription_id = "305d2b06-c358-4814-9905-ff2d46fadfbe"
  tenant_id       = "2a89548a-7e9e-4ea6-99b6-74b88fd981c2"
}

provider "azurerm" {
  alias = "managedservices"
  features {}

  client_id       = "b4330e38-3476-48a6-8ae1-217f9c8f9ba3"
  client_secret   = var.azure_token
  subscription_id = "4ecd58f4-0e91-4745-85d0-e07245b52245"
  tenant_id       = "2a89548a-7e9e-4ea6-99b6-74b88fd981c2"
}

provider "azurerm" {
  alias = "cli"

  features {}

  subscription_id = "305d2b06-c358-4814-9905-ff2d46fadfbe"
}

# provider "azuread" {
#   alias     = "dev"
#   tenant_id = azurerm_aadb2c_directory.dev.tenant_id
# }

# provider "azuread" {
#   alias     = "test"
#   tenant_id = azurerm_aadb2c_directory.test.tenant_id
# }

# provider "azuread" {
#   alias     = "staging"
#   tenant_id = azurerm_aadb2c_directory.staging.tenant_id
# }

# provider "azuread" {
#   alias     = "live"
#   tenant_id = azurerm_aadb2c_directory.live.tenant_id
# }

# provider "azuread" {
#   alias     = "migrate"
#   tenant_id = azurerm_aadb2c_directory.migrate.tenant_id
# }


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

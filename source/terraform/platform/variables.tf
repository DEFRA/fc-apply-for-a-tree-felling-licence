# General Variables
variable "prefix" {
  type        = string
  description = "Prefix to use across resources"
  default     = "fs"
}

# Azure Variables
variable "azure_location" {
  type        = string
  description = "Azure region to store azure resoruces"
  default     = "uksouth"

}

variable "azure_tags" {
  type        = map(string)
  description = "Default tags to be applied to azure resources"
  default = {
    Source      = "Terraform"
    Release     = "FLOv2"
    Application = "FLO"
    Team        = "Platform"
    CostCenter  = "FLO"
    Customer    = "Forest Services"
  }
}

# Cluster Variables
variable "cluster_name" {
  type        = string
  description = "Cluster name that will be created"
  default     = "fs_flov2"
}

# Cluster System Variables
variable "cluster_system_vm_size" {
  type        = string
  description = "The VM Size to be used for the system node pool"
  default     = "Standard_D2ds_v5"
}

variable "cluster_system_node_count" {
  type        = string
  description = "The amount of nodes for the system node pool"
  default     = "2"
}

variable "cluster_system_zones" {
  type        = list(string)
  description = "The availablity zones to use for the system node pool"
  default     = ["1", "2"]
}

variable "cluster_system_max_pods" {
  type        = string
  description = "The maximum number of pods that can run on each agent"
  default     = 110
}

variable "cluster_system_os_disk_size_gb" {
  type        = string
  description = "The size of the OS Disk which should be used for each agent in the System Node Pool"
  default     = "75"
}

variable "cluster_system_node_labels" {
  type        = map(string)
  description = "Kubernetes labels which should be applied to nodes in the System node pool"
  default = {
    environment = "system"
  }
}

# Cluster Dev Variables

variable "cluster_dev_name" {
  type        = string
  description = "The name for the dev node pool"
  default     = "development"
}

variable "cluster_dev_node_count" {
  type        = string
  description = "The amount of nodes for the dev node pool"
  default     = "2"
}

variable "cluster_dev_zones" {
  type        = list(string)
  description = "The availablity zones to use for the dev node pool"
  default     = ["1", "2"]
}

variable "cluster_dev_vm_size" {
  type        = string
  description = "The VM Size to be used for the dev node pool"
  default     = "Standard_B4ms"
}

variable "cluster_dev_max_pods" {
  type        = string
  description = "The maximum number of pods that can run on each agent"
  default     = 30
}

variable "cluster_dev_os_disk_size_gb" {
  type        = string
  description = "The size of the OS Disk which should be used for each agent in the dev Node Pool"
  default     = "32"
}

variable "cluster_dev_node_labels" {
  type        = map(string)
  description = "Kubernetes labels which should be applied to nodes in the dev node pool"
  default = {
    environment = "development"
  }
}

# Cluster Productions Variables

variable "cluster_prod_name" {
  type        = string
  description = "The name for the prod node pool"
  default     = "production"
}

variable "cluster_prod_node_count" {
  type        = string
  description = "The amount of nodes for the prod node pool"
  default     = "3"
}

variable "cluster_prod_zones" {
  type        = list(string)
  description = "The availablity zones to use for the prod node pool"
  default     = ["1", "2", "3"]
}

variable "cluster_prod_vm_size" {
  type        = string
  description = "The VM Size to be used for the prod node pool"
  default     = "Standard_D4ds_v5"
}

variable "cluster_prod_max_pods" {
  type        = string
  description = "The maximum number of pods that can run on each agent"
  default     = 30
}

variable "cluster_prod_os_disk_size_gb" {
  type        = string
  description = "The size of the OS Disk which should be used for each agent in the prod Node Pool"
  default     = "150"
}

variable "cluster_prod_node_labels" {
  type        = map(string)
  description = "Kubernetes labels which should be applied to nodes in the prod node pool"
  default = {
    environment = "production"
  }
}

#Traefik Variables
variable "traefik_chart" {
  type        = string
  description = "Traefik Helm chart name."
  default     = "traefik"
}

variable "traefik_repository" {
  type        = string
  description = "Traefik Helm repository name."
  default     = "https://helm.traefik.io/traefik"
}

#postfix Variables
variable "postfix_chart" {
  type        = string
  description = "postfix Helm chart name."
  default     = "mail"
}

variable "postfix_repository" {
  type        = string
  description = "postfix Helm repository name."
  default     = "https://bokysan.github.io/docker-postfix/"
}

#mailhogl Variables
variable "mailhog_chart" {
  type        = string
  description = "mailhog Helm chart name."
  default     = "mailhog"
}

variable "mailhog_repository" {
  type        = string
  description = "mailhog Helm repository name."
  default     = "https://codecentric.github.io/helm-charts"
}

#PGAdmin Variables
variable "pgadmin_chart" {
  type        = string
  description = "PGAdmin Helm chart name."
  default     = "pgadmin4"
}

variable "pgadmin_repository" {
  type        = string
  description = "PGAdmin Helm repository name."
  default     = "https://helm.runix.net"
}

#Cert Manager Variables

variable "cert_manager_chart" {
  type        = string
  description = "Cert Manager Helm name."
  default     = "cert-manager"
}

variable "cert_manager_repository" {
  type        = string
  description = "Cert Manager Helm repository name."
  default     = "https://charts.jetstack.io"
}

# Cloudflare Variables
variable "cloudflare_email" {
  type        = string
  description = "Cloudflare email account"
  default     = "accounts@qxlva.com"
}

#External DNS Variables
variable "externaldns_chart" {
  type        = string
  description = "External DNS Helm chart name."
  default     = "external-dns"
}

variable "externaldnsk_repository" {
  type        = string
  description = "External DNS Helm repository name."
  default     = "https://charts.bitnami.com/bitnami"
}

#Prometheus Stack Variables
variable "prometheus_chart" {
  type        = string
  description = "Prometheus Chart Name"
  default     = "kube-prometheus-stack"
}

variable "prometheus_repository" {
  type        = string
  description = "Prometheus Helm Repository Name"
  default     = "https://prometheus-community.github.io/helm-charts"
}

variable "prometheus_version" {
  type        = string
  description = "Prometheus chart version"
  default     = "27.2.1"
}

# Metric Server Veriables
variable "metrics_server_chart" {
  type        = string
  description = "Metric Server Chart name"
  default     = "metrics-server"
}

variable "metrics_server_repository" {
  type        = string
  description = "Metric Server Helm Repository Name"
  default     = "https://charts.bitnami.com/bitnami"
}

#AzureAD Auth Variables
variable "azuread_chart" {
  type        = string
  description = "AzureAD Auth Helm chart name."
  default     = "azuread-auth"
}

variable "azuread_repository" {
  type        = string
  description = "AzureAD Auth Helm repository name."
  default     = "https://qxprod.azurecr.io/helm/v1/repo/"
}

variable "azuread_username" {
  type        = string
  description = "AzureAD Helm repository username."
  default     = "qxprod"
}
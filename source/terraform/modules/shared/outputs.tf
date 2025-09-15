# Secrets
output "tls_crt" {
  value     = data.azurerm_key_vault_secret.tls_crt.value
  sensitive = true
}

output "tls_key" {
  value     = data.azurerm_key_vault_secret.tls_key.value
  sensitive = true
}

output "cloudflare_api_access_token" {
  value     = data.azurerm_key_vault_secret.cloudflare_api_access_token.value
  sensitive = true
}

output "cloudflare_token" {
  value     = data.azurerm_key_vault_secret.cloudflare_token.value
  sensitive = true
}

output "traefik_dashboard_users" {
  value     = data.azurerm_key_vault_secret.traefik_dashboard_users.value
  sensitive = true
}

output "tf_client_id" {
  value     = data.azurerm_key_vault_secret.tf_client_id.value
  sensitive = true
}

output "tc_client_id" {
  value     = data.azurerm_key_vault_secret.tc_client_id.value
  sensitive = true
}

output "azure_sp_token" {
  value     = data.azurerm_key_vault_secret.azure_sp_token.value
  sensitive = true
}

output "tf_tenant_id" {
  value     = data.azurerm_key_vault_secret.tf_tenant_id.value
  sensitive = true
}

output "tf_sub_id" {
  value     = data.azurerm_key_vault_secret.tf_sub_id.value
  sensitive = true
}

output "tf_m_sub_id" {
  value     = data.azurerm_key_vault_secret.tf_m_sub_id.value
  sensitive = true
}

output "pg_staging_pass" {
  value     = data.azurerm_key_vault_secret.pg_staging_pass.value
  sensitive = true
}

output "pg_migrate_pass" {
  value     = data.azurerm_key_vault_secret.pg_migrate_pass.value
  sensitive = true
}

output "pg_live_pass" {
  value     = data.azurerm_key_vault_secret.pg_live_pass.value
  sensitive = true
}

# Variables
output "prefix" {
  value = var.prefix
}

output "azure_location" {
  value = var.azure_location
}

output "azure_tags" {
  value = var.azure_tags
}

output "rg_name" {
  value = var.rg_name
}

output "cluster_name" {
  value = var.cluster_name
}

output "cluster_system_vm_size" {
  value = var.cluster_system_vm_size
}

output "cluster_system_node_count" {
  value = var.cluster_system_node_count
}

output "cluster_system_zones" {
  value = var.cluster_system_zones
}

output "cluster_system_max_pods" {
  value = var.cluster_system_max_pods
}

output "cluster_system_os_disk_size_gb" {
  value = var.cluster_system_os_disk_size_gb
}

output "cluster_system_node_labels" {
  value = var.cluster_system_node_labels
}

output "cluster_dev_name" {
  value = var.cluster_dev_name
}

output "cluster_dev_node_count" {
  value = var.cluster_dev_node_count
}

output "cluster_dev_zones" {
  value = var.cluster_dev_zones
}

output "cluster_dev_vm_size" {
  value = var.cluster_dev_vm_size
}

output "cluster_dev_max_pods" {
  value = var.cluster_dev_max_pods
}

output "cluster_dev_os_disk_size_gb" {
  value = var.cluster_dev_os_disk_size_gb
}

output "cluster_dev_node_labels" {
  value = var.cluster_dev_node_labels
}

output "cluster_prod_name" {
  value = var.cluster_prod_name
}

output "cluster_prod_node_count" {
  value = var.cluster_prod_node_count
}

output "cluster_prod_zones" {
  value = var.cluster_prod_zones
}

output "cluster_prod_vm_size" {
  value = var.cluster_prod_vm_size
}

output "cluster_prod_max_pods" {
  value = var.cluster_prod_max_pods
}

output "cluster_prod_os_disk_size_gb" {
  value = var.cluster_prod_os_disk_size_gb
}

output "cluster_prod_node_labels" {
  value = var.cluster_prod_node_labels
}

output "traefik_chart" {
  value = var.traefik_chart
}

output "traefik_repository" {
  value = var.traefik_repository
}

output "postfix_chart" {
  value = var.postfix_chart
}

output "postfix_repository" {
  value = var.postfix_repository
}

output "mailhog_chart" {
  value = var.mailhog_chart
}

output "mailhog_repository" {
  value = var.mailhog_repository
}

output "pgadmin_chart" {
  value = var.pgadmin_chart
}

output "pgadmin_repository" {
  value = var.pgadmin_repository
}

output "cert_manager_chart" {
  value = var.cert_manager_chart
}

output "cert_manager_repository" {
  value = var.cert_manager_repository
}

output "cloudflare_email" {
  value = var.cloudflare_email
}

output "externaldns_chart" {
  value = var.externaldns_chart
}

output "externaldnsk_repository" {
  value = var.externaldnsk_repository
}

output "prometheus_chart" {
  value = var.prometheus_chart
}

output "prometheus_repository" {
  value = var.prometheus_repository
}

output "prometheus_version" {
  value = var.prometheus_version
}

output "metrics_server_chart" {
  value = var.metrics_server_chart
}

output "metrics_server_repository" {
  value = var.metrics_server_repository
}

output "azuread_chart" {
  value = var.azuread_chart
}

output "azuread_repository" {
  value = var.azuread_repository
}

output "azuread_username" {
  value = var.azuread_username
}
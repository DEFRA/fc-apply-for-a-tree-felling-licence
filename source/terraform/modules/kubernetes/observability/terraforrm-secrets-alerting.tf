
# Keyvault
data "azurerm_key_vault" "flov2_kv" {
  name                = "forestry-uksouth-kv"
  resource_group_name = "fs_flov2"
}

# DEV API Key
data "azurerm_key_vault_secret" "alerting_DEV_API_KEY" {
  name         = "alerting-DEV-API-KEY"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "alerting_DEV_API_KEY" {
  value     = data.azurerm_key_vault_secret.alerting_DEV_API_KEY.value
  sensitive = true
}

# PREPROD API Key
data "azurerm_key_vault_secret" "alerting_PREPROD_API_KEY" {
  name         = "alerting-PREPROD-API-KEY"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "alerting_PREPROD_API_KEY" {
  value     = data.azurerm_key_vault_secret.alerting_PREPROD_API_KEY.value
  sensitive = true
}

# Prod API Key
data "azurerm_key_vault_secret" "alerting_PROD_API_KEY" {
  name         = "alerting-PROD-API-KEY"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "alerting_PROD_API_KEY" {
  value     = data.azurerm_key_vault_secret.alerting_PROD_API_KEY.value
  sensitive = true
}

# INFRA API Key
data "azurerm_key_vault_secret" "alerting_INFRA_API_KEY" {
  name         = "alerting-INFRA-API-KEY"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "alerting_INFRA_API_KEY" {
  value     = data.azurerm_key_vault_secret.alerting_INFRA_API_KEY.value
  sensitive = true
}

# alerting Jira USERNAME
data "azurerm_key_vault_secret" "alerting_jira_USERNAME" {
  name         = "alerting-jira-USERNAME"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "alerting_jira_USERNAME" {
  value     = data.azurerm_key_vault_secret.alerting_jira_USERNAME.value
  sensitive = true
}

# alerting Jira API Token
data "azurerm_key_vault_secret" "alerting_jira_API_TOKEN" {
  name         = "alerting-jira-API-TOKEN"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "alerting_jira_API_TOKEN" {
  value     = data.azurerm_key_vault_secret.alerting_jira_API_TOKEN.value
  sensitive = true
}

# alerting grafana password
data "azurerm_key_vault_secret" "alerting_grafana_password" {
  provider     = azurerm
  name         = "alerting-grafana-password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "alerting_grafana_password" {
  value     = data.azurerm_key_vault_secret.alerting_grafana_password.value
  sensitive = true
}


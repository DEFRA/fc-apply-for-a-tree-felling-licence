# Azure AD
data "azurerm_key_vault_secret" "live_azuread_tenant_id" {
  name         = "live-azuread-tenant-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_azuread_tenant_id" {
  value     = data.azurerm_key_vault_secret.live_azuread_tenant_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "live_azuread_client_id" {
  name         = "live-azuread-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_azuread_client_id" {
  value     = data.azurerm_key_vault_secret.live_azuread_client_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "live_azuread_client_secret" {
  name         = "live-azuread-client-secret"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_azuread_client_secret" {
  value     = data.azurerm_key_vault_secret.live_azuread_client_secret.value
  sensitive = true
}

# Storage / Blob
data "azurerm_key_vault_secret" "live_blob_account_key" {
  name         = "live-blob-AccountKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_blob_account_key" {
  value     = data.azurerm_key_vault_secret.live_blob_account_key.value
  sensitive = true
}

# Cron jobs
data "azurerm_key_vault_secret" "live_cronjobs_api_key" {
  name         = "live-cronJobs-apiKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_cronjobs_api_key" {
  value     = data.azurerm_key_vault_secret.live_cronjobs_api_key.value
  sensitive = true
}

# RabbitMQ
data "azurerm_key_vault_secret" "live_rabbitmq_username" {
  name         = "live-rabbitmq-username"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_rabbitmq_username" {
  value     = data.azurerm_key_vault_secret.live_rabbitmq_username.value
  sensitive = true
}

data "azurerm_key_vault_secret" "live_rabbitmq_password" {
  name         = "live-rabbitmq-password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_rabbitmq_password" {
  value     = data.azurerm_key_vault_secret.live_rabbitmq_password.value
  sensitive = true
}

data "azurerm_key_vault_secret" "live_rabbitmq_erlang_cookie" {
  name         = "live-rabbitmq-erlang-cookie"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_rabbitmq_erlang_cookie" {
  value     = data.azurerm_key_vault_secret.live_rabbitmq_erlang_cookie.value
  sensitive = true
}

# OneLogin
data "azurerm_key_vault_secret" "live_onelogin_client_id" {
  name         = "live-oneLogin-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_onelogin_client_id" {
  value     = data.azurerm_key_vault_secret.live_onelogin_client_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "live_onelogin_private_key" {
  name         = "live-onelogin-private-key"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_onelogin_private_key" {
  value     = data.azurerm_key_vault_secret.live_onelogin_private_key.value
  sensitive = true
}

# govuk notify
data "azurerm_key_vault_secret" "live_govuk_notify_apikey" {
  name         = "live-govuk-notify-apikey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "live_govuk_notify_apikey" {
  value     = data.azurerm_key_vault_secret.live_govuk_notify_apikey.value
  sensitive = true
}
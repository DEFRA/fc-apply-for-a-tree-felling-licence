# Azure AD
data "azurerm_key_vault_secret" "migrate_azuread_tenant_id" {
  name         = "migrate-azuread-tenant-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_azuread_tenant_id" {
  value     = data.azurerm_key_vault_secret.migrate_azuread_tenant_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "migrate_azuread_client_id" {
  name         = "migrate-azuread-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_azuread_client_id" {
  value     = data.azurerm_key_vault_secret.migrate_azuread_client_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "migrate_azuread_client_secret" {
  name         = "migrate-azuread-client-secret"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_azuread_client_secret" {
  value     = data.azurerm_key_vault_secret.migrate_azuread_client_secret.value
  sensitive = true
}

# Storage / Blob
data "azurerm_key_vault_secret" "migrate_blob_account_key" {
  name         = "migrate-blob-AccountKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_blob_account_key" {
  value     = data.azurerm_key_vault_secret.migrate_blob_account_key.value
  sensitive = true
}

# Cron jobs
data "azurerm_key_vault_secret" "migrate_cronjobs_api_key" {
  name         = "migrate-cronJobs-apiKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_cronjobs_api_key" {
  value     = data.azurerm_key_vault_secret.migrate_cronjobs_api_key.value
  sensitive = true
}

# RabbitMQ
data "azurerm_key_vault_secret" "migrate_rabbitmq_username" {
  name         = "migrate-rabbitmq-username"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_rabbitmq_username" {
  value     = data.azurerm_key_vault_secret.migrate_rabbitmq_username.value
  sensitive = true
}

data "azurerm_key_vault_secret" "migrate_rabbitmq_password" {
  name         = "migrate-rabbitmq-password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_rabbitmq_password" {
  value     = data.azurerm_key_vault_secret.migrate_rabbitmq_password.value
  sensitive = true
}

data "azurerm_key_vault_secret" "migrate_rabbitmq_erlang_cookie" {
  name         = "migrate-rabbitmq-erlang-cookie"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_rabbitmq_erlang_cookie" {
  value     = data.azurerm_key_vault_secret.migrate_rabbitmq_erlang_cookie.value
  sensitive = true
}

# OneLogin
data "azurerm_key_vault_secret" "migrate_onelogin_client_id" {
  name         = "migrate-oneLogin-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_onelogin_client_id" {
  value     = data.azurerm_key_vault_secret.migrate_onelogin_client_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "migrate_onelogin_private_key" {
  name         = "migrate-onelogin-private-key"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_onelogin_private_key" {
  value     = data.azurerm_key_vault_secret.migrate_onelogin_private_key.value
  sensitive = true
}

# govuk notify
data "azurerm_key_vault_secret" "migrate_govuk_notify_apikey" {
  name         = "migrate-govuk-notify-apikey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "migrate_govuk_notify_apikey" {
  value     = data.azurerm_key_vault_secret.migrate_govuk_notify_apikey.value
  sensitive = true
}
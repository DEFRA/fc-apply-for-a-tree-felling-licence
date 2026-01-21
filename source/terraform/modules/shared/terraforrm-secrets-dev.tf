# Database
data "azurerm_key_vault_secret" "dev_db_password" {
  provider     = azurerm
  name         = "dev-db-password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_db_password" {
  value     = data.azurerm_key_vault_secret.dev_db_password.value
  sensitive = true
}

data "azurerm_key_vault_secret" "dev_db_postgresPassword" {
  provider     = azurerm
  name         = "dev-db-postgresPassword"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_db_postgresPassword" {
  value     = data.azurerm_key_vault_secret.dev_db_postgresPassword.value
  sensitive = true
}

# Azure AD
data "azurerm_key_vault_secret" "dev_azuread_tenant_id" {
  name         = "dev-azuread-tenant-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_azuread_tenant_id" {
  value     = data.azurerm_key_vault_secret.dev_azuread_tenant_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "dev_azuread_client_id" {
  name         = "dev-azuread-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_azuread_client_id" {
  value     = data.azurerm_key_vault_secret.dev_azuread_client_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "dev_azuread_client_secret" {
  name         = "dev-azuread-client-secret"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_azuread_client_secret" {
  value     = data.azurerm_key_vault_secret.dev_azuread_client_secret.value
  sensitive = true
}

# Storage / Blob
data "azurerm_key_vault_secret" "dev_blob_account_key" {
  name         = "dev-blob-AccountKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_blob_account_key" {
  value     = data.azurerm_key_vault_secret.dev_blob_account_key.value
  sensitive = true
}

# Cron jobs
data "azurerm_key_vault_secret" "dev_cronjobs_api_key" {
  name         = "dev-cronJobs-apiKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_cronjobs_api_key" {
  value     = data.azurerm_key_vault_secret.dev_cronjobs_api_key.value
  sensitive = true
}

# RabbitMQ
data "azurerm_key_vault_secret" "dev_rabbitmq_username" {
  name         = "dev-rabbitmq-username"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_rabbitmq_username" {
  value     = data.azurerm_key_vault_secret.dev_rabbitmq_username.value
  sensitive = true
}

data "azurerm_key_vault_secret" "dev_rabbitmq_password" {
  name         = "dev-rabbitmq-password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_rabbitmq_password" {
  value     = data.azurerm_key_vault_secret.dev_rabbitmq_password.value
  sensitive = true
}

data "azurerm_key_vault_secret" "dev_rabbitmq_erlang_cookie" {
  name         = "dev-rabbitmq-erlang-cookie"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_rabbitmq_erlang_cookie" {
  value     = data.azurerm_key_vault_secret.dev_rabbitmq_erlang_cookie.value
  sensitive = true
}

# OneLogin
data "azurerm_key_vault_secret" "dev_onelogin_client_id" {
  name         = "dev-oneLogin-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_onelogin_client_id" {
  value     = data.azurerm_key_vault_secret.dev_onelogin_client_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "dev_onelogin_private_key" {
  name         = "dev-onelogin-private-key"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_onelogin_private_key" {
  value     = data.azurerm_key_vault_secret.dev_onelogin_private_key.value
  sensitive = true
}

# govuk notify
data "azurerm_key_vault_secret" "dev_govuk_notify_apikey" {
  name         = "dev-govuk-notify-apikey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "dev_govuk_notify_apikey" {
  value     = data.azurerm_key_vault_secret.dev_govuk_notify_apikey.value
  sensitive = true
}
# Database
data "azurerm_key_vault_secret" "test_db_password" {
  provider     = azurerm
  name         = "test-db-password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_db_password" {
  value     = data.azurerm_key_vault_secret.test_db_password.value
  sensitive = true
}

data "azurerm_key_vault_secret" "test_db_postgresPassword" {
  provider     = azurerm
  name         = "test-db-postgresPassword"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_db_postgresPassword" {
  value     = data.azurerm_key_vault_secret.test_db_postgresPassword.value
  sensitive = true
}

# Azure AD
data "azurerm_key_vault_secret" "test_azuread_tenant_id" {
  name         = "test-azuread-tenant-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_azuread_tenant_id" {
  value     = data.azurerm_key_vault_secret.test_azuread_tenant_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "test_azuread_client_id" {
  name         = "test-azuread-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_azuread_client_id" {
  value     = data.azurerm_key_vault_secret.test_azuread_client_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "test_azuread_client_secret" {
  name         = "test-azuread-client-secret"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_azuread_client_secret" {
  value     = data.azurerm_key_vault_secret.test_azuread_client_secret.value
  sensitive = true
}

# Storage / Blob
data "azurerm_key_vault_secret" "test_blob_account_key" {
  name         = "test-blob-AccountKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_blob_account_key" {
  value     = data.azurerm_key_vault_secret.test_blob_account_key.value
  sensitive = true
}

# Cron jobs
data "azurerm_key_vault_secret" "test_cronjobs_api_key" {
  name         = "test-cronJobs-apiKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_cronjobs_api_key" {
  value     = data.azurerm_key_vault_secret.test_cronjobs_api_key.value
  sensitive = true
}

# RabbitMQ
data "azurerm_key_vault_secret" "test_rabbitmq_username" {
  name         = "test-rabbitmq-username"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_rabbitmq_username" {
  value     = data.azurerm_key_vault_secret.test_rabbitmq_username.value
  sensitive = true
}

data "azurerm_key_vault_secret" "test_rabbitmq_password" {
  name         = "test-rabbitmq-password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_rabbitmq_password" {
  value     = data.azurerm_key_vault_secret.test_rabbitmq_password.value
  sensitive = true
}

data "azurerm_key_vault_secret" "test_rabbitmq_erlang_cookie" {
  name         = "test-rabbitmq-erlang-cookie"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_rabbitmq_erlang_cookie" {
  value     = data.azurerm_key_vault_secret.test_rabbitmq_erlang_cookie.value
  sensitive = true
}

# OneLogin
data "azurerm_key_vault_secret" "test_onelogin_client_id" {
  name         = "test-oneLogin-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_onelogin_client_id" {
  value     = data.azurerm_key_vault_secret.test_onelogin_client_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "test_onelogin_private_key" {
  name         = "test-onelogin-private-key"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_onelogin_private_key" {
  value     = data.azurerm_key_vault_secret.test_onelogin_private_key.value
  sensitive = true
}

# govuk notify
data "azurerm_key_vault_secret" "test_govuk_notify_apikey" {
  name         = "test-govuk-notify-apikey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "test_govuk_notify_apikey" {
  value     = data.azurerm_key_vault_secret.test_govuk_notify_apikey.value
  sensitive = true
}
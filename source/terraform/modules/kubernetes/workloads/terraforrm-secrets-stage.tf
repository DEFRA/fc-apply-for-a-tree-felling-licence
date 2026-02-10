# Passwords for stage environment
# Azure AD
data "azurerm_key_vault_secret" "stage_azuread_tenant_id" {
  name         = "stage-azuread-tenant-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_azuread_tenant_id" {
  value     = data.azurerm_key_vault_secret.stage_azuread_tenant_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "stage_azuread_client_id" {
  name         = "stage-azuread-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_azuread_client_id" {
  value     = data.azurerm_key_vault_secret.stage_azuread_client_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "stage_azuread_client_secret" {
  name         = "stage-azuread-client-secret"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_azuread_client_secret" {
  value     = data.azurerm_key_vault_secret.stage_azuread_client_secret.value
  sensitive = true
}

# Storage / Blob
data "azurerm_key_vault_secret" "stage_blob_account_key" {
  name         = "stage-blob-AccountKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_blob_account_key" {
  value     = data.azurerm_key_vault_secret.stage_blob_account_key.value
  sensitive = true
}

# Cron jobs
data "azurerm_key_vault_secret" "stage_cronjobs_api_key" {
  name         = "stage-cronJobs-apiKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_cronjobs_api_key" {
  value     = data.azurerm_key_vault_secret.stage_cronjobs_api_key.value
  sensitive = true
}

# RabbitMQ
data "azurerm_key_vault_secret" "stage_rabbitmq_username" {
  name         = "stage-rabbitmq-username"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_rabbitmq_username" {
  value     = data.azurerm_key_vault_secret.stage_rabbitmq_username.value
  sensitive = true
}

data "azurerm_key_vault_secret" "stage_rabbitmq_password" {
  name         = "stage-rabbitmq-password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_rabbitmq_password" {
  value     = data.azurerm_key_vault_secret.stage_rabbitmq_password.value
  sensitive = true
}

data "azurerm_key_vault_secret" "stage_rabbitmq_erlang_cookie" {
  name         = "stage-rabbitmq-erlang-cookie"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_rabbitmq_erlang_cookie" {
  value     = data.azurerm_key_vault_secret.stage_rabbitmq_erlang_cookie.value
  sensitive = true
}

# OneLogin
data "azurerm_key_vault_secret" "stage_onelogin_client_id" {
  name         = "stage-oneLogin-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_onelogin_client_id" {
  value     = data.azurerm_key_vault_secret.stage_onelogin_client_id.value
  sensitive = true
}

data "azurerm_key_vault_secret" "stage_onelogin_private_key" {
  name         = "stage-onelogin-private-key"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_onelogin_private_key" {
  value     = data.azurerm_key_vault_secret.stage_onelogin_private_key.value
  sensitive = true
}

# govuk notify
data "azurerm_key_vault_secret" "stage_govuk_notify_apikey" {
  name         = "stage-govuk-notify-apikey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "stage_govuk_notify_apikey" {
  value     = data.azurerm_key_vault_secret.stage_govuk_notify_apikey.value
  sensitive = true
}
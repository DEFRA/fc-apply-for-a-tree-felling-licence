
# Get harris key Vault
data "azurerm_key_vault" "flov2_kv" {
  provider            = azurerm
  name                = "forestry-uksouth-kv"
  resource_group_name = "fs_flov2"
}

# Cloudflare
data "azurerm_key_vault_secret" "tls_crt" {
  provider     = azurerm
  name         = "forestry-tls-crt"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "tls_key" {
  provider     = azurerm
  name         = "forestry-tls-key"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "cloudflare_api_access_token" {
  provider     = azurerm
  name         = "cloudflare-api-access-token"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "cloudflare_token" {
  provider     = azurerm
  name         = "cloudflare-token"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "flov2_cloudflare_zone_settings_api" {
  provider     = azurerm
  name         = "flov2-cloudflare-zone-settings-api"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

# Traefik
data "azurerm_key_vault_secret" "traefik_dashboard_users" {
  provider     = azurerm
  name         = "traefik-dashboard-users"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

# Azure
data "azurerm_key_vault_secret" "tf_client_id" {
  provider     = azurerm
  name         = "tf-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "azure_sp_token" {
  provider     = azurerm
  name         = "azure-sp-token"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "tf_tenant_id" {
  provider     = azurerm
  name         = "tf-tenant-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "tf_sub_id" {
  provider     = azurerm
  name         = "tf-sub-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "tf_m_sub_id" {
  provider     = azurerm
  name         = "tf-m-sub-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "tc_client_id" {
  provider     = azurerm
  name         = "tc-client-id"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "qxprod_password" {
  provider     = azurerm
  name         = "qxprod-password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

# Databases
data "azurerm_key_vault_secret" "pg_staging_pass" {
  provider     = azurerm
  name         = "pg-staging-pass"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "pg_migrate_pass" {
  provider     = azurerm
  name         = "pg-migrate-pass"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

data "azurerm_key_vault_secret" "pg_live_pass" {
  provider     = azurerm
  name         = "pg-live-pass"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}


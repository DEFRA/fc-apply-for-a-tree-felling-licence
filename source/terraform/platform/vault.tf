# Do not delete this Key Vault without retrieving the secrets
resource "azurerm_key_vault" "main" {
  name                        = "forestry-uksouth-kv"
  location                    = "UK South"
  resource_group_name         = "fs_flov2"
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  sku_name                    = "standard"
  purge_protection_enabled    = true
  soft_delete_retention_days  = 7

  lifecycle {
    prevent_destroy = true
  }

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get", "List", "Set", "Delete", "Recover", "Backup", "Restore"
    ]
  }

  tags = {
    environment = "platform"
    team        = "devops"
  }
}

data "azurerm_client_config" "current" {}

# Cloudflare
data "azurerm_key_vault_secret" "tls_crt" {
  provider     = azurerm
  name         = "forestry-tls-crt"
  key_vault_id = azurerm_key_vault.main.id
}

data "azurerm_key_vault_secret" "tls_key" {
  provider     = azurerm
  name         = "forestry-tls-key"
  key_vault_id = azurerm_key_vault.main.id
}

data "azurerm_key_vault_secret" "cloudflare_api_access_token" {
  name         = "cloudflare-api-access-token"
  key_vault_id = azurerm_key_vault.main.id
}

data "azurerm_key_vault_secret" "cloudflare_token" {
  provider     = azurerm
  name         = "cloudflare-token"
  key_vault_id = azurerm_key_vault.main.id
}

# Traefik
data "azurerm_key_vault_secret" "traefik_dashboard_users" {
  provider     = azurerm
  name         = "traefik-dashboard-users"
  key_vault_id = azurerm_key_vault.main.id
}

# Azure
data "azurerm_key_vault_secret" "azure_sp_token" {
  provider     = azurerm
  name         = "azure-sp-token"
  key_vault_id = azurerm_key_vault.main.id
}

data "azurerm_key_vault_secret" "qxprod_password" {
  provider     = azurerm
  name         = "qxprod-password"
  key_vault_id = azurerm_key_vault.main.id
}

# Databases
data "azurerm_key_vault_secret" "pg_staging_pass" {
  provider     = azurerm
  name         = "pg-staging-pass"
  key_vault_id = azurerm_key_vault.main.id
}

data "azurerm_key_vault_secret" "pg_migrate_pass" {
  provider     = azurerm
  name         = "pg-migrate-pass"
  key_vault_id = azurerm_key_vault.main.id
}

data "azurerm_key_vault_secret" "pg_live_pass" {
  provider     = azurerm
  name         = "pg-live-pass"
  key_vault_id = azurerm_key_vault.main.id
}


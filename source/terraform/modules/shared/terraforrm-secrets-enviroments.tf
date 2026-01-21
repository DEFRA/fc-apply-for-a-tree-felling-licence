
# Environment wide secrets
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


# teamcity artifact token
data "azurerm_key_vault_secret" "teamcity_artifact_token" {
  name         = "teamcity-artifact-token"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "teamcity_artifact_token" {
  value     = data.azurerm_key_vault_secret.teamcity_artifact_token.value
  sensitive = true
}

# GitHub token
data "azurerm_key_vault_secret" "github_token" {
  name         = "github-token"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "github_token" {
  value     = data.azurerm_key_vault_secret.github_token.value
  sensitive = true
}

# landInformationSearch
data "azurerm_key_vault_secret" "landInformationSearch_clientID" {
  name         = "landInformationSearch-clientID"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "landInformationSearch_clientID" {
  value     = data.azurerm_key_vault_secret.landInformationSearch_clientID.value
  sensitive = true
}

data "azurerm_key_vault_secret" "landInformationSearch_clientSecret" {
  name         = "landInformationSearch-clientSecret"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "landInformationSearch_clientSecret" {
  value     = data.azurerm_key_vault_secret.landInformationSearch_clientSecret.value
  sensitive = true
}

# esri-APIKey
data "azurerm_key_vault_secret" "esri_APIKey" {
  name         = "esri-APIKey"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "esri_APIKey" {
  value     = data.azurerm_key_vault_secret.esri_APIKey.value
  sensitive = true
}

# ESRI Forester token service
data "azurerm_key_vault_secret" "esri_Forester_GenerateTokenService_Username" {
  name         = "esri-Forester-GenerateTokenService-Username"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "esri_Forester_GenerateTokenService_Username" {
  value     = data.azurerm_key_vault_secret.esri_Forester_GenerateTokenService_Username.value
  sensitive = true
}

data "azurerm_key_vault_secret" "esri_Forester_GenerateTokenService_Password" {
  name         = "esri-Forester-GenerateTokenService-Password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "esri_Forester_GenerateTokenService_Password" {
  value     = data.azurerm_key_vault_secret.esri_Forester_GenerateTokenService_Password.value
  sensitive = true
}

# ESRI Forestry token service (client credentials)
data "azurerm_key_vault_secret" "esri_Forestry_GenerateTokenService_ClientID" {
  name         = "esri-Forestry-GenerateTokenService-ClientID"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "esri_Forestry_GenerateTokenService_ClientID" {
  value     = data.azurerm_key_vault_secret.esri_Forestry_GenerateTokenService_ClientID.value
  sensitive = true
}

data "azurerm_key_vault_secret" "esri_Forestry_GenerateTokenService_ClientSecret" {
  name         = "esri-Forestry-GenerateTokenService-ClientSecret"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "esri_Forestry_GenerateTokenService_ClientSecret" {
  value     = data.azurerm_key_vault_secret.esri_Forestry_GenerateTokenService_ClientSecret.value
  sensitive = true
}

# ESRI Public Register token service
data "azurerm_key_vault_secret" "esri_PublicRegister_GenerateTokenService_Password" {
  name         = "esri-PublicRegister-GenerateTokenService-Password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "esri_PublicRegister_GenerateTokenService_Password" {
  value     = data.azurerm_key_vault_secret.esri_PublicRegister_GenerateTokenService_Password.value
  sensitive = true
}

data "azurerm_key_vault_secret" "esri_PublicRegister_GenerateTokenService_Username" {
  name         = "esri-PublicRegister-GenerateTokenService-Username"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "esri_PublicRegister_GenerateTokenService_Username" {
  value     = data.azurerm_key_vault_secret.esri_PublicRegister_GenerateTokenService_Username.value
  sensitive = true
}

#smtp credentials
data "azurerm_key_vault_secret" "smtp_username" {
  name         = "smtp-username"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "smtp_username" {
  value     = data.azurerm_key_vault_secret.smtp_username.value
  sensitive = true
}

data "azurerm_key_vault_secret" "smtp_password" {
  name         = "smtp-password"
  key_vault_id = data.azurerm_key_vault.flov2_kv.id
}

output "smtp_password" {
  value     = data.azurerm_key_vault_secret.smtp_password.value
  sensitive = true
}
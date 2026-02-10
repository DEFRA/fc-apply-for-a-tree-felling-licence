output "key_vault" {
  description = "Key Vault details for alerting."
  value = {
    id   = azurerm_key_vault.main.id
    name = azurerm_key_vault.main.name
  }
}


output "pg_servers" {
  description = "PostgreSQL flexible servers used by FLOv2"
  value = {
    staging = {
      name = azurerm_postgresql_flexible_server.staging.name
      id   = azurerm_postgresql_flexible_server.staging.id
    }
    migrate = {
      name = azurerm_postgresql_flexible_server.migrate.name
      id   = azurerm_postgresql_flexible_server.migrate.id
    }
    live = {
      name = azurerm_postgresql_flexible_server.live.name
      id   = azurerm_postgresql_flexible_server.live.id
    }
    live_replica = {
      name = azurerm_postgresql_flexible_server.live_replica.name
      id   = azurerm_postgresql_flexible_server.live_replica.id
    }
  }
}

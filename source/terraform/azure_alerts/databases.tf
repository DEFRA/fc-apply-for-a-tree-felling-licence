locals {
  pg_servers_outputs = data.terraform_remote_state.databases.outputs.pg_servers

  pg_servers = {
    staging = {
      id  = local.pg_servers_outputs.staging.id
      env = "nonprod"
    }
    migrate = {
      id  = local.pg_servers_outputs.migrate.id
      env = "nonprod"
    }
    live = {
      id  = local.pg_servers_outputs.live.id
      env = "prod"
    }
    live_replica = {
      id  = local.pg_servers_outputs.live_replica.id
      env = "prod"
    }
  }
}

resource "azurerm_monitor_metric_alert" "pg_cpu_high" {
  for_each            = local.pg_servers
  name                = "pg-${each.key}-cpu-high"
  resource_group_name = local.rg_name
  scopes              = [each.value.id]
  severity            = each.value.env == "prod" ? 1 : 2
  description         = "Postgres ${each.key}: CPU high"
  enabled             = true

  frequency   = "PT5M"
  window_size = "PT15M"

  criteria {
    metric_namespace = "Microsoft.DBforPostgreSQL/flexibleServers"
    metric_name      = "cpu_percent"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 80
  }

  action {
    action_group_id = local.action_group_id[each.value.env]
  }

  tags = { service = "postgres", env = each.value.env }
}

resource "azurerm_monitor_metric_alert" "pg_memory_high" {
  for_each            = local.pg_servers
  name                = "pg-${each.key}-memory-high"
  resource_group_name = local.rg_name
  scopes              = [each.value.id]
  severity            = each.value.env == "prod" ? 2 : 3
  description         = "Postgres ${each.key}: memory percent high"
  enabled             = true

  frequency   = "PT5M"
  window_size = "PT15M"

  criteria {
    metric_namespace = "Microsoft.DBforPostgreSQL/flexibleServers"
    metric_name      = "memory_percent"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 85
  }

  action {
    action_group_id = local.action_group_id[each.value.env]
  }

  tags = { service = "postgres", env = each.value.env }
}

resource "azurerm_monitor_metric_alert" "pg_storage_high" {
  for_each            = local.pg_servers
  name                = "pg-${each.key}-storage-percent-high"
  resource_group_name = local.rg_name
  scopes              = [each.value.id]
  severity            = each.value.env == "prod" ? 1 : 2
  description         = "Postgres ${each.key}: storage percent high"
  enabled             = true

  frequency   = "PT5M"
  window_size = "PT15M"

  criteria {
    metric_namespace = "Microsoft.DBforPostgreSQL/flexibleServers"
    metric_name      = "storage_percent"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 85
  }

  action {
    action_group_id = local.action_group_id[each.value.env]
  }

  tags = { service = "postgres", env = each.value.env }
}

resource "azurerm_monitor_metric_alert" "pg_connections_high" {
  for_each            = local.pg_servers
  name                = "pg-${each.key}-connections-high"
  resource_group_name = local.rg_name
  scopes              = [each.value.id]
  severity            = each.value.env == "prod" ? 2 : 3
  description         = "Postgres ${each.key}: active connections high"
  enabled             = true

  frequency   = "PT5M"
  window_size = "PT15M"

  criteria {
    metric_namespace = "Microsoft.DBforPostgreSQL/flexibleServers"
    metric_name      = "active_connections"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 150 # tune per sku/app behaviour
  }

  action {
    action_group_id = local.action_group_id[each.value.env]
  }

  tags = { service = "postgres", env = each.value.env }
}

resource "azurerm_monitor_metric_alert" "pg_failed_connections" {
  for_each            = local.pg_servers
  name                = "pg-${each.key}-failed-connections"
  resource_group_name = local.rg_name
  scopes              = [each.value.id]
  severity            = each.value.env == "prod" ? 2 : 3
  description         = "Postgres ${each.key}: failed connections detected"
  enabled             = true

  frequency   = "PT5M"
  window_size = "PT5M"

  criteria {
    metric_namespace = "Microsoft.DBforPostgreSQL/flexibleServers"
    metric_name      = "connections_failed"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = each.value.env == "prod" ? 10 : 0
  }

  action {
    action_group_id = local.action_group_id[each.value.env]
  }

  tags = { service = "postgres", env = each.value.env }
}

resource "azurerm_monitor_metric_alert" "pg_replica_lag_high" {
  for_each            = { for k, v in local.pg_servers : k => v if k == "live_replica" }
  name                = "pg-${each.key}-replica-lag-high"
  resource_group_name = local.rg_name
  scopes              = [each.value.id]
  severity            = 1
  description         = "Postgres ${each.key}: replication lag high"
  enabled             = true

  frequency   = "PT5M"
  window_size = "PT15M"

  criteria {
    metric_namespace = "Microsoft.DBforPostgreSQL/flexibleServers"
    metric_name      = "physical_replication_delay_in_seconds"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 60 # seconds
  }

  action {
    action_group_id = local.action_group_id["prod"]
  }

  tags = { service = "postgres", env = "prod" }
}
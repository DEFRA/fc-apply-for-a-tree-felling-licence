locals {
  aks_id = data.terraform_remote_state.kubernetes_infra.outputs.aks_id
}

resource "azurerm_monitor_metric_alert" "aks_node_cpu_high" {
  name                = "aks-node-cpu-high"
  resource_group_name = local.rg_name
  scopes              = [local.aks_id]

  severity    = 2
  description = "AKS: node CPU usage high (cluster average)"
  enabled     = true

  frequency   = "PT5M"
  window_size = "PT15M"

  criteria {
    metric_namespace = "Microsoft.ContainerService/managedClusters"
    metric_name      = "node_cpu_usage_percentage"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 80
  }

  action {
    action_group_id = local.action_group_id.prod
  }

  tags = { service = "aks", area = "infra" }
}

resource "azurerm_monitor_metric_alert" "aks_node_mem_ws_high" {
  name                = "aks-node-mem-working-set-high"
  resource_group_name = local.rg_name
  scopes              = [local.aks_id]

  severity    = 2
  description = "AKS: node memory working set high (cluster average)"
  enabled     = true

  frequency   = "PT5M"
  window_size = "PT15M"

  criteria {
    metric_namespace = "Microsoft.ContainerService/managedClusters"
    metric_name      = "node_memory_working_set_percentage"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 85
  }

  action {
    action_group_id = local.action_group_id.prod
  }

  tags = { service = "aks", area = "infra" }
}

resource "azurerm_monitor_metric_alert" "aks_node_disk_high" {
  name                = "aks-node-disk-high"
  resource_group_name = local.rg_name
  scopes              = [local.aks_id]

  severity    = 2
  description = "AKS: node disk usage high (cluster average)"
  enabled     = true

  frequency   = "PT5M"
  window_size = "PT15M"

  criteria {
    metric_namespace = "Microsoft.ContainerService/managedClusters"
    metric_name      = "node_disk_usage_percentage"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 85
  }

  action {
    action_group_id = local.action_group_id.prod
  }

  tags = { service = "aks", area = "infra" }
}

resource "azurerm_monitor_metric_alert" "aks_node_not_ready" {
  name                = "aks-node-not-ready"
  resource_group_name = local.rg_name
  scopes              = [local.aks_id]

  severity    = 1
  description = "AKS: at least one node NotReady"
  enabled     = true

  frequency   = "PT5M"
  window_size = "PT5M"

  criteria {
    metric_namespace = "Microsoft.ContainerService/managedClusters"
    metric_name      = "kube_node_status_condition"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 0

    # These dimension names/values MUST match what Azure shows for the metric.
    # If they don't, Azure will 400.
    dimension {
      name     = "condition"
      operator = "Include"
      values   = ["Ready"]
    }
    dimension {
      name     = "status"
      operator = "Include"
      values   = ["false"]
    }
  }

  action {
    action_group_id = local.action_group_id.prod
  }

  tags = { service = "aks", area = "infra" }
}

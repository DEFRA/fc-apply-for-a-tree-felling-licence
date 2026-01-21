locals {
  stress_tags = merge(
    module.shared.azure_tags,
    {
      StressTesting = "true"
      Environment   = "performance"
      Owner         = "flov2-stress-tests"
    }
  )

  k8s_stress_labels = merge(
    module.shared.k8s_common_labels,
    {
      StressTesting = "true"
      Environment   = "performance"
      Owner         = "flov2-stress-tests"
    }
  )
}

# Turn on and off stress testing
variable "enable_stress_testing_environment" {
  type        = bool
  description = "Create the stress-testing environment"
  default     = false
}

# Perf App Node Pool Variables
variable "stress_app_pool_name" {
  type        = string
  description = "Name of the stress testing application node pool."
  default     = "perfapp"
}

variable "stress_app_node_count" {
  type        = number
  description = "Node count for the stress testing application node pool."
  default     = 1
}

variable "stress_app_vm_size" {
  type        = string
  description = "VM size for the stress testing application node pool."
  default     = "Standard_D4ds_v5"
}

variable "stress_app_zones" {
  type        = list(string)
  description = "Availability zones for the stress app node pool."
  default     = ["1"]
}

variable "stress_app_os_disk_size_gb" {
  type        = number
  description = "OS disk size for stress app nodes."
  default     = 32
}

variable "stress_app_max_pods" {
  type        = number
  description = "Maximum pods per node for stress app pool."
  default     = 30
}

variable "stress_app_node_labels" {
  type        = map(string)
  description = "Labels for the stress app node pool."
  default = {
    "environment" = "performance"
    "workload"    = "flov2-app"
  }
}

# Perf Load Node Pool Variables (JMeter / loadgen)
variable "stress_load_pool_name" {
  type        = string
  description = "Name of the stress testing load generation node pool."
  default     = "perfload"
}

variable "stress_load_node_count" {
  type        = number
  description = "Node count for the load generation node pool."
  default     = 1
}

variable "stress_load_vm_size" {
  type        = string
  description = "VM size for the load gen node pool."
  default     = "Standard_D8ds_v5" # Higher CPU for JMeter
}

variable "stress_load_zones" {
  type        = list(string)
  description = "Availability zones for the load gen node pool."
  default     = ["1"]
}

variable "stress_load_os_disk_size_gb" {
  type        = number
  description = "OS disk size for stress load nodes."
  default     = 32
}

variable "stress_load_max_pods" {
  type        = number
  description = "Maximum pods for load gen pool."
  default     = 30
}

variable "stress_load_node_labels" {
  type        = map(string)
  description = "Labels for the stress load node pool."
  default = {
    "environment" = "performance"
    "workload"    = "loadgen"
  }
}

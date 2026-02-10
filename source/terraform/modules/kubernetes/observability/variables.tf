#Prometheus Stack Variables
variable "prometheus_chart" {
  type        = string
  description = "Prometheus Chart Name"
  default     = "kube-prometheus-stack"
}

variable "prometheus_repository" {
  type        = string
  description = "Prometheus Helm Repository Name"
  default     = "https://prometheus-community.github.io/helm-charts"
}

variable "prometheus_version" {
  type        = string
  description = "Prometheus chart version"
  default     = "27.2.1"
}
resource "kubernetes_secret_v1" "alertmanager_opsgenie_keys" {

  metadata {
    name      = "alertmanager-opsgenie-keys"
    namespace = kubernetes_namespace_v1.prometheus.metadata[0].name
  }

  type = "Opaque"

  data = {
    DEV_API_KEY     = module.kube_observability.alerting_DEV_API_KEY
    PREPROD_API_KEY = module.kube_observability.alerting_PREPROD_API_KEY
    PROD_API_KEY    = module.kube_observability.alerting_PROD_API_KEY
    INFRA_API_KEY   = module.kube_observability.alerting_INFRA_API_KEY
  }
}

resource "kubernetes_secret_v1" "alertmanager_heartbeat_auth" {

  metadata {
    name      = "alertmanager-heartbeat-auth"
    namespace = kubernetes_namespace_v1.prometheus.metadata[0].name
  }

  type = "Opaque"

  data = {
    USERNAME  = module.kube_observability.alerting_jira_USERNAME
    API_TOKEN = module.kube_observability.alerting_jira_API_TOKEN
  }
}

resource "kubernetes_secret_v1" "grafana_admin" {
  metadata {
    name      = "grafana-admin"
    namespace = kubernetes_namespace_v1.prometheus.metadata[0].name
  }

  type = "Opaque"

  data = {
    admin-user     = "admin"
    admin-password = module.kube_observability.alerting_grafana_password
  }
}
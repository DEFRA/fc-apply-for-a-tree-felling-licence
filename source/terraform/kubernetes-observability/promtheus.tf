#provide a namespace for Prometheus
resource "kubernetes_namespace_v1" "prometheus" {
  wait_for_default_service_account = false

  metadata {
    name = "prometheus"

    labels = {
      "kubernetes.io/metadata.name" = "prometheus"
      "name"                        = "prometheus"
    }
  }
}

resource "kubectl_manifest" "prometheus_ingressroute" {
  yaml_body = file("values/prometheus/ingress-prometheus.yaml")

  # depends_on = [
  #   data.terraform_remote_state.kubernetes.outputs.helm_release.traefik
  # ]
  depends_on = [
    helm_release.kube-prometheus-stack
  ]
}

resource "kubectl_manifest" "grafana_ingressroute" {
  yaml_body = file("values/prometheus/ingress-grafana.yaml")

  depends_on = [
    helm_release.kube-prometheus-stack
  ]
}

# Deploy Kubernetes Prometheus Stack
resource "helm_release" "kube-prometheus-stack" {
  name       = "prometheus"
  repository = "https://prometheus-community.github.io/helm-charts"
  chart      = "kube-prometheus-stack"
  namespace  = kubernetes_namespace_v1.prometheus.metadata[0].name
  version    = "61.7.0"

  values = [
    templatefile("values/prometheus/values.yaml", {
      storageClass        = "azurefile",
      geo                 = "uk",
      region              = "uksouth",
      cloud               = "Azure",
      cluster             = "fs_flov2",
      prometheus_replicas = 2
    }),

    # rules are static YAML (no templating needed)
    file("values/prometheus/rules-flo-workloads.yaml"),
  ]

  depends_on = [
    kubernetes_namespace_v1.prometheus,
    kubernetes_secret_v1.alertmanager_opsgenie_keys,
    kubernetes_secret_v1.alertmanager_heartbeat_auth,
    kubernetes_secret_v1.grafana_admin
  ]

}

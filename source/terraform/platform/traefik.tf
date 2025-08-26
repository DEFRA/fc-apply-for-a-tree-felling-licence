resource "kubernetes_namespace" "traefik" {
  metadata {
    name = "traefik"

    labels = {
      "kubernetes.io/metadata.name" = "traefik"
      "name"                        = "traefik"
    }
  }
}

resource "kubectl_manifest" "traefik-crt" {
  yaml_body = file("values/traefik/traefik-web-crt.yaml")

  depends_on = [
    kubernetes_namespace.traefik,
    helm_release.cert-manager
  ]
}


# Deploy Traefik Ingress Controller
resource "helm_release" "traefik" {
  name       = var.traefik_chart
  repository = var.traefik_repository
  chart      = var.traefik_chart
  namespace  = kubernetes_namespace.traefik.metadata[0].name

  version = "10.21.1"

  values = [
    file("values/traefik/values.yaml")
  ]

  depends_on = [
    helm_release.external-dns
    #helm_release.kube-prometheus-stack
  ]

  timeout = 600

}

# Deploy AzureAD Auth Helm Chart
resource "helm_release" "azuread-auth" {
  name                = var.azuread_chart
  repository          = var.azuread_repository
  chart               = var.azuread_chart
  namespace           = kubernetes_namespace.traefik.metadata[0].name
  repository_username = "qxprod"
  repository_password = data.azurerm_key_vault_secret.qxprod_password.value

  values = [
    file("values/azuread-auth/values.yaml")
  ]

  depends_on = [
    helm_release.traefik
  ]

  lifecycle {
    ignore_changes = [
      metadata[0].annotations,
      metadata[0].labels
    ]
  }
}


# BasicAuth Secret
# Login: See Password Safe
resource "kubernetes_secret" "traefik-dashboard-auth" {
  metadata {
    name      = "traefik-dashboard-auth"
    namespace = kubernetes_namespace.traefik.metadata[0].name
  }

  data = {
    users = data.azurerm_key_vault_secret.traefik_dashboard_users.value
  }

  depends_on = [
    helm_release.traefik,
    helm_release.azuread-auth
  ]
}


# Deploy BasicAuth for Dashboard
resource "kubectl_manifest" "traefik-dashboard-basicauth" {
  yaml_body = file("values/traefik/middleware-basicauth.yaml")

  depends_on = [
    kubernetes_secret.traefik-dashboard-auth
  ]

}

# Deploy Dashboard IngressRoute
resource "kubectl_manifest" "ingress_dashboard" {
  yaml_body = file("values/traefik/ingress-dashboard.yaml")

  depends_on = [
    kubectl_manifest.traefik-dashboard-basicauth
  ]

}

# Deploy Metrics Service
resource "kubectl_manifest" "metrics_service" {
  yaml_body = file("values/traefik/metrics-service.yaml")

  depends_on = [
    helm_release.traefik
  ]
}

# Deploy traefik service monitor
#resource "kubectl_manifest" "traefik-service-monitor" {
#    yaml_body = file("values/traefik/traefik-service-monitor.yaml")
#
#    depends_on = [
#        kubernetes_namespace.traefik,
#        helm_release.kube-prometheus-stack
#    ]
#}
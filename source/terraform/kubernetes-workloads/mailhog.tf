resource "kubernetes_namespace_v1" "mailhog" {
  wait_for_default_service_account = false

  metadata {
    name = "mailhog"

    labels = {
      "kubernetes.io/metadata.name" = "mailhog"
      "name"                        = "mailhog"
    }
  }
}

# Deploy mailhog
resource "helm_release" "mailhog" {
  name       = module.shared.mailhog_chart
  repository = module.shared.mailhog_repository
  chart      = module.shared.mailhog_chart
  namespace  = kubernetes_namespace_v1.mailhog.metadata[0].name
  version    = "5.8.0"

  timeout = 600

  depends_on = [
    kubernetes_namespace_v1.mailhog
  ]

}
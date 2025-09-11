resource "kubernetes_namespace" "mailhog" {
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
  namespace  = kubernetes_namespace.mailhog.metadata[0].name

  timeout = 600
}
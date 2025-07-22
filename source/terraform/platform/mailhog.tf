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
  name       = var.mailhog_chart
  repository = var.mailhog_repository
  chart      = var.mailhog_chart
  namespace  = kubernetes_namespace.mailhog.metadata[0].name

  timeout = 600
}
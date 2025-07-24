resource "kubernetes_namespace" "postfix" {
  metadata {
    name = "postfix"

    labels = {
      "kubernetes.io/metadata.name" = "postfix"
      "name"                        = "postfix"
    }
  }
}

# Deploy postfix
resource "helm_release" "postfix" {
  name       = var.postfix_chart
  repository = var.postfix_repository
  chart      = var.postfix_chart
  namespace  = kubernetes_namespace.postfix.metadata[0].name

  values = [
    file("values/postfix/values.yaml")
  ]

  timeout = 600
}
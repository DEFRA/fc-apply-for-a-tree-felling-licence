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
  name       = module.shared.postfix_chart
  repository = module.shared.postfix_repository
  chart      = module.shared.postfix_chart
  namespace  = kubernetes_namespace.postfix.metadata[0].name

  values = [
    file("values/postfix/values.yaml")
  ]

  timeout = 600
}
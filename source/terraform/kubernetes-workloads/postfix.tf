resource "kubernetes_namespace_v1" "postfix" {
  wait_for_default_service_account = false

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
  namespace  = kubernetes_namespace_v1.postfix.metadata[0].name
  version    = "v4.4.0"

  values = [
    file("values/postfix/values.yaml")
  ]

  timeout = 600

  depends_on = [
    kubernetes_namespace_v1.postfix
  ]
}
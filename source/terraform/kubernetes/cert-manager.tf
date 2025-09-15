resource "kubernetes_namespace" "cert-manager" {
  metadata {
    name = "cert-manager"

    labels = {
      "kubernetes.io/metadata.name" = "cert-manager"
      "name"                        = "cert-manager"
    }
  }
}

resource "kubernetes_secret" "ca-key-pair" {
  metadata {
    name      = "ca-key-pair"
    namespace = kubernetes_namespace.cert-manager.metadata[0].name
  }

  binary_data = {
    "tls.crt" = module.shared.tls_crt
    "tls.key" = module.shared.tls_key
  }

  type = "kubernetes.io/tls"
}

resource "kubernetes_secret" "cloudflare-api-token" {
  metadata {
    name      = "cloudflare-api-token"
    namespace = kubernetes_namespace.cert-manager.metadata[0].name
  }

  data = {
    api-token = module.shared.cloudflare_token
  }

  type = "opaque"
}


# Deploy Cert Manager
resource "helm_release" "cert-manager" {
  name       = module.shared.cert_manager_chart
  repository = module.shared.cert_manager_repository
  chart      = module.shared.cert_manager_chart
  namespace  = kubernetes_namespace.cert-manager.metadata[0].name

  create_namespace = false

  values = [
    file("values/cert-manager/values.yaml")
  ]

  #depends_on = [
  #  helm_release.kube-prometheus-stack
  #]
}


resource "kubectl_manifest" "ca-issuer" {
  yaml_body = file("values/cert-manager/ca-issuer.yaml")

  depends_on = [
    kubernetes_secret.ca-key-pair,
    helm_release.cert-manager
  ]

}

resource "kubectl_manifest" "le-issuer" {
  yaml_body = file("values/cert-manager/le-issuer.yaml")

  depends_on = [
    kubernetes_secret.cloudflare-api-token,
    helm_release.cert-manager
  ]

}

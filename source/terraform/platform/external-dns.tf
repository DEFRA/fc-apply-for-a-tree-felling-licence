resource "kubernetes_namespace" "external-dns" {
  metadata {
    name = "external-dns"

    labels = {
      "kubernetes.io/metadata.name" = "external-dns"
      "name"                        = "external-dns"
    }
  }
}

resource "kubernetes_secret" "external-dns-cloudflare-api-token" {
  metadata {
    name      = "cloudflare-api-token"
    namespace = kubernetes_namespace.external-dns.metadata[0].name
  }

  data = {
    cloudflare_api_token = "${var.cloudflare_api_token}"
  }

  type = "opaque"
}


# Deploy External-DNS
resource "helm_release" "external-dns" {
  name       = var.externaldns_chart
  repository = var.externaldnsk_repository
  chart      = var.externaldns_chart
  namespace  = kubernetes_namespace.external-dns.metadata[0].name
  version    = "6.5.6"

  values = [
    file("values/external-dns/values.yml")
  ]

  depends_on = [
    kubernetes_secret.external-dns-cloudflare-api-token
    #helm_release.kube-prometheus-stack
  ]
}
# resource "kubernetes_namespace" "external-dns" {
#   metadata {
#     name = "external-dns"

#     labels = {
#       "kubernetes.io/metadata.name" = "external-dns"
#       "name"                        = "external-dns"
#     }
#   }
# }

# resource "kubernetes_secret" "external-dns-cloudflare-api-token" {
#   metadata {
#     name      = "cloudflare-api-token"
#     namespace = kubernetes_namespace.external-dns.metadata[0].name
#   }

#   data = {
#     cloudflare_api_token = module.shared.cloudflare_api_access_token
#   }

#   type = "opaque"
# }


# # Deploy External-DNS
# resource "helm_release" "external-dns" {
#   name       = module.shared.externaldns_chart
#   repository = module.shared.externaldnsk_repository
#   chart      = module.shared.externaldns_chart
#   namespace  = kubernetes_namespace.external-dns.metadata[0].name
#   version    = "6.5.6"

#   values = [
#     file("values/external-dns/values.yml")
#   ]

#   depends_on = [
#     kubernetes_secret.external-dns-cloudflare-api-token
#     #helm_release.kube-prometheus-stack
#   ]
# }
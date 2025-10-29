#resource "kubernetes_namespace" "migrate" {
#  metadata {
#    name = "migrate-flo"
#
#    labels = {
#      "kubernetes.io/metadata.name" = "migrate-flo"
#      "name"                        = "migrate-flo"
#    }
#  }
#}
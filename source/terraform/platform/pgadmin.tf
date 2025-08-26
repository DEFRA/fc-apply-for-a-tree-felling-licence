resource "kubernetes_namespace" "pgadmin" {
  metadata {
    name = "pgadmin"

    labels = {
      "kubernetes.io/metadata.name" = "pgadmin"
      "name"                        = "pgadmin"
    }
  }
}


# Deploy PGAdmin
resource "helm_release" "pgadmin" {
  name       = var.pgadmin_chart
  repository = var.pgadmin_repository
  chart      = var.pgadmin_chart
  namespace  = kubernetes_namespace.pgadmin.metadata[0].name

  values = [
    file("values/pgadmin/values.yaml")
  ]

  depends_on = [
    helm_release.external-dns
    #helm_release.kube-prometheus-stack
  ]

  timeout = 600

}
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
  name       = module.shared.pgadmin_chart
  repository = module.shared.pgadmin_repository
  chart      = module.shared.pgadmin_chart
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
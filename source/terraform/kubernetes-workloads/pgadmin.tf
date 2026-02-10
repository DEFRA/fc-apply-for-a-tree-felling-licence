resource "kubernetes_namespace_v1" "pgadmin" {
  wait_for_default_service_account = false

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
  namespace  = kubernetes_namespace_v1.pgadmin.metadata[0].name
  version    = "1.50.0"

  values = [
    file("values/pgadmin/values.yaml")
  ]

  depends_on = [
    #helm_release.external-dns
    #helm_release.kube-prometheus-stack
    kubernetes_namespace_v1.pgadmin
  ]

  timeout = 600
}
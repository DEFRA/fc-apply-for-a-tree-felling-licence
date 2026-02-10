# resource "kubectl_manifest" "pvc_dev_flo_postgres" {
#   yaml_body = file("${path.module}/values/persistent-volume/pvc-dev-flo-postgresql.yaml")
# }

# resource "kubectl_manifest" "pvc_test_flo_postgres" {
#   yaml_body = file("${path.module}/values/persistent-volume/pvc-test-flo-postgresql.yaml")
# }

resource "kubernetes_persistent_volume_claim_v1" "dev_flo_postgres" {
  metadata {
    name      = "data-dev-flo-postgresql-0"
    namespace = "dev-flo"
    labels = {
      "app.kubernetes.io/component" = "primary"
      "app.kubernetes.io/instance"  = "dev-flo"
      "app.kubernetes.io/name"      = "postgresql"
    }
  }

  spec {
    access_modes       = ["ReadWriteOnce"]
    storage_class_name = "default"

    resources {
      requests = {
        storage = "20Gi"
      }
    }
  }

  depends_on = [
    kubernetes_namespace_v1.dev-flo
  ]

}

resource "kubernetes_persistent_volume_claim_v1" "test_flo_postgres" {
  metadata {
    name      = "data-test-flo-postgresql-0"
    namespace = "test-flo"
    labels = {
      "app.kubernetes.io/component" = "primary"
      "app.kubernetes.io/instance"  = "test-flo"
      "app.kubernetes.io/name"      = "postgresql"
    }
  }

  spec {
    access_modes       = ["ReadWriteOnce"]
    storage_class_name = "default"

    resources {
      requests = {
        storage = "20Gi"
      }
    }
  }

  depends_on = [
    kubernetes_namespace_v1.test-flo
  ]
}

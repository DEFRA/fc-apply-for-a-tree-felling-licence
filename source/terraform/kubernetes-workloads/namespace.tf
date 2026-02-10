resource "kubernetes_namespace_v1" "dev-flo" {
  wait_for_default_service_account = false

  metadata {
    name = "dev-flo"
    labels = merge(
      {
        "environment"                 = "development"
        "kubernetes.io/metadata.name" = "dev-flo"
        "name"                        = "dev-flo"
    }, module.shared.k8s_main_labels)
  }
}

resource "kubernetes_namespace_v1" "test-flo" {
  wait_for_default_service_account = false

  metadata {
    name = "test-flo"
    labels = merge(
      {
        "environment"                 = "test"
        "kubernetes.io/metadata.name" = "test-flo"
        "name"                        = "test-flo"
    }, module.shared.k8s_main_labels)
  }
}

resource "kubernetes_namespace_v1" "stage-flo" {
  wait_for_default_service_account = false

  metadata {
    name = "stage-flo"
    labels = merge(
      {
        "environment"                 = "stage"
        "kubernetes.io/metadata.name" = "stage-flo"
        "name"                        = "stage-flo"
    }, module.shared.k8s_main_labels)
  }
}

resource "kubernetes_namespace_v1" "migrate-flo" {
  wait_for_default_service_account = false

  metadata {
    name = "migrate-flo"
    labels = merge(
      {
        "environment"                 = "migrate"
        "kubernetes.io/metadata.name" = "migrate-flo"
        "name"                        = "migrate-flo"
    }, module.shared.k8s_main_labels)
  }

  lifecycle {
    ignore_changes = [metadata[0].labels]
  }
}

resource "kubernetes_namespace_v1" "live-flo" {
  wait_for_default_service_account = false

  metadata {
    name = "live-flo"
    labels = merge(
      {
        "environment"                 = "live"
        "kubernetes.io/metadata.name" = "live-flo"
        "name"                        = "live-flo"
    }, module.shared.k8s_main_labels)
  }
}


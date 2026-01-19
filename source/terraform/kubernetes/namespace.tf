resource "kubernetes_namespace" "dev-flo" {
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

resource "kubernetes_namespace" "test-flo" {
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

resource "kubernetes_namespace" "stage-flo" {
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

resource "kubernetes_namespace" "migrate-flo" {
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

resource "kubernetes_namespace" "live-flo" {
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


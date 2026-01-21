resource "kubernetes_namespace" "perf-flo" {
  metadata {
    name = "perf-flo"
    labels = merge(
      {
        "kubernetes.io/metadata.name" = "perf-flo"
        "name"                        = "perf-flo"
    }, local.k8s_stress_labels)
  }
}

resource "kubernetes_namespace" "perf-tools" {
  metadata {
    name = "perf-tools"
    labels = merge(
      {
        "kubernetes.io/metadata.name" = "perf-tools"
        "name"                        = "perf-tools"
    }, local.k8s_stress_labels)
  }
}


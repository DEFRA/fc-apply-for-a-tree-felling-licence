resource "kubernetes_namespace" "cloudflare" {
  metadata {
    name = "cloudflare"

    labels = {
      "kubernetes.io/metadata.name" = "cloudflare"
      "name"                        = "cloudflare"
    }
  }
}

resource "kubernetes_deployment" "cloudflared" {
  metadata {
    name = "cloudflared-deployment"
    labels = {
      app = "cloudflared"
    }
    namespace = kubernetes_namespace.cloudflare.metadata[0].name
  }

  spec {
    replicas = 2

    selector {
      match_labels = {
        pod = "cloudflared"
      }
    }

    template {
      metadata {
        labels = {
          pod = "cloudflared"
        }
      }

      spec {
        container {
          image = "cloudflare/cloudflared:latest"
          name  = "cloudflared"

          command = ["cloudflared", "tunnel", "--metrics", "0.0.0.0:2000", "run"]
          args    = ["--token", "eyJhIjoiMzk3OWY4NTNmY2ZhYjBjNzY5YTE5YWE5Nzk3NThkNTgiLCJ0IjoiN2YwYmU2MTktZGI4YS00N2E0LTg1YzctOWJkNDM4MDQ1ZjIzIiwicyI6Ill6UXlZbVpoWkdNdFpUUmpPUzAwT0RkbUxXRTVORGd0TkRjMk0yVTVaalE1TW1FNCJ9"]

          resources {
            limits = {
              cpu    = "0.5"
              memory = "512Mi"
            }
            requests = {
              cpu    = "250m"
              memory = "50Mi"
            }
          }

          liveness_probe {
            http_get {
              path = "/ready"
              port = 2000
            }

            failure_threshold     = 1
            initial_delay_seconds = 10
            period_seconds        = 10
          }
        }
      }
    }
  }
}

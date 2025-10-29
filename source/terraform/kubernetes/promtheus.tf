# resource "kubectl_manifest" "prometheus_ingressroute" {
#   yaml_body = file("values/prometheus/ingress-prometheus.yaml")

#   depends_on = [
#     helm_release.traefik
#   ]
# }

locals {
  # Database string
  stress_db_server   = "stress-testing-flo.postgres.database.azure.com"
  stress_db_name     = "flo2-stress"
  stress_db_username = "qxlva"
  stress_db_password = module.shared.pg_staging_pass
  # Generate the full PostgreSQL connection string
  stress_db_connection_string = format(
    "Server=%s;Database=%s;Port=5432;User Id=%s;Password=%s;",
    local.stress_db_server,
    local.stress_db_name,
    local.stress_db_username,
    local.stress_db_password
  )

  # Blob Storage string
  stress_blob_accountname = "testflo"
  stress_blob_accountkey  = module.shared.test_blob_account_key
  # Connection string for Azure Blob Storage
  stress_blob_connection_string = format(
    "DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=%s;EndpointSuffix=core.windows.net",
    local.stress_blob_accountname,
    local.stress_blob_accountkey
  )

  stress_github_token = module.shared.github_token
  stress_github_connection_string = format(
    "https://Harris-HealthAlliance:%s@github.com/Harris-HealthAlliance/flo-v2.git",
    local.stress_github_token
  )
}

resource "kubernetes_secret" "stress-flo-all-secrets" {
  metadata {
    name      = "stress-flo-all-secrets"
    namespace = kubernetes_namespace.perf-flo.metadata[0].name
    labels = merge(
      {
        "app"                         = "shared"
        "environment"                 = "stress"
        "purpose"                     = "performance-testing"
        "kubernetes.io/metadata.name" = "stress-flo"
        "name"                        = "stress-flo"
    }, module.shared.k8s_main_labels)
  }

  data = {
    "db.connectionstring" = local.stress_db_connection_string

    "azuread.tennant_id"     = module.shared.test_azuread_tenant_id
    "azureb2c.client_id"     = module.shared.test_azuread_client_id
    "azureb2c.client_secret" = module.shared.test_azuread_client_secret

    "blob.connectionString" = local.stress_blob_connection_string

    "landInformationSearch.clientID"     = module.shared.landInformationSearch_clientID
    "landInformationSearch.clientSecret" = module.shared.landInformationSearch_clientSecret

    "cronJobs.apiKey"         = module.shared.test_cronjobs_api_key
    "teamcity.artifact_token" = module.shared.teamcity_artifact_token

    "smtp.username" = module.shared.smtp_username
    "smtp.password" = module.shared.smtp_password

    "govuk.notify_apikey" = module.shared.test_govuk_notify_apikey

    "esri.APIKey"                                       = module.shared.esri_APIKey
    "esri.Forester.GenerateTokenService.Username"       = module.shared.esri_Forester_GenerateTokenService_Username
    "esri.Forester.GenerateTokenService.Password"       = module.shared.esri_Forester_GenerateTokenService_Password
    "esri.Forestry.GenerateTokenService.ClientID"       = module.shared.esri_Forestry_GenerateTokenService_ClientID
    "esri.Forestry.GenerateTokenService.ClientSecret"   = module.shared.esri_Forestry_GenerateTokenService_ClientSecret
    "esri.PublicRegister.GenerateTokenService.Username" = module.shared.esri_PublicRegister_GenerateTokenService_Username
    "esri.PublicRegister.GenerateTokenService.Password" = module.shared.esri_PublicRegister_GenerateTokenService_Password

    # PDF Generator (GitHub Token Embedded in Origin URL)
    "github.token" = local.stress_github_connection_string

    "smtp.password" = module.shared.smtp_password
    "smtp.username" = module.shared.smtp_username

    # RabbitMQ
    "rabbitmq.username"      = module.shared.test_rabbitmq_username
    "rabbitmq.password"      = module.shared.test_rabbitmq_password
    "rabbitmq.erlang_cookie" = module.shared.test_rabbitmq_erlang_cookie
  }

  type = "Opaque"

  depends_on = [
    kubernetes_namespace.perf-flo
  ]

}





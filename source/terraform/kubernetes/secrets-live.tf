locals {
  # Database string
  live_db_server   = "live-flo.postgres.database.azure.com"
  live_db_name     = "flo2-live"
  live_db_username = "qxlva"
  live_db_password = module.shared.pg_live_pass
  # Generate the full PostgreSQL connection string
  live_db_connection_string = format(
    "Server=%s;Database=%s;Port=5432;User Id=%s;Password=%s;",
    local.live_db_server,
    local.live_db_name,
    local.live_db_username,
    local.live_db_password
  )

  # Blob Storage string
  live_blob_accountname = "liveflo"
  live_blob_accountkey  = module.shared.live_blob_account_key
  # Connection string for Azure Blob Storage
  live_blob_connection_string = format(
    "DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=%s;EndpointSuffix=core.windows.net",
    local.live_blob_accountname,
    local.live_blob_accountkey
  )

  live_github_token = module.shared.github_token
  live_github_connection_string = format(
    "https://Harris-HealthAlliance:%s@github.com/Harris-HealthAlliance/flo-v2.git",
    local.live_github_token
  )
}

resource "kubernetes_secret" "live-flo-all-secrets" {
  metadata {
    name      = "live-flo-all-secrets"
    namespace = kubernetes_namespace.live-flo.metadata[0].name
    labels = merge(
      {
        "app"                         = "shared"
        "environment"                 = "live"
        "kubernetes.io/metadata.name" = "live-flo"
        "name"                        = "live-flo"
    }, module.shared.k8s_main_labels)
  }

  data = {
    "db.connectionstring" = local.live_db_connection_string

    "azuread.tennant_id"     = module.shared.live_azuread_tenant_id
    "azureb2c.client_id"     = module.shared.live_azuread_client_id
    "azureb2c.client_secret" = module.shared.live_azuread_client_secret

    "blob.connectionString" = local.live_blob_connection_string

    "landInformationSearch.clientID"     = module.shared.landInformationSearch_clientID
    "landInformationSearch.clientSecret" = module.shared.landInformationSearch_clientSecret

    "cronJobs.apiKey"         = module.shared.live_cronjobs_api_key
    "teamcity.artifact_token" = module.shared.teamcity_artifact_token

    "smtp.username" = module.shared.smtp_username
    "smtp.password" = module.shared.smtp_password

    "govuk.notify_apikey" = module.shared.live_govuk_notify_apikey

    "onelogin.client_id"   = module.shared.live_onelogin_client_id
    "onelogin.private_key" = module.shared.live_onelogin_private_key

    "esri.APIKey"                                       = module.shared.esri_APIKey
    "esri.Forester.GenerateTokenService.Username"       = module.shared.esri_Forester_GenerateTokenService_Username
    "esri.Forester.GenerateTokenService.Password"       = module.shared.esri_Forester_GenerateTokenService_Password
    "esri.Forestry.GenerateTokenService.ClientID"       = module.shared.esri_Forestry_GenerateTokenService_ClientID
    "esri.Forestry.GenerateTokenService.ClientSecret"   = module.shared.esri_Forestry_GenerateTokenService_ClientSecret
    "esri.PublicRegister.GenerateTokenService.Username" = module.shared.esri_PublicRegister_GenerateTokenService_Username
    "esri.PublicRegister.GenerateTokenService.Password" = module.shared.esri_PublicRegister_GenerateTokenService_Password

    # PDF Generator (GitHub Token Embedded in Origin URL)
    "github.token" = local.live_github_connection_string

    "smtp.password" = module.shared.smtp_password
    "smtp.username" = module.shared.smtp_username

    # RabbitMQ
    "rabbitmq.username"      = module.shared.live_rabbitmq_username
    "rabbitmq.password"      = module.shared.live_rabbitmq_password
    "rabbitmq.erlang_cookie" = module.shared.live_rabbitmq_erlang_cookie
  }

  type = "Opaque"

  depends_on = [
    kubernetes_namespace.live-flo
  ]

}





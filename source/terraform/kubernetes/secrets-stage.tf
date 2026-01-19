locals {
  # Database string
  stage_db_server   = "staging-flo.postgres.database.azure.com"
  stage_db_name     = "flo2-staging"
  stage_db_username = "qxlva"
  stage_db_password = module.shared.pg_staging_pass
  # Generate the full PostgreSQL connection string
  stage_db_connection_string = format(
    "Server=%s;Database=%s;Port=5432;User Id=%s;Password=%s;",
    local.stage_db_server,
    local.stage_db_name,
    local.stage_db_username,
    local.stage_db_password
  )

  # Blob Storage string
  stage_blob_accountname = "stageflo"
  stage_blob_accountkey  = module.shared.stage_blob_account_key
  # Connection string for Azure Blob Storage
  stage_blob_connection_string = format(
    "DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=%s;EndpointSuffix=core.windows.net",
    local.stage_blob_accountname,
    local.stage_blob_accountkey
  )

  stage_github_token = module.shared.github_token
  stage_github_connection_string = format(
    "https://Harris-HealthAlliance:%s@github.com/Harris-HealthAlliance/flo-v2.git",
    local.stage_github_token
  )
}

resource "kubernetes_secret" "stage-flo-all-secrets" {
  metadata {
    name      = "stage-flo-all-secrets"
    namespace = kubernetes_namespace.stage-flo.metadata[0].name
    labels = merge(
      {
        "app"                         = "shared"
        "environment"                 = "stage"
        "kubernetes.io/metadata.name" = "stage-flo"
        "name"                        = "stage-flo"
    }, module.shared.k8s_main_labels)
  }

  data = {
    "db.connectionstring" = local.stage_db_connection_string

    "azuread.tennant_id"     = module.shared.stage_azuread_tenant_id
    "azureb2c.client_id"     = module.shared.stage_azuread_client_id
    "azureb2c.client_secret" = module.shared.stage_azuread_client_secret

    "blob.connectionString" = local.stage_blob_connection_string

    "landInformationSearch.clientID"     = module.shared.landInformationSearch_clientID
    "landInformationSearch.clientSecret" = module.shared.landInformationSearch_clientSecret

    "cronJobs.apiKey"         = module.shared.stage_cronjobs_api_key
    "teamcity.artifact_token" = module.shared.teamcity_artifact_token

    "smtp.username" = module.shared.smtp_username
    "smtp.password" = module.shared.smtp_password

    "govuk.notify_apikey" = module.shared.stage_govuk_notify_apikey

    "onelogin.client_id"   = module.shared.stage_onelogin_client_id
    "onelogin.private_key" = module.shared.stage_onelogin_private_key

    "esri.APIKey"                                       = module.shared.esri_APIKey
    "esri.Forester.GenerateTokenService.Username"       = module.shared.esri_Forester_GenerateTokenService_Username
    "esri.Forester.GenerateTokenService.Password"       = module.shared.esri_Forester_GenerateTokenService_Password
    "esri.Forestry.GenerateTokenService.ClientID"       = module.shared.esri_Forestry_GenerateTokenService_ClientID
    "esri.Forestry.GenerateTokenService.ClientSecret"   = module.shared.esri_Forestry_GenerateTokenService_ClientSecret
    "esri.PublicRegister.GenerateTokenService.Username" = module.shared.esri_PublicRegister_GenerateTokenService_Username
    "esri.PublicRegister.GenerateTokenService.Password" = module.shared.esri_PublicRegister_GenerateTokenService_Password

    # PDF Generator (GitHub Token Embedded in Origin URL)
    "github.token" = local.stage_github_connection_string

    "smtp.password" = module.shared.smtp_password
    "smtp.username" = module.shared.smtp_username

    # RabbitMQ
    "rabbitmq.username"      = module.shared.stage_rabbitmq_username
    "rabbitmq.password"      = module.shared.stage_rabbitmq_password
    "rabbitmq.erlang_cookie" = module.shared.stage_rabbitmq_erlang_cookie
  }

  type = "Opaque"

  depends_on = [
    kubernetes_namespace.stage-flo
  ]

}





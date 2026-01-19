locals {
  # Database string
  dev_db_server   = "dev-flo-postgresql"
  dev_db_name     = "flo"
  dev_db_username = "qxlva"
  dev_db_password = module.shared.dev_db_password
  # Generate the full PostgreSQL connection string
  dev_db_connection_string = format(
    "Server=%s;Database=%s;Port=5432;User Id=%s;Password=%s;",
    local.dev_db_server,
    local.dev_db_name,
    local.dev_db_username,
    local.dev_db_password
  )

  # Blob Storage string
  dev_blob_accountname = "devflo"
  dev_blob_accountkey  = module.shared.dev_blob_account_key
  # Connection string for Azure Blob Storage
  dev_blob_connection_string = format(
    "DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=%s;EndpointSuffix=core.windows.net",
    local.dev_blob_accountname,
    local.dev_blob_accountkey
  )

  dev_github_token = module.shared.github_token
  dev_github_connection_string = format(
    "https://Harris-HealthAlliance:%s@github.com/Harris-HealthAlliance/flo-v2.git",
    local.dev_github_token
  )
}

resource "kubernetes_secret" "dev-flo-all-secrets" {
  metadata {
    name      = "dev-flo-all-secrets"
    namespace = kubernetes_namespace.dev-flo.metadata[0].name
    labels = merge(
      {
        "app"                         = "shared"
        "environment"                 = "development"
        "kubernetes.io/metadata.name" = "dev-flo"
        "name"                        = "dev-flo"
    }, module.shared.k8s_main_labels)
  }

  data = {
    "db.username"         = local.dev_db_username
    "db.password"         = module.shared.dev_db_password
    "db.postgresPassword" = module.shared.dev_db_postgresPassword
    "db.connectionstring" = local.dev_db_connection_string

    "azuread.tennant_id"     = module.shared.dev_azuread_tenant_id
    "azureb2c.client_id"     = module.shared.dev_azuread_client_id
    "azureb2c.client_secret" = module.shared.dev_azuread_client_secret

    "blob.connectionString" = local.dev_blob_connection_string

    "landInformationSearch.clientID"     = module.shared.landInformationSearch_clientID
    "landInformationSearch.clientSecret" = module.shared.landInformationSearch_clientSecret

    "cronJobs.apiKey"         = module.shared.dev_cronjobs_api_key
    "teamcity.artifact_token" = module.shared.teamcity_artifact_token

    "smtp.username" = module.shared.smtp_username
    "smtp.password" = module.shared.smtp_password

    "govuk.notify_apikey" = module.shared.dev_govuk_notify_apikey

    "onelogin.client_id"   = module.shared.dev_onelogin_client_id
    "onelogin.private_key" = module.shared.dev_onelogin_private_key

    "esri.APIKey"                                       = module.shared.esri_APIKey
    "esri.Forester.GenerateTokenService.Username"       = module.shared.esri_Forester_GenerateTokenService_Username
    "esri.Forester.GenerateTokenService.Password"       = module.shared.esri_Forester_GenerateTokenService_Password
    "esri.Forestry.GenerateTokenService.ClientID"       = module.shared.esri_Forestry_GenerateTokenService_ClientID
    "esri.Forestry.GenerateTokenService.ClientSecret"   = module.shared.esri_Forestry_GenerateTokenService_ClientSecret
    "esri.PublicRegister.GenerateTokenService.Username" = module.shared.esri_PublicRegister_GenerateTokenService_Username
    "esri.PublicRegister.GenerateTokenService.Password" = module.shared.esri_PublicRegister_GenerateTokenService_Password

    # PDF Generator (GitHub Token Embedded in Origin URL)
    "github.token" = local.dev_github_connection_string

    "smtp.password" = module.shared.smtp_password
    "smtp.username" = module.shared.smtp_username

    # RabbitMQ
    "rabbitmq.username"      = module.shared.dev_rabbitmq_username
    "rabbitmq.password"      = module.shared.dev_rabbitmq_password
    "rabbitmq.erlang_cookie" = module.shared.dev_rabbitmq_erlang_cookie
  }

  type = "Opaque"

  depends_on = [
    kubernetes_namespace.dev-flo
  ]

}





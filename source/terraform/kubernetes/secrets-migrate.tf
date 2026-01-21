locals {
  # Database string
  migrate_db_server   = "migrate-flo.postgres.database.azure.com"
  migrate_db_name     = "flo2-migrate"
  migrate_db_username = "qxlva"
  migrate_db_password = module.shared.pg_migrate_pass
  # Generate the full PostgreSQL connection string
  migrate_db_connection_string = format(
    "Server=%s;Database=%s;Port=5432;User Id=%s;Password=%s;",
    local.migrate_db_server,
    local.migrate_db_name,
    local.migrate_db_username,
    local.migrate_db_password
  )

  # Blob Storage string
  migrate_blob_accountname = "migrateflo"
  migrate_blob_accountkey  = module.shared.migrate_blob_account_key
  # Connection string for Azure Blob Storage
  migrate_blob_connection_string = format(
    "DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=%s;EndpointSuffix=core.windows.net",
    local.migrate_blob_accountname,
    local.migrate_blob_accountkey
  )

  migrate_github_token = module.shared.github_token
  migrate_github_connection_string = format(
    "https://Harris-HealthAlliance:%s@github.com/Harris-HealthAlliance/flo-v2.git",
    local.migrate_github_token
  )
}

resource "kubernetes_secret" "migrate-flo-all-secrets" {
  metadata {
    name      = "migrate-flo-all-secrets"
    namespace = kubernetes_namespace.migrate-flo.metadata[0].name
    labels = merge(
      {
        "app"                         = "shared"
        "environment"                 = "migrate"
        "kubernetes.io/metadata.name" = "migrate-flo"
        "name"                        = "migrate-flo"
    }, module.shared.k8s_main_labels)
  }

  data = {
    "db.connectionstring" = local.migrate_db_connection_string

    "azuread.tennant_id"     = module.shared.migrate_azuread_tenant_id
    "azureb2c.client_id"     = module.shared.migrate_azuread_client_id
    "azureb2c.client_secret" = module.shared.migrate_azuread_client_secret

    "blob.connectionString" = local.migrate_blob_connection_string

    "landInformationSearch.clientID"     = module.shared.landInformationSearch_clientID
    "landInformationSearch.clientSecret" = module.shared.landInformationSearch_clientSecret

    "cronJobs.apiKey"         = module.shared.migrate_cronjobs_api_key
    "teamcity.artifact_token" = module.shared.teamcity_artifact_token

    "smtp.username" = module.shared.smtp_username
    "smtp.password" = module.shared.smtp_password

    "govuk.notify_apikey" = module.shared.migrate_govuk_notify_apikey

    "onelogin.client_id"   = module.shared.migrate_onelogin_client_id
    "onelogin.private_key" = module.shared.migrate_onelogin_private_key

    "esri.APIKey"                                       = module.shared.esri_APIKey
    "esri.Forester.GenerateTokenService.Username"       = module.shared.esri_Forester_GenerateTokenService_Username
    "esri.Forester.GenerateTokenService.Password"       = module.shared.esri_Forester_GenerateTokenService_Password
    "esri.Forestry.GenerateTokenService.ClientID"       = module.shared.esri_Forestry_GenerateTokenService_ClientID
    "esri.Forestry.GenerateTokenService.ClientSecret"   = module.shared.esri_Forestry_GenerateTokenService_ClientSecret
    "esri.PublicRegister.GenerateTokenService.Username" = module.shared.esri_PublicRegister_GenerateTokenService_Username
    "esri.PublicRegister.GenerateTokenService.Password" = module.shared.esri_PublicRegister_GenerateTokenService_Password

    # PDF Generator (GitHub Token Embedded in Origin URL)
    "github.token" = local.migrate_github_connection_string

    "smtp.password" = module.shared.smtp_password
    "smtp.username" = module.shared.smtp_username

    # RabbitMQ
    "rabbitmq.username"      = module.shared.migrate_rabbitmq_username
    "rabbitmq.password"      = module.shared.migrate_rabbitmq_password
    "rabbitmq.erlang_cookie" = module.shared.migrate_rabbitmq_erlang_cookie
  }

  type = "Opaque"

  depends_on = [
    kubernetes_namespace.migrate-flo
  ]

}





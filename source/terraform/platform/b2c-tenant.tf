resource "azurerm_aadb2c_directory" "dev" {
  provider                = azurerm.cli
  country_code            = "GB"
  data_residency_location = "Europe"
  display_name            = "DEV FLOv2"
  domain_name             = "devflo2.onmicrosoft.com"
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  sku_name                = "PremiumP2"
}

# resource "azuread_application" "dev_web_app" {
#   provider     = azuread.dev
#   display_name = "FLO v2 Development TEST"

#   sign_in_audience = "AzureADandPersonalMicrosoftAccount"

#   web {
#     logout_url    = "https://devflo.qxlva.io/signout-callback-oidc"
#     redirect_uris = ["https://localhost:7253/signin-oidc", "https://localhost:7254/signin-oidc", "https://devflo.qxlva.io/signin-oidc", "https://internaldevflo.qxlva.io/signin-oidc"]

#     implicit_grant {
#       id_token_issuance_enabled = true
#     }
#   }

#   api {
#     requested_access_token_version = "2"
#   }

#   required_resource_access {
#     resource_app_id = "00000003-0000-0000-c000-000000000000"

#     resource_access {
#       id   = "37f7f235-527c-4136-accd-4a02d197296e"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "7427e0e9-2fba-42fe-b0c0-848c9e6a8182"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "2903d63d-4611-4d43-99ce-a33f3f52e343"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "281892cc-4dbf-4e3a-b6cc-b21029bb4e82"
#       type = "Scope"
#     }

#     # Directory.Read.All
#     resource_access {
#       id   = "7ab1d382-f21e-4acd-a863-ba3e13f7da61"
#       type = "Role"
#     }

#     # Directory.ReadWrite.All
#     resource_access {
#       id   = "19dbc75e-c2e2-444c-a770-ec69d8559fc7"
#       type = "Role"
#     }

#     # User.Read.All
#     resource_access {
#       id   = "df021288-bdef-4463-88db-98f22de89214"
#       type = "Role"
#     }

#     # User.ReadWrite.All
#     resource_access {
#       id   = "741f803b-c850-494e-b5df-cde7c675a1ca"
#       type = "Role"
#     }
#   }
# }

# resource "azuread_application_password" "dev" {
#   provider       = azuread.dev
#   display_name   = "FLOv2"
#   application_id = azuread_application.dev_web_app.id
#   end_date       = timeadd(timestamp(), "87600h")
# }

# output "dev_password" {
#   value     = azuread_application_password.dev.value
#   sensitive = true
# }

resource "azurerm_aadb2c_directory" "test" {
  provider                = azurerm.cli
  country_code            = "GB"
  data_residency_location = "Europe"
  display_name            = "TEST FLOv2"
  domain_name             = "testflo2.onmicrosoft.com"
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  sku_name                = "PremiumP2"
}

# resource "azuread_application" "test_web_app" {
#   provider     = azuread.test
#   display_name = "FLO v2 Test"

#   sign_in_audience = "AzureADandPersonalMicrosoftAccount"

#   web {
#     logout_url    = "https://testflo.qxlva.io/signout-callback-oidc"
#     redirect_uris = ["https://localhost:7253/signin-oidc", "https://localhost:7254/signin-oidc", "https://testflo.qxlva.io/signin-oidc", "https://internaltestflo.qxlva.io/signin-oidc"]

#     implicit_grant {
#       id_token_issuance_enabled = true
#     }
#   }

#   api {
#     requested_access_token_version = "2"
#   }

#   required_resource_access {
#     resource_app_id = "00000003-0000-0000-c000-000000000000"

#     resource_access {
#       id   = "37f7f235-527c-4136-accd-4a02d197296e"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "7427e0e9-2fba-42fe-b0c0-848c9e6a8182"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "2903d63d-4611-4d43-99ce-a33f3f52e343"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "281892cc-4dbf-4e3a-b6cc-b21029bb4e82"
#       type = "Scope"
#     }

#     # Directory.Read.All
#     resource_access {
#       id   = "7ab1d382-f21e-4acd-a863-ba3e13f7da61"
#       type = "Role"
#     }

#     # Directory.ReadWrite.All
#     resource_access {
#       id   = "19dbc75e-c2e2-444c-a770-ec69d8559fc7"
#       type = "Role"
#     }

#     # User.Read.All
#     resource_access {
#       id   = "df021288-bdef-4463-88db-98f22de89214"
#       type = "Role"
#     }

#     # User.ReadWrite.All
#     resource_access {
#       id   = "741f803b-c850-494e-b5df-cde7c675a1ca"
#       type = "Role"
#     }
#   }
# }

# resource "azuread_application_password" "test" {
#   provider       = azuread.test
#   display_name   = "FLOv2"
#   application_id = azuread_application.test_web_app.id
#   end_date       = timeadd(timestamp(), "87600h")
# }

# output "test_password" {
#   value     = azuread_application_password.test.value
#   sensitive = true
# }

resource "azurerm_aadb2c_directory" "staging" {
  provider                = azurerm.cli
  country_code            = "GB"
  data_residency_location = "Europe"
  display_name            = "STAGING FLOv2"
  domain_name             = "stageflo2.onmicrosoft.com"
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  sku_name                = "PremiumP2"
}

# resource "azuread_application" "stage_web_app" {
#   provider     = azuread.staging
#   display_name = "FLO v2 Staging"

#   sign_in_audience = "AzureADandPersonalMicrosoftAccount"

#   web {
#     logout_url    = "https://stageflo.qxlva.io/signout-callback-oidc"
#     redirect_uris = ["https://localhost:7253/signin-oidc", "https://localhost:7254/signin-oidc", "https://stageflo.qxlva.io/signin-oidc", "https://internalstageflo.qxlva.io/signin-oidc"]

#     implicit_grant {
#       id_token_issuance_enabled = true
#     }
#   }

#   api {
#     requested_access_token_version = "2"
#   }

#   required_resource_access {
#     resource_app_id = "00000003-0000-0000-c000-000000000000"

#     resource_access {
#       id   = "37f7f235-527c-4136-accd-4a02d197296e"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "7427e0e9-2fba-42fe-b0c0-848c9e6a8182"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "2903d63d-4611-4d43-99ce-a33f3f52e343"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "281892cc-4dbf-4e3a-b6cc-b21029bb4e82"
#       type = "Scope"
#     }

#     # Directory.Read.All
#     resource_access {
#       id   = "7ab1d382-f21e-4acd-a863-ba3e13f7da61"
#       type = "Role"
#     }

#     # Directory.ReadWrite.All
#     resource_access {
#       id   = "19dbc75e-c2e2-444c-a770-ec69d8559fc7"
#       type = "Role"
#     }

#     # User.Read.All
#     resource_access {
#       id   = "df021288-bdef-4463-88db-98f22de89214"
#       type = "Role"
#     }

#     # User.ReadWrite.All
#     resource_access {
#       id   = "741f803b-c850-494e-b5df-cde7c675a1ca"
#       type = "Role"
#     }
#   }
# }

# resource "azuread_application_password" "stage" {
#   provider       = azuread.staging
#   display_name   = "FLOv2"
#   application_id = azuread_application.stage_web_app.id
#   end_date       = timeadd(timestamp(), "87600h")
# }

# output "stage_password" {
#   value     = azuread_application_password.stage.value
#   sensitive = true
# }

resource "azurerm_aadb2c_directory" "live" {
  provider                = azurerm.cli
  country_code            = "GB"
  data_residency_location = "Europe"
  display_name            = "LIVE FLOv2"
  domain_name             = "liveflo.onmicrosoft.com"
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  sku_name                = "PremiumP2"
}

# resource "azuread_application" "live_web_app" {
#   provider     = azuread.live
#   display_name = "FLO v2 Live"

#   sign_in_audience = "AzureADandPersonalMicrosoftAccount"

#   web {
#     logout_url    = "https://liveflo.qxlva.io/signout-callback-oidc"
#     redirect_uris = ["https://localhost:7253/signin-oidc", "https://localhost:7254/signin-oidc", "https://liveflo.qxlva.io/signin-oidc", "https://internalliveflo.qxlva.io/signin-oidc"]

#     implicit_grant {
#       id_token_issuance_enabled = true
#     }
#   }

#   api {
#     requested_access_token_version = "2"
#   }

#   required_resource_access {
#     resource_app_id = "00000003-0000-0000-c000-000000000000"

#     resource_access {
#       id   = "37f7f235-527c-4136-accd-4a02d197296e"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "7427e0e9-2fba-42fe-b0c0-848c9e6a8182"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "2903d63d-4611-4d43-99ce-a33f3f52e343"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "281892cc-4dbf-4e3a-b6cc-b21029bb4e82"
#       type = "Scope"
#     }

#     # Directory.Read.All
#     resource_access {
#       id   = "7ab1d382-f21e-4acd-a863-ba3e13f7da61"
#       type = "Role"
#     }

#     # Directory.ReadWrite.All
#     resource_access {
#       id   = "19dbc75e-c2e2-444c-a770-ec69d8559fc7"
#       type = "Role"
#     }

#     # User.Read.All
#     resource_access {
#       id   = "df021288-bdef-4463-88db-98f22de89214"
#       type = "Role"
#     }

#     # User.ReadWrite.All
#     resource_access {
#       id   = "741f803b-c850-494e-b5df-cde7c675a1ca"
#       type = "Role"
#     }
#   }
# }

# resource "azuread_application_password" "live" {
#   provider       = azuread.live
#   display_name   = "FLOv2"
#   application_id = azuread_application.live_web_app.id
#   end_date       = timeadd(timestamp(), "87600h")
# }

# output "live_password" {
#   value     = azuread_application_password.live.value
#   sensitive = true
# }

resource "azurerm_aadb2c_directory" "migrate" {
  provider                = azurerm.cli
  country_code            = "GB"
  data_residency_location = "Europe"
  display_name            = "MIGRATE FLOv2"
  domain_name             = "migrateflo.onmicrosoft.com"
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  sku_name                = "PremiumP2"
}

# resource "azuread_application" "migrate_web_app" {
#   provider     = azuread.migrate
#   display_name = "FLO v2 Migrate"

#   sign_in_audience = "AzureADandPersonalMicrosoftAccount"

#   web {
#     logout_url    = "https://migrateflo.qxlva.io/signout-callback-oidc"
#     redirect_uris = ["https://localhost:7253/signin-oidc", "https://localhost:7254/signin-oidc", "https://migrateflo.qxlva.io/signin-oidc", "https://internalmigrateflo.qxlva.io/signin-oidc"]

#     implicit_grant {
#       id_token_issuance_enabled = true
#     }
#   }

#   api {
#     requested_access_token_version = "2"
#   }

#   required_resource_access {
#     resource_app_id = "00000003-0000-0000-c000-000000000000"

#     resource_access {
#       id   = "37f7f235-527c-4136-accd-4a02d197296e"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "7427e0e9-2fba-42fe-b0c0-848c9e6a8182"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "2903d63d-4611-4d43-99ce-a33f3f52e343"
#       type = "Scope"
#     }

#     resource_access {
#       id   = "281892cc-4dbf-4e3a-b6cc-b21029bb4e82"
#       type = "Scope"
#     }

#     # Directory.Read.All
#     resource_access {
#       id   = "7ab1d382-f21e-4acd-a863-ba3e13f7da61"
#       type = "Role"
#     }

#     # Directory.ReadWrite.All
#     resource_access {
#       id   = "19dbc75e-c2e2-444c-a770-ec69d8559fc7"
#       type = "Role"
#     }

#     # User.Read.All
#     resource_access {
#       id   = "df021288-bdef-4463-88db-98f22de89214"
#       type = "Role"
#     }

#     # User.ReadWrite.All
#     resource_access {
#       id   = "741f803b-c850-494e-b5df-cde7c675a1ca"
#       type = "Role"
#     }
#   }
# }

# resource "azuread_application_password" "migrate" {
#   provider       = azuread.migrate
#   display_name   = "FLOv2"
#   application_id = azuread_application.migrate_web_app.id
#   end_date       = timeadd(timestamp(), "87600h")
# }

# output "migrate_password" {
#   value     = azuread_application_password.migrate.value
#   sensitive = true
# }

#In order to complete the Azure B2C tenant, there are manual steps that are required to be run. Please see flo-v2\source\terraform\platform\values\tenant\readme.md for more info
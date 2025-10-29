# Configuration
The following table details each of the main configuration values that are expected by the system (standard .NET configuration entries such as log levels have been omitted).

Anyone carrying out deployments should pay particular attention to the fields listed as 'Always required', as these really must be set in every deployment.

Key                                                                                 | Always required | Default                                           | Description 
------------------------------------------------------------------------------------| --------------- | ------------------------------------------------- | -----------
ConnectionStrings:DefaultConnection                                                 | Y               | n/a                                               | The Postgres connection string to the main FLO database including server location, schema, username, and password.
AzureAdB2C:Instance                                                                 | Y               | n/a                                               | The URL of the Azure AD B2C instance for user registration
AzureAdB2C:ClientId                                                                 | Y               | n/a                                               | The client ID of the Azure AD B2C instance for user registration
AzureAdB2C:ClientSecret                                                             | Y               | n/a                                               | The client secret of the Azure AD B2C instance for user registration
AzureAdB2C:Domain                                                                   | Y               | n/a                                               | The domain URL of the Azure AD B2C instance for user registration
AzureAdB2C:SignedOutCallbackPath                                                    | Y               | n/a                                               | The URL for the sign-out callback
AzureAdB2C:SignUpSignInPolicyId                                                     | Y               | n/a                                               | The sign-up sign-in policy id of the Azure AD B2C instance for user registration
QuicksilvaVirusScannerService:AvEndpoint                                            | Y               | n/a                                               | The full URL to Quicksilva''s anti-virus & malware detection service (when using `QuicksilvaVirusScannerService`)
QuicksilvaVirusScannerService:IsEnabled                                             | N               | Y                                                 | Enable the anti-virus & malware detection service (`QuicksilvaVirusScannerService`). Disabling will not stop files being processed
FileStorage:StorageRootPath                                                         | N               | n/a                                               | The root path (when using `PersistentDiskFileStorageService`) where all documents in the system will be stored within
FileStorage:ConnectionString                                                        | N               | n/a                                               | The connection string to use for the Azure File Storage
FileStorage:Container                                                               | N               | n/a                                               | The container name to use in the Azure File Storage
UserFileUpload:MaxFileSizeBytes                                                     | Y               | 33554432 (32Mb)                                   | The Maximum size of a file which can be uploaded by an internal user
UserFileUpload:MaxNumberOfDocuments                                                 | Y               | 50                                                | The Maximum number of documents which can be uploaded by an internal user against a sinlge FLA
ApiSecurity:AuthenticationHeaderKey                                                 | Y               | X-Api-Key                                         | The Name of the HTTP Header which will store the API Key value for a connecting system to use.
ApiSecurity:AuthenticationHeaderValue                                               | Y               | iazo54uwhDnydbAqbrHcDvUr4UTf4w5zr1hKwSm4JxJGNvCsS | The Value of the HTTP Header key which a connecting system will need to supply to gain access to relevant end-points.
ApiFileUpload:MaxFileSizeBytes                                                      | Y               | 33554432 (32Mb)                                   | The Maximum size of a file which will be processed by the system (`FileStorage` service)
LandInformationSearch:DeepLinkUrlAndPath                                            | Y               | n/a                                               | The deeplink URL into the Forester web LIS tool.
LandInformationSearch:LisConfig                                                     | Y               | n/a                                               | The esri supplied value sent to the LIS as part of the deeplink
LandInformationSearch:BaseUrl                                                       | Y               | n/a                                               | The full URL to the AGOL
LandInformationSearch:FeaturePath                                                   | Y               | n/a                                               | Path to the Feature service to push Case ID attribute and submitted compartment geometries
LandInformationSearch:TokenUrl                                                      | Y               | n/a                                               | Get Token Url
LandInformationSearch:TokenPath                                                     | Y               | n/a                                               | Path at the Get Token Url
LandInformationSearch:ClientId                                                      | Y               | n/a                                               | The client id to use to connect to the AGOL feature service
LandInformationSearch:ClientSecret                                                  | Y               | n/a                                               | The client secret to use to connect to the AGOL feature service
WoodlandOfficerReview:PublicRegisterPeriod                                          | Y               | n/a                                               | The default length of time an application should remain on the public register; used to calculate expiry timestamp
WoodlandOfficerReview:DefaultCaseTypeOnPublishToPublicRegister                      | N               | 02                                                | The default case type code sent in the request to ESRI to publish the application to the consultation public register
WoodlandOfficerReview:DefaultCaseStatusOnPublishForMobileApps                       | N               | 02                                                | The default case status code sent in the request to ESRI to add the application to the mobile app layers
WoodlandOfficerReview:UseDevPublicRegister                                          | N               | false                                             | A flag to indicate that a fake Public Register integration should be used (just logs PR interactions to the log file). For Dev environments.
WoodlandOfficerReview:UseDevMobileAppsLayer                                         | N               | false                                             | A flag to indicate that a fake ESRI mobile apps integration should be used (just logs interactions to the log file). For Dev environments.
ExternalApplicantSite:BaseUrl                                                       | Y               | n/a                                               | The base URL for the external applicant site, for notifications
UserInvite:InviteLinkExpiryDays                                                     | Y               | n/a                                               | The number of days that user access links are valid for
EsriConfig:Forestry:BaseUrl																      | Y				|n/a                                                | All the Root URL ALL services should run from 
EsriConfig:Forestry:GenerateTokenService:Path											      | Y				| oauth2/token									    | The path to the token Service
EsriConfig:Forestry:GenerateTokenService:ClientID									          | Y			    | n/a                 							    | The ID to give the token service
EsriConfig:Forestry:GenerateTokenService:ClientSecret								          | Y				| n/a                             				    | The Password for the token service
EsriConfig:Forestry:GenerateTokenService:ApiKey												  | Y				| n/a                             				    | The ForestryService API passed with certain calls to ensure valid use
EsriConfig:Forestry:FeaturesService:IsPublic        									      | Y				| false               							    | If the URI is to use the AGOL Base URI
EsriConfig:Forestry:FeaturesService:NeedsToken        									      | Y				| true               							    | If the service requires a token generated
EsriConfig:Forestry:FeaturesService:Path												      | Y				| content/features								    | The path to the service
EsriConfig:Forestry:FeaturesService:GenerateService										      | Y				| n/a												| Contains all the "Server" side settings needed to upload and convert a shape file
EsriConfig:Forestry:FeaturesService:GenerateService:Path								      | Y				| generate										    | The Path to the service
EsriConfig:Forestry:FeaturesService:GenerateService:SupportedFileImports				      | Y				| [ ".zip", ".kml", ".kmz", ".json", ".geojson" ]	| The supported file types
EsriConfig:Forestry:FeaturesService:GenerateService:MaxRecords							      | Y				| 1000											    | The max number of records to process 
EsriConfig:Forestry:FeaturesService:GenerateService:EnforceInputFileSizeLimit			      | Y				| true											    | Should file limits be enforced
EsriConfig:Forestry:FeaturesService:EnforceOutputJsonSizeLimitGenerateService			      | Y				| true											    | Should the output file be enforced
EsriConfig:Forestry:GeometryService:Path                                                      | Y               | n/a                                               | The path to the service 
EsriConfig:Forestry:GeometryService:IsPublic        									      | Y				| n/a               							    | If the URI is to use the AGOL Base URI
EsriConfig:Forestry:GeometryService:NeedsToken        									      | Y				| n/a              							        | If the service requires a token generated
EsriConfig:Forestry:GeometryService:IntersectService:Path                                     | Y               | n/a                                               | The path to the service 
EsriConfig:Forestry:GeometryService:Area:Path												  | Y               | n/a                                               | The path to the service 
EsriConfig:Forestry:GeometryService:ProjectService:Path                                       | Y               | n/a                                               | The path to the service 
EsriConfig:Forestry:GeometryService:ProjectService:OutSR                                      | Y               | n/a                                               | The Projection code that points should be returned in
EsriConfig:Forestry:GeometryService:ProjectService:GridLength                                 | Y               | n/a                                               | The number of characters in the Grid format
EsriConfig:Forestry:GeometryService:ProjectService:IncludeSpaces                              | Y               | n/a                                               | If the grid should use spaces
EsriConfig:Forestry:GeometryService:UnionService:Path									      | Y               | n/a                                               | The path to the service 
EsriConfig:Forestry:LayerServices														      | Y               | n/a                                               | A collection of layers that can be qurreid
EsriConfig:Forester:BaseUrl																	  | Y				| n/a                                               | All the Root URL ALL services should run from 
EsriConfig:Forester:GenerateTokenService												      | Y               | n/a                                               | The settings to generate the Token
EsriConfig:Forester:GenerateTokenService:Username											  | Y			    | n/a                 							    | The ID to give the token service
EsriConfig:Forester:GenerateTokenService:Password										      | Y				| n/a                             				    | The Password for the token service
EsriConfig:Forester:GenerateTokenService:Path												  | Y               | n/a                                               | The path to the service 
EsriConfig:Forester:UtilitesService:Path                                                      | Y               | n/a                                               | The path to the service 
EsriConfig:Forester:UtilitesService:IsPublic        									      | Y				| n/a               							    | If the URI is to use the AGOL Base URI
EsriConfig:Forester:UtilitesService:NeedsToken        									      | Y				| n/a              							        | If the service requires a token generated
EsriConfig:Forester:UtilitesService:ExportService:Path										  | Y               | n/a                                               | The path to the service 
EsriConfig:Forester:UtilitesService:ExportService:DefaultFormat                               | Y               | PGN8                                              | The Default format to return the image in 
EsriConfig:Forester:UtilitesService:ExportService:BaseMap									  | Y               | n/a                                               | The URI of the BaseMap to use
EsriConfig:Forester:UtilitesService:ExportService:BaseMapIDs								  | Y				| n/a												| The ID of the base map
EsriConfig:Forester:UtilitesService:ExportService:TextOverrides:Copyright					  | Y				| n/a												| The text to use for the copyright
EsriConfig:Forester:UtilitesService:ExportService:TextOverrides:FellingTitle				  | Y				| n/a												| The text to use for the Felling Title
EsriConfig:Forester:UtilitesService:ExportService:TextOverrides:RestockingTitle				  | Y				| n/a												| The text to use for the RestockingTitle Title
EsriConfig:Forester:UtilitesService:JobStatusService:Path								      | Y				| n/a												| The path to the service
EsriConfig:Forester:UtilitesService:JobStatusService:Status:FailedStates					  | Y				| n/a												| The states that come under the failed status
EsriConfig:Forester:UtilitesService:JobStatusService:Status:SuccessStates					  | Y				| n/a												| The states that come under the success status
EsriConfig:Forester:UtilitesService:JobStatusService:Status:PendingStates					  | Y				| n/a												| The pending states that tell the service to keep trying
EsriConfig:PublicRegister:BaseUrl														  | Y				| n/a                                               | All the Root URL ALL services should run from 
EsriConfig:PublicRegister:GenerateTokenService                                            | Y               | n/a                                               | The settings to generate the Token
EsriConfig:PublicRegister:GenerateTokenService:Username                                   | Y			    | n/a                 							    | The ID to give the token service
EsriConfig:PublicRegister:GenerateTokenService:Password								      | Y				| n/a                             				    | The Password for the token service
EsriConfig:PublicRegister:GenerateTokenService:Path                                       | Y               | n/a                                               | The path to the service 
EsriConfig:PublicRegister:Boundaries                                                      | Y               | n/a                                               | The settings for the boundary layer 
EsriConfig:PublicRegister:Boundaries:Path                                                 | Y               | n/a                                               | The path to the service 
EsriConfig:PublicRegister:Compartments                                                    | Y               | n/a                                               | The settings for the compartment layer
EsriConfig:PublicRegister:Compartments:Path                                               | Y               | n/a                                               | The path to the service 
EsriConfig:PublicRegister:Comments                                                        | Y               | n/a                                               | The settings for the comments layer
EsriConfig:PublicRegister:Comments:Path                                                   | Y               | n/a                                               | The path to the service 
EsriConfig:SpatialReference																  | Y				| 27700												| The projection system to use in the application
EsriConfig:RequestFormat																  | Y				| json												| The format the JSON will return
ConditionsBuilder:ConditionA:ConditionText                                          | Y               | n/a                                               | The array of lines of text making up Condition A
ConditionsBuilder:ConditionA:ConditionParameters[0]:Index                           | Y               | n/a                                               | The index of the replacer within the conditions text that this parameter is for
ConditionsBuilder:ConditionA:ConditionParameters[0]:DefaultValue                    | N               | ""                                                | The default value for this conditions text parameter
ConditionsBuilder:ConditionA:ConditionParameters[0]:Description                     | N               | ""                                                | A description of the parameter to aid the user
ConditionsBuilder:ConditionB:ConditionText                                          | Y               | n/a                                               | The array of lines of text making up Condition B
ConditionsBuilder:ConditionB:ConditionParameters[0]:Index                           | Y               | n/a                                               | The index of the replacer within the conditions text that this parameter is for
ConditionsBuilder:ConditionB:ConditionParameters[0]:DefaultValue                    | N               | ""                                                | The default value for this conditions text parameter
ConditionsBuilder:ConditionB:ConditionParameters[0]:Description                     | N               | ""                                                | A description of the parameter to aid the user
ConditionsBuilder:ConditionC:ConditionText                                          | Y               | n/a                                               | The array of lines of text making up Condition C
ConditionsBuilder:ConditionC:ConditionParameters[0]:Index                           | Y               | n/a                                               | The index of the replacer within the conditions text that this parameter is for
ConditionsBuilder:ConditionC:ConditionParameters[0]:DefaultValue                    | N               | ""                                                | The default value for this conditions text parameter
ConditionsBuilder:ConditionC:ConditionParameters[0]:Description                     | N               | ""                                                | A description of the parameter to aid the user
DocumentVisibilities:ExternalLisConstraintReport:VisibleToApplicant					| Y               | n/a                                               | A flag representing whether External LIS Constraint Reports are visible to external applicants
DocumentVisibilities:ExternalLisConstraintReport:VisibleToConsultee					| Y               | n/a                                               | A flag representing whether External LIS Constraint Reports are visible to external consultees
DocumentVisibilities:FcLisConstraintReport:VisibleToApplicant						| Y               | n/a                                               | A flag representing whether FC LIS constraint reports are visible to external applicants
DocumentVisibilities:FcLisConstraintReport:VisibleToConsultee						| Y               | n/a                                               | A flag representing whether FC LIS constraint reports are visible to external consultees
DocumentVisibilities:SiteVisitAttachment:VisibleToApplicant							| Y               | n/a                                               | A flag representing whether Site Visit Attachments are visible to external applicants
DocumentVisibilities:SiteVisitAttachment:VisibleToConsultee							| Y               | n/a                                               | A flag representing whether Site Visit Attachments are visible to external consultees
DocumentVisibilities:ApplicationDocument:VisibleToApplicant							| Y               | n/a                                               | A flag representing whether Application Documents are visible to external applicants
DocumentVisibilities:ApplicationDocument:VisibleToConsultee							| Y               | n/a                                               | A flag representing whether Application Documents are visible to external consultees
ApplicationExtension:ExtensionLength												| Y               | n/a                                               | The length of an extension if the final action date is surpassed without the application being processed
ApplicationExtension:ThresholdBeforeFinalActionDate									| Y               | n/a                                               | The time prior to the final action date that notifications should start being sent to assigned FC staff members
PublicRegisterExpiry:ThresholdBeforePublicRegisterPeriodEnd							| Y               | n/a                                               | The time prior to the public register period end date that notifications should start being sent to assigned FC staff members
VoluntaryWithdrawApplication:ThresholdAfterWithApplicantStatusDate					| Y               | n/a                                               | The time prior to the status of an application being set to with applicant before the applicant will be notified that they can volunteer to withdraw the application
PDFGeneratorAPI:Baseurl																| Y               | n/a                                               | The URL path for requesting the pdf from the pdf generator api for producing the application
RabbitMqOptions:Url																    | Y               | n/a                                               | The URL path for communicating with the RabbitMQ messaging service
RabbitMqOptions:Username														    | Y               | n/a                                               | The username for authenticating with the RabbitMQ messaging service
RabbitMqOptions:Password															| Y               | n/a                                               | The password for authenticating with the RabbitMQ messaging service
FellingLicenceApplicationOptions:CitizensCharterDateLength							| N               | 77.00:00:00                                       | The timespan following receipt of an application to calculate the citizens charter date
FellingLicenceApplicationOptions:FinalActionDateDaysFromSubmission					| Y               | n/a												  | The number of days following submission that an application's final action date should be set to
PermittedRegisteredUser:PermittedEmailDomainsForRegisteredUser						| Y				  | qxlva.com & forestrycommission.gov.uk             | The list of permitted email domains which are checked when a user authorised in AD B2C is then authorised in the FLO internal app
SiteAnalytics:GoogleAnalytics:Enabled						                              | Y               | n/a												| Is Google Analytics enabled on the site (a visitor/user who does not provide consent via cookie will effectively override this setting if this flag is set to true during their visit/session)
SiteAnalytics:GoogleAnalytics:TrackingId						                          | Y               | n/a												| The unique Google Analytics tracking id
SiteAnalytics:MicrosoftClarity:Enabled						                              | Y               | n/a												| Is Microsoft Clarity enabled on the site (a visitor/user who does not provide consent via cookie will effectively override this setting if this flag is set to true during their visit/session)
SiteAnalytics:MicrosoftClarity:TrackingId						                          | Y               | n/a												| The unique Microsoft Clarity  tracking id
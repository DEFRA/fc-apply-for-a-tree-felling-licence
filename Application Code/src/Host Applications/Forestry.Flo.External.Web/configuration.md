# Configuration
The following table details each of the main configuration values that are expected by the system (standard .NET configuration entries such as log levels have been omitted).

Anyone carrying out deployments should pay particular attention to the fields listed as 'Always required', as these really must be set in every deployment.

Key                                                                                       | Always required | Default                                           | Description 
------------------------------------------------------------------------------------------| --------------- | ------------------------------------------------- | -----------
ConnectionStrings:DefaultConnection                                                       | Y               | n/a                                               | The Postgres connection string to the main FLO database including server location, schema, username, and password.
AzureAdB2C:Instance                                                                       | Y               | n/a                                               | The URL of the Azure AD B2C instance for user registration
AzureAdB2C:ClientId                                                                       | Y               | n/a                                               | The client ID of the Azure AD B2C instance for user registration
AzureAdB2C:ClientSecret                                                                   | Y               | n/a                                               | The client secret of the Azure AD B2C instance for user registration
AzureAdB2C:Domain                                                                         | Y               | n/a                                               | The domain URL of the Azure AD B2C instance for user registration
AzureAdB2C:SignedOutCallbackPath                                                          | Y               | n/a                                               | The URL for the sign-out callback
AzureAdB2C:SignInPolicyId																  | Y               | n/a                                               | The sign-in policy id of the Azure AD B2C instance for user sign in
AzureAdB2C:SignUpPolicyId																  | Y               | n/a                                               | The sign-up policy id of the Azure AD B2C instance for user registration
GIS:ESRIConfig                                                                            | Y               | n/a                                               | The Connection and Service settings for using the ESRI Service.
QuicksilvaVirusScannerService:AvEndpoint                                                  | Y               | n/a                                               | The full URL to Quicksilva''s anti-virus & malware detection service (when using `QuicksilvaVirusScannerService`)
QuicksilvaVirusScannerService:IsEnabled                                                   | N               | Y                                                 | Enable the anti-virus & malware detection service (`QuicksilvaVirusScannerService`). Disabling will not stop files being processed
FileStorage:StorageRootPath                                                               | N               | n/a                                               | The root path (when using `PersistentDiskFileStorageService`) where all documents in the system will be stored within
FileStorage:ConnectionString                                                              | N               | n/a                                               | The connection string to use for the Azure File Storage
FileStorage:Container                                                                     | N               | n/a                                               | The container name to use in the Azure File Storage
UserFileUpload:MaxFileSizeBytes                                                           | N               | 4194304 (10Mb)                                    | The Maximum size of a file which can be uploaded by an applicant user
UserFileUpload:MaxNumberOfDocuments                                                       | N               | 10                                                | The Maximum number of documents which can be uploaded by an applicant user against a sinlge FLA
ApiSecurity:AuthenticationHeaderKey                                                       | Y               | X-Api-Key                                         | The Name of the HTTP Header which will store the API Key value for a connecting system to use.
ApiSecurity:AuthenticationHeaderValue                                                     | Y               | iazo54uwhDnydbAqbrHcDvUr4UTf4w5zr1hKwSm4JxJGNvCsS | The Value of the HTTP Header key which a connecting system will need to supply to gain access to relevant end-points.
ApiFileUpload:MaxFileSizeBytes                                                            | Y               | 33554432 (32Mb)                                   | The Maximum size of a file which will be processed by the system (`FileStorage` service)
EsriConfig:Forestry:BaseUrl																  | Y				|n/a                                                | All the Root URL ALL services should run from 
EsriConfig:Forestry:CountryCode																  | Y				|n/a                                                | The Country code to check items are in England
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
EsriConfig:Forester:CountryCode																  | Y				|n/a                                                | The Country code to check items are in England
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
EsriConfig:Forester:LayerServices														      | Y               | n/a                                               | A collection of layers that can be qurreid
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
EsriConfig:LayoutTemplate																  | N				| Letter ANSI A Landscape   						| When the map is sent to the Print service, we can supply a template that the service to format the returned exported item
LandInformationSearch:BaseUrl															  | Y               | n/a                                               | The full URL to the AGOL
LandInformationSearch:GenerateTokenService												  | Y				| n/a												| All the details needed to generate the token.
LandInformationSearch:GenerateTokenService:Path											  | Y				| oauth2/token									    | The path to the token Service
LandInformationSearch:GenerateTokenService:ClientID										  | Y			    | n/a                 							    | The ID to give the token service
LandInformationSearch:GenerateTokenService:ClientSecret									  | Y				| n/a                             				    | The Password for the token service
LandInformationSearch:DeepLinkUrlAndPath												  | Y               | n/a                                               | The deeplink URL into the Forester web LIS tool.
LandInformationSearch:LisConfig															  | Y               | n/a                                               | The esri supplied value sent to the LIS as part of the deeplink
LandInformationSearch:LayerServices:FLOv2_LIS_integration_TESTv2:ServiceURI				  | Y               | n/a                                               | Path to the Feature service to push Case ID attribute and submitted compartment geometries
DocumentVisibilities:ExternalLisConstraintReport:VisibleToApplicant						  | Y               | n/a                                               | A flag representing whether External LIS Constraint Reports are visible to external applicants
DocumentVisibilities:ExternalLisConstraintReport:VisibleToConsultee						  | Y               | n/a                                               | A flag representing whether External LIS Constraint Reports are visible to external consultees
DocumentVisibilities:FcLisConstraintReport:VisibleToApplicant							  | Y               | n/a                                               | A flag representing whether FC LIS constraint reports are visible to external applicants
DocumentVisibilities:FcLisConstraintReport:VisibleToConsultee							  | Y               | n/a                                               | A flag representing whether FC LIS constraint reports are visible to external consultees
DocumentVisibilities:SiteVisitAttachment:VisibleToApplicant								  | Y               | n/a                                               | A flag representing whether Site Visit Attachments are visible to external applicants
DocumentVisibilities:SiteVisitAttachment:VisibleToConsultee								  | Y               | n/a                                               | A flag representing whether Site Visit Attachments are visible to external consultees
DocumentVisibilities:ApplicationDocument:VisibleToApplicant								  | Y               | n/a                                               | A flag representing whether Application Documents are visible to external applicants
DocumentVisibilities:ApplicationDocument:VisibleToConsultee								  | Y               | n/a                                               | A flag representing whether Application Documents are visible to external consultees
InternalUserSite:BaseUrl																  | Y				| n/a                             				    | The base URL for the internal user site, for notifications
FcAgency:PermittedEmailDomainsForFcAgent												  | N				| qxlva.com & forestrycommission.gov.uk				| The list of permitted email domains which are checked when determining whether to allow a User to be an FC Agent for the FC Agency
PDFGeneratorAPI:Baseurl																	  | Y               | n/a                                               | The URL path for requesting the pdf from the pdf generator api for producing the application
RabbitMqOptions:Url																		  | Y               | n/a                                               | The URL path for communicating with the RabbitMQ messaging service
RabbitMqOptions:Username															      | Y               | n/a                                               | The username for authenticating with the RabbitMQ messaging service
RabbitMqOptions:Password															      | Y               | n/a                                               | The password for authenticating with the RabbitMQ messaging service
RabbitMqOptions:QueueExpiration															  | N               | 18000                                             | The time in seconds for queue expiration
RabbitMqOptions:RetryCount															      | N               | 3                                                 | The retry limit for consuming messages
RabbitMqOptions:RetryIntervalMilliseconds												  | N               | 10000                                             | A fixed interval between retries in milliseconds
RabbitMqOptions:PrefetchCount															  | N               | 16                                                | The amount of messages to prefetch from the RabbitMQ message broker
FellingLicenceApplicationOptions:CitizensCharterDateLength								  | N               | 77.00:00:00                                       | The timespan following receipt of an application to calculate the citizens charter date
FellingLicenceApplicationOptions:FinalActionDateDaysFromSubmission						  | Y               | n/a												| The number of days following submission that an application's final action date should be set to
FellingLicenceApplication:PostFix														  | N				| n/a												| The post fix to be used for the Felling Licence Application
FellingLicenceApplication:StartCounter													  | N				| n/a												| The starting counter for the Felling Licence Application	
SiteAnalytics:GoogleAnalytics:Enabled						                              | Y               | n/a												| Is Google Analytics enabled on the site (a visitor/user who does not provide consent via cookie will effectively override this setting if this flag is set to true during their visit/session)
SiteAnalytics:GoogleAnalytics:TrackingId						                          | Y               | n/a												| The unique Google Analytics tracking id
SiteAnalytics:MicrosoftClarity:Enabled						                              | Y               | n/a												| Is Microsoft Clarity enabled on the site (a visitor/user who does not provide consent via cookie will effectively override this setting if this flag is set to true during their visit/session)
SiteAnalytics:MicrosoftClarity:TrackingId						                          | Y               | n/a												| The unique Microsoft Clarity  tracking id
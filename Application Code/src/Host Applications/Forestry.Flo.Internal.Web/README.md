# Felling Licence Online v2 (flov2) - Internal
The Felling License Online v2 tool, housed in this repository, serves as a comprehensive platform for managing the internal aspects of felling licensing processes. Tailored for internal staff responsible for application review, this tool provides a systematic approach to evaluating applications for felling or restocking trees on registered properties. Internal users are facilitated with an organized workflow that begins with a thorough examination of each application, including the verification of property details, compliance with conditions, and assessment of supporting documents. This is followed by a decisive stage where applications are either approved or denied, based on their alignment with stipulated requirements. Through a user-friendly interface designed exclusively for internal use, the Felling License Online v2 tool streamlines the complex licensing procedures, ensuring an efficient and consistent review process.

### Dependencies
- PostgreSQL database
- pdfGenerator
- fileStorage

### Authentication
The system utilizes Azure AD B2C for authentication, providing a scalable solution that offers identity management and access control. Out-of-the-box configurations align with standard security protocols, supporting various identity providers and custom policies. These settings are generally sufficient for production but can be tailored to specific needs through the application's configuration or environment variables if necessary.

### HTTP Endpoints
The system makes available the following HTTP endpoints:

URL                                                                         | HTTP Method   | Is Authenticated | Description
--------------------------------------------------------------------------- | ------------- | ---------------- | -----------
/                                                                           | GET           | N                | Will return homepage if the web server is running. All other pages require the authentication.
/api/ExtendApplications/ApplicationExtension/ExtendApplications             | GET           | Y				   | This endpoint extends the application final action dates for felling licenses and returns a success response upon completion.
/api/lis/\{applicationId\}                                                  | PUT           | Y				   | This endpoint allows the ESRI/Forester to submit an LIS (Land Information Search) Constraint PDF Report for a specified application ID, with a maximum file size of 32MB.
/api/PublicRegisterExpiryController/PublicRegisterExpiryNotification        | GET           | Y				   | This endpoint notifies assigned FC (Forestry Commission) staff of nearing expirations of applications' public registry periods and returns a success response upon completion.
/api/VoluntaryWithdrawNotification/VoluntaryWithdrawalNotificationFla       | GET           | Y				   | This endpoint sends notifications for application withdrawal if the application has been inactive with the user for a period defined by VoluntaryWithdrawApplication:ThresholdAfterWithApplicantStatusDate, and no prior withdrawal notification was sent, returning a success response upon completion.

### Logging
The system employs a logging configuration with varying log levels for different components. By default, the log level is set to "Information," but specific components like Microsoft's ASP.NET Core are set to "Error." These defaults are typically suitable for production but can be overridden in the application's configuration files or environment variables if specialized behavior is required.

### Configuration
Please following the link below to the configuration file.

[configuration readme](configuration.md)

These configuration values provide granular control over various aspects of the application, ranging from authentication and notifications to file handling and document visibility. They can mostly be found and modified within the application's configuration files, appsettings.json.

### Database
Please following the link below to the database file.

[database readme](../../database.md)

### Auditing
Auditing within the system is a fundamental process that ensures accountability, transparency, and compliance with regulations. All audit-related data is meticulously stored within the database, in a dedicated schema called "Audit." This schema captures various details such as the actions performed, the users involved, timestamps of activities, changes made to records, and any associated comments or reasons. Storing this information within the database ensures that the audit trails are secure, easily accessible, and can be referenced or analyzed whenever required. The use of the database for audit records supports robust reporting capabilities and offers a stable foundation for monitoring and investigating system activities. Whether for regulatory compliance, internal oversight, or historical reference, the integration of auditing within the database is a vital part of the system's architecture.

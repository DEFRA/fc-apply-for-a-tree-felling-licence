# Felling Licence Online v2 (flov2) - External
The Felling License Online v2 tool for external use, housed in this repository, serves as a comprehensive platform for managing felling licensing processes. The tool begins by enabling the creation of user accounts, accommodating different types, including external users or agency accounts. Once registered, users have the ability to add properties to their profile, detailing the specific areas under their management. Following this, the tool provides a streamlined process for applying for licenses, whether for felling or restocking trees on the registered properties. Through a user-friendly interface and an organized workflow, this Felling License Online v2 tool simplifies the complex licensing procedures, ensuring an efficient and accessible experience for all users.

### Dependencies
- PostgreSQL database
- pdfGenerator
- fileStorage

### Authentication
The system utilizes Azure AD B2C for authentication, providing a scalable solution that offers identity management and access control. Out-of-the-box configurations align with standard security protocols, supporting various identity providers and custom policies. These settings are generally sufficient for production but can be tailored to specific needs through the application's configuration or environment variables if necessary.

### HTTP Endpoints
The system makes available the following HTTP endpoints:

URL                                                        | HTTP Method   | Is Authenticated | Description
-----------------------------------------------------------| ------------- | ---------------- | -----------
/                                                          | GET           | N                | Will return homepage if the web server is running. All other pages require the authentication.
/api/Gis/heartBeat                                         | GET           | N                | Returns a heartbeat with the current date and time.
/api/Gis/UploadSettings                                    | GET           | Y                | Retrieves supported file types for uploading and the maximum file size allowed.
/api/Gis/GetShapes                                         | POST          | Y                | Extracts shapes from a given file, returning success or failure status.
/api/Gis/GetShapesFromString                               | POST          | Y                | Extracts shapes from a string input, returning success or failure status.
/api/Gis/GetPropertyDetails                                | GET           | Y                | Retrieves property details including nearest town and compartments for bulk import.
/api/Gis/FellingAndRestockingSelectedCompartments          | POST          | Y                | Fetches selected compartments for felling and restocking for a specified application.
/api/Gis/Import                                            | POST          | Y                | Handles bulk import of compartments, returning success or failure status for each item.
/api/Gis/IsInEngland	                                   | POST          | Y                | Retrieves if the shapes are in England.
/api/lis/\{applicationId\}                                 | PUT           | Y			      | This endpoint allows the ESRI/Forester to submit an LIS (Land Information Search) Constraint PDF Report for a specified application ID, with a maximum file size of 32MB.

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

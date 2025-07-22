The system leverages a PostgreSQL database to manage all forestry services data, encompassing a broad spectrum of information. The data is organized into distinct schemas, including:

schema                                | Description 
--------------------------------------| ---------------
Admin hubs	                          | This schema manages administrative hubs' information, organizing data related to the hubs and the officers within the system.
Applicants                            | This schema handles information related to external users who apply for various licenses. It includes details like personal information, and account status.
Audit       	                      | The Audit schema is vital for tracking changes and activities within the system. It maintains logs and records of actions, such as modifications to data, user activities, and system events, facilitating transparency and accountability.
Conditions	                          | The Conditions schema stores information related to various conditions and prerequisites associated with licenses, such as regulations, standards, and compliance criteria that must be met by the applicants.
Felling Licence Applications          | This schema is specifically dedicated to the management of applications for felling licenses. It contains details about the application, property, species of trees, quantity, and other related data essential for processing felling permits.
Internal users                        | The Internal users schema is concerned with the data of users within the organization, such as employees and staff members. 
Notifications                         | This schema oversees the management of notifications within the system.
Property Profiles                     | The Property Profiles schema contains detailed information about properties that are associated with felling or other forestry-related activities. This encompasses attributes like location, size, and specific characteristics of the forested area.

These schemas are systematically created from classes referred to as 'entities' using Entity Framework Core. This structure supports modularity and efficiency in data handling, making it suitable for both development and production environments.

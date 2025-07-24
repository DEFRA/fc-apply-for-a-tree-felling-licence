# Adding EF Migrations
The following steps should be run from within Visual Studio to add a new migration to any of the service projects:

- Set the VS startup project to Forestry.Flo.Services.Migrations.Startup
- Open the VS Package Manager Console
- Set the Default project in the Package Manager Console to the service you wish to create a migration for
- Within the Package Manager Console execute the Add-Migration command (e.g. Add-Migration -name Initial-Migration)
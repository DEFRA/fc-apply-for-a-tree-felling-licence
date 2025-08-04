# FLO Migration Tool 

## Introduction

This article describes the Migration tool configuration settings and usage.

### Configuration Settings

| Configuration Path | Must Be Specified | Default Value | Description |
| -------- | -------- | -------- | -------|
| DataMigrationConfiguration.DatabaseStorage.V1DefaultConnection | Y | N/A | The connection string to the FLOv1 source database |
| DataMigrationConfiguration.DatabaseStorage.V2DefaultConnection | Y | N/A | The connection string to the FLOv2 target database |
| DataMigrationConfiguration.AzureBlobStorage.ConnectionString | Y | N/A | The connection string to the Azure Blob Store used to hold files being migrated to FLOv2 |
| DataMigrationConfiguration.AzureBlobStorage.ContainerName | Y | N/A | The container to be used to store FLOv1 files which are being migrated to FLOv2|
| DataMigrationConfiguration.ParallelismSettings.MaxDegreeOfParallelism | N | 12 | The maximum number of cores to be used when performing parallel options (such as calls to db inserts)|

### Command Line Usage
The  executable file is named 'flo-data-migrater'

it must have one of the following verbs passed to it, each verb may have zero or multiple possible options passed to it.

| Verb | Mandatory Option/s | Additional information |
| ---- | ------ | ---------------------------- |
| reset | N/A | the FLOV2 target environment|Such as truncation of FLOv2 Database tables |
| prevalidate | -m {migrationSourceType} | prevalidates the inbound FLOv1 dataset for the specificed migration source type |
| migrate | -m {migrationSourceType} | Runs the actual processes involved with extraction from FLOv1 source db/file system, transformation and load into FLOv2. By default this will also run the pre validation routine for the specified migration type. To disable the prevalidation step additionally provide the `-d` option |

#### Migration Source Types ###
| Source Types |
|--------------|
|ExternalUsers|
|ManagedOwners|
|AgentAuthorityForms|
|Properties|
|Compartments|
|CompartmentGIS|
|etc|

#### Example valid execution commands

|Description|Command Sample|
|-----------|--------------|
| Reset the target FLOv2 system | ```.\flo-data-migrater.exe reset``` |
| Pre-validate a migration of the ExternalUsers migration process | ```.\flo-data-migrater.exe prevalidate --migration ExternalUsers``` |
| Migrate FLOv1 users in test mode, where the **FLOv2 system is not altered**, but all other aspects are ran such as pre-validation | ```.\flo-data-migrater.exe migrate --migration ExternalUsers -t``` |
| Migrate FLOv1 users, **without** the prevalidation step before-hand | ```.\flo-data-migrater.exe migrate --migration ExternalUsers -d``` |
| Migrate FLOv1 users (includes prevalidation step before-hand) | ```.\flo-data-migrater.exe migrate --migration ExternalUsers``` |
| Migrate FLOv1 users, but swap source email addresses with a custom email address during insertion for test purposes, the following sample becoming `paul.winslade+{FLOv1UserID}@qxlva.com`  | ```.\flo-data-migrater.exe migrate --migration ExternalUsers -e paul.winslade@qxlva.com``` |
| Migrate FLOv1 managed owners (includes prevalidation step before-hand) | ```.\flo-data-migrater.exe migrate --migration ManagedOwners``` |
| Migrate FLOv1 managed owners, **without** the prevalidation step before-hand | ```.\flo-data-migrater.exe migrate --migration ManagedOwners -d``` |

using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MigrationService.Configuration;
using MigrationService.Services;

namespace Forestry.FloV1.DataMigration.MigrationHostApp.Tests.MigrationModules.ExternalUsersMigrator;

public abstract class ExternalUsersMigratorTestsBase
{
    protected global::MigrationHostApp.MigrationModules.ExternalUsersMigrator ExternalUsersMigrator;

    protected AutoFixture.Fixture Fixture = new();
    protected readonly IDatabaseServiceV1 DatabaseServiceV1 = A.Fake<IDatabaseServiceV1>();
    protected readonly IDatabaseServiceV2 DatabaseServiceV2 = A.Fake<IDatabaseServiceV2>();

    protected void CreateSut(DataMigrationConfiguration? settings = null)
    {
        settings ??= new DataMigrationConfiguration {ParallelismSettings = new ParallelismSettings()};

        ExternalUsersMigrator = new global::MigrationHostApp.MigrationModules.ExternalUsersMigrator(
            DatabaseServiceV1,
            DatabaseServiceV2,
            new OptionsWrapper<DataMigrationConfiguration>(settings),
            new NullLogger<global::MigrationHostApp.MigrationModules.ExternalUsersMigrator>());
    }
}
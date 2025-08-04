using AutoFixture;
using AutoFixture.Xunit2;

namespace Forestry.FloV1.DataMigration.MigrationHostApp.Tests.FixtureCustomizations;

public class AutoDataWithValidFloUserRoleName : AutoDataAttribute
{
    public AutoDataWithValidFloUserRoleName()
        : base(() => new Fixture().Customize(new ValidRoleNameAutoFixtureCustomization()))
    {
    }
}
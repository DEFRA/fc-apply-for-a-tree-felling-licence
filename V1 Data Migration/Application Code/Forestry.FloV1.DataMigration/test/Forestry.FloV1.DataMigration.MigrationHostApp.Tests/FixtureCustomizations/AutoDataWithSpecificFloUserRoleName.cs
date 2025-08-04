using AutoFixture;
using AutoFixture.Xunit2;

namespace Forestry.FloV1.DataMigration.MigrationHostApp.Tests.FixtureCustomizations;

public class AutoDataWithSpecificFloUserRoleName : AutoDataAttribute
{
    public AutoDataWithSpecificFloUserRoleName(string roleName)
        : base(() => new Fixture().Customize(new SpecificRoleNameAutoFixtureCustomization(roleName)))
    {
    }
}
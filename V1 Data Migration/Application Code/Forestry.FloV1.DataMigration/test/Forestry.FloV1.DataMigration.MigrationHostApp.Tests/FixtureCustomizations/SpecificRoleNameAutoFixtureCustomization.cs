using AutoFixture;
using Domain.V1;

namespace Forestry.FloV1.DataMigration.MigrationHostApp.Tests.FixtureCustomizations;

public class SpecificRoleNameAutoFixtureCustomization : ICustomization
{
    private readonly string _roleName;

    public SpecificRoleNameAutoFixtureCustomization(string roleName)
    {
        _roleName = roleName;
    }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<FloUser>(a =>
            a.With(p => p.RoleName, _roleName));
    }
}
using System.ComponentModel;
using AutoFixture;
using Domain.V1;
using MigrationService.Services;

namespace Forestry.FloV1.DataMigration.MigrationHostApp.Tests.FixtureCustomizations;

public class ValidRoleNameAutoFixtureCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<FloUser>(a =>
            a.With(p => p.RoleName, fixture.Create<RoleNames>().GetDescription()));
    }

    private enum RoleNames
    {
        [Description("owner")]
        Owner,

        [Description("agent")]
        Agent,

        [Description("FC Internal Agent")]
        FCInternalAgent
    }

}
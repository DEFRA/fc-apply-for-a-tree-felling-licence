using AutoFixture;
using Forestry.Flo.Services.Applicants.Entities.Agent;

namespace Forestry.Flo.Tests.Common;

public class AlwaysNonFcAgencyAutoFixtureCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<Agency>(a => a.With(p => p.IsFcAgency, false));
    }
}
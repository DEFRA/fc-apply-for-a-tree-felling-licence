using AutoFixture;
using AutoFixture.Xunit2;

namespace Forestry.Flo.Tests.Common;

public class AutoDataWithNonFcAgencyAttribute : AutoDataAttribute
{
    public AutoDataWithNonFcAgencyAttribute()
        : base(() => new Fixture().Customize(new AlwaysNonFcAgencyAutoFixtureCustomization()))
    {
    }
}
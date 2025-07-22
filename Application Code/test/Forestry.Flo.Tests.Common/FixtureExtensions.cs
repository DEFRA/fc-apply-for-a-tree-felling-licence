using AutoFixture;
using AutoFixture.AutoMoq;

namespace Forestry.Flo.Tests.Common;

public static class FixtureExtensions
{
    public static IFixture CustomiseFixtureForFellingLicenceApplications(this IFixture fixture)
    {
        fixture.Customize(new CompositeCustomization(
            new AutoMoqCustomization(),
            new SupportMutableValueTypesCustomization()));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));

        return fixture;
    }
}
using AutoFixture;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Services;

public class ExternalApplicantTests
{
    private readonly Fixture _fixture = new();

    [Theory, CombinatorialData]
    public void GetEmailAddress_ReturnsCorrectClaimType(AuthenticationProvider provider)
    {
        var email = _fixture.Create<string>();
        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            emailAddress: email, 
            provider: provider);

        var externalApplicant = new ExternalApplicant(principal);

        Assert.Equal(email, externalApplicant.EmailAddress);
    }

    [Theory, CombinatorialData]
    public void GetIdentityProviderId_ReturnsCorrectClaimType(AuthenticationProvider provider)
    {
        var identityProviderId = _fixture.Create<string>();
        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            identityProviderId: identityProviderId,
            provider: provider);

        var externalApplicant = new ExternalApplicant(principal);

        Assert.Equal(identityProviderId, externalApplicant.IdentityProviderId);
    }
}
using AutoFixture;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class InternalUserTests
{
    private readonly Fixture _fixture = new();

    [Theory, CombinatorialData]
    public void GetEmailAddress_ReturnsCorrectClaimType(AuthenticationProvider provider)
    {
        var email = _fixture.Create<string>();
        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            emailAddress: email,
            provider: provider);

        var internalUser = new InternalUser(principal);

        Assert.Equal(email, internalUser.EmailAddress);
    }

    [Theory, CombinatorialData]
    public void GetIdentityProviderId_ReturnsCorrectClaimType(AuthenticationProvider provider)
    {
        var identityProviderId = _fixture.Create<string>();
        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            identityProviderId: identityProviderId,
            provider: provider);

        var internalUser = new InternalUser(principal);

        Assert.Equal(identityProviderId, internalUser.IdentityProviderId);
    }
}
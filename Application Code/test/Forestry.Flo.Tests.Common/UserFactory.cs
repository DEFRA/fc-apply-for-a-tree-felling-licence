using System.Security.Claims;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Tests.Common;

public static class UserFactory
{
    private const string IdentityProviderEmailClaimType = "emails";

    public static ClaimsPrincipal CreateExternalApplicantIdentityProviderClaimsPrincipal(
        string? identityProviderId = null, 
        string? emailAddress = null,
        Guid? localAccountId = null,
        Guid? woodlandOwnerId = null,
        AccountTypeExternal accountTypeExternal = AccountTypeExternal.WoodlandOwner,
        Guid? agencyId = null,
        bool hasAcceptedTermsAndConditionsAndPrivacyPolicy = true,
        bool isFcUser = false,
        string? woodlandOwnerName = null,
        bool invited = false)
    {
        if (string.IsNullOrWhiteSpace(identityProviderId))
            identityProviderId = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(emailAddress))
            emailAddress = Guid.NewGuid().ToString();
        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, identityProviderId));
        claims.Add(new Claim(IdentityProviderEmailClaimType, emailAddress));

        if (hasAcceptedTermsAndConditionsAndPrivacyPolicy)
        {
            claims.Add(new(FloClaimTypes.AcceptedPrivacyPolicy, "true"));
            claims.Add(new(FloClaimTypes.AcceptedTermsAndConditions, "true"));
        }

        if (isFcUser)
        {
            claims.Add(new(FloClaimTypes.FcUser, "true"));
        }

        if (invited)
        {
            claims.Add(new Claim(FloClaimTypes.Invited, "true"));
        }

        var identity = new ClaimsIdentity(claims, "test-setup");  //TODO - set authentication type to match what B2C sets
        var result = new ClaimsPrincipal(identity);

        if (localAccountId != null || woodlandOwnerId != null || agencyId != null)
        {
            var localClaims = new List<Claim> { new(FloClaimTypes.LocalAccountId, localAccountId?.ToString() ?? Guid.NewGuid().ToString()) };
            if (woodlandOwnerId != null)
            {
                localClaims.Add(new Claim(FloClaimTypes.WoodlandOwnerId, woodlandOwnerId.ToString()));
            }
            if (woodlandOwnerName != null)
            {
                localClaims.Add(new Claim(FloClaimTypes.WoodlandOwnerName, woodlandOwnerName));
            }
            if (agencyId != null)
            {
                localClaims.Add(new Claim(FloClaimTypes.AgencyId, agencyId.ToString()!));
            }
            localClaims.Add(new Claim(FloClaimTypes.AccountType, accountTypeExternal.ToString()));
            
            var localIdentity = new ClaimsIdentity(localClaims, FloClaimTypes.ClaimsIdentityAuthenticationType);
            result.AddIdentity(localIdentity);
        }

        return result;
    }

    public static ClaimsPrincipal CreateInternalUserIdentityProviderClaimsPrincipal(
        string? identityProviderId = null,
        string? emailAddress = null,
        Guid? localAccountId = null,
        string? username = null,
        Services.Common.User.AccountTypeInternal accountTypeInternal =
            Services.Common.User.AccountTypeInternal.FcStaffMember)
    {
        if (string.IsNullOrWhiteSpace(identityProviderId))
            identityProviderId = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(emailAddress))
            emailAddress = "test@forestrycommission.gov.uk";
        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, identityProviderId));
        claims.Add(new Claim(IdentityProviderEmailClaimType, emailAddress));

        var identity = new ClaimsIdentity(claims, "test-setup");  //TODO - set authentication type to match what B2C sets
        var result = new ClaimsPrincipal(identity);

        if (localAccountId != null || string.IsNullOrWhiteSpace(username) == false)
        {
            var localClaims = new List<Claim> { new(FloClaimTypes.LocalAccountId, localAccountId?.ToString() ?? Guid.NewGuid().ToString()) };
            localClaims.Add(new Claim(FloClaimTypes.AccountType, accountTypeInternal.ToString()));
            if (string.IsNullOrWhiteSpace(username))
                username = Guid.NewGuid().ToString();
            localClaims.Add(new Claim(FloClaimTypes.UserName, username));

            var localIdentity = new ClaimsIdentity(localClaims, FloClaimTypes.ClaimsIdentityAuthenticationType);
            result.AddIdentity(localIdentity);
            result.AddIdentity(new ClaimsIdentity { Label = FloClaimTypes.InternalUserLocalSourceClaimLabel });
        }

        return result;
    }

    public static ClaimsPrincipal CreateUnauthenticatedUser()
    {
        var identity = new ClaimsIdentity();
        return new ClaimsPrincipal(identity);
    }
}
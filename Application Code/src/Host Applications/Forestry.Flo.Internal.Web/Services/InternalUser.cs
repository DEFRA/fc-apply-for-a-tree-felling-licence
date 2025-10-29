using Ardalis.GuardClauses;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using System.Security.Claims;

namespace Forestry.Flo.Internal.Web.Services;

public class InternalUser
{
    private readonly ClaimsPrincipal _principal;

    public InternalUser(ClaimsPrincipal principal)
    {
        _principal = Guard.Against.Null(principal);

        if (_principal.Identities.All(x => x.Label != FloClaimTypes.InternalUserLocalSourceClaimLabel))
        {
            _principal.AddIdentity(new ClaimsIdentity { Label = FloClaimTypes.InternalUserLocalSourceClaimLabel });
        }
    }

    public string? IdentityProviderId
    {
        get
        {
            var claimType = AuthenticationProvider switch
            {
                AuthenticationProvider.OneLogin => OneLoginPrincipalClaimTypes.NameIdentifier,
                _ => ClaimTypes.NameIdentifier
            };

            return GetClaimValue(claimType);
        }
    }

    public Guid? UserAccountId
    {
        get
        {
            if (Guid.TryParse(GetClaimValue(FloClaimTypes.LocalAccountId), out var result))
                return result;

            return null;
        }
    }

    public AccountTypeInternal? AccountType
    {
        get
        {
            var claim = GetClaimValue(FloClaimTypes.AccountType);
            return string.IsNullOrWhiteSpace(claim)
                ? null
                : Enum.Parse<AccountTypeInternal>(claim);
        }
    }

    public AccountTypeInternalOther? AccountTypeOther
    {
        get
        {
            var claim = GetClaimValue(FloClaimTypes.AccountTypeOther);
            return string.IsNullOrWhiteSpace(claim)
                ? null
                : Enum.Parse<AccountTypeInternalOther>(claim);
        }
    }
    public AuthenticationProvider AuthenticationProvider
    {
        get
        {
            var claim = GetClaimValue(FloClaimTypes.AuthenticationProvider);
            return Enum.Parse<AuthenticationProvider>(claim!);
        }
    }

    public string? EmailAddress
    {
        get
        {
            var claimType = AuthenticationProvider switch
            {
                AuthenticationProvider.OneLogin => OneLoginPrincipalClaimTypes.EmailAddress,
                _ => ClaimsPrincipalExtensions.IdentityProviderEmailClaimType
            };

            return GetClaimValue(claimType);
        }
    }

    public string? FullName => GetClaimValue(FloClaimTypes.UserName);

    public bool CanApproveApplications
    {
        get
        {
            var canApprove = GetClaimValue(FloClaimTypes.UserCanApproveApplications);
            return string.IsNullOrWhiteSpace(canApprove) == false && bool.Parse(canApprove);
        }
    }

    /// <summary>
    /// Gets a reference to the underlying claims principal for this user.
    /// </summary>
    public ClaimsPrincipal Principal => _principal;

    private string? GetClaimValue(string claimType)
    {
        return _principal
            .Claims
            .FirstOrDefault(x => x.Type == claimType)
            ?.Value;
    }
}
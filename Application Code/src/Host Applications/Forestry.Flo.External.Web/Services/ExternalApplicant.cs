using System.Security.Claims;
using Ardalis.GuardClauses;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.External.Web.Services;

public class ExternalApplicant
{
    private const string LocalClaimLabel = "ExternalApplicantLocalSource";
    private const string IdentityProviderEmailClaimType = "emails";

    private readonly ClaimsPrincipal _principal;

    public ExternalApplicant(ClaimsPrincipal principal)
    {
        _principal = Guard.Against.Null(principal);
        if (_principal.Identities.All(x => x.Label != LocalClaimLabel))
        {
            _principal.AddIdentity(new ClaimsIdentity { Label = LocalClaimLabel });
        }
    }

    public bool IsLoggedIn => _principal.IsLoggedIn();

    public bool IsNotLoggedIn => _principal.IsNotLoggedIn();

    public string? IdentityProviderId => GetClaimValue(ClaimTypes.NameIdentifier);

    public bool HasRegisteredLocalAccount =>
        _principal.Identities.Any(x => x.AuthenticationType == FloClaimTypes.ClaimsIdentityAuthenticationType);

    public Guid? UserAccountId
    {
        get
        {
            if (Guid.TryParse(GetClaimValue(FloClaimTypes.LocalAccountId), out var result))
                return result;

            return null;
        }
    }

    public string? WoodlandOwnerId => GetClaimValue(FloClaimTypes.WoodlandOwnerId);
    public string? AgencyId => GetClaimValue(FloClaimTypes.AgencyId);

    public string? WoodlandOwnerName => GetClaimValue(FloClaimTypes.WoodlandOwnerName);

    public AccountTypeExternal? AccountType
    {
        get
        {
            var claim = GetClaimValue(FloClaimTypes.AccountType);
            return string.IsNullOrWhiteSpace(claim)
                ? null
                : Enum.Parse<AccountTypeExternal>(claim);
        }
    }

    public string? EmailAddress => GetClaimValue(IdentityProviderEmailClaimType);

    public string? FullName => GetClaimValue(FloClaimTypes.UserName);
    public DateTime? LastChanged => DateTime.TryParse(GetClaimValue(FloClaimTypes.LastChanged), out var lastChanged) ? lastChanged.ToUniversalTime() : null;

    public bool HasCompletedAccountRegistration
    {
        get
        {
            var claim1 = GetClaimValue(FloClaimTypes.AcceptedTermsAndConditions);
            var claim2 = GetClaimValue(FloClaimTypes.AcceptedPrivacyPolicy);
            return string.IsNullOrWhiteSpace(claim1) == false && bool.Parse(claim1)
                && string.IsNullOrWhiteSpace(claim2) == false && bool.Parse(claim2);
        }
    }

    public bool IsAnInvitedUser
    {
        get
        {
            var invite = GetClaimValue(FloClaimTypes.Invited);
            return string.IsNullOrWhiteSpace(invite) == false && bool.Parse(invite);
        }
    }

    public bool IsDeactivatedAccount
    {
        get
        {
            var accountStatus = GetClaimValue(FloClaimTypes.AccountStatus);
            return accountStatus != null && accountStatus == UserAccountStatus.Deactivated.GetDisplayName();
        }
    }

    public bool IsFcUser
    {
        get
        {
            var isFcUser = GetClaimValue(FloClaimTypes.FcUser);
            return string.IsNullOrWhiteSpace(isFcUser) == false && bool.Parse(isFcUser);
        }
    }

    public bool HasSelectedAgentWoodlandOwner
    {
        get
        {
            switch (AccountType)
            {
                case AccountTypeExternal.Agent:
                case AccountTypeExternal.AgentAdministrator:
                case AccountTypeExternal.FcUser:
                    return !string.IsNullOrWhiteSpace(WoodlandOwnerId);
                default: 
                    return true;
            }
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
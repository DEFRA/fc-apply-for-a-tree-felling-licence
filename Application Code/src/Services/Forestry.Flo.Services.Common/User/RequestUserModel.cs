using Ardalis.GuardClauses;
using System.Security.Claims;

namespace Forestry.Flo.Services.Common.User;


/// <summary>
/// Class representing the current user of the system.
/// </summary>
public class RequestUserModel
{
    private readonly ClaimsPrincipal _principal;

    public ActorType ActorType { get; } = ActorType.ExternalApplicant;

    public RequestUserModel(ClaimsPrincipal principal)
    {
        _principal = Guard.Against.Null(principal);

        if (principal.Identity == null) return;

        ActorType = CalculateActorType();
    }

    private ActorType CalculateActorType()
    {
        var claim = GetClaimValue(FloClaimTypes.AccountType);

        return string.IsNullOrEmpty(claim) switch
        {
            false when Enum.TryParse<AccountTypeExternal>(claim, out _) => ActorType.ExternalApplicant,
            false when Enum.TryParse<AccountTypeInternal>(claim, out _) => ActorType.InternalUser,
            _ => ActorType.System
        };
    }

    private string? GetClaimValue(string claimType)
    {
        return _principal
            .Claims
            .FirstOrDefault(x => x.Type == claimType)
            ?.Value;
    }
}

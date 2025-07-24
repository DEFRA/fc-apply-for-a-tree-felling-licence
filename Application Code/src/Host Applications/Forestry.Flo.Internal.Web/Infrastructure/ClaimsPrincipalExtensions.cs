using System.Security.Claims;

namespace Forestry.Flo.Internal.Web.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public const string IdentityProviderEmailClaimType = "emails";

    public static bool IsLoggedIn(this ClaimsPrincipal principal)
        => principal.Identity != null && principal.Identity.IsAuthenticated;

    public static bool IsNotLoggedIn(this ClaimsPrincipal principal)
        => !IsLoggedIn(principal);
}
using Forestry.Flo.Services.Common.Infrastructure;
using System.Security.Claims;
using Forestry.Flo.Internal.Web.Infrastructure;

namespace Forestry.Flo.Internal.Web.Services;

/// <summary>
/// Provides extension methods for extracting authentication-related information from claims.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Retrieves the email address from the provided claims based on the specified authentication provider.
    /// </summary>
    /// <param name="claims">The collection of claims to search.</param>
    /// <param name="provider">The authentication provider used for determining the claim type.</param>
    /// <returns>
    /// The email address if found; otherwise, <c>null</c>.
    /// </returns>
    public static string? GetEmailFromClaims(this IEnumerable<Claim> claims, AuthenticationProvider provider)
    {
        var claimType = provider switch
        {
            AuthenticationProvider.Azure => ClaimsPrincipalExtensions.IdentityProviderEmailClaimType,
            AuthenticationProvider.OneLogin => OneLoginPrincipalClaimTypes.EmailAddress,
            _ => ClaimTypes.Email,
        };

        return claims.SingleOrDefault(x => x.Type == claimType)?.Value;
    }

    /// <summary>
    /// Retrieves the identity provider identifier from the provided claims based on the specified authentication provider.
    /// </summary>
    /// <param name="claims">The collection of claims to search.</param>
    /// <param name="provider">The authentication provider used for determining the claim type.</param>
    /// <returns>
    /// The identity provider identifier if found; otherwise, <c>null</c>.
    /// </returns>
    public static string? GetIdentityProviderId(this IEnumerable<Claim> claims, AuthenticationProvider provider)
    {
        var claimType = provider switch
        {
            AuthenticationProvider.OneLogin => OneLoginPrincipalClaimTypes.NameIdentifier,
            _ => ClaimTypes.NameIdentifier,
        };

        return claims.SingleOrDefault(x => x.Type == claimType)?.Value;
    }
}
using System.Security.Claims;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;

namespace Forestry.Flo.Services.Applicants.Services;

public interface ISignInApplicant
{
    /// <summary>
    /// This method is invoked by the infrastructure when a user authenticates using Azure B2C and is redirected to our web application.
    /// Implementations may use this method to amend the <see cref="ClaimsPrincipal"/> representing the user before the data is written
    /// to the User property of the current Http Context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The primary purpose of this method is for the local user repository information to append any additional data for the user that
    /// has authenticated. This is done by adding one or more additional <see cref="ClaimsIdentity"/> instances to the provided
    /// <see cref="ClaimsPrincipal"/> populated with one or more <see cref="Claim"/> values.
    /// </para>
    /// <para>
    /// The provided user is guaranteed to have a <see cref="Claim"/> of type <see cref="ClaimTypes.NameIdentifier"/> which represents the
    /// user's unique identifier within Azure AD B2C. This value can be used as a primary key within a local user repository to maintain
    /// a 1:1 mapping if required.
    /// </para>
    /// </remarks>
    /// <param name="user">The authenticated user that has been returned from Azure AD B2C.</param>
    /// <param name="inviteToken">Invitation token if the user has been invited</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task HandleUserLoginAsync(ClaimsPrincipal user, string? inviteToken, CancellationToken cancellationToken = default);
}
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Contract for a service that implements tasks to amend user accounts.
/// </summary>
public interface IAmendUserAccounts
{
    /// <summary>
    /// Amends a <see cref="UserAccount"/> entity with a populated <see cref="UpdateUserAccountModel"/>.
    /// </summary>
    /// <param name="userModel">A populated <see cref="UpdateUserAccountModel"/> instance.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the account has been successfully updated containing a bool that signifies whether the account details have been changed.</returns>
    Task<Result<bool>> UpdateUserAccountDetailsAsync(UpdateUserAccountModel userModel, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the account status of an applicant <see cref="UserAccount"/>.
    /// </summary>
    /// <param name="userId">The applicant's user ID.</param>
    /// <param name="requestedStatus">The requested status for the user account.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="UserAccountModel"/>.</returns>
    Task<Result<UserAccountModel>> UpdateApplicantAccountStatusAsync(
        Guid userId,
        UserAccountStatus requestedStatus,
        CancellationToken cancellationToken);
}
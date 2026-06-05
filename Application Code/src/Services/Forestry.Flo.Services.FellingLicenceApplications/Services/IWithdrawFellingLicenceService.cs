using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service that updates a felling licence application for withdrawal.
/// </summary>
public interface IWithdrawFellingLicenceService
{
    /// <summary>
    /// Withdraws the application and returns the result containing a list of internal users assigned to the application, or an empty IList if no internal user is assigned to it.
    /// </summary>
    /// <param name="applicationId">The id of the application to withdraw.</param>
    /// <param name="userAccessModel">The user access model used to check permission to the felling licence application.</param>
    /// <param name="withdrawalReasons">The provided selection of reasons for withdrawing the application.</param>
    /// <param name="withdrawalReasonsOtherDetails">The details provided if <see cref="WithdrawalReason.Other"/> was selected.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating the outcome and providing a list of user ids of internal
    /// users that were assigned to the application that will need to be notified.</returns>
    Task<Result<List<Guid>>> WithdrawApplicationAsync(
        Guid applicationId, 
        UserAccessModel userAccessModel, 
        List<WithdrawalReason> withdrawalReasons,
        string? withdrawalReasonsOtherDetails,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the <see cref="PublicRegister"/> entity for an application with the removed timestamp.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The optional id of the user making the update.</param>
    /// <param name="removedDateTime">The date and time that the application was removed from the public register.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> UpdatePublicRegisterEntityToRemovedAsync(
        Guid applicationId,
        Guid? userId,
        DateTime removedDateTime,
        CancellationToken cancellationToken);
}

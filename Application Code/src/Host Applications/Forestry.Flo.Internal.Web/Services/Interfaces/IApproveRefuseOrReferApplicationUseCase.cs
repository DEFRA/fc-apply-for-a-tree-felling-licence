using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for a use case that allows a field manager to approve, refuse, or refer a felling licence application.
/// </summary>
public interface IApproveRefuseOrReferApplicationUseCase
{
    /// <summary>
    /// Approves, refuses, or refers an application that has been sent for approval.
    /// </summary>
    /// <param name="user">The internal user making the request.</param>
    /// <param name="applicationId">The application id.</param>
    /// <param name="requestedStatus">The requested status for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="FinaliseFellingLicenceApplicationResult"/> indicating the outcome of the operation.
    /// </returns>
    Task<FinaliseFellingLicenceApplicationResult> ApproveOrRefuseOrReferApplicationAsync(
        InternalUser user,
        Guid applicationId,
        FellingLicenceStatus requestedStatus,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the approver id and licence expiry date for a given application.
    /// </summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="approverId">The approver id to set, or null to remove.</param>
    /// <param name="licenceExpiryDate">The licence expiry date, or null if not applicable.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the outcome of the update.
    /// </returns>
    Task<Result> UpdateApplicationApproverAndExpiryDateAsync(
        Guid applicationId,
        Guid? approverId,
        DateTime? licenceExpiryDate,
        CancellationToken cancellationToken);
}

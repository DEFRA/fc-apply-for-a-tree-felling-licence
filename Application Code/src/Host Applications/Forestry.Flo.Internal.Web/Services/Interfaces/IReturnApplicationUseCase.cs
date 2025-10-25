using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract for a use case that allows a field manager to return a felling licence application to a Woodland Officer or Admin Officer.
/// </summary>
public interface IReturnApplicationUseCase
{
    /// <summary>
    /// Returns an application that has been sent for approval, reverting it to the previous status (Woodland Officer Review or Admin Officer Review).
    /// </summary>
    /// <param name="user">The internal user making the request.</param>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="returnReason">The reason supplied for returning the application, including case note details and visibility settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="FinaliseFellingLicenceApplicationResult"/> indicating the outcome of the operation, including any non-blocking failures.
    /// </returns>
    Task<FinaliseFellingLicenceApplicationResult> ReturnApplication(
        InternalUser user,
        Guid applicationId,
        FormLevelCaseNote returnReason,
        CancellationToken cancellationToken);
}
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for the CBW check use case in the admin officer review process.
/// </summary>
public interface ICBWCheckUseCase
{
    /// <summary>
    /// Gets a populated <see cref="CBWCheckModel"/> for admin officers to verify mapping for an application in review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="CBWCheckModel"/> containing details of an application's mapping.</returns>
    Task<Result<CBWCheckModel>> GetCBWCheckModelAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Completes the CBW check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="isCheckPassed">A flag indicating whether the CBW check is successful.</param>
    /// <param name="failureReason">A textual reason why the CBW check has failed, if the check is unsuccessful.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the CBW check has been updated successfully.</returns>
    Task<Result> CompleteCBWCheckAsync(
        Guid applicationId,
        Guid performingUserId,
        bool isCheckPassed,
        string? failureReason,
        CancellationToken cancellationToken);
}
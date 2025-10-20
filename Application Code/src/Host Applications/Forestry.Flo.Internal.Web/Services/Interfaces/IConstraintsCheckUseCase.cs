using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for constraints check operations in the admin officer review process.
/// </summary>
public interface IConstraintsCheckUseCase
{
    /// <summary>
    /// Gets a populated <see cref="ConstraintsCheckModel"/> for admin officers to verify mapping for an application in review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ConstraintsCheckModel"/> containing details of an application's mapping.</returns>
    Task<Result<ConstraintsCheckModel>> GetConstraintsCheckModel(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Completes the constraints check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="isAgencyApplication">A flag indicating if the application is an Agency application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="isCheckComplete">A flag indicating whether the constraints check is complete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the admin officer review has been updated successfully.</returns>
    Task<Result> CompleteConstraintsCheckAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool isCheckComplete,
        CancellationToken cancellationToken);
}
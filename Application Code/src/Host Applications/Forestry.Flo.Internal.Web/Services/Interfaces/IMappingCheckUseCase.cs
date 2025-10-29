using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

public interface IMappingCheckUseCase
{
    /// <summary>
    /// Gets a populated <see cref="MappingCheckModel"/> for admin officers to verify mapping for an application in review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="MappingCheckModel"/> containing details of an application's mapping.</returns>
    Task<Result<MappingCheckModel>> GetMappingCheckModelAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Completes the mapping check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="isCheckPassed">A flag indicating whether the mapping check is successful.</param>
    /// <param name="failureReason">A textual reason why the mapping check has failed, if the check is unsuccessful.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the mapping check has been updated successfully.</returns>
    Task<Result> CompleteMappingCheckAsync(
        Guid applicationId,
        Guid performingUserId,
        bool isCheckPassed,
        string? failureReason,
        CancellationToken cancellationToken);
}
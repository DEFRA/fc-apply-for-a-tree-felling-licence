using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract for a service that handles the tree health check tasks for the Admin Officer
/// review process.
/// </summary>
public interface IAdminOfficerTreeHealthCheckUseCase
{
    /// <summary>
    /// Gets the view model for the tree health check step in the admin officer review process.
    /// </summary>
    /// <param name="applicationId">The Id of the application to retrieve data for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct containing the view model, or a failure.</returns>
    Task<Result<CheckTreeHealthIssuesViewModel>> GetTreeHealthCheckAdminOfficerViewModelAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes the tree health check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The id of the task being completed.</param>
    /// <param name="user">The user confirming they have checked the tree health issues.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success of the operation.</returns>
    Task<Result> ConfirmTreeHealthCheckedAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken);
}
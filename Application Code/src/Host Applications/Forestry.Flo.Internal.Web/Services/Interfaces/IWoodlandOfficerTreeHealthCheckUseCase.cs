using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

public interface IWoodlandOfficerTreeHealthCheckUseCase
{
    /// <summary>
    /// Gets the view model for the tree health confirmation step in the woodland officer review process.
    /// </summary>
    /// <param name="applicationId">The Id of the application to retrieve data for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct containing the view model, or a failure.</returns>
    Task<Result<ConfirmTreeHealthIssuesViewModel>> GetTreeHealthCheckWoodlandOfficerViewModelAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes the tree health check task in the woodland officer review.
    /// </summary>
    /// <param name="applicationId">The id of the task being completed.</param>
    /// <param name="user">The user confirming the tree health issues.</param>
    /// <param name="isConfirmed">A flag indicating whether the woodland officer is happy with
    /// the data input by the applicant.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success of the operation.</returns>
    Task<Result> ConfirmTreeHealthIssuesAsync(
        Guid applicationId,
        InternalUser user,
        bool isConfirmed,
        CancellationToken cancellationToken);
}
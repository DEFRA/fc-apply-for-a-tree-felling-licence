using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for admin officer review use case operations.
/// </summary>
public interface IAdminOfficerReviewUseCase
{
    /// <summary>
    /// Retrieves the admin officer review model for the specified application.
    /// </summary>
    /// <param name="applicationId">The identifier for the application to review.</param>
    /// <param name="user">The internal user requesting the review.</param>
    /// <param name="hostingPage">The page to return to after adding an activity feed comment.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the populated <see cref="AdminOfficerReviewModel"/>.</returns>
    Task<Result<AdminOfficerReviewModel>> GetAdminOfficerReviewAsync(
        Guid applicationId,
        InternalUser user,
        string hostingPage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Confirms the completion of the admin officer review for the specified application.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="user">The internal user confirming the review.</param>
    /// <param name="internalLinkToApplication">The internal link to the application.</param>
    /// <param name="dateReceived">The date the application was received.</param>
    /// <param name="isAgentApplication">Indicates if the application was submitted by an agent.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure of the confirmation.</returns>
    Task<Result> ConfirmAdminOfficerReview(
        Guid applicationId,
        InternalUser user,
        string internalLinkToApplication,
        DateTime dateReceived,
        bool isAgentApplication,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes the larch check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the larch check has been updated successfully.</returns>
    Task<Result> CompleteLarchCheckAsync(
        Guid applicationId,
        Guid performingUserId,
        CancellationToken cancellationToken);
}
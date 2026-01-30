using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for woodland officer review use case operations.
/// </summary>
public interface IWoodlandOfficerReviewUseCase
{
    /// <summary>
    /// Retrieves the woodland officer review model for a given application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="user">The internal user requesting the review.</param>
    /// <param name="hostingPage">The name of the hosting page.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{Result}"/> containing the <see cref="WoodlandOfficerReviewModel"/> if successful.
    /// </returns>
    Task<Result<WoodlandOfficerReviewModel>> WoodlandOfficerReviewAsync(
        Guid applicationId,
        InternalUser user,
        string hostingPage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes the woodland officer review for a given application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="recommendedLicenceDuration">The recommended licence duration.</param>
    /// <param name="recommendationForDecisionPublicRegister">Recommendation for decision public register.</param>
    /// <param name="recommendationForPublicRegisterReason">Reason for public register recommendation.</param>
    /// <param name="internalLinkToApplication">Internal link to the application.</param>
    /// <param name="user">The internal user completing the review.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{Result}"/> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> CompleteWoodlandOfficerReviewAsync(
        Guid applicationId,
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool? recommendationForDecisionPublicRegister,
        string recommendationForPublicRegisterReason,
        string internalLinkToApplication,
        string? supplementaryPoints,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes the mapping check (Larch check) task in the woodland officer review.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="performingUserId">The identifier of the internal user performing the check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{Result}"/> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> CompleteLarchCheckAsync(
        Guid applicationId,
        Guid performingUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes the Confirmed Felling and Restocking details task in the woodland officer review.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="user">The internal user performing the update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{Result}"/> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> CompleteConfirmedFellingAndRestockingDetailsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes the Environmental Impact Assessment (EIA) screening check for the specified application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="user">The internal user performing the update.</param>
    /// <param name="isScreeningCompleted">A flag to indicate if screening has been completed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Task{Result}"/> indicating the success or failure of the EIA screening completion.
    /// </returns>
    Task<Result> CompleteEiaScreeningAsync(
        Guid applicationId,
        InternalUser user,
        bool isScreeningCompleted,
        CancellationToken cancellationToken);
}
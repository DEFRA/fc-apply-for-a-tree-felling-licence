using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service that updates a felling licence application for tasks
/// undertaken during an admin officer review. 
/// </summary>
public interface IUpdateAdminOfficerReviewService
{
    /// <summary>
    /// Completes the admin officer review of an application, transitioning the application
    /// to the woodland officer review stage.
    /// </summary>
    /// <param name="applicationId">The id of the application to complete the admin officer review for.</param>
    /// <param name="performingUserId">The id of the user completing the review.</param>
    /// <param name="completedDateTime">The date and time that the review was completed.</param>
    /// <param name="isAgencyApplication">A flag indicating whether the application was submitted by an agent.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="CompleteAdminOfficerReviewNotificationsModel"/> providing the ids of the
    /// users that require notifications of the completion of the admin officer review.</returns>
    Task<Result<CompleteAdminOfficerReviewNotificationsModel>> CompleteAdminOfficerReviewAsync(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        bool isAgencyApplication,
        bool requireWOReview,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the completion status of the agent authority form check.
    /// </summary>
    /// <param name="applicationId">The id of the application to complete the check for.</param>
    /// <param name="isAgencyApplication">A flag indicating whether the application is an agency application.</param>
    /// <param name="performingUserId">The id of the user completing the check.</param>
    /// <param name="complete">A flag indicating whether the check is complete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the check has been marked as the correct status.</returns>
    Task<Result> SetAgentAuthorityCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the completion status of the mapping check.
    /// </summary>
    /// <param name="applicationId">The id of the application to complete the check for.</param>
    /// <param name="isAgencyApplication">A flag indicating whether the application is an agency application.</param>
    /// <param name="performingUserId">The id of the user completing the check.</param>
    /// <param name="complete">A flag indicating whether the check is complete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the check has been marked as the correct status.</returns>
    Task<Result> SetMappingCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the completion status of the constraints check.
    /// </summary>
    /// <param name="applicationId">The id of the application to complete the check for.</param>
    /// <param name="isAgencyApplication">A flag indicating whether the application is an agency application.</param>
    /// <param name="performingUserId">The id of the user completing the check.</param>
    /// <param name="complete">A flag indicating whether the check is complete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the check has been marked as the correct status.</returns>
    Task<Result> SetConstraintsCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the completion status of the Larch check.
    /// </summary>
    /// <param name="applicationId">The id of the application to complete the check for.</param>
    /// <param name="isAgencyApplication">A flag indicating whether the application is an agency application.</param>
    /// <param name="performingUserId">The id of the user completing the check.</param>
    /// <param name="complete">A flag indicating whether the check is complete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the check has been marked as the correct status.</returns>
    Task<Result> SetLarchCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the completion status of the CBW check.
    /// </summary>
    /// <param name="applicationId">The id of the application to complete the check for.</param>
    /// <param name="isAgencyApplication">A flag indicating whether the application is an agency application.</param>
    /// <param name="performingUserId">The id of the user completing the check.</param>
    /// <param name="complete">A flag indicating whether the check is complete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the check has been marked as the correct status.</returns>
    Task<Result> SetCBWCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the check passed and failure reason values for agent authority form checks in an admin officer review entity.
    /// </summary>
    /// <param name="applicationId">The id of the application to complete the check for.</param>
    /// <param name="performingUserId">The id of the user completing the check.</param>
    /// <param name="isCheckPassed">Whether or not the check was successful.</param>
    /// <param name="failureReason">A reason why the check has failed, if it was unsuccessful.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the agent authority entity has been updated to include the check values.</returns>
    Task<Result> UpdateAgentAuthorityFormDetailsAsync(
        Guid applicationId,
        Guid performingUserId,
        bool isCheckPassed,
        string? failureReason,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the check passed and failure reason values for mapping checks in an admin officer review entity.
    /// </summary>
    /// <param name="applicationId">The id of the application to complete the check for.</param>
    /// <param name="performingUserId">The id of the user completing the check.</param>
    /// <param name="isCheckPassed">Whether or not the check was successful.</param>
    /// <param name="failureReason">A reason why the check has failed, if it was unsuccessful.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the admin officer entity has been updated to include the check values.</returns>
    Task<Result> UpdateMappingCheckDetailsAsync(
        Guid applicationId,
        Guid performingUserId,
        bool isCheckPassed,
        string? failureReason,
        CancellationToken cancellationToken);
}
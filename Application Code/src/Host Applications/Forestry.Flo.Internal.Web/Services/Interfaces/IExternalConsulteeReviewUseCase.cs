using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for the ExternalConsulteeReview use case.
/// </summary>
public interface IExternalConsulteeReviewUseCase
{
    /// <summary>
    /// Validates the access code for an external consultee review.
    /// </summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <param name="accessCode">The access code to validate.</param>
    /// <param name="emailAddress">The email address associated with the access code.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the validated <see cref="ExternalInviteLink"/> or an error message.</returns>
    Task<Result<ExternalInviteLink>> ValidateAccessCodeAsync(
        Guid applicationId,
        Guid accessCode,
        string emailAddress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the application summary for consultee review.
    /// </summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <param name="externalInviteLink">The external invite link.</param>
    /// <param name="accessCode">The access code.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the <see cref="ExternalConsulteeReviewViewModel"/> or an error message.</returns>
    Task<Result<ExternalConsulteeReviewViewModel>> GetApplicationSummaryForConsulteeReviewAsync(
        Guid applicationId,
        ExternalInviteLink externalInviteLink,
        Guid accessCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a consultee comment to the application.
    /// </summary>
    /// <param name="model">The model containing comment details.</param>
    /// <param name="consulteeAttachmentFiles">The collection of attachment files.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AddConsulteeCommentAsync(
        AddConsulteeCommentModel model,
        FormFileCollection consulteeAttachmentFiles,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a supporting document for the application.
    /// </summary>
    /// <param name="applicationId">The application identifier.</param>
    /// <param name="accessCode">The access code.</param>
    /// <param name="documentIdentifier">The document identifier.</param>
    /// <param name="emailAddress">The email address associated with the access code.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the <see cref="FileContentResult"/> or an error message.</returns>
    Task<Result<FileContentResult>> GetSupportingDocumentAsync(
        Guid applicationId,
        Guid accessCode,
        Guid documentIdentifier,
        string emailAddress,
        CancellationToken cancellationToken);
}
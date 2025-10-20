using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Notifications.Models;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for a use case that manages public register operations for felling licence applications.
/// </summary>
public interface IPublicRegisterUseCase
{
    /// <summary>
    /// Retrieves the public register details for a given application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the public register view model or an error.</returns>
    Task<Result<PublicRegisterViewModel>> GetPublicRegisterDetailsAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Stores the exemption status and reason for a public register entry.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="isExempt">Indicates if the application is exempt from the public register.</param>
    /// <param name="exemptionReason">The reason for exemption.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> StorePublicRegisterExemptionAsync(Guid applicationId, bool isExempt, string? exemptionReason, InternalUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes an application to the consultation public register.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="period">The period for which the application should be on the register.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> PublishToConsultationPublicRegisterAsync(Guid applicationId, TimeSpan period, InternalUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Removes an application from the consultation public register.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="esriId">The ESRI identifier for the register entry.</param>
    /// <param name="applicationReference">The application reference.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> RemoveFromPublicRegisterAsync(Guid applicationId, InternalUser user, int esriId, string applicationReference, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a specific public register comment for an application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="commentId">The unique identifier of the comment.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the review comment model or an error.</returns>
    Task<Result<ReviewCommentModel>> GetPublicRegisterCommentAsync(Guid applicationId, Guid commentId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the details of a public register comment.
    /// </summary>
    /// <param name="commentId">The unique identifier of the comment.</param>
    /// <param name="model">The notification history model containing updated details.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UpdatePublicRegisterDetailsAsync(Guid commentId, NotificationHistoryModel model, CancellationToken cancellationToken);
}
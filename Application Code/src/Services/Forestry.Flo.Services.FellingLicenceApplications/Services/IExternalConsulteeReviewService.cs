using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for services implementing functionality for external consultee reviews.
/// </summary>
public interface IExternalConsulteeReviewService
{
    /// <summary>
    /// Attempts to retrieve an external access link based on a given application id, email address
    /// and access code.
    /// </summary>
    /// <param name="applicationId">The application id of the external access link to retrieve.</param>
    /// <param name="accessCode">The access code of the external access link to retrieve.</param>
    /// <param name="emailAddress">The email address of the external access link to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A populated <see cref="ExternalAccessLinkModel"/> if a matching link is found, otherwise <see cref="Maybe.None"/></returns>
    Task<Maybe<ExternalAccessLinkModel>> VerifyAccessCodeAsync(
        Guid applicationId,
        Guid accessCode, 
        string emailAddress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves any existing consultee comments from an author email address for a given application id.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve comments for.</param>
    /// <param name="emailAddress">The email address of the author to retrieve comments for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of any existing consultee comments for the given parameters.</returns>
    Task<List<ConsulteeCommentModel>> RetrieveConsulteeCommentsForAuthorAsync(
        Guid applicationId,
        string emailAddress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new consultee comment to an application.
    /// </summary>
    /// <param name="model">A populated <see cref="ConsulteeCommentModel"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> AddCommentAsync(
        ConsulteeCommentModel model,
        CancellationToken cancellationToken);
}
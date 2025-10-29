using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for use cases related to inviting and managing external consultees for felling licence applications.
/// </summary>
public interface IExternalConsulteeInviteUseCase
{
    /// <summary>
    /// Retrieves the existing invited external consultees and the not needed/complete status of consultations for the application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the <see cref="ExternalConsulteeIndexViewModel"/> for the consultee invite index page.</returns>
    Task<Result<ExternalConsulteeIndexViewModel>> GetConsulteeInvitesIndexViewModelAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the woodland officer review record to set consultations as not needed.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="user">The user performing the update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating if successful.</returns>
    Task<Result> SetDoesNotRequireConsultationsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the woodland officer review record to set consultations as complete.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="user">The user performing the update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating if successful.</returns>
    Task<Result> SetConsultationsCompleteAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a new External Consultee Invite View Model from application data by the given application id.
    /// </summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the <see cref="ExternalConsulteeInviteFormModel"/> for inviting a new consultee.</returns>
    Task<Result<ExternalConsulteeInviteFormModel>> GetNewExternalConsulteeInviteViewModelAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends an invitation to an external consultee to review the application.
    /// </summary>
    /// <param name="externalConsulteeInviteModel">The consultee invite view model.</param>
    /// <param name="applicationId">The application Id.</param>
    /// <param name="user">A current system user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result of the operation.</returns>
    Task<Result> InviteExternalConsulteeAsync(
        ExternalConsulteeInviteModel externalConsulteeInviteModel,
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a view model to display the received comments for a given application id and access code.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve the received comments for.</param>
    /// <param name="accessCode">The access code of the particular invite to retrieve comments for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the <see cref="ReceivedConsulteeCommentsViewModel"/> model of the data to display.</returns>
    Task<Result<ReceivedConsulteeCommentsViewModel>> GetReceivedCommentsAsync(
        Guid applicationId,
        Guid accessCode,
        CancellationToken cancellationToken);
}
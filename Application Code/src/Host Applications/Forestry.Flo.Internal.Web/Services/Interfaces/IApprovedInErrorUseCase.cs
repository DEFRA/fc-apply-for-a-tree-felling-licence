using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines use case operations for handling the Approved In Error process for felling licence applications.
/// </summary>
public interface IApprovedInErrorUseCase
{
    /// <summary>
    /// Retrieves the Approved In Error view model for a specific felling licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application to review.</param>
    /// <param name="viewingUser">The internal user viewing the summary.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>
    /// A <see cref="Maybe{ApprovedInErrorViewModel}"/> containing the review summary model if found, otherwise <see cref="Maybe.None"/>.
    /// </returns>
    Task<Maybe<ApprovedInErrorViewModel>> RetrieveApprovedInErrorAsync(
        Guid applicationId,
        InternalUser viewingUser,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves the Approved In Error details for a given application.
    /// </summary>
    /// <param name="model">The Approved In Error model containing the details to save.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="viewApplicationUrl">The URL to view the application.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the outcome of the save operation.
    /// </returns>
    Task<Result> ConfirmApprovedInErrorAsync(
        ApprovedInErrorModel model,
        InternalUser user,
        string viewApplicationUrl,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the Re-approve In Error view model for a specific felling licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="viewingUser">The internal user viewing the summary.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>
    /// A <see cref="Maybe{ReApproveInErrorViewModel}"/> containing the view model if found, otherwise <see cref="Maybe.None"/>.
    /// </returns>
    Task<Maybe<ReApproveInErrorViewModel>> RetrieveReApproveInErrorAsync(
        Guid applicationId,
        InternalUser viewingUser,
        CancellationToken cancellationToken);

    /// <summary>
    /// Re-approves an application after correcting errors, updating the approved in error details, 
    /// the licence expiry date, and regenerating the PDF document.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="model">The approved in error model containing the corrected details.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating if successful, or an error if unsuccessful.
    /// </returns>
    Task<Result> ReApprovedInErrorAsync(
        Guid applicationId,
        InternalUser user,
        ApprovedInErrorModel model,
        CancellationToken cancellationToken);
}
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service that retrieves the current details of the woodland
/// officer review for felling licence applications.
/// </summary>
public interface IGetWoodlandOfficerReviewService
{
    /// <summary>
    /// Retrieves the overall status of the woodland officer review for an application, including
    /// the status of each task for the review, comments, and the woodland officers recommendation.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve the woodland officer review for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="WoodlandOfficerReviewStatusModel"/> instance.</returns>
    Task<Result<WoodlandOfficerReviewStatusModel>> GetWoodlandOfficerReviewStatusAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the current public register details for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve public register details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="PublicRegisterModel"/> instance, if a record exists for the application.</returns>
    Task<Result<Maybe<PublicRegisterModel>>> GetPublicRegisterDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the current site visit details for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve site visit details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="SiteVisitModel"/> instance, if a record exists for the application.</returns>
    Task<Result<Maybe<SiteVisitModel>>> GetSiteVisitDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the current PW14 checks details for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve PW14 checks details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="Pw14ChecksModel"/> instance, if a record exists for the application.</returns>
    Task<Result<Maybe<Pw14ChecksModel>>> GetPw14ChecksAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a model containing the data required to publish the application to the public register.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve data for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ApplicationDetailsForPublicRegisterModel"/> instance.</returns>
    Task<Result<ApplicationDetailsForPublicRegisterModel>> GetApplicationDetailsToSendToPublicRegisterAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current status of conditions for the application in the woodland officer review.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve conditions status for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<Result<ConditionsStatusModel>> GetConditionsStatusAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the required details of the application for sending the calculated conditions to the applicant.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ApplicationDetailsForConditionsNotification"/> record.</returns>
    Task<Result<ApplicationDetailsForConditionsNotification>> GetDetailsForConditionsNotificationAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the compartment designations entered by the woodland officer for the application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve data for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ApplicationSubmittedCompartmentDesignations"/> model of the designations data.</returns>
    Task<Result<ApplicationSubmittedCompartmentDesignations>> GetCompartmentDesignationsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the current (most recent) felling and restocking amendment review for the specified application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve the current amendment review for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the current <see cref="FellingAndRestockingAmendmentReviewModel"/> instance.
    /// </returns>
    Task<Result<Maybe<FellingAndRestockingAmendmentReviewModel>>> GetCurrentFellingAndRestockingAmendmentReviewAsync(
        Guid applicationId,
        CancellationToken cancellationToken);
}
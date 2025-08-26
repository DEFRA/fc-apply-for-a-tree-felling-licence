using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service that updates the current details of the woodland
/// officer review for felling licence applications.
/// </summary>
public interface IUpdateWoodlandOfficerReviewService
{
    /// <summary>
    /// Updates the <see cref="PublicRegister"/> entity for an application to set it as exempt from publishing
    /// to the PR with a given reason.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The id of the user making the update.</param>
    /// <param name="isExempt">A flag indicating whether or not the application is exempt from the public register.</param>
    /// <param name="exemptReason">The given reason for why it is exempt from the PR.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A boolean flag indicating whether an update was made.</returns>
    Task<Result<bool>> SetPublicRegisterExemptAsync(
        Guid applicationId, 
        Guid userId, 
        bool isExempt,
        string? exemptReason, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the <see cref="PublicRegister"/> entity for an application with the published
    /// timestamp and also calculates the expiry date.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The id of the user making the update.</param>
    /// <param name="esriId">The id of the application on the public register, returned from the ESRI interface.</param>
    /// <param name="publishedDateTime">The date and time that the application was published to the public register.</param>
    /// <param name="publicRegisterPeriod">The period of time that the application should be on the public register.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> PublishedToPublicRegisterAsync(
        Guid applicationId,
        Guid userId,
        int esriId,
        DateTime publishedDateTime,
        TimeSpan publicRegisterPeriod,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the <see cref="PublicRegister"/> entity for an application with the removed
    /// timestamp.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The id of the user making the update.</param>
    /// <param name="removedDateTime">The date and time that the application was removed from the public register.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> RemovedFromPublicRegisterAsync(
        Guid applicationId,
        Guid userId,
        DateTime removedDateTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the stored values for the PW14 checks for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="model">A populated <see cref="Pw14ChecksModel"/> containing the new values.</param>
    /// <param name="userId">The id of the user making the update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> UpdatePw14ChecksAsync(
        Guid applicationId,
        Pw14ChecksModel model,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the <see cref="WoodlandOfficerReview"/> entity for an application to set the site visit as not needed
    /// and save a site visit comment case note with the given text.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The id of the user making the update.</param>
    /// <param name="siteVisitNotNeededReason">The given reason for why the application does not require a site visit.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A boolean flag indicating whether an update was made.</returns>
    Task<Result<bool>> SetSiteVisitNotNeededAsync(
        Guid applicationId,
        Guid userId,
        string siteVisitNotNeededReason,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the <see cref="WoodlandOfficerReview"/> to indicate that the site visit artefacts have been
    /// created and the application published to the mobile app layers.
    /// </summary>
    /// <param name="applicationId">The id of the application.</param>
    /// <param name="userId">The id of the user.</param>
    /// <param name="publishedDateTime">The date and time the application site visit artefacts were generated.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> PublishedToSiteVisitMobileLayersAsync(
        Guid applicationId,
        Guid userId,
        DateTime publishedDateTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the <see cref="WoodlandOfficerReview"/> to indicate that site visit notes have been retrieved
    /// for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application.</param>
    /// <param name="userId">The id of the user.</param>
    /// <param name="retrievedDateTime">The date and time that the site visit notes were retrieved.</param>
    /// <param name="retrievedNotes">A list of site visit notes retrieved from the mobile app layers.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> SiteVisitNotesRetrievedAsync(
        Guid applicationId,
        Guid userId,
        DateTime retrievedDateTime,
        List<SiteVisitNotes<Guid>> retrievedNotes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes the woodland officer review of an application, transitioning the application
    /// to the woodland officer review stage.
    /// </summary>
    /// <param name="applicationId">The id of the application to complete the woodland officer review for.</param>
    /// <param name="performingUserId">The id of the user completing the review.</param>
    /// <param name="recommendedLicenceDuration">The chosen <see cref="RecommendedLicenceDuration"/> value from the woodland officer.</param>
    /// <param name="recommendationForDecisionPublicRegister">The woodland officer's recommendation for whether to publish this application
    /// to the decision public register.</param>
    /// <param name="completedDateTime">The date and time that the review was completed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="CompleteWoodlandOfficerReviewNotificationsModel"/> providing the ids of the
    /// users that require notifications of the completion of the woodland officer review.</returns>
    Task<Result<CompleteWoodlandOfficerReviewNotificationsModel>> CompleteWoodlandOfficerReviewAsync(
        Guid applicationId,
        Guid performingUserId,
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool? recommendationForDecisionPublicRegister,
        DateTime completedDateTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the stored values for the conditions status for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="model">A populated <see cref="ConditionsStatusModel"/> containing the new values.</param>
    /// <param name="userId">The id of the user making the update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> UpdateConditionalStatusAsync(
        Guid applicationId,
        ConditionsStatusModel model,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Enacts changes to a <see cref="WoodlandOfficerReview"/> after confirmed felling and restocking details are modified.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The id of the user updating the confirmed felling and restocking details.</param>
    /// <param name="complete">A flag indicating whether the confirmed felling and restocking details are finalised.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<Result> HandleConfirmedFellingAndRestockingChangesAsync(
        Guid applicationId,
        Guid userId,
        bool complete,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes the Larch check for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to be updated.</param>
    /// <param name="userId">The id of the user performing the update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<Result> UpdateLarchCheckAsync(
        Guid applicationId, 
        Guid userId, 
        CancellationToken cancellationToken);
}
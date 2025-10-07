using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
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
        FormLevelCaseNote siteVisitNotNeededReason,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the <see cref="WoodlandOfficerReview"/> entity for an application to set the site visit as needed
    /// with the given arrangements flag and arrangements case note.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The id of the user making the update.</param>
    /// <param name="siteVisitArrangementsMade">A flag indicating whether arrangements for the site visit have been made.</param>
    /// <param name="siteVisitArrangements">A case note describing the arrangements.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the update was successful.</returns>
    Task<Result> SaveSiteVisitArrangementsAsync(
        Guid applicationId,
        Guid userId,
        bool? siteVisitArrangementsMade,
        FormLevelCaseNote siteVisitArrangements,
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
    /// <param name="recommendationForDecisionPublicRegisterReason">The woodland officer's reason for their recommendation for
    /// whether to publish this application to the decision public register.</param>
    /// <param name="completedDateTime">The date and time that the review was completed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="CompleteWoodlandOfficerReviewNotificationsModel"/> providing the ids of the
    /// users that require notifications of the completion of the woodland officer review.</returns>
    Task<Result<CompleteWoodlandOfficerReviewNotificationsModel>> CompleteWoodlandOfficerReviewAsync(
        Guid applicationId,
        Guid performingUserId,
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool? recommendationForDecisionPublicRegister,
        string recommendationForDecisionPublicRegisterReason,
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

    /// <summary>
    /// Completes the EIA (Environmental Impact Assessment) screening check for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to be updated.</param>
    /// <param name="userId">The ID of the user performing the update.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> CompleteEiaScreeningCheckAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the site visit evidence documents for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to be updated.</param>
    /// <param name="userId">The id of the user performing the update.</param>
    /// <param name="evidence">An array of <see cref="SiteVisitEvidenceDocument"/> models representing the documents</param>
    /// <param name="observations">A <see cref="FormLevelCaseNote"/> with any additional observations from the woodland officer.</param>
    /// <param name="isComplete">A flag indicating whether the site visit is confirmed as complete.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An awaitable task.</returns>
    Task<Result> UpdateSiteVisitEvidenceAsync(
        Guid applicationId,
        Guid userId,
        SiteVisitEvidenceDocument[] evidence,
        FormLevelCaseNote observations,
        bool isComplete,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the status of consultations for an application.
    /// </summary>
    /// <param name="applicationId">The application to update.</param>
    /// <param name="userId">The id of the user making the update.</param>
    /// <param name="isNeeded">An optional flag indicating whether consultations are needed.</param>
    /// <param name="isComplete">An optional flag indicating if the consultations phase is complete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating if the operation was successful.</returns>
    Task<Result> UpdateConsultationsStatusAsync(
        Guid applicationId,
        Guid userId,
        bool? isNeeded,
        bool? isComplete,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the felling and restocking amendment review details for a given application.
    /// </summary>
    /// <param name="model">A <see cref="FellingAndRestockingAmendmentReviewUpdateRecord"/> containing the amendment review update details.</param>
    /// <param name="userId">The id of the user making the update.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> UpdateFellingAndRestockingAmendmentReviewAsync(
        FellingAndRestockingAmendmentReviewUpdateRecord model,
        Guid userId,
        CancellationToken cancellationToken);
    Task<Result<FellingAndRestockingAmendmentReview>> CreateFellingAndRestockingAmendmentReviewAsync(Guid applicationId, Guid userId, DateTime responseDeadline, string? amendmentsReason, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the selected compartment designations for an application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to be updated.</param>
    /// <param name="userId">The ID of the user performing the update.</param>
    /// <param name="designations">A model of the designations for a compartment.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating success or failure.</returns>
    Task<Result> UpdateCompartmentDesignationsAsync(
        Guid applicationId,
        Guid userId,
        SubmittedCompartmentDesignationsModel designations,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the woodland officer review to indicate that all compartment designations have been reviewed and completed.
    /// </summary>
    /// <param name="applicationId">The ID of the application to be updated.</param>
    /// <param name="userId">The ID of the user performing the update.</param>
    /// <param name="isComplete">A flag to indicate if the compartment designations are complete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating success or failure.</returns>
    Task<Result> UpdateApplicationCompartmentDesignationsCompletedAsync(
        Guid applicationId,
        Guid userId,
        bool isComplete,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks a felling and restocking amendment review as completed.
    /// </summary>
    /// <param name="amendmentReviewId">The unique identifier of the amendment review to complete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> CompleteFellingAndRestockingAmendmentReviewAsync(Guid amendmentReviewId, CancellationToken cancellationToken);
}
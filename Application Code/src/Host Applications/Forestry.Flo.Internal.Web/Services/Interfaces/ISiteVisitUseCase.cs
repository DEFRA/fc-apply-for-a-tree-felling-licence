using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for site visit use case operations in the woodland officer review process.
/// </summary>
public interface ISiteVisitUseCase
{
    /// <summary>
    /// Retrieves the site visit details for a felling licence application, including comments and summary information.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve details for.</param>
    /// <param name="hostingPage">The hosting page.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result{SiteVisitViewModel}"/> representing the current state of the site visit.</returns>
    Task<Result<SiteVisitViewModel>> GetSiteVisitDetailsAsync(
        Guid applicationId,
        string hostingPage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the site visit status for a felling licence application, indicating that a site visit is not needed.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update site visit details for.</param>
    /// <param name="user">The user making the update.</param>
    /// <param name="siteVisitNotNeededReason">The reason for not needing a site visit, to be stored as a case note.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> SiteVisitIsNotNeededAsync(
        Guid applicationId,
        InternalUser user,
        FormLevelCaseNote siteVisitNotNeededReason,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the site visit arrangements for a felling licence application, including whether arrangements have been made and any notes about the arrangements.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update.</param>
    /// <param name="user">The user making the update.</param>
    /// <param name="siteVisitArrangementsMade">A flag indicating if any arrangements have been made.</param>
    /// <param name="siteVisitArrangements">Details of the arrangements, to be stored as a case note.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> SetSiteVisitArrangementsAsync(
        Guid applicationId,
        InternalUser user,
        bool? siteVisitArrangementsMade,
        FormLevelCaseNote siteVisitArrangements,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the site visit summary for a felling licence application, including comments and summary information.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve a summary for.</param>
    /// <param name="hostingPage">The hosting page.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result{SiteVisitSummaryModel}"/> representing the application details required for a site visit summary document.</returns>
    Task<Result<SiteVisitSummaryModel>> GetSiteVisitSummaryAsync(
        Guid applicationId,
        string hostingPage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the site visit evidence model for a felling licence application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve details for.</param>
    /// <param name="hostingPage">The hosting page.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result{AddSiteVisitEvidenceModel}"/> representing the evidence model.</returns>
    Task<Result<AddSiteVisitEvidenceModel>> GetSiteVisitEvidenceModelAsync(
        Guid applicationId,
        string hostingPage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds site visit evidence documents and comments for a felling licence application.
    /// </summary>
    /// <param name="model">A <see cref="AddSiteVisitEvidenceModel"/> representing the data to be stored.</param>
    /// <param name="siteVisitAttachmentFiles">A <see cref="FormFileCollection"/> containing the new files to be stored.</param>
    /// <param name="user">The user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> AddSiteVisitEvidenceAsync(
        AddSiteVisitEvidenceModel model,
        FormFileCollection siteVisitAttachmentFiles,
        InternalUser user,
        CancellationToken cancellationToken);
}
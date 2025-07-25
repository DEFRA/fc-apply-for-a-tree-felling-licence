﻿using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

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
    /// Gets the details of an application required to publish the application to the mobile apps layers.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve data for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ApplicationDetailsForSiteVisitMobileLayers"/> instance.</returns>
    Task<Result<ApplicationDetailsForSiteVisitMobileLayers>> GetApplicationDetailsForSiteVisitMobileLayersAsync(
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
}
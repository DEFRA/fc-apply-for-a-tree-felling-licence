﻿using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using System.Security.Policy;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// An implementation of <see cref="IGetWoodlandOfficerReviewService"/> that retrieves the woodland officer
/// review status using <see cref="IFellingLicenceApplicationInternalRepository"/> and <see cref="IViewCaseNotesService"/>
/// instances.
/// </summary>
public class GetWoodlandOfficerReviewService : IGetWoodlandOfficerReviewService
{
    private readonly ILogger<GetWoodlandOfficerReviewService> _logger;
    private readonly IFellingLicenceApplicationInternalRepository _internalFlaRepository;
    private readonly IForesterServices _iForesterServices;
    private readonly IViewCaseNotesService _viewCaseNotesService;

    /// <summary>
    /// Creates a new instance of <see cref="GetWoodlandOfficerReviewService"/>.
    /// </summary>
    /// <param name="internalFlaRepository">An <see cref="IFellingLicenceApplicationInternalRepository"/> instance.</param>
    /// <param name="iForesterServices">An <see cref="IForestryServices"/> instance.</param>
    /// <param name="viewCaseNotesService">An <see cref="IViewCaseNotesService"/> instance.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance.</param>
    public GetWoodlandOfficerReviewService(
        IFellingLicenceApplicationInternalRepository internalFlaRepository,
        IForesterServices iForesterServices,
        IViewCaseNotesService viewCaseNotesService,
        ILogger<GetWoodlandOfficerReviewService> logger)
    {
        _logger = logger ?? new NullLogger<GetWoodlandOfficerReviewService>();
        _internalFlaRepository = Guard.Against.Null(internalFlaRepository);
        _iForesterServices = Guard.Against.Null(iForesterServices);
        _viewCaseNotesService = Guard.Against.Null(viewCaseNotesService);
    }

    /// <inheritdoc/>
    public async Task<Result<WoodlandOfficerReviewStatusModel>> GetWoodlandOfficerReviewStatusAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve Woodland Officer Review status for FLA with id {ApplicationId}", applicationId);

        try
        {
            var woodlandOfficerReview = await
                _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);

            var publicRegister = await _internalFlaRepository.GetPublicRegisterAsync(applicationId, cancellationToken);

            var proposedFellingAndRestockingDetails = await
                _internalFlaRepository.GetProposedFellingAndRestockingDetailsForApplicationAsync(
                    applicationId, cancellationToken);

            if (proposedFellingAndRestockingDetails.IsFailure)
            {
                _logger.LogError(
                    "Could not retrieve proposed felling and restocking details for application with id {ApplicationId}",
                    applicationId);
                return proposedFellingAndRestockingDetails.ConvertFailure<WoodlandOfficerReviewStatusModel>();
            }

            var confirmedFellingAndRestockingDetails = await
                _internalFlaRepository.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(
                    applicationId, cancellationToken);

            if (confirmedFellingAndRestockingDetails.IsFailure)
            {
                _logger.LogError(
                    "Could not retrieve confirmed felling and restocking details for application with id {ApplicationId}",
                    applicationId);
                return confirmedFellingAndRestockingDetails.ConvertFailure<WoodlandOfficerReviewStatusModel>();
            }

            var fAndRStatus = confirmedFellingAndRestockingDetails.Value.Item1.Any() == false &&
                              confirmedFellingAndRestockingDetails.Value.Item2.Any() == false
                ? InternalReviewStepStatus.NotStarted
                : woodlandOfficerReview.HasValue && woodlandOfficerReview.Value.ConfirmedFellingAndRestockingComplete
                    ? InternalReviewStepStatus.Completed
                    : InternalReviewStepStatus.InProgress;

            var conditionsStatus = woodlandOfficerReview.HasNoValue || woodlandOfficerReview.Value.IsConditional.HasValue == false
                ? InternalReviewStepStatus.NotStarted
                : woodlandOfficerReview.Value.ConditionsToApplicantDate.HasValue || woodlandOfficerReview.Value.IsConditional.Value == false
                    ? InternalReviewStepStatus.Completed
                    : InternalReviewStepStatus.InProgress;

            var larchCheckStatus = fAndRStatus != InternalReviewStepStatus.Completed
                ? InternalReviewStepStatus.CannotStartYet
                : woodlandOfficerReview.HasNoValue || woodlandOfficerReview.Value.LarchCheckComplete.HasValue == false
                    ? InternalReviewStepStatus.NotStarted
                    : woodlandOfficerReview.Value.LarchCheckComplete.Value == true
                        ? InternalReviewStepStatus.Completed
                        : InternalReviewStepStatus.InProgress;

            var caseNotes = await _viewCaseNotesService.GetSpecificCaseNotesAsync(
                applicationId,
                new[]
                {
                    CaseNoteType.WoodlandOfficerReviewComment
                },
                cancellationToken);

            var isLarchApplication = fAndRStatus == InternalReviewStepStatus.NotStarted
                ? IsFellingLarchSpecies(proposedFellingAndRestockingDetails.Value.Item1)
                : IsFellingLarchSpecies(confirmedFellingAndRestockingDetails.Value.Item1);

            var LarchFlyoverComplete = woodlandOfficerReview.HasValue && woodlandOfficerReview.Value.FellingLicenceApplication?.LarchCheckDetails?.FlightDate != null;
            // Flyover is required only when it's a larch application AND the inspection log was confirmed true during larch check
            var isFlyoverRequired = isLarchApplication && (woodlandOfficerReview.HasValue && woodlandOfficerReview.Value.FellingLicenceApplication?.LarchCheckDetails?.ConfirmInspectionLog == true);
            var LarchFlyoverStatus = !isFlyoverRequired
                ? InternalReviewStepStatus.NotRequired
                : larchCheckStatus != InternalReviewStepStatus.Completed
                    ? InternalReviewStepStatus.CannotStartYet
                    : LarchFlyoverComplete
                        ? InternalReviewStepStatus.Completed
                        : InternalReviewStepStatus.NotStarted;

            var consultationsStatus = InternalReviewStepStatus.NotStarted;
            if (woodlandOfficerReview.HasValue)
            {
                if (woodlandOfficerReview.Value.ConsultationsComplete)
                {
                    consultationsStatus = InternalReviewStepStatus.Completed;
                }
                else if (woodlandOfficerReview.Value.ApplicationNeedsConsultations is false)
                {
                    consultationsStatus = InternalReviewStepStatus.NotRequired;
                }
                else
                {
                    var existingLinks = await _internalFlaRepository
                        .GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(
                            applicationId, ExternalAccessLinkType.ConsulteeInvite, cancellationToken);
                    if (existingLinks.Any())
                    {
                        consultationsStatus = InternalReviewStepStatus.InProgress;
                    }
                }
            }
            var requiresEia = proposedFellingAndRestockingDetails.Value.Item1.ShouldProposedFellingDetailsRequireEia();

            var taskListStates = new WoodlandOfficerReviewTaskListStates(
                publicRegister.GetPublicRegisterStatus(),
                woodlandOfficerReview.GetSiteVisitStatus(),
                woodlandOfficerReview.GetPw14ChecksStatus(),
                fAndRStatus,
                conditionsStatus,
                consultationsStatus,
                isLarchApplication ? larchCheckStatus : InternalReviewStepStatus.NotRequired,
                LarchFlyoverStatus,
                woodlandOfficerReview.GetEiaScreeningStatus(requiresEia),
                InternalReviewStepStatus.NotStarted);

            var result = new WoodlandOfficerReviewStatusModel
            {
                RecommendedLicenceDuration = woodlandOfficerReview.HasValue
                    ? woodlandOfficerReview.Value.RecommendedLicenceDuration
                    : null,
                RecommendationForDecisionPublicRegister = woodlandOfficerReview.HasValue
                    ? woodlandOfficerReview.Value.RecommendationForDecisionPublicRegister
                    : null,
                RecommendationForDecisionPublicRegisterReason = woodlandOfficerReview.HasValue
                    ? woodlandOfficerReview.Value.RecommendationForDecisionPublicRegisterReason
                    : null,
                WoodlandOfficerReviewTaskListStates = taskListStates,
                WoodlandOfficerReviewComments = caseNotes
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetWoodlandOfficerReviewStatusAsync");
            return Result.Failure<WoodlandOfficerReviewStatusModel>(ex.Message);
        }
    }

    private static bool IsFellingLarchSpecies(List<ProposedFellingDetail> detailsList) =>
        detailsList
            .SelectMany(detail => detail.FellingSpecies)
            .Select(species => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == species.Species))
            .Any(specie => specie?.IsLarch ?? false);

    private static bool IsFellingLarchSpecies(List<ConfirmedFellingDetail> detailsList) =>
        detailsList
            .SelectMany(detail => detail.ConfirmedFellingSpecies)
            .Select(species => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == species.Species))
            .Any(specie => specie?.IsLarch ?? false);

    /// <inheritdoc/>
    public async Task<Result<Maybe<PublicRegisterModel>>> GetPublicRegisterDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve Public Register details for FLA with id {ApplicationId}", applicationId);

        try
        {
            var publicRegister = await _internalFlaRepository.GetPublicRegisterAsync(applicationId, cancellationToken);

            if (publicRegister.HasNoValue)
            {
                return Result.Success(Maybe<PublicRegisterModel>.None);
            }

            var result = new PublicRegisterModel
            {
                WoodlandOfficerSetAsExemptFromConsultationPublicRegister = publicRegister.Value.WoodlandOfficerSetAsExemptFromConsultationPublicRegister,
                WoodlandOfficerConsultationPublicRegisterExemptionReason = publicRegister.Value.WoodlandOfficerConsultationPublicRegisterExemptionReason,
                ConsultationPublicRegisterPublicationTimestamp = publicRegister.Value.ConsultationPublicRegisterPublicationTimestamp,
                ConsultationPublicRegisterExpiryTimestamp = publicRegister.Value.ConsultationPublicRegisterExpiryTimestamp,
                ConsultationPublicRegisterRemovedTimestamp = publicRegister.Value.ConsultationPublicRegisterRemovedTimestamp,
                EsriId = publicRegister.Value.EsriId
            };

            return Result.Success(Maybe<PublicRegisterModel>.From(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetPublicRegisterDetailsAsync");
            return Result.Failure<Maybe<PublicRegisterModel>>(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Maybe<SiteVisitModel>>> GetSiteVisitDetailsAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve Site Visit details for FLA with id {ApplicationId}", applicationId);

        try
        {
            var woodlandOfficerReview = await
                _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);

            var caseNotes = await _viewCaseNotesService.GetSpecificCaseNotesAsync(
                applicationId,
                [
                    CaseNoteType.SiteVisitComment
                ],
                cancellationToken);

            if (woodlandOfficerReview.HasNoValue && caseNotes.Count == 0)
            {
                return Result.Success(Maybe<SiteVisitModel>.None);
            }

            var siteVisitAttachments = new List<SiteVisitEvidenceDocument>();
            if (woodlandOfficerReview.HasValue && woodlandOfficerReview.Value.SiteVisitEvidences.Any())
            {
                var applicationDocs = await _internalFlaRepository.GetApplicationDocumentsAsync(applicationId, cancellationToken);

                siteVisitAttachments = woodlandOfficerReview.Value.SiteVisitEvidences.Select(x =>
                    {
                        var document = applicationDocs.FirstOrDefault(doc => doc.Id == x.DocumentId);
                        return document != null
                            ? new SiteVisitEvidenceDocument
                            {
                                DocumentId = document.Id,
                                FileName = document.FileName,
                                VisibleToApplicant = document.VisibleToApplicant,
                                VisibleToConsultee = document.VisibleToConsultee,
                                Label = x.Label,
                                Comment = x.Comment
                            }
                            : null;
                    })
                    .Where(evidence => evidence != null)
                    .Select(x => x!)
                    .ToList();
            }

            var result = new SiteVisitModel
            {
                SiteVisitNeeded = woodlandOfficerReview.HasValue ? woodlandOfficerReview.Value?.SiteVisitNeeded : null,
                SiteVisitArrangementsMade = woodlandOfficerReview.HasValue ? woodlandOfficerReview.Value?.SiteVisitArrangementsMade : null,
                SiteVisitComplete = woodlandOfficerReview is { HasValue: true, Value.SiteVisitComplete: true },
                SiteVisitComments = caseNotes,
                SiteVisitAttachments = siteVisitAttachments
            };

            return Result.Success(Maybe.From(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetSiteVisitDetailsAsync");
            return Result.Failure<Maybe<SiteVisitModel>>(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Maybe<Pw14ChecksModel>>> GetPw14ChecksAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve PW14 Checks details for FLA with id {ApplicationId}", applicationId);

        try
        {
            var woodlandOfficerReview = await
                _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);

            if (woodlandOfficerReview.HasNoValue)
            {
                return Result.Success(Maybe<Pw14ChecksModel>.None);
            }

            var result = new Pw14ChecksModel
            {
                LandInformationSearchChecked = woodlandOfficerReview.Value.LandInformationSearchChecked,
                AreProposalsUkfsCompliant = woodlandOfficerReview.Value.AreProposalsUkfsCompliant,
                TpoOrCaDeclared = woodlandOfficerReview.Value.TpoOrCaDeclared,
                IsApplicationValid = woodlandOfficerReview.Value.IsApplicationValid,
                EiaThresholdExceeded = woodlandOfficerReview.Value.EiaThresholdExceeded,
                EiaTrackerCompleted = woodlandOfficerReview.Value.EiaTrackerCompleted,
                EiaChecklistDone = woodlandOfficerReview.Value.EiaChecklistDone,
                LocalAuthorityConsulted = woodlandOfficerReview.Value.LocalAuthorityConsulted,
                InterestDeclared = woodlandOfficerReview.Value.InterestDeclared,
                InterestDeclarationCompleted = woodlandOfficerReview.Value.InterestDeclarationCompleted,
                ComplianceRecommendationsEnacted = woodlandOfficerReview.Value.ComplianceRecommendationsEnacted,
                MapAccuracyConfirmed = woodlandOfficerReview.Value.MapAccuracyConfirmed,
                EpsLicenceConsidered = woodlandOfficerReview.Value.EpsLicenceConsidered,
                Stage1HabitatRegulationsAssessmentRequired = woodlandOfficerReview.Value.Stage1HabitatRegulationsAssessmentRequired,
            };

            return Result.Success(Maybe<Pw14ChecksModel>.From(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetPw14ChecksAsync");
            return Result.Failure<Maybe<Pw14ChecksModel>>(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ApplicationDetailsForPublicRegisterModel>> GetApplicationDetailsToSendToPublicRegisterAsync(
       Guid applicationId,
       CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve and calculate the relevant data to publish application with id {ApplicationId} to the public register", applicationId);

        try
        {
            var fla = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);

            if (fla.HasNoValue
                || fla.Value.SubmittedFlaPropertyDetail == null
                || fla.Value.LinkedPropertyProfile == null)
            {
                _logger.LogError("Could not retrieve application with id {ApplicationId}", applicationId);
                return Result.Failure<ApplicationDetailsForPublicRegisterModel>("Could not retrieve application with given id");
            }

            // TODO: LAYER IS NOT THE CORRECT LAYER, https://harrishealthalliance.atlassian.net/browse/FLOV2-1595
            var localAuthority = await GetLocalAuthorityForFellingLicenceApplicationAsync(fla.Value, cancellationToken);

            // TODO: NEEDS TO BE THE CONFIRMED F&R DETAILS, When entities straightened out!!!
            var fellingDetails =
                fla.Value.LinkedPropertyProfile.ProposedFellingDetails ?? new List<ProposedFellingDetail>(0);

            var totalArea = fellingDetails.Sum(x => x.AreaToBeFelled);
            double broadleaf = 0;
            double conifer = 0;
            double compartmentAreaTotal = 0;

            foreach (var fellingDetail in fellingDetails)
            {
                var compartmentArea = fellingDetail.AreaToBeFelled;
                compartmentAreaTotal += compartmentArea;
            }

            var compartmentDetails =
                fla.Value.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments
                    .Select(x => x.ToInternalCompartmentDetails())
                    .ToList();

            var result = new ApplicationDetailsForPublicRegisterModel
            {
                CaseReference = fla.Value.ApplicationReference,
                PropertyName = fla.Value.SubmittedFlaPropertyDetail.Name,
                GridReference = fla.Value.OSGridReference ?? string.Empty,
                NearestTown = fla.Value.SubmittedFlaPropertyDetail.NearestTown ?? string.Empty,
                LocalAuthority = localAuthority,
                AdminRegion = fla.Value.AdministrativeRegion ?? string.Empty,
                TotalArea = fla.Value.LinkedPropertyProfile.ProposedFellingDetails.Sum(x => x.AreaToBeFelled),
                BroadleafArea = broadleaf,
                ConiferousArea = conifer,
                OpenGroundArea = totalArea - compartmentAreaTotal,
                Compartments = compartmentDetails,
                CentrePoint = fla.HasValue && !string.IsNullOrEmpty(fla.Value.CentrePoint)
                    ? JsonConvert.DeserializeObject<Point>(fla.Value.CentrePoint)
                    : null,
                AssignedInternalUserIds = fla.Value.AssigneeHistories
                    .Where(x => x.Role != AssignedUserRole.Applicant && x.Role != AssignedUserRole.Author && x.TimestampUnassigned == null)
                    .Select(x => x.AssignedUserId)
                    .ToList()

            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetApplicationDetailsForPublicRegisterAsync");
            return Result.Failure<ApplicationDetailsForPublicRegisterModel>(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ConditionsStatusModel>> GetConditionsStatusAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to get conditions status for application with id {ApplicationId}", applicationId);

        try
        {
            var result = new ConditionsStatusModel();

            var woodlandOfficerReview = await
                _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);

            if (woodlandOfficerReview.HasValue)
            {
                result.IsConditional = woodlandOfficerReview.Value.IsConditional;
                result.ConditionsToApplicantDate = woodlandOfficerReview.Value.ConditionsToApplicantDate;
            }

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetConditionsStatusAsync");
            return Result.Failure<ConditionsStatusModel>(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ApplicationDetailsForConditionsNotification>> GetDetailsForConditionsNotificationAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to get application details for the conditions notification for application with id {ApplicationId}", applicationId);

        try
        {
            var fla = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);

            if (fla.HasNoValue)
            {
                _logger.LogError("Could not retrieve application with id {ApplicationId}", applicationId);
                return Result.Failure<ApplicationDetailsForConditionsNotification>("Could not retrieve application with given id");
            }

            var result = new ApplicationDetailsForConditionsNotification
            {
                ApplicationReference = fla.Value.ApplicationReference,
                ApplicationAuthorId = fla.Value.CreatedById,
                PropertyName = fla.Value.SubmittedFlaPropertyDetail?.Name,
                WoodlandOwnerId = fla.Value.WoodlandOwnerId,
                AdministrativeRegion = fla.Value.AdministrativeRegion
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetDetailsForConditionsNotificationAsync");
            return Result.Failure<ApplicationDetailsForConditionsNotification>(ex.Message);
        }
    }

    private async Task<string> GetLocalAuthorityForFellingLicenceApplicationAsync(FellingLicenceApplication application, CancellationToken cancellationToken)
    {
        var centrePoint = !string.IsNullOrEmpty(application.CentrePoint)
            ? JsonConvert.DeserializeObject<Point>(application.CentrePoint)
            : null;

        if (centrePoint != null)
        {
            var getLocalAuthorityResult = await _iForesterServices.GetLocalAuthorityAsync(centrePoint, cancellationToken);

            if (getLocalAuthorityResult.IsSuccess)
            {
                if (getLocalAuthorityResult.Value != null)
                {
                    _logger.LogDebug("Local authority is {LocalAuthorityName} for application having Id {ApplicationId}", getLocalAuthorityResult.Value.Name, application.Id);
                    return getLocalAuthorityResult.Value.Name;
                }

                _logger.LogWarning("Local authority not found for coordinates {Coordinates}, for application having Id {ApplicationId}", centrePoint, application.Id);
            }
            else
            {
                _logger.LogWarning("Failed to get local authority for application having Id {ApplicationId}, error is {Error}", application.Id, getLocalAuthorityResult.Error);
            }
        }
        else
        {
            _logger.LogWarning("Center point coordinates could not be found on the application, so Local authority cannot be calculated for application having Id {ApplicationId}", application.Id);
        }

        return string.Empty;
    }
}
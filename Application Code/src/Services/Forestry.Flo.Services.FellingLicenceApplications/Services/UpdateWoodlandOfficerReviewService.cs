using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// An implementation of <see cref="IUpdateWoodlandOfficerReviewService"/> that updates the woodland officer
/// review details using an <see cref="IFellingLicenceApplicationInternalRepository"/>
/// instance.
/// </summary>
public class UpdateWoodlandOfficerReviewService(
    IFellingLicenceApplicationInternalRepository internalFlaRepository,
    IClock clock,
    IOptions<WoodlandOfficerReviewOptions> publicRegisterOptions,
    IViewCaseNotesService caseNotesService,
    IAddDocumentService addDocumentService,
    ILogger<UpdateWoodlandOfficerReviewService> logger,
    IOptions<DocumentVisibilityOptions> options)
    : IUpdateWoodlandOfficerReviewService
{
    private readonly IFellingLicenceApplicationInternalRepository _internalFlaRepository = Guard.Against.Null(internalFlaRepository);
    private readonly IClock _clock = Guard.Against.Null(clock);
    private readonly WoodlandOfficerReviewOptions _woodlandOfficerReviewOptions = Guard.Against.Null(publicRegisterOptions.Value);
    private readonly IViewCaseNotesService _caseNotesService = Guard.Against.Null(caseNotesService);
    private readonly IAddDocumentService _addDocumentService = Guard.Against.Null(addDocumentService);
    private readonly DocumentVisibilityOptions _options = Guard.Against.Null(options.Value);

    /// <inheritdoc />
    public async Task<Result<bool>> SetPublicRegisterExemptAsync(
        Guid applicationId, 
        Guid userId, 
        bool isExempt,
        string? exemptReason, 
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Attempting to update the Public Register entity for an application with id {ApplicationId} to set to Exempt from PR", applicationId);

        try
        {
            if (await AssertApplication(applicationId, userId, cancellationToken) == false)
            {
                return Result.Failure<bool>("Application woodland officer review unable to be updated");
            }

            var maybeExistingPr = await _internalFlaRepository.GetPublicRegisterAsync(applicationId, cancellationToken);

            if (maybeExistingPr.HasValue)
            {
                if (maybeExistingPr.Value.WoodlandOfficerSetAsExemptFromConsultationPublicRegister == isExempt &&
                    maybeExistingPr.Value.WoodlandOfficerConsultationPublicRegisterExemptionReason == exemptReason)
                {
                    logger.LogDebug("Existing public register details match the given update, returning no change made.");
                    return Result.Success(false);
                }

                if (maybeExistingPr.Value.ConsultationPublicRegisterPublicationTimestamp.HasValue && isExempt)
                {
                    logger.LogError("Attempt to set application to exempt from PR when it has already been published.");
                    return Result.Failure<bool>(
                        "Cannot set application to exempt from public register as it has already been published to the PR.");
                }

                maybeExistingPr.Value.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = isExempt;
                maybeExistingPr.Value.WoodlandOfficerConsultationPublicRegisterExemptionReason = exemptReason;
            }
            else
            {
                var entity = new PublicRegister
                {
                    FellingLicenceApplicationId = applicationId,
                    WoodlandOfficerSetAsExemptFromConsultationPublicRegister = isExempt,
                    WoodlandOfficerConsultationPublicRegisterExemptionReason = exemptReason
                };

                await _internalFlaRepository.AddPublicRegisterAsync(entity, cancellationToken);
            }

            var updateReviewResult = await UpdateWoodlandOfficerReviewLastUpdateDateAndBy(applicationId, userId, cancellationToken);

            if (updateReviewResult.IsFailure)
                return Result.Failure<bool>(updateReviewResult.Error);

            var saveResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                logger.LogError("Could not save changes to public register and woodland officer review, error: {Error}", saveResult.Error);
                return Result.Failure<bool>(saveResult.Error.ToString());
            }

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught in GetWoodlandOfficerReviewStatusAsync");
            return Result.Failure<bool>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> PublishedToPublicRegisterAsync(
        Guid applicationId, 
        Guid userId, 
        int esriId,
        DateTime publishedDateTime,
        TimeSpan publicRegisterPeriod,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Attempting to update the public register published and expiry dates");

        try
        {
            if (await AssertApplication(applicationId, userId, cancellationToken) == false)
            {
                return Result.Failure("Application woodland officer review unable to be updated");
            }

            var maybeExistingPr = await _internalFlaRepository.GetPublicRegisterAsync(applicationId, cancellationToken);

            if (maybeExistingPr.HasValue)
            {
                maybeExistingPr.Value.WoodlandOfficerConsultationPublicRegisterExemptionReason = null;
                maybeExistingPr.Value.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = false;
                maybeExistingPr.Value.ConsultationPublicRegisterPublicationTimestamp = publishedDateTime;
                maybeExistingPr.Value.ConsultationPublicRegisterExpiryTimestamp = publishedDateTime.Add(_woodlandOfficerReviewOptions.PublicRegisterPeriod);
                maybeExistingPr.Value.EsriId = esriId;
            }
            else
            {
                var entity = new PublicRegister
                {
                    FellingLicenceApplicationId = applicationId,
                    ConsultationPublicRegisterPublicationTimestamp = publishedDateTime,
                    ConsultationPublicRegisterExpiryTimestamp = publishedDateTime.Add(publicRegisterPeriod),
                    EsriId = esriId
                };

                await _internalFlaRepository.AddPublicRegisterAsync(entity, cancellationToken);
            }

            var updateReviewResult = await UpdateWoodlandOfficerReviewLastUpdateDateAndBy(applicationId, userId, cancellationToken);

            if (updateReviewResult.IsFailure)
                return Result.Failure(updateReviewResult.Error);

            var saveResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                logger.LogError("Could not save changes to public register and woodland officer review, error: {Error}", saveResult.Error);
                return Result.Failure(saveResult.Error.ToString());
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught in PublishedToPublicRegisterAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RemovedFromPublicRegisterAsync(
        Guid applicationId, 
        Guid userId, 
        DateTime removedDateTime,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Attempting to update the public register removed date");

        try
        {
            if (await AssertApplication(applicationId, userId, cancellationToken) == false)
            {
                return Result.Failure("Application woodland officer review unable to be updated");
            }

            var maybeExistingPr = await _internalFlaRepository.GetPublicRegisterAsync(applicationId, cancellationToken);

            if (maybeExistingPr.HasNoValue || maybeExistingPr.Value.ConsultationPublicRegisterPublicationTimestamp.HasValue is false)
            {
                logger.LogWarning("Attempt to set removed from public register date but no prior publication date exists, returning failure");
                return Result.Failure("Public register does not have a publication date.");
            }

            maybeExistingPr.Value.ConsultationPublicRegisterRemovedTimestamp = removedDateTime;

            var updateReviewResult = await UpdateWoodlandOfficerReviewLastUpdateDateAndBy(applicationId, userId, cancellationToken);

            if (updateReviewResult.IsFailure)
                return Result.Failure(updateReviewResult.Error);

            var saveResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                logger.LogError("Could not save changes to public register and woodland officer review, error: {Error}", saveResult.Error);
                return Result.Failure(saveResult.Error.ToString());
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught in RemovedFromPublicRegisterAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdatePw14ChecksAsync(
        Guid applicationId, 
        Pw14ChecksModel model, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(model);
        logger.LogDebug("Attempting to update the PW14 check details for application with id {ApplicationId}", applicationId);

        try
        {
            if (await AssertApplication(applicationId, userId, cancellationToken) == false)
            {
                return Result.Failure("Application woodland officer review unable to be updated");
            }

            var entity = new WoodlandOfficerReview
            {
                FellingLicenceApplicationId = applicationId
            };

            var maybeExistingWoodlandOfficerReview =
                await _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);

            if (maybeExistingWoodlandOfficerReview.HasValue)
            {
                entity = maybeExistingWoodlandOfficerReview.Value;
            }

            entity.LandInformationSearchChecked = model.LandInformationSearchChecked;
            entity.AreProposalsUkfsCompliant = model.AreProposalsUkfsCompliant;
            entity.TpoOrCaDeclared = model.TpoOrCaDeclared;
            entity.IsApplicationValid = model.IsApplicationValid;
            entity.EiaThresholdExceeded = model.EiaThresholdExceeded;
            entity.EiaTrackerCompleted = model.EiaTrackerCompleted;
            entity.EiaChecklistDone = model.EiaChecklistDone;
            entity.LocalAuthorityConsulted = model.LocalAuthorityConsulted;
            entity.InterestDeclared = model.InterestDeclared;
            entity.InterestDeclarationCompleted = model.InterestDeclarationCompleted;
            entity.ComplianceRecommendationsEnacted = model.ComplianceRecommendationsEnacted;
            entity.MapAccuracyConfirmed = model.MapAccuracyConfirmed;
            entity.EpsLicenceConsidered = model.EpsLicenceConsidered;
            entity.Stage1HabitatRegulationsAssessmentRequired = model.Stage1HabitatRegulationsAssessmentRequired;

            entity.Pw14ChecksComplete = model.Pw14ChecksComplete;

            entity.LastUpdatedById = userId;
            entity.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();

            if (maybeExistingWoodlandOfficerReview.HasNoValue)
            {
                await _internalFlaRepository.AddWoodlandOfficerReviewAsync(entity, cancellationToken);
            }
            
            var saveResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                logger.LogError("Could not save changes to woodland officer review, error: {Error}", saveResult.Error);
                return Result.Failure(saveResult.Error.ToString());
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught in UpdatePw14ChecksAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> SetSiteVisitNotNeededAsync(
        Guid applicationId, 
        Guid userId, 
        FormLevelCaseNote siteVisitNotNeededReason,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Attempting to update the woodland officer review for an application with id {ApplicationId} to set to site visit as not needed", applicationId);

        try
        {
            if (await AssertApplication(applicationId, userId, cancellationToken) == false)
            {
                return Result.Failure<bool>("Application woodland officer review unable to be updated");
            }

            var entity = new WoodlandOfficerReview
            {
                FellingLicenceApplicationId = applicationId
            };

            var maybeExistingWoodlandOfficerReview = await _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);

            if (maybeExistingWoodlandOfficerReview.HasValue)
            {
                // check if site visit not needed flag already set, if so check if there's an existing comment with the same text
                if (maybeExistingWoodlandOfficerReview.Value.SiteVisitNeeded is false)
                {
                    var existingComments = await _internalFlaRepository.GetCaseNotesAsync(
                        applicationId,
                        new[] { CaseNoteType.SiteVisitComment }, 
                        cancellationToken);

                    if (existingComments.Any(x => !string.IsNullOrWhiteSpace(x.Text) 
                                                  && x.Text.Equals(siteVisitNotNeededReason.CaseNote, StringComparison.OrdinalIgnoreCase)))
                    {
                        logger.LogWarning("Woodland officer review for application with id {ApplicationId} already set to site visit not needed and a case note exists with the given reason.", applicationId);
                        return Result.Success(false);
                    }
                }

                entity = maybeExistingWoodlandOfficerReview.Value;
            }

            entity.SiteVisitNeeded = false;
            entity.LastUpdatedById = userId;
            entity.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();

            var caseNote = new CaseNote
            {
                FellingLicenceApplicationId = applicationId,
                Text = siteVisitNotNeededReason.CaseNote,
                Type = CaseNoteType.SiteVisitComment,
                VisibleToApplicant = siteVisitNotNeededReason.VisibleToApplicant,
                VisibleToConsultee = siteVisitNotNeededReason.VisibleToConsultee,
                CreatedByUserId = userId,
                CreatedTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc()
            };
            await _internalFlaRepository.AddCaseNoteAsync(caseNote, cancellationToken);

            if (maybeExistingWoodlandOfficerReview.HasNoValue)
            {
                await _internalFlaRepository.AddWoodlandOfficerReviewAsync(entity, cancellationToken);
            }

            var saveResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                logger.LogError("Could not save changes to case notes and woodland officer review, error: {Error}", saveResult.Error);
                return Result.Failure<bool>(saveResult.Error.ToString());
            }

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught in SetSiteVisitNotNeededAsync");
            return Result.Failure<bool>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveSiteVisitArrangementsAsync(
        Guid applicationId, 
        Guid userId, 
        bool? siteVisitArrangementsMade,
        FormLevelCaseNote siteVisitArrangements, 
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Attempting to update the woodland officer review for an application with id {ApplicationId} to set to site visit arrangements", applicationId);

        try
        {
            if (await AssertApplication(applicationId, userId, cancellationToken) == false)
            {
                return Result.Failure("Application woodland officer review unable to be updated");
            }

            var entity = new WoodlandOfficerReview
            {
                FellingLicenceApplicationId = applicationId
            };

            var maybeExistingWoodlandOfficerReview = await _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);

            if (maybeExistingWoodlandOfficerReview.HasValue)
            {
                entity = maybeExistingWoodlandOfficerReview.Value;
            }

            entity.SiteVisitNeeded = true;
            entity.SiteVisitArrangementsMade = siteVisitArrangementsMade;
            entity.LastUpdatedById = userId;
            entity.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();

            var caseNote = new CaseNote
            {
                FellingLicenceApplicationId = applicationId,
                Text = siteVisitArrangements.CaseNote,
                Type = CaseNoteType.SiteVisitComment,
                VisibleToApplicant = siteVisitArrangements.VisibleToApplicant,
                VisibleToConsultee = siteVisitArrangements.VisibleToConsultee,
                CreatedByUserId = userId,
                CreatedTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc()
            };
            await _internalFlaRepository.AddCaseNoteAsync(caseNote, cancellationToken);

            if (maybeExistingWoodlandOfficerReview.HasNoValue)
            {
                await _internalFlaRepository.AddWoodlandOfficerReviewAsync(entity, cancellationToken);
            }

            var saveResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                logger.LogError("Could not save changes to case notes and woodland officer review, error: {Error}", saveResult.Error);
                return Result.Failure(saveResult.Error.ToString());
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught in SaveSiteVisitArrangementsAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<CompleteWoodlandOfficerReviewNotificationsModel>> CompleteWoodlandOfficerReviewAsync(
        Guid applicationId, 
        Guid performingUserId, 
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool? recommendationForDecisionPublicRegister,
        DateTime completedDateTime,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Attempting to complete Woodland Officer review for application with id {ApplicationId}", applicationId);

            var applicationMaybe = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);

            if (applicationMaybe.HasNoValue)
            {
                logger.LogError("Unable to find an application with the id of {Id}", applicationId);
                return Result.Failure<CompleteWoodlandOfficerReviewNotificationsModel>("Unable to find an application with supplied id");
            }

            if (AssertApplicationIsInWoodlandOfficerReviewState(applicationMaybe.Value) == false)
            {
                logger.LogError("Application with id {ApplicationId} is not in the correct state to complete the Woodland Officer review", applicationId);
                return Result.Failure<CompleteWoodlandOfficerReviewNotificationsModel>("Unable to complete Woodland Officer review for given application");
            }

            if (AssertPerformingUserIsAssignedWoodlandOfficer(applicationMaybe.Value, performingUserId) == false)
            {
                logger.LogError("User with id {UserId} is not the assigned woodland officer for application with id {ApplicationId} and so is unauthorised to complete the Woodland Officer review", performingUserId, applicationId);
                return Result.Failure<CompleteWoodlandOfficerReviewNotificationsModel>("Unable to complete Woodland Officer review for given application");
            }
            
            if (AssertHasAssignedFieldManager(applicationMaybe.Value) == false)
            {
                logger.LogError("Application with id {ApplicationId} does not have an assigned field manager, unable to complete the Woodland Officer review", applicationId);
                return Result.Failure<CompleteWoodlandOfficerReviewNotificationsModel>("Unable to complete Woodland Officer review for given application");
            }

            var woodlandOfficerReview = await _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);

            if (await AssertWoodlandOfficerReviewTasksComplete(applicationId, woodlandOfficerReview, cancellationToken) == false)
            {
                return Result.Failure<CompleteWoodlandOfficerReviewNotificationsModel>("Unable to complete Woodland Officer review for given application");
            }

            if (AssertConditions(applicationId, woodlandOfficerReview) == false)
            {
                return Result.Failure<CompleteWoodlandOfficerReviewNotificationsModel>("Unable to complete Woodland Officer review for given application");
            }

            var fieldManagerId = applicationMaybe.Value.AssigneeHistories.First(
                x => x.Role == AssignedUserRole.FieldManager && x.TimestampUnassigned.HasValue == false).AssignedUserId;

            var applicantId = applicationMaybe.Value.CreatedById;

            var result = new CompleteWoodlandOfficerReviewNotificationsModel(applicationMaybe.Value.ApplicationReference, applicantId, fieldManagerId, applicationMaybe.Value.AdministrativeRegion);

            applicationMaybe.Value.StatusHistories.Add(new StatusHistory
            {
                Created = completedDateTime,
                FellingLicenceApplicationId = applicationId,
                FellingLicenceApplication = applicationMaybe.Value,
                Status = FellingLicenceStatus.SentForApproval
            });

            woodlandOfficerReview.Value.WoodlandOfficerReviewComplete = true;
            woodlandOfficerReview.Value.RecommendedLicenceDuration = recommendedLicenceDuration;
            woodlandOfficerReview.Value.RecommendationForDecisionPublicRegister = recommendationForDecisionPublicRegister;
            woodlandOfficerReview.Value.LastUpdatedById = performingUserId;
            woodlandOfficerReview.Value.LastUpdatedDate = completedDateTime;

            var saveResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult.IsFailure) 
            {
                logger.LogError("Could not update application for completed woodland officer review stage, error {Error}", saveResult.Error);
                return Result.Failure<CompleteWoodlandOfficerReviewNotificationsModel>("Unable to complete Woodland Officer review for given application");
            }

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught in CompleteWoodlandOfficerReviewAsync");
            return Result.Failure<CompleteWoodlandOfficerReviewNotificationsModel>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateConditionalStatusAsync(
        Guid applicationId,
        ConditionsStatusModel model,
        Guid userId,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(model);
        logger.LogDebug("Attempting to update the conditional status for application with id {ApplicationId}", applicationId);

        try
        {
            if (await AssertApplication(applicationId, userId, cancellationToken) == false)
            {
                return Result.Failure("Application woodland officer review unable to be updated");
            }

            var entity = new WoodlandOfficerReview
            {
                FellingLicenceApplicationId = applicationId
            };

            var maybeExistingWoodlandOfficerReview =
                await _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);

            if (maybeExistingWoodlandOfficerReview.HasValue)
            {
                entity = maybeExistingWoodlandOfficerReview.Value;
            }

            entity.IsConditional = model.IsConditional;
            entity.ConditionsToApplicantDate = model.ConditionsToApplicantDate;

            entity.LastUpdatedById = userId;
            entity.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();

            if (maybeExistingWoodlandOfficerReview.HasNoValue)
            {
                await _internalFlaRepository.AddWoodlandOfficerReviewAsync(entity, cancellationToken);
            }

            var saveResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                logger.LogError("Could not save changes to woodland officer review, error: {Error}", saveResult.Error);
                return Result.Failure(saveResult.Error.ToString());
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught in UpdateConditionalStatusAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> HandleConfirmedFellingAndRestockingChangesAsync(
        Guid applicationId,
        Guid userId,
        bool complete,
        CancellationToken cancellationToken)
    {
        if (await AssertApplication(applicationId, userId, cancellationToken) == false)
        {
            return Result.Failure("Application woodland officer review unable to be updated");
        }

        var (_, isFailure, review, error) = await UpdateWoodlandOfficerReviewLastUpdateDateAndBy(
            applicationId,
            userId,
            cancellationToken);

        if (isFailure)
        {
            return Result.Failure(error);
        }

        review.ConfirmedFellingAndRestockingComplete = complete;

        await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateLarchCheckAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (await AssertApplication(applicationId, userId, cancellationToken) == false)
        {
            return Result.Failure("Application woodland officer review unable to be updated");
        }

        var (_, isFailure, review, error) = await UpdateWoodlandOfficerReviewLastUpdateDateAndBy(
            applicationId,
            userId,
            cancellationToken);

        if (isFailure)
        {
            return Result.Failure(error);
        }

        review.LarchCheckComplete = true;

        await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result<WoodlandOfficerReview>> UpdateWoodlandOfficerReviewLastUpdateDateAndBy(Guid applicationId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var maybeExistingWoodlandOfficerReview =
                await _internalFlaRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);

            if (maybeExistingWoodlandOfficerReview.HasValue)
            {
                maybeExistingWoodlandOfficerReview.Value.LastUpdatedById = userId;
                maybeExistingWoodlandOfficerReview.Value.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();
                return Result.Success(maybeExistingWoodlandOfficerReview.Value);
            }

            var entity = new WoodlandOfficerReview
            {
                FellingLicenceApplicationId = applicationId,
                LastUpdatedById = userId,
                LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc()
            };
            await _internalFlaRepository.AddWoodlandOfficerReviewAsync(entity, cancellationToken);

            return Result.Success(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught in UpdateWoodlandOfficerReviewLastUpdateDateAndBy");
            return Result.Failure<WoodlandOfficerReview>(ex.Message);
        }
    }

    private async Task<bool> AssertApplication(Guid applicationId, Guid performingUserId, CancellationToken cancellationToken)
    {
        if (await AssertApplicationIsInWoodlandOfficerReviewState(applicationId, cancellationToken) == false)
        {
            logger.LogError("Cannot update woodland officer review for application with id {ApplicationId} as it is not in the Woodland Officer Review state", applicationId);
            return false;
        }

        if (await AssertPerformingUserIsAssignedWoodlandOfficer(applicationId, performingUserId, cancellationToken) == false)
        {
            logger.LogError("Cannot update woodland officer review for application with id {ApplicationId} as performing user with id {UserId} is not the assigned woodland officer", 
                applicationId, performingUserId);
            return false;
        }

        logger.LogDebug("Application with id {ApplicationId} and user with id {UserId} passed state checks to update woodland officer review details",
            applicationId, performingUserId);
        return true;
    }
    
    private async Task<bool> AssertPerformingUserIsAssignedWoodlandOfficer(Guid applicationId, Guid performingUserId, CancellationToken cancellationToken)
    {
        var assigneeHistory = await _internalFlaRepository.GetAssigneeHistoryForApplicationAsync(
            applicationId, cancellationToken);
        var assignedWo = assigneeHistory.SingleOrDefault(x =>
            x.Role == AssignedUserRole.WoodlandOfficer && x.TimestampUnassigned.HasValue == false);

        return assignedWo?.AssignedUserId == performingUserId;
    }

    private bool AssertPerformingUserIsAssignedWoodlandOfficer(FellingLicenceApplication application, Guid performingUserId)
    {
        var assigneeHistory = application.AssigneeHistories;
        var assignedAo = assigneeHistory.SingleOrDefault(x =>
            x.Role == AssignedUserRole.WoodlandOfficer && x.TimestampUnassigned.HasValue == false);

        return assignedAo?.AssignedUserId == performingUserId;
    }

    private async Task<bool> AssertApplicationIsInWoodlandOfficerReviewState(Guid applicationId, CancellationToken cancellationToken)
    {
        var statuses = await _internalFlaRepository.GetStatusHistoryForApplicationAsync(applicationId, cancellationToken);
        return statuses.MaxBy(x => x.Created)?.Status == FellingLicenceStatus.WoodlandOfficerReview;
    }

    private bool AssertApplicationIsInWoodlandOfficerReviewState(FellingLicenceApplication application)
    {
        var statuses = application.StatusHistories;
        return statuses.MaxBy(x => x.Created)?.Status == FellingLicenceStatus.WoodlandOfficerReview;
    }

    private bool AssertHasAssignedFieldManager(FellingLicenceApplication application)
    {
        var assigneeHistory = application.AssigneeHistories;
        return assigneeHistory.Any(x =>
            x.Role == AssignedUserRole.FieldManager&& x.TimestampUnassigned.HasValue == false);
    }

    private async Task<bool> AssertWoodlandOfficerReviewTasksComplete(Guid applicationId, Maybe<WoodlandOfficerReview> woodlandOfficerReview, CancellationToken cancellationToken)
    {
        if (woodlandOfficerReview.HasNoValue)
        {
            logger.LogError("Could not find Woodland Officer Review entity for application with id {ApplicationId}", applicationId);
            return false;
        }

        var publicRegister = await _internalFlaRepository.GetPublicRegisterAsync(applicationId, cancellationToken);
        if (publicRegister.HasNoValue)
        {
            logger.LogError("Could not find Public Register entity for application with id {ApplicationId}", applicationId);
            return false;
        }

        if (publicRegister.GetPublicRegisterStatus() != InternalReviewStepStatus.Completed)
        {
            logger.LogError("Public Register incomplete for application with id {ApplicationId}", applicationId);
            return false;
        }

        if (woodlandOfficerReview.GetSiteVisitStatus() != InternalReviewStepStatus.Completed)
        {
            logger.LogError("Site Visit incomplete for application with id {ApplicationId}", applicationId);
            return false;
        }

        if (woodlandOfficerReview.GetPw14ChecksStatus() != InternalReviewStepStatus.Completed)
        {
            logger.LogError("PW14 checks incomplete for application with id {ApplicationId}", applicationId);
            return false;
        }

        if (woodlandOfficerReview.Value.ConfirmedFellingAndRestockingComplete == false)
        {
            logger.LogError("Confirmed felling and restocking details incomplete for application with id {ApplicationId}", applicationId);
            return false;
        }

        if (woodlandOfficerReview.Value.LarchCheckComplete == false)
        {
            logger.LogError("Confirmed larch check details incomplete for application with id {ApplicationId}", applicationId);
            return false;
        }

        return true;
    }

    private bool AssertConditions(
        Guid applicationId,
        Maybe<WoodlandOfficerReview> woodlandOfficerReview)
    {
        if (woodlandOfficerReview.HasNoValue)
        {
            logger.LogError("Could not find Woodland Officer Review entity for application with id {ApplicationId}", applicationId);
            return false;
        }

        if (woodlandOfficerReview.Value.IsConditional.HasValue == false)
        {
            logger.LogError("Woodland officer review has not set if is conditional or not");
            return false;
        }

        if (woodlandOfficerReview.Value.IsConditional is true 
            && woodlandOfficerReview.Value.ConditionsToApplicantDate.HasValue == false)
        {
            logger.LogError("Woodland officer review indicates the application is conditional but the conditions have not been sent to the applicant");
            return false;
        }

        return true;
    }
}
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class UpdateAdminOfficerReviewService : IUpdateAdminOfficerReviewService
{
    private readonly IFellingLicenceApplicationInternalRepository _internalFlaRepository;
    private readonly ILogger<UpdateAdminOfficerReviewService> _logger;
    private readonly IClock _clock;

    public UpdateAdminOfficerReviewService(
        IFellingLicenceApplicationInternalRepository internalFlaRepository,
        ILogger<UpdateAdminOfficerReviewService> logger,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(internalFlaRepository);
        ArgumentNullException.ThrowIfNull(clock);

        _internalFlaRepository = internalFlaRepository;
        _logger = logger;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<CompleteAdminOfficerReviewNotificationsModel>> CompleteAdminOfficerReviewAsync(
        Guid applicationId,
        Guid performingUserId,
        DateTime completedDateTime,
        bool isAgencyApplication,
        bool requireWOReview,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to complete Admin Officer review for application with id {ApplicationId}", applicationId);

        var applicationMaybe = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);

        if (applicationMaybe.HasNoValue)
        {
            _logger.LogError("Unable to find an application with the id of {Id}", applicationId);
            return Result.Failure<CompleteAdminOfficerReviewNotificationsModel>("Unable to find an application with supplied id");
        }

        if (AssertApplicationIsInAdminOfficerReviewState(applicationMaybe.Value) == false)
        {
            _logger.LogError("Application with id {ApplicationId} is not in the correct state to complete the Admin Officer review", applicationId);
            return Result.Failure<CompleteAdminOfficerReviewNotificationsModel>("Unable to complete Admin Officer review for given application");
        }

        if (AssertPerformingUserIsAssignedAdminOfficer(applicationMaybe.Value, performingUserId) == false)
        {
            _logger.LogError("User with id {UserId} is not the assigned admin officer for application with id {ApplicationId} and so is unauthorised to complete the Admin Officer review", performingUserId, applicationId);
            return Result.Failure<CompleteAdminOfficerReviewNotificationsModel>("Unable to complete Admin Officer review for given application");
        }

        if (AssertAdminOfficerReviewCanBeCompleted(applicationMaybe.Value.AdminOfficerReview, isAgencyApplication) == false)
        {
            _logger.LogError("All admin officer review tasks must be completed to complete the review for application {ApplicationId}", applicationId);
            return Result.Failure<CompleteAdminOfficerReviewNotificationsModel>("Unable to complete Admin Officer review for given application");
        }

        if (AssertHasAssignedWoodlandOfficer(applicationMaybe.Value, requireWOReview) == false)
        {
            _logger.LogError("Application with id {ApplicationId} does not have an assigned woodland officer, unable to complete the Admin Officer review", applicationId);
            return Result.Failure<CompleteAdminOfficerReviewNotificationsModel>("Unable to complete Admin Officer review for given application");
        }

        Guid woodlandOfficerId = Guid.Empty;
        foreach (var assignee in applicationMaybe.Value.AssigneeHistories)
        {
            if (requireWOReview)
            {
                if (assignee.Role == AssignedUserRole.WoodlandOfficer && !assignee.TimestampUnassigned.HasValue)
                {
                    woodlandOfficerId = assignee.AssignedUserId;
                    break;
                }
            }
            else
            {
                if (assignee.Role == AssignedUserRole.FieldManager && !assignee.TimestampUnassigned.HasValue)
                {
                    woodlandOfficerId = assignee.AssignedUserId;
                    break;
                }
            }
        }
        if (woodlandOfficerId == Guid.Empty)
        {
            _logger.LogError("Application with id {ApplicationId} does not have an assigned woodland officer or approver, unable to complete the Admin Officer review", applicationId);
            return Result.Failure<CompleteAdminOfficerReviewNotificationsModel>("Unable to complete Admin Officer review for given application");
        }

        var applicantId = applicationMaybe.Value.CreatedById;

        var result = new CompleteAdminOfficerReviewNotificationsModel(applicationMaybe.Value.ApplicationReference, applicantId, woodlandOfficerId, applicationMaybe.Value.AdministrativeRegion);

        applicationMaybe.Value.StatusHistories.Add(new StatusHistory
        {
            Created = completedDateTime,
            FellingLicenceApplicationId = applicationId,
            FellingLicenceApplication = applicationMaybe.Value,
            Status = requireWOReview ? FellingLicenceStatus.WoodlandOfficerReview : FellingLicenceStatus.SentForApproval
        });

        var saveResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            _logger.LogError("Could not update application for completed admin officer review stage, error {Error}", saveResult.Error);
            return Result.Failure<CompleteAdminOfficerReviewNotificationsModel>("Unable to complete Admin Officer review for given application");
        }

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> SetAgentAuthorityCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken) =>
        await SetCompletionFlagAsync(
            applicationId,
            isAgencyApplication,
            performingUserId,
            complete,
            AdminOfficerReviewSections.AgentAuthorityForm,
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result> SetMappingCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken) =>
        await SetCompletionFlagAsync(
            applicationId,
            isAgencyApplication,
            performingUserId,
            complete,
            AdminOfficerReviewSections.MappingCheck,
            cancellationToken);

    /// <inheritdoc />
    public async Task<Result> SetConstraintsCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken) =>
        await SetCompletionFlagAsync(
            applicationId,
            isAgencyApplication,
            performingUserId,
            complete,
            AdminOfficerReviewSections.ConstraintsCheck,
            cancellationToken);

    /// <inheritdoc />
    public async Task<Result> SetLarchCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken) =>
        await SetCompletionFlagAsync(
            applicationId,
            isAgencyApplication,
            performingUserId,
            complete,
            AdminOfficerReviewSections.LarchCheck,
            cancellationToken);

    /// <inheritdoc />
    public async Task<Result> SetCBWCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken) =>
        await SetCompletionFlagAsync(
            applicationId,
            isAgencyApplication,
            performingUserId,
            complete,
            AdminOfficerReviewSections.CBWCheck,
            cancellationToken);

    public async Task<Result> SetEiaCheckCompletionAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        CancellationToken cancellationToken) =>
        await SetCompletionFlagAsync(
            applicationId,
            isAgencyApplication,
            performingUserId,
            complete,
            AdminOfficerReviewSections.EiaCheck,
            cancellationToken);


    public async Task<Result> UpdateAgentAuthorityFormDetailsAsync(
        Guid applicationId,
        Guid performingUserId,
        bool isCheckPassed,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var (_, cannotBeAmended, adminOfficerReview, error) =
            await CheckOrCreateAdminOfficerReviewAsync(applicationId, performingUserId, cancellationToken);

        if (cannotBeAmended)
        {
            return Result.Failure(error);
        }

        adminOfficerReview.AgentAuthorityCheckPassed = isCheckPassed;
        adminOfficerReview.AgentAuthorityCheckFailureReason = isCheckPassed
            ? null
            : failureReason;
        adminOfficerReview.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();
        adminOfficerReview.LastUpdatedById = performingUserId;
        adminOfficerReview.AgentAuthorityFormChecked = true;

        var updateResult = await _internalFlaRepository.UnitOfWork
            .SaveEntitiesAsync(cancellationToken)
            .ConfigureAwait(false);

        return updateResult.IsFailure
            ? Result.Failure(updateResult.Error.ToString())
            : Result.Success();
    }

    public async Task<Result> UpdateMappingCheckDetailsAsync(
        Guid applicationId,
        Guid performingUserId,
        bool isCheckPassed,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var (_, cannotBeAmended, adminOfficerReview, error) =
            await CheckOrCreateAdminOfficerReviewAsync(applicationId, performingUserId, cancellationToken);

        if (cannotBeAmended)
        {
            return Result.Failure(error);
        }

        adminOfficerReview.MappingCheckPassed = isCheckPassed;
        adminOfficerReview.MappingCheckFailureReason = isCheckPassed
            ? null
            : failureReason;
        adminOfficerReview.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();
        adminOfficerReview.LastUpdatedById = performingUserId;
        adminOfficerReview.MappingChecked = true;

        var updateResult = await _internalFlaRepository.UnitOfWork
            .SaveEntitiesAsync(cancellationToken)
            .ConfigureAwait(false);

        return updateResult.IsFailure
            ? Result.Failure(updateResult.Error.ToString())
            : Result.Success();
    }

    /// <inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> AddEnvironmentalImpactAssessmentRequestHistoryAsync(
        EnvironmentalImpactAssessmentRequestHistoryRecord record,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding EIA request history record for application with id {ApplicationId}", record.ApplicationId);

        var result = await _internalFlaRepository.AddEnvironmentalImpactAssessmentRequestHistoryAsync(record, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Could not add EIA request history record for application with id {ApplicationId}, error: {Error}", record.ApplicationId, result.Error);
            return UnitResult.Failure(result.Error);
        }

        _logger.LogInformation("Successfully added EIA request history record for application with id {ApplicationId}", record.ApplicationId);
        return UnitResult.Success<UserDbErrorReason>();
    }

    public enum AdminOfficerReviewSections
    {
        AgentAuthorityForm,
        MappingCheck,
        ConstraintsCheck,
        LarchCheck,
        CBWCheck,
        EiaCheck
    }

    private async Task<Result> SetCompletionFlagAsync(
        Guid applicationId,
        bool isAgencyApplication,
        Guid performingUserId,
        bool complete,
        AdminOfficerReviewSections section,
        CancellationToken cancellationToken)
    {
        var (_, adminOfficerReviewCheckFailure, adminOfficerReview, error) = await CheckOrCreateAdminOfficerReviewAsync(
                applicationId,
                performingUserId,
                cancellationToken,
                section == AdminOfficerReviewSections.LarchCheck)
            .ConfigureAwait(false);

        if (adminOfficerReviewCheckFailure)
        {
            return Result.Failure(error);
        }

        if (section is AdminOfficerReviewSections.ConstraintsCheck)
        {
            if (adminOfficerReview.MappingCheckPassed is null or false)
            {
                _logger.LogWarning("The application mapping checks must be passed before constraints can be checked, " +
                                   "MappingCheckPassed is `{mappingCheckPassed}`", adminOfficerReview.MappingCheckPassed);

                return Result.Failure("Mapping checks must be passed before constraints can be checked");
            }

            if (isAgencyApplication && adminOfficerReview.AgentAuthorityCheckPassed is null or false)
            {
                _logger.LogWarning("For an agency application the Agent Authority checks must be passed before constraints can be checked, " +
                                   "AgentAuthorityCheckPassed is `{agentAuthorityCheckPassed}`", adminOfficerReview.AgentAuthorityCheckPassed);

                return Result.Failure("Agent Authority checks must be passed before constraints can be checked");
            }
        }

        switch (section)
        {
            case AdminOfficerReviewSections.AgentAuthorityForm:
                adminOfficerReview.AgentAuthorityFormChecked = complete;
                break;
            case AdminOfficerReviewSections.MappingCheck:
                adminOfficerReview.MappingChecked = complete;
                break;
            case AdminOfficerReviewSections.ConstraintsCheck:
                adminOfficerReview.ConstraintsChecked = complete;
                break;
            case AdminOfficerReviewSections.LarchCheck:
                adminOfficerReview.LarchChecked = complete;
                break;
            case AdminOfficerReviewSections.CBWCheck:
                adminOfficerReview.CBWChecked = complete;
                break;
            case AdminOfficerReviewSections.EiaCheck:
                adminOfficerReview.EiaChecked = complete;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(section), section, null);
        }

        adminOfficerReview.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();
        adminOfficerReview.LastUpdatedById = performingUserId;

        var updateResult = await _internalFlaRepository.UnitOfWork
            .SaveEntitiesAsync(cancellationToken)
            .ConfigureAwait(false);

        return updateResult.IsSuccess
            ? Result.Success()
            : Result.Failure("Unable to update admin officer review entity");
    }

    private async Task<Result<AdminOfficerReview>> CheckOrCreateAdminOfficerReviewAsync(
        Guid applicationId,
        Guid performingUserId,
        CancellationToken cancellationToken,
        bool ignoreAOcheck = false)
    {
        var applicationMaybe =
        await _internalFlaRepository.GetAsync(applicationId, cancellationToken);

        if (applicationMaybe.HasNoValue)
        {
            _logger.LogError("Unable to find an application with the id of {Id}", applicationId);
            return Result.Failure<AdminOfficerReview>("Unable to find an application with supplied id");
        }

        var checkResult = CheckAdminOfficerReviewCanBeAmended(applicationMaybe.Value, performingUserId);

        if (checkResult.IsFailure && !ignoreAOcheck)
        {
            return checkResult.ConvertFailure<AdminOfficerReview>();
        }

        applicationMaybe.Value.AdminOfficerReview ??= new AdminOfficerReview();

        return applicationMaybe.Value.AdminOfficerReview;
    }

    private Result CheckAdminOfficerReviewCanBeAmended(FellingLicenceApplication fellingLicenceApplication, Guid performingUserId)
    {
        if (AssertApplicationIsInAdminOfficerReviewState(fellingLicenceApplication) is false)
        {
            _logger.LogError("Cannot update admin officer review for application not in submitted state, for application {id}", fellingLicenceApplication.Id);
            return Result.Failure("Cannot update admin officer review for application not in submitted state");
        }

        if (AssertPerformingUserIsAssignedAdminOfficer(fellingLicenceApplication, performingUserId) is false)
        {
            _logger.LogError("User with id {UserId} is not the assigned admin officer for application with id {ApplicationId} and so is unauthorised to update admin officer review statuses", performingUserId, fellingLicenceApplication.Id);
            return Result.Failure("User is not the assigned admin officer");
        }

        return Result.Success();
    }

    private bool AssertAdminOfficerReviewCanBeCompleted(AdminOfficerReview? adminOfficerReview, bool isAgencyApplication)
    {
        return adminOfficerReview is not null &&
               (adminOfficerReview.AgentAuthorityFormChecked is true || isAgencyApplication is false) &&
               adminOfficerReview is
               {
                   MappingChecked: true,
                   ConstraintsChecked: true
               };
    }

    private bool AssertApplicationIsInAdminOfficerReviewState(FellingLicenceApplication application)
    {
        var statuses = application.StatusHistories;
        return statuses.MaxBy(x => x.Created)?.Status == FellingLicenceStatus.AdminOfficerReview &&
               application.AdminOfficerReview?.AdminOfficerReviewComplete is not true;
    }

    private bool AssertPerformingUserIsAssignedAdminOfficer(FellingLicenceApplication application, Guid performingUserId)
    {
        var assigneeHistory = application.AssigneeHistories;
        var assignedAo = assigneeHistory.SingleOrDefault(x =>
            x.Role == AssignedUserRole.AdminOfficer && x.TimestampUnassigned.HasValue == false);

        return assignedAo?.AssignedUserId == performingUserId;
    }

    private bool AssertHasAssignedWoodlandOfficer(FellingLicenceApplication application, bool requireWOReview)
    {
        if (requireWOReview)
        {
            return application.AssigneeHistories.Any(x =>
                (x.Role == AssignedUserRole.WoodlandOfficer) && x.TimestampUnassigned.HasValue == false);
        }
        else
        {
            return application.AssigneeHistories.Any(x =>
                (x.Role == AssignedUserRole.FieldManager) && x.TimestampUnassigned.HasValue == false);
        }

    }
}
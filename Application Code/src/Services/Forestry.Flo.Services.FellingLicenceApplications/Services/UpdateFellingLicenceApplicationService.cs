using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Service class for retrieving <see cref="FellingLicenceApplication"/>. 
/// </summary>
public class UpdateFellingLicenceApplicationService : IUpdateFellingLicenceApplication
{
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly IAmendCaseNotes _caseNotesService;
    private readonly IGetConfiguredFcAreas _configuredFcAreas;
    private readonly IClock _clock;
    private readonly ILogger<UpdateFellingLicenceApplicationService> _logger;
    private readonly IOptions<FellingLicenceApplicationOptions> _options;
    private readonly List<FellingLicenceStatus> _dateReceivedUpdatableStatuses = new List<FellingLicenceStatus>
    {
        FellingLicenceStatus.Received,
        FellingLicenceStatus.Draft,
        FellingLicenceStatus.Submitted,
        FellingLicenceStatus.AdminOfficerReview,
        FellingLicenceStatus.WithApplicant,
        FellingLicenceStatus.ReturnedToApplicant
    };
    
    public UpdateFellingLicenceApplicationService(
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IAmendCaseNotes caseNotesService,
        IGetConfiguredFcAreas configuredFcAreas,
        IClock clock,
        ILogger<UpdateFellingLicenceApplicationService> logger,
        IOptions<FellingLicenceApplicationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fellingLicenceApplicationInternalRepository);
        ArgumentNullException.ThrowIfNull(caseNotesService);
        ArgumentNullException.ThrowIfNull(configuredFcAreas);
        ArgumentNullException.ThrowIfNull(clock);

        _options = options;
        _configuredFcAreas = configuredFcAreas;
        _fellingLicenceApplicationInternalRepository = fellingLicenceApplicationInternalRepository;
        _caseNotesService = caseNotesService;
        _clock = clock;
        _logger = logger ?? new NullLogger<UpdateFellingLicenceApplicationService>();
    }
    
    /// <inheritdoc />>
    public async Task AddStatusHistoryAsync(
        Guid userId,
        Guid applicationId,
        FellingLicenceStatus newStatus,
        CancellationToken cancellationToken)
    {
        await _fellingLicenceApplicationInternalRepository.AddStatusHistory(
            userId,
            applicationId,
            newStatus,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateDateReceivedAsync(
        Guid applicationId,
        DateTime dateReceived,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request to update the Date Received field for application with id {ApplicationId}", applicationId);

        var (applicationRetrieved, fellingLicenceApplication) = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);

        if (applicationRetrieved is false)
        {
            _logger.LogWarning("Could not find application in repository with id {ApplicationId}", applicationId);
            return Result.Failure("Unable to retrieve felling licence application");
        }

        var currentApplicationState = 
            fellingLicenceApplication.StatusHistories.MaxBy(y => y.Created)?.Status ?? FellingLicenceStatus.Draft;

        if (_dateReceivedUpdatableStatuses.Any(x => x == currentApplicationState) == false)
        {
            _logger.LogWarning("Application with id {ApplicationId} is in state {ApplicationState} so DateReceived cannot be updated", applicationId, currentApplicationState);
            return Result.Failure($"Date received cannot be updated for applications in state {currentApplicationState}");
        }

        if (fellingLicenceApplication is
            {
                CitizensCharterDate: not null,
                DateReceived: not null
            } && fellingLicenceApplication.DateReceived.Value.Equals(dateReceived))
        {
            return Result.Success();
        }

        fellingLicenceApplication.DateReceived = dateReceived;

        var citizenCharterDate = dateReceived.Add(_options.Value.CitizensCharterDateLength);

        fellingLicenceApplication.CitizensCharterDate = citizenCharterDate;

        var saveResult = await _fellingLicenceApplicationInternalRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return saveResult.IsSuccess
            ? Result.Success()
            : Result.Failure(saveResult.Error.GetDescription());
    }

    /// <inheritdoc />
    public async Task<Result<List<Guid>>> ReturnToApplicantAsync(ReturnToApplicantRequest request, CancellationToken cancellationToken)
    {
        // check that the applicant we're returning the application to is allowed to see it
        var checkAccessResult = await _fellingLicenceApplicationInternalRepository
            .CheckUserCanAccessApplicationAsync(request.ApplicationId, request.ApplicantToReturnTo, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure)
        {
            return Result.Failure<List<Guid>>("Could not retrieve the application specified in the request");
        }

        if (checkAccessResult.Value is false)
        {
            return Result.Failure<List<Guid>>("Given applicant to return the application to does not have access to the application");
        }

        // add the new returned to applicant or with applicant status
        var statuses = await _fellingLicenceApplicationInternalRepository
            .GetStatusHistoryForApplicationAsync(request.ApplicationId, cancellationToken)
            .ConfigureAwait(false);
        var currentStatus = statuses.MaxBy(x => x.Created)?.Status;
        if (currentStatus is FellingLicenceStatus.Draft or FellingLicenceStatus.ReturnedToApplicant
            or FellingLicenceStatus.WithApplicant or FellingLicenceStatus.Withdrawn)
        {
            return Result.Failure<List<Guid>>($"Application is currently in state {currentStatus} and so cannot be returned to applicant");
        }

        var assigneeHistories = await _fellingLicenceApplicationInternalRepository
            .GetAssigneeHistoryForApplicationAsync(request.ApplicationId, cancellationToken)
            .ConfigureAwait(false);

        // double-check the user can return the application to the applicant
        if (!request.PerformingUserIsAccountAdmin)
        {
            if (!CheckCanReturnToApplicant(request.PerformingUserId, currentStatus, assigneeHistories))
            {
                return Result.Failure<List<Guid>>("User cannot return the application to the applicant as they are not assigned to it");
            }
        }

        var newStatus = currentStatus switch
        {
            FellingLicenceStatus.Submitted => FellingLicenceStatus.ReturnedToApplicant,
            FellingLicenceStatus.AdminOfficerReview => FellingLicenceStatus.ReturnedToApplicant,
            _ => FellingLicenceStatus.WithApplicant
        };
        await _fellingLicenceApplicationInternalRepository.AddStatusHistory(
            request.PerformingUserId, request.ApplicationId, newStatus, cancellationToken).ConfigureAwait(false);

        // determine the case note type so the note will appear in the relevant activity feed
        var caseNoteType = currentStatus switch
        {
            FellingLicenceStatus.AdminOfficerReview => CaseNoteType.AdminOfficerReviewComment,
            FellingLicenceStatus.WoodlandOfficerReview => CaseNoteType.WoodlandOfficerReviewComment,
            _ => CaseNoteType.ReturnToApplicantComment
        };

        // add returned to applicant case note
        if (!string.IsNullOrWhiteSpace(request.CaseNoteContent))
        {
            var caseNote = new AddCaseNoteRecord(
                request.ApplicationId,
                caseNoteType,
                request.CaseNoteContent,
                true,
                false);

            var addCaseNoteResult = await _caseNotesService
                .AddCaseNoteAsync(caseNote, request.PerformingUserId, cancellationToken)
                .ConfigureAwait(false);

            if (addCaseNoteResult.IsFailure)
            {
                return addCaseNoteResult.ConvertFailure<List<Guid>>();
            }
        }

        // update the step statuses on the application to indicate what needs attention
        var updateStepStatusesResult = await _fellingLicenceApplicationInternalRepository
            .UpdateApplicationStepStatusAsync(request.ApplicationId, request.SectionsRequiringAttention, cancellationToken)
            .ConfigureAwait(false);
        if (updateStepStatusesResult.IsFailure)
        {
            return Result.Failure<List<Guid>>("Could not update application sections requiring attention");
        }

        var timestamp = _clock.GetCurrentInstant().ToDateTimeUtc();

        // assign the application back to the applicant
        await _fellingLicenceApplicationInternalRepository.AssignFellingLicenceApplicationToStaffMemberAsync(
            request.ApplicationId,
            request.ApplicantToReturnTo.UserAccountId,
            AssignedUserRole.Applicant,
            timestamp,
            cancellationToken).ConfigureAwait(false);


        // now get the assigned staff that need to be sent a notification
        var staffMemberIds = new List<Guid>();

        foreach (var assigneeHistory in assigneeHistories.Where(x => 
                     x.TimestampUnassigned.HasNoValue()
                     && x.Role != AssignedUserRole.Applicant
                     && x.Role != AssignedUserRole.Author))
        {
            staffMemberIds.Add(assigneeHistory.AssignedUserId);
        }

        // if returned from submitted/ao review, unassign any assigned AO or WO
        if (newStatus == FellingLicenceStatus.ReturnedToApplicant)
        {
            await _fellingLicenceApplicationInternalRepository.RemoveAssignedRolesFromApplicationAsync(
                    request.ApplicationId, new[] { AssignedUserRole.AdminOfficer, AssignedUserRole.WoodlandOfficer }, timestamp, cancellationToken)
                .ConfigureAwait(false);
        }

        return Result.Success(staffMemberIds);
    }

    /// <inheritdoc />
    public async Task<Result<AssignToUserResponse>> AssignToInternalUserAsync(AssignToUserRequest request, CancellationToken cancellationToken)
    {
        var originalReference = await _fellingLicenceApplicationInternalRepository
            .GetApplicationReferenceAsync(request.ApplicationId, cancellationToken)
            .ConfigureAwait(false);

        if (originalReference.IsFailure)
        {
            _logger.LogError("Could not retrieve application reference for application with id {ApplicationId}", request.ApplicationId);
            return originalReference.ConvertFailure<AssignToUserResponse>();
        }

        // check if we need to update the state of the application to Operations Admin Officer Review
        if (request.AssignedRole == AssignedUserRole.AdminOfficer)
        {
            // get the current status of the application - if Submitted then add new status code
            var statuses = await _fellingLicenceApplicationInternalRepository
                .GetStatusHistoryForApplicationAsync(request.ApplicationId, cancellationToken)
                .ConfigureAwait(false);
            var currentStatus = statuses.MaxBy(x => x.Created)?.Status;

            if (currentStatus is FellingLicenceStatus.Submitted)
            {
                await _fellingLicenceApplicationInternalRepository.AddStatusHistory(
                        request.PerformingUserId, request.ApplicationId, FellingLicenceStatus.AdminOfficerReview, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // check if the area code needs to be updated
        if (request.AssignedRole is AssignedUserRole.AdminOfficer or AssignedUserRole.WoodlandOfficer)
        {
            var adminHubName = await GetAdministrativeRegionForAreaCostCodeAsync(request.FcAreaCostCode, cancellationToken);

            var updateAreaCodeResult = await _fellingLicenceApplicationInternalRepository
                .UpdateAreaCodeAsync(request.ApplicationId, request.FcAreaCostCode, adminHubName, cancellationToken)
                .ConfigureAwait(false);

            if (updateAreaCodeResult.IsFailure)
            {
                _logger.LogError("Could not update Area Code for application being assigned to Admin Officer/Woodland Officer role");
                return Result.Failure<AssignToUserResponse>("Could not assign user to application");
            }
        }

        // add returned to applicant case note
        if (!string.IsNullOrWhiteSpace(request.CaseNoteContent))
        {
            var caseNote = new AddCaseNoteRecord(
                request.ApplicationId,
                CaseNoteType.CaseNote,
                request.CaseNoteContent,
                request.VisibleToApplicant,
                request.VisibleToConsultee);

            var addCaseNoteResult = await _caseNotesService
                .AddCaseNoteAsync(caseNote, request.PerformingUserId, cancellationToken)
                .ConfigureAwait(false);

            if (addCaseNoteResult.IsFailure)
            {
                _logger.LogError("Could not add case note for comment on assigning user");
                return addCaseNoteResult.ConvertFailure<AssignToUserResponse>();
            }
        }

        var timestamp = _clock.GetCurrentInstant().ToDateTimeUtc();

        // now assign the new role to the application
        var assignResult = await _fellingLicenceApplicationInternalRepository.AssignFellingLicenceApplicationToStaffMemberAsync(
            request.ApplicationId,
            request.AssignToUserId,
            request.AssignedRole,
            timestamp,
            cancellationToken)
            .ConfigureAwait(false);

        var updatedReference = await _fellingLicenceApplicationInternalRepository
            .GetApplicationReferenceAsync(request.ApplicationId, cancellationToken)
            .ConfigureAwait(false);

        if (updatedReference.IsFailure)
        {
            _logger.LogError("Could not retrieve application reference for application with id {ApplicationId}", request.ApplicationId);
            return updatedReference.ConvertFailure<AssignToUserResponse>();
        }

        return Result.Success(new AssignToUserResponse(
            updatedReference.Value, originalReference.Value, assignResult.UserAlreadyAssigned, assignResult.UserUnassigned));
    }

    /// <inheritdoc />
    public async Task<Result> AddDecisionPublicRegisterDetailsAsync(
        Guid applicationId, 
        DateTime publishedDateTime, 
        DateTime expiryDateTime,
        CancellationToken cancellationToken)
    {
        var statuses = await _fellingLicenceApplicationInternalRepository
            .GetStatusHistoryForApplicationAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        if (statuses.Count == 0)
        {
            _logger.LogWarning("The application having {Id} with its statuses could not be found", applicationId);
            return Result.Failure("Application with its statuses could not be found");
        }

        var currentStatus = statuses.MaxBy(x => x.Created)?.Status;

        if (currentStatus != FellingLicenceStatus.Approved 
            && currentStatus != FellingLicenceStatus.ReferredToLocalAuthority 
            && currentStatus != FellingLicenceStatus.Refused)
        {
            _logger.LogWarning("The application having {Id} does not have the correct current status for this operation, " +
                               "it is {CurrentStatus}", applicationId, currentStatus);

            return Result.Failure("Incorrect application current state found");
        }

        var result = await _fellingLicenceApplicationInternalRepository.AddDecisionPublicRegisterDetailsAsync(applicationId,
            publishedDateTime,
            expiryDateTime,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Could not successfully update the decision public register details for the application " +
                               "having {Id}, error was {Error}", applicationId, result.Error);

            return Result.Failure("Could not add the decision public register detail to the application on the local system.");
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> SetRemovalDateOnDecisionPublicRegisterEntryAsync(
        Guid applicationId,
        DateTime removedDateTime,
        CancellationToken cancellationToken)
    {
        var statuses = await _fellingLicenceApplicationInternalRepository
            .GetStatusHistoryForApplicationAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        if (statuses.Count == 0)
        {
            _logger.LogWarning("The application having {Id} with its statuses could not be found", applicationId);
            return Result.Failure("Application with its statuses could not be found");
        }

        var currentStatus = statuses.MaxBy(x => x.Created)?.Status;

        if (currentStatus is not FellingLicenceStatus.Approved and not FellingLicenceStatus.Refused 
            and not FellingLicenceStatus.ReferredToLocalAuthority){
            _logger.LogWarning("The application having {Id} does not have one of the correct statuses required to have it set to expired, " +
                               "the current status is {CurrentStatus}", applicationId, currentStatus);

            return Result.Failure("Incorrect current application status found");
        }

        var result = await _fellingLicenceApplicationInternalRepository.ExpireDecisionPublicRegisterEntryAsync(applicationId, removedDateTime, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Could not successfully add the date of removal from the decision public register for the application " +
                               "having {Id}, error was {Error}", applicationId, result.Error);

            return Result.Failure("Could not set the date of removal for the application's decision public register entry on the local system");
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> SetRemovalDateOnConsultationPublicRegisterEntryAsync(Guid applicationId, DateTime removedDateTime,
        CancellationToken cancellationToken)
    {
        var result = await _fellingLicenceApplicationInternalRepository.ExpireConsultationPublicRegisterEntryAsync(applicationId, removedDateTime, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Could not successfully add the date of removal from the consultation public register for the application " +
                             "having {Id}, error was {Error}", applicationId, result.Error);

            return Result.Failure("Could not set the date of removal for the application's consultation public register entry on the local system");
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateFinalActionDateAsync(
        Guid applicationId,
        DateTime finalActionDate,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request to update the Final Action Date field for application with id {ApplicationId}", applicationId);

        var (applicationRetrieved, fellingLicenceApplication) = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);

        if (applicationRetrieved is false)
        {
            _logger.LogWarning("Could not find application in repository with id {ApplicationId}", applicationId);
            return Result.Failure("Unable to retrieve felling licence application");
        }

        if (fellingLicenceApplication.FinalActionDate.HasValue && fellingLicenceApplication.FinalActionDate.Value.Equals(finalActionDate))
        {
            return Result.Success();
        }

        fellingLicenceApplication.FinalActionDate = finalActionDate;

        var saveResult = await _fellingLicenceApplicationInternalRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return saveResult.IsSuccess
            ? Result.Success()
            : Result.Failure(saveResult.Error.GetDescription());
    }

    /// <inheritdoc />
    public async Task<Result> TryRevertApplicationFromWithdrawnAsync(
        Guid performingUserId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var (fellingLicenceFound, fellingLicence) = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (fellingLicenceFound is false)
        {
            _logger.LogWarning("Could not find application in repository with id {ApplicationId}", applicationId);
            return Result.Failure("Unable to retrieve felling licence application");
        }

        var currentStatus = fellingLicence.GetCurrentStatus();
        if (currentStatus is not FellingLicenceStatus.Withdrawn)
        {
            _logger.LogWarning("Application with id {ApplicationId} is in state {CurrentStatus} so cannot be reverted from withdrawn", applicationId, currentStatus);
            return Result.Failure($"Application is currently in state {currentStatus} and so cannot be reverted from withdrawn");
        }

        var (hasPreviousStatus, previousStatus) = fellingLicence.GetNthStatus(1);
        if (hasPreviousStatus is false)
        {
            _logger.LogWarning("Application with id {ApplicationId} does not have a previous status to revert to", applicationId);
            return Result.Failure("Application does not have a previous status to revert to");
        }

        if (previousStatus is FellingLicenceStatus.ReturnedToApplicant)
        {
            // reset the admin officer tasks
            fellingLicence.AdminOfficerReview = new AdminOfficerReview
            {
                LastUpdatedById = performingUserId,
                LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc()
            };
        }

        // revert the application to the state it was in before it was withdrawn
        await AddStatusHistoryAsync(
            performingUserId, 
            applicationId, 
            previousStatus, 
            cancellationToken);

        _logger.LogDebug("Reverting application with id {ApplicationId} from withdrawn to previous status of {PreviousStatus}", applicationId, previousStatus);

        var saveResult = await _fellingLicenceApplicationInternalRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return saveResult.IsSuccess
            ? Result.Success()
            : Result.Failure(saveResult.Error.GetDescription());
    }

    public async Task<Result> SetApplicationApproverAsync(Guid applicationId, Guid? approverId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request to set application approver for application with id {ApplicationId}", applicationId);

        var updateResult = await _fellingLicenceApplicationInternalRepository
            .SetApplicationApproverAsync(applicationId, approverId, cancellationToken);

        if (updateResult.IsFailure)
        {
            _logger.LogError("Could not set application approver for application with id {ApplicationId}, error was {Error}", applicationId, updateResult.Error);
            return Result.Failure("Could not update application approver id");
        }

        await _fellingLicenceApplicationInternalRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateEnvironmentalImpactAssessmentAsync(
        Guid applicationId,
        EnvironmentalImpactAssessmentRecord eiaRecord,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request to update the Environmental Impact Assessment as an applicant for application with id {ApplicationId}", applicationId);

        var result =
            await _fellingLicenceApplicationInternalRepository.UpdateEnvironmentalImpactAssessmentAsync(
                applicationId,
                eiaRecord,
                cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Could not update EEnvironmental Impact Assessment as an applicant for application with id {ApplicationId}, error was {Error}", applicationId, result.Error);
            return Result.Failure("Could not update Environmental Impact Assessment");
        }

        _logger.LogInformation("Updated Environmental Impact Assessment as an applicant for application with id {ApplicationId}", applicationId);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
        Guid applicationId,
        EnvironmentalImpactAssessmentAdminOfficerRecord eiaRecord,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request to update the Environmental Impact Assessment as an admin officer for application with id {ApplicationId}", applicationId);

        var result =
            await _fellingLicenceApplicationInternalRepository.UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
                applicationId,
                eiaRecord,
                cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Could not update Environmental Impact Assessment status for application with id {ApplicationId}, error was {Error}", applicationId, result.Error);
            return Result.Failure("Could not update Environmental Impact Assessment as an admin officer");
        }

        _logger.LogInformation("Updated Environmental Impact Assessment as an admin officer for application with id {ApplicationId}", applicationId);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        await _fellingLicenceApplicationInternalRepository.BeginTransactionAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Result> UpdateEnvironmentalImpactAssessmentStatusAsync(
        Guid applicationId,
        bool status,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request to update the Environmental Impact Assessment status for application with id {ApplicationId}", applicationId);

        var result = await _fellingLicenceApplicationInternalRepository.UpdateApplicationStepStatusAsync(
            applicationId,
            new ApplicationStepStatusRecord
            {
                EnvironmentalImpactAssessmentComplete = status
            },
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Could not update Environmental Impact Assessment status for application with id {ApplicationId}, error was {Error}", applicationId, result.Error);
            return Result.Failure("Could not update Environmental Impact Assessment status");
        }

        _logger.LogInformation("Updated Environmental Impact Assessment status for application with id {ApplicationId} to {Status}", applicationId, status);
        return Result.Success();
    }

    private static bool CheckCanReturnToApplicant(
        Guid performingUserId, 
        FellingLicenceStatus? currentStatus, 
        IList<AssigneeHistory> assigneeHistories)
    {
        var assignedAo = assigneeHistories.FirstOrDefault(x =>
            x.Role == AssignedUserRole.AdminOfficer && x.TimestampUnassigned.HasNoValue());

        if (currentStatus is FellingLicenceStatus.AdminOfficerReview)  // AO review = only assigned AO can return to applicant
        {
            if (assignedAo?.AssignedUserId != performingUserId)
            {
                return false;
            }
        }

        var assignedWo = assigneeHistories.FirstOrDefault(x =>
            x.Role == AssignedUserRole.WoodlandOfficer && x.TimestampUnassigned.HasNoValue());

        if (currentStatus is FellingLicenceStatus.WoodlandOfficerReview)  // WO review = only assigned AO or WO can return to applicant
        {
            if (assignedAo?.AssignedUserId != performingUserId
                && assignedWo?.AssignedUserId != performingUserId)
            {
                return false;
            }
        }

        var assignedApprover = assigneeHistories.FirstOrDefault(x =>
            x.Role == AssignedUserRole.FieldManager && x.TimestampUnassigned.HasNoValue());

        if (currentStatus is FellingLicenceStatus.SentForApproval)
        {
            if (assignedAo?.AssignedUserId != performingUserId
                && assignedWo?.AssignedUserId != performingUserId
                && assignedApprover?.AssignedUserId != performingUserId)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<string?> GetAdministrativeRegionForAreaCostCodeAsync(string areaCostCode, CancellationToken cancellationToken)
    {
        var (isSuccess, _, value, error) = await _configuredFcAreas.GetAllAsync(cancellationToken);

        if (isSuccess)
        {
            var adminHubName = value.FirstOrDefault(x => x.AreaCostCode == areaCostCode)?.AdminHubName;
            if (!string.IsNullOrEmpty(adminHubName))
            {
                return adminHubName;
            }
            _logger.LogWarning("Configured FC Areas were retrieved, but no Administrative region name was found for the provided area cost code {AreaCostCode}", areaCostCode);
        }
        else
        {
            _logger.LogWarning("Unable to get Administrative region name for the provided area cost code of {AreaCostCode}, error was {Error}", areaCostCode, error);
        }

        return null;
    }
}
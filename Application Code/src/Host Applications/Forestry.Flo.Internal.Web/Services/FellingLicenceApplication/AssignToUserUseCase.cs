using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using AssignedUserRole = Forestry.Flo.Services.FellingLicenceApplications.Entities.AssignedUserRole;
namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

public class AssignToUserUseCase : FellingLicenceApplicationUseCaseBase
{
    private readonly IAuditService<AssignToUserUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ISendNotifications _notificationsService;
    private readonly IGetFellingLicenceApplicationForInternalUsers _getFellingLicenceApplicationService;
    private readonly IUpdateFellingLicenceApplication _updateFellingLicenceApplicationService;
    private readonly ILogger<AssignToUserUseCase> _logger;

    public AssignToUserUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IAuditService<AssignToUserUseCase> auditService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        RequestContext requestContext,
        ISendNotifications notificationsService,
        IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplicationService,
        IUpdateFellingLicenceApplication updateFellingLicenceApplicationService,
        IAgentAuthorityService agentAuthorityService,
        ILogger<AssignToUserUseCase> logger)
        : base(internalUserAccountService, 
            externalUserAccountService,
            fellingLicenceApplicationInternalRepository, 
            woodlandOwnerService, 
            agentAuthorityService,
            getConfiguredFcAreasService)
    {
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _notificationsService = Guard.Against.Null(notificationsService);
        _getFellingLicenceApplicationService = Guard.Against.Null(getFellingLicenceApplicationService);
        _updateFellingLicenceApplicationService = Guard.Against.Null(updateFellingLicenceApplicationService);
        _logger = logger;
    }

    public async Task<Result<ConfirmReassignApplicationModel>> ConfirmReassignApplicationForRole(
        Guid applicationId,
        AssignedUserRole selectedRole,
        string returnUrl,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        var fellingLicenceApplication = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);
        if (fellingLicenceApplication.IsFailure)
        {
            _logger.LogError(fellingLicenceApplication.Error);
            await AuditErrorAsync(user, applicationId, null, selectedRole, fellingLicenceApplication.Error, cancellationToken);
            return fellingLicenceApplication.ConvertFailure<ConfirmReassignApplicationModel>();
        }

        var model = new ConfirmReassignApplicationModel
        {
            FellingLicenceApplicationSummary = fellingLicenceApplication.Value,
            SelectedRole = selectedRole,
            ReturnUrl = returnUrl
        };
        return Result.Success(model);
    }

    public async Task<Result<AssignToUserModel>> RetrieveDetailsToAssignFlaToUserAsync(
        Guid applicationId,
        AssignedUserRole selectedRole,
        string returnUrl,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        var users = await GetConfirmedUserAccountsAsync(cancellationToken);
        if (users.IsFailure)
        {
            _logger.LogError(users.Error);
            await AuditErrorAsync(user, applicationId, null, selectedRole, users.Error, cancellationToken);
            return users.ConvertFailure<AssignToUserModel>();
        }

        var fellingLicenceApplication = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);
        if (fellingLicenceApplication.IsFailure)
        {
            _logger.LogError(fellingLicenceApplication.Error);
            await AuditErrorAsync(user, applicationId, null, selectedRole, fellingLicenceApplication.Error, cancellationToken);
            return fellingLicenceApplication.ConvertFailure<AssignToUserModel>();
        }

        var currentStatus = fellingLicenceApplication.Value.StatusHistories.MaxBy(x => x.Created)!.Status;
        if (ApplicationCompletedStatuses.Contains(currentStatus))
        {
            var errorStatuses = $"The application with Id {applicationId} is {currentStatus.GetDisplayNameByActorType(ActorType.InternalUser)} and a user cannot be assigned to it.";
            _logger.LogError(errorStatuses);
            await AuditErrorAsync(user, applicationId, null, null, errorStatuses, cancellationToken);
            return Result.Failure<AssignToUserModel>($"Cannot assign to an application that has been {currentStatus.GetDisplayNameByActorType(ActorType.InternalUser)}.");
        }

        var resultUsers = await GetUsersthatCanApprove(
            selectedRole,
            fellingLicenceApplication.Value.Id,
            cancellationToken,
            users.Value);

        if (resultUsers.IsFailure)
        {
            _logger.LogError(resultUsers.Error);
            await AuditErrorAsync(user, applicationId, null, selectedRole, resultUsers.Error, cancellationToken);
            return resultUsers.ConvertFailure<AssignToUserModel>();
        }

        var getConfiguredFcAreasResult = await GetConfiguredFcAreasService.GetAllAsync(cancellationToken);

        if (getConfiguredFcAreasResult.IsFailure)
        {
            _logger.LogError(getConfiguredFcAreasResult.Error);
            await AuditErrorAsync(user, applicationId, null, null, getConfiguredFcAreasResult.Error, cancellationToken);
            return getConfiguredFcAreasResult.ConvertFailure<AssignToUserModel>();
        }

        var model = new AssignToUserModel
        {
            FellingLicenceApplicationSummary = fellingLicenceApplication.Value,
            UserAccounts = resultUsers.Value,
            SelectedRole = selectedRole,
            ReturnUrl = returnUrl,
            HiddenAccounts = resultUsers.Value.Count() != users.Value.Count(),
            SelectedUserId = selectedRole == AssignedUserRole.AdminOfficer ? user.UserAccountId : null,
            ConfiguredFcAreas = getConfiguredFcAreasResult.Value,
            CurrentFcAreaCostCode = fellingLicenceApplication.Value.AreaCode,
            AdministrativeRegion = fellingLicenceApplication.Value.AdministrativeRegion
        };

        return Result.Success(model);
    }

    public async Task<Result> AssignToUserAsync(
        Guid applicationId,
        Guid assignToUserId,
        AssignedUserRole selectedRole,
        string selectedFcAreaCostCode,
        InternalUser performingUser,
        string linkToApplication,
        string? caseNote,
        string adminHubName,
        CancellationToken cancellationToken,
        bool visibleToApplicant = false,
        bool visibleToConsultee = false)
    {
        var userAccessModel = new UserAccessModel
        {
            UserAccountId = performingUser.UserAccountId!.Value,
            IsFcUser = true
        };

        var assignToUserAccount = await InternalUserAccountService.GetUserAccountAsync(assignToUserId, cancellationToken);
        if (assignToUserAccount.HasNoValue)
        {
            _logger.LogError("Could not locate internal user with Id {UserId}", assignToUserId);
            await AuditErrorAsync(performingUser, applicationId, assignToUserId, selectedRole,
                "No user account found for given assign to id", cancellationToken);
            return Result.Failure("Could not locate user with given id.");
        }

        // check for assigning field manager role to a user without requisite permissions
        if (selectedRole == AssignedUserRole.FieldManager && assignToUserAccount.Value.CanApproveApplications is false)
        {
            _logger.LogError(
                "User with ID {UserId} does not have permission to approve applications so cannot be assigned as Field Manager role", 
                assignToUserAccount.Value.Id);
            await AuditErrorAsync(performingUser, applicationId, assignToUserId, selectedRole,
                "Selected user cannot be assigned Field Manager role", cancellationToken);
            return Result.Failure("Cannot assign Field Manager role to user without Approve Applications permission");
        }

        // check for assigning field manager role to the same user that submitted the application
        if (selectedRole == AssignedUserRole.FieldManager)
        {
            var statusHistory = await _getFellingLicenceApplicationService
                .GetApplicationStatusHistory(applicationId, userAccessModel, cancellationToken)
                .ConfigureAwait(false);

            if (statusHistory.IsFailure)
            {
                _logger.LogError("Could not retrieve status history for application with id {ApplicationId}", applicationId);
                await AuditErrorAsync(performingUser, applicationId, assignToUserId, selectedRole, statusHistory.Error,
                    cancellationToken);
                return statusHistory.ConvertFailure();
            }

            var submitted = statusHistory.Value
                .OrderByDescending(x => x.Created)
                .FirstOrDefault(x => x.Status == FellingLicenceStatus.Submitted);
            if (submitted != null)
            {
                var submittedByAccount = 
                    await GetSubmittingUserAsync(submitted.CreatedById!.Value, cancellationToken).ConfigureAwait(false);

                if (submittedByAccount.IsFailure)
                {
                    _logger.LogError("Could not retrieve details of submitting user for application with id {ApplicationId}", applicationId);
                    await AuditErrorAsync(performingUser, applicationId, assignToUserId, selectedRole,
                        submittedByAccount.Error, cancellationToken);
                    return submittedByAccount.ConvertFailure();
                }

                if (submittedByAccount.Value.Email?.Equals(assignToUserAccount.Value.Email, StringComparison.CurrentCultureIgnoreCase) ?? false)
                {
                    _logger.LogError(
                        "Cannot assign field manager role for application with id {ApplicationId} to user with id {UserId} as the same user submitted it",
                        applicationId, assignToUserId);
                    await AuditErrorAsync(performingUser, applicationId, assignToUserId, selectedRole,
                        "Cannot assign the same user that submitted the application as Field Manager", cancellationToken);
                    return Result.Failure("A user may not be assigned as field manager to an application that they submitted");
                }
            }
        }

        var request = new AssignToUserRequest(
            applicationId,
            performingUser.UserAccountId!.Value,
            assignToUserId,
            selectedRole,
            selectedFcAreaCostCode,
            caseNote,
            visibleToApplicant,
            visibleToConsultee);
        
        var assignResult = await _updateFellingLicenceApplicationService
            .AssignToInternalUserAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (assignResult.IsFailure)
        {
            _logger.LogError("Could not assign user to application with id {ApplicationId}", applicationId);
            await AuditErrorAsync(performingUser, applicationId, assignToUserId, selectedRole, assignResult.Error,
                cancellationToken);
            return assignResult.ConvertFailure();
        }

        Maybe<bool> sendNotificationOutcome = Maybe<bool>.None;
        
        // if this is a new assignment, notify the user
        if (!assignResult.Value.ApplicationAlreadyAssignedToThisUser)
        {
            var recipient = new NotificationRecipient(
                assignToUserAccount.Value.Email,
                assignToUserAccount.Value.FullName(false));

            var adminHubFooter = await GetAdminHubAddressDetailsAsync(adminHubName, cancellationToken);

            var notificationModel = new UserAssignedToApplicationDataModel
            {
                ApplicationReference = assignResult.Value.UpdatedApplicationReference,
                AssignedRole = selectedRole.GetDisplayName()!,
                Name = recipient.Name!,
                ViewApplicationURL = linkToApplication,
                SenderName = performingUser.FullName,
                SenderEmail = performingUser.EmailAddress,
                AdminHubFooter = adminHubFooter
            };

            var sendNotificationResult = await _notificationsService.SendNotificationAsync(
                notificationModel,
                NotificationType.UserAssignedToApplication,
                recipient,
                null,
                senderName: performingUser.FullName,
                cancellationToken: cancellationToken);

            if (sendNotificationResult.IsFailure)
            {
                _logger.LogError("Could not send notification for assignment of user Id {UserId} to application {ApplicationId}: {Error}",
                    assignToUserId, applicationId, sendNotificationResult.Error);
            }

            sendNotificationOutcome = Maybe.From(sendNotificationResult.IsSuccess);
        }


        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.AssignFellingLicenceApplicationToStaffMember,
            applicationId,
            performingUser.UserAccountId,
            _requestContext,
            new
            {
                AssignedStaffMemberId = assignToUserId,
                AssignedUserRole = selectedRole,
                UnassignedStaffMemberId = assignResult.Value.IdOfUnassignedUser.HasValue ? (Guid?)assignResult.Value.IdOfUnassignedUser.Value : null,
                NotificationSent = sendNotificationOutcome.HasValue ? (bool?)sendNotificationOutcome.Value : null,
                AreaCodeRequiredUpdate = assignResult.Value.UpdatedApplicationReference != assignResult.Value.OriginalApplicationReference,
                OriginalApplicationReference = assignResult.Value.OriginalApplicationReference,
                AreaCode = selectedFcAreaCostCode,
                CurrentApplicationReference = assignResult.Value.UpdatedApplicationReference,
            }), cancellationToken);

        return Result.Success();
    }

    private static List<AssignedUserRole> GetValidAssignedUserRoles =>
        new()
        {
            AssignedUserRole.AdminOfficer,
            AssignedUserRole.WoodlandOfficer,
            AssignedUserRole.FieldManager
        };

    private async Task<Result<IEnumerable<UserAccountModel>>> GetConfirmedUserAccountsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve confirmed user accounts to assign FLA to.");

        //TODO - should there be some error handling in the user service that can result in this returning result.failure
        var accounts = await InternalUserAccountService.ListConfirmedUserAccountsAsync(cancellationToken);
        var models = ModelMapping.ToUserAccountModels(accounts);

        return Result.Success(models);
    }

    private async Task AuditErrorAsync(
        InternalUser user, 
        Guid? applicationId, 
        Guid? assignedStaffMember,
        AssignedUserRole? assignedRole,
        string error, 
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure,
            applicationId,
            user.UserAccountId,
            _requestContext,
            new
            {
                AssignedStaffMemberId = assignedStaffMember,
                AssignedUserRole = assignedRole,
                Error = error
            }), cancellationToken);
    }

    private async Task<Result<IEnumerable<UserAccountModel>>> GetUsersthatCanApprove(
        AssignedUserRole selectedRole,
        Guid flaId,
        CancellationToken cancellationToken,
        IEnumerable<UserAccountModel>? users = null)
    {
        if (users == null || !users.Any())
        {
            var usersResult = await GetConfirmedUserAccountsAsync(cancellationToken);
            if (usersResult.IsFailure)
            {
                _logger.LogError(usersResult.Error);
                return Result.Failure<IEnumerable<UserAccountModel>>("Could not locate any approved internal users.");
            }
            users = usersResult.Value;
        }

        if (selectedRole != AssignedUserRole.FieldManager)
        {
            return Result.Success(users);
        }

        _logger.LogDebug($"Attempting to retrieve status History of the felling licence application with Id: {flaId}.");

        var statusHistory = await FellingLicenceRepository.GetStatusHistoryForApplicationAsync(flaId, cancellationToken);
        if (statusHistory.IsNullOrEmpty())
        {
            _logger.LogError($"Failed to retrieve the status history of the application with Id: {flaId}");
            return Result.Failure<IEnumerable<UserAccountModel>>("Failed to retrieve the status history of the applicaiton.");
        }

        _logger.LogDebug($"Attempting to retrieve id of the user who submitted the application with Id: {flaId}.");

        var submittedById = statusHistory.OrderByDescending(x => x.Created).First(x => x.Status == FellingLicenceStatus.Submitted)!.CreatedById;
        if (submittedById == null)
        {
            _logger.LogError("Could not retrieve the user who submitted the application.");
            return Result.Failure<IEnumerable<UserAccountModel>>("Could not locate the user who submitted the application.");
        }

        _logger.LogDebug($"Attempting to retrieve the user with Id; {submittedById} who submitted the application with Id: {flaId}.");

        var submittingUserResult = await GetSubmittingUserAsync((Guid)submittedById!, cancellationToken);
        if (submittingUserResult.IsFailure)
        {
            _logger.LogError("Unable to retireve the details of the external user who submitted the application, application id: {ApplicationId}, external user: {createdById} , error {Error}", flaId, flaId, submittingUserResult.Error);
            return Result.Failure<IEnumerable<UserAccountModel>>("Could not locate the details of the external user who submitted the application.");
        }

        var refinedUsers = users.Where(x => x.Email != submittingUserResult.Value.Email && x.CanApproveApplications);

        return Result.Success(refinedUsers);
    }

    private static bool CanReturnApplicationToApplicant(
        Guid performingUser, 
        FellingLicenceStatus currentStatus, 
        IList<AssigneeHistoryModel> assignedUsers)
    {
        var assignedAo = assignedUsers.SingleOrDefault(x =>
            x.TimestampUnassigned.HasNoValue() && x.Role == AssignedUserRole.AdminOfficer);

        if (currentStatus == FellingLicenceStatus.AdminOfficerReview)
        {
            return assignedAo?.UserAccount?.Id == performingUser;
        }

        var assignedWo = assignedUsers.SingleOrDefault(x =>
            x.TimestampUnassigned.HasNoValue() && x.Role == AssignedUserRole.WoodlandOfficer);

        if (currentStatus == FellingLicenceStatus.WoodlandOfficerReview)
        {
            return assignedAo?.UserAccount?.Id == performingUser
                || assignedWo?.UserAccount?.Id == performingUser;
        }

        var assignedApprover = assignedUsers.SingleOrDefault(x =>
            x.TimestampUnassigned.HasNoValue() && x.Role == AssignedUserRole.FieldManager);

        if (currentStatus == FellingLicenceStatus.SentForApproval)
        {
            return assignedAo?.UserAccount?.Id == performingUser
                || assignedWo?.UserAccount?.Id == performingUser
                || assignedApprover?.UserAccount?.Id == performingUser;
        }

        return true;
    }
}


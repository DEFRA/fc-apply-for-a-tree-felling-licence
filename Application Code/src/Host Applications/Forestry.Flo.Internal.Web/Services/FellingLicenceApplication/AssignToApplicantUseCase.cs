using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

public class AssignToApplicantUseCase : FellingLicenceApplicationUseCaseBase, IAssignToApplicantUseCase
{
    private readonly IAuditService<AssignToApplicantUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly IGetFellingLicenceApplicationForInternalUsers _getFellingLicenceApplicationService;
    private readonly ILogger<AssignToApplicantUseCase> _logger;
    private readonly ExternalApplicantSiteOptions _settings;
    private readonly ISendNotifications _notificationsService;
    private readonly IUpdateFellingLicenceApplication _updateApplications;
    private readonly ILarchCheckService _larchCheckService;
    private readonly IPublicRegister _publicRegister;
    private readonly IClock _clock;
    private readonly LarchOptions _larchOptions;

    public AssignToApplicantUseCase(
        IUserAccountService internalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IAuditService<AssignToApplicantUseCase> auditService,
        RequestContext requestContext,
        ILogger<AssignToApplicantUseCase> logger,
        IOptions<ExternalApplicantSiteOptions> options,
        ISendNotifications notificationsService,
        IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplicationService,
        IRetrieveUserAccountsService retrieveUserAccountsService,
        IUpdateFellingLicenceApplication updateApplications,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IOptions<LarchOptions> larchOptions,
        ILarchCheckService larchCheckService,
        IPublicRegister publicRegister,
        IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
        IClock clock)
        : base(internalUserAccountService,
            retrieveUserAccountsService, 
            fellingLicenceApplicationInternalRepository, 
            woodlandOwnerService,
            agentAuthorityService,
            getConfiguredFcAreasService,
            woodlandOfficerReviewSubStatusService)
    {
        ArgumentNullException.ThrowIfNull(updateApplications);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(notificationsService);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(getFellingLicenceApplicationService);
        ArgumentNullException.ThrowIfNull(larchCheckService);
        ArgumentNullException.ThrowIfNull(publicRegister);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(larchOptions);
        ArgumentNullException.ThrowIfNull(clock);

        _auditService = auditService;
        _requestContext = requestContext;
        _logger = logger;
        _getFellingLicenceApplicationService = getFellingLicenceApplicationService;
        _updateApplications = updateApplications;
        _settings = options.Value;
        _notificationsService = notificationsService;
        _larchCheckService = larchCheckService;
        _publicRegister = publicRegister;
        _clock = clock;
        _larchOptions = larchOptions.Value;
    }

    /// <inheritdoc />
    public async Task<Result<AssignBackToApplicantModel>> GetValidExternalApplicantsForAssignmentAsync(
        InternalUser internalUser,
        Guid applicationId,
        string returnUrl,
        CancellationToken cancellationToken)
    {
        var fellingLicenceApplication = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (fellingLicenceApplication.IsFailure)
        {
            _logger.LogError("Failed to get valid external applications for application Id {ApplicationId}, error: {Error}", applicationId, fellingLicenceApplication.Error);
            await AuditErrorAsync(internalUser.UserAccountId, applicationId, fellingLicenceApplication.Error, cancellationToken);
            return fellingLicenceApplication.ConvertFailure<AssignBackToApplicantModel>();
        }

        var currentStatus = fellingLicenceApplication.Value.StatusHistories.MaxBy(x => x.Created)!.Status;
        if (fellingLicenceApplication.Value.CanBeReturnedToApplicant == false)
        {
            var errorStatuses = $"The application with Id {applicationId} is {currentStatus.GetDisplayNameByActorType(ActorType.InternalUser)} and cannot be returned to applicant."; 
            _logger.LogError(
                "The application with Id {ApplicationId} is in status {CurrentStatus} and cannot be returned to the applicant.",
                applicationId,
                currentStatus.GetDisplayNameByActorType(ActorType.InternalUser));
            await AuditErrorAsync(internalUser.UserAccountId, applicationId, errorStatuses, cancellationToken);
            return Result.Failure<AssignBackToApplicantModel>($"Could not assign to an application that has been {currentStatus.GetDisplayNameByActorType(ActorType.InternalUser)}.");
        }

        var authorId = fellingLicenceApplication.Value.CreatedById;

        var isFcAuthor = await ExternalUserAccountService.IsUserAccountLinkedToFcAgencyAsync(authorId, cancellationToken);

        var result = new List<UserAccountModel>();

        if (isFcAuthor is { IsSuccess: true, Value: true })
        {
            var getFcAccounts = await ExternalUserAccountService.RetrieveUserAccountsForFcAgencyAsync(cancellationToken);
            if (getFcAccounts.IsSuccess)
            {
                result.AddRange(getFcAccounts.Value);
            }
        }
        else
        {
            var getAccounts = await ExternalUserAccountService.RetrieveUserAccountsForWoodlandOwnerAsync(
                fellingLicenceApplication.Value.WoodlandOwnerId!.Value, cancellationToken);
            if (getAccounts.IsSuccess)
            {
                result.AddRange(getAccounts.Value);
            }
        }

        var sectionDict = Enum.GetValues<FellingLicenceApplicationSection>()
            .Where(x => x is not FellingLicenceApplicationSection.FlaTermsAndConditionsViewModel 
                && x is not FellingLicenceApplicationSection.TenYearLicence)
            .ToDictionary(section => section, _ => false);

        if (!fellingLicenceApplication.Value.HasPaws)
        {
            sectionDict.Remove(FellingLicenceApplicationSection.PawsAndIawp);
        }

        if (fellingLicenceApplication.Value.IsPriorityOpenHabitat != true)
        {
            sectionDict.Remove(FellingLicenceApplicationSection.HabitatRestoration);
        }

        var compartmentDict = fellingLicenceApplication.Value.DetailsList
            .Select(x => x.CompartmentId)
            .Distinct()
            .ToDictionary(comp => comp, _ => false);

        // if the author of the FLA is contained in the list of applicants to return to, default to that applicant;
        // if not, and there's only one applicant, default to that one, otherwise no default
        Guid? defaultSelectedUserId = result.Any(r => r.UserAccountId == authorId) 
            ? authorId 
            : result.Count == 1
                ? result.Single().UserAccountId
                : null;
        
        // don't show the list of users if its an FC application and the author is still available to return to;
        // if not FC, only show the list of users if there is actually more than one to return to
        var canShowListOfUsers = isFcAuthor is { IsSuccess: true, Value: true }
            ? defaultSelectedUserId.HasNoValue()
            : result.Count > 1;

        var larchCheckDetails = await _larchCheckService.GetLarchCheckDetailsAsync(applicationId, cancellationToken);

        var confirmReassignBackToExternalApplicantModel = new AssignBackToApplicantModel
        {
            FellingLicenceApplicationId = applicationId,
            FellingLicenceApplicationSummary = fellingLicenceApplication.Value,
            ExternalApplicants = result,
            ReturnUrl = returnUrl,
            SectionsToReview = sectionDict,
            CompartmentIdentifiersToReview = compartmentDict,
            LarchCheckSplit = larchCheckDetails.HasValue && 
                (RecommendSplitApplicationEnum)larchCheckDetails.Value.RecommendSplitApplicationDue != RecommendSplitApplicationEnum.DontReturnApplication,
            LarchCheckSplitRecommendation = larchCheckDetails.HasValue
                ? (RecommendSplitApplicationEnum)larchCheckDetails.Value.RecommendSplitApplicationDue
                : RecommendSplitApplicationEnum.DontReturnApplication,
            ExternalApplicantId = defaultSelectedUserId,
            ShowListOfUsers = canShowListOfUsers
        };

        return Result.Success(confirmReassignBackToExternalApplicantModel);
    }

    /// <inheritdoc />
    public async Task<Result> AssignApplicationToApplicantAsync(
        Guid applicationId, 
        InternalUser internalUser, 
        Guid applicantId, 
        string returnReason, 
        string viewFLAUrl, 
        Dictionary<FellingLicenceApplicationSection, bool> amendmentSections,
        Dictionary<Guid, bool> amendmentCompartments,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(applicationId);
        Guard.Against.Null(internalUser);
        Guard.Against.Null(applicantId);

        var userAccess = await ExternalUserAccountService
            .RetrieveUserAccessAsync(applicantId, cancellationToken)
            .ConfigureAwait(false);

        if (userAccess.IsFailure)
        {
            await LogAndAuditAssignToApplicantAsync(userAccess.Error, internalUser.UserAccountId!.Value, applicationId, cancellationToken, applicantId)
                .ConfigureAwait(false);
            return Result.Failure("Could not assign the application back to the applicant");
        }

        var sectionsRequiringAttention = new ApplicationStepStatusRecord
        {
            AgentAuthorityFormComplete = amendmentSections.TryGetValue(FellingLicenceApplicationSection.AgentAuthorityForm, out var ticked0) && ticked0 ? false : null,
            SelectedCompartmentsComplete = amendmentSections.TryGetValue(FellingLicenceApplicationSection.SelectedCompartments, out var ticked1) && ticked1 ? false : null,
            OperationDetailsComplete = amendmentSections.TryGetValue(FellingLicenceApplicationSection.OperationDetails, out var ticked2) && ticked2  ? false : null,
            FellingAndRestockingDetailsComplete = amendmentSections.TryGetValue(FellingLicenceApplicationSection.FellingAndRestockingDetails, out var ticked3) && ticked3
                ? amendmentCompartments.Select(x => new CompartmentFellingRestockingStatus 
                {
                    CompartmentId = x.Key,
                    FellingStatuses = new List<FellingStatus>()
                }).ToList()
                : new List<CompartmentFellingRestockingStatus>(),
            ConstraintsCheckComplete = amendmentSections.TryGetValue(FellingLicenceApplicationSection.ConstraintCheck, out var ticked4) && ticked4 ? false : null,
            SupportingDocumentationComplete = amendmentSections.TryGetValue(FellingLicenceApplicationSection.SupportingDocumentation, out var ticked5) && ticked5 ? false : null,
            PawsCheckComplete = amendmentSections.TryGetValue(FellingLicenceApplicationSection.PawsAndIawp, out var ticked6) && ticked6 ? false : null,
            HabitatRestorationComplete = amendmentSections.TryGetValue(FellingLicenceApplicationSection.HabitatRestoration, out var ticked7) && ticked7 ? false : null,
            TreeHealthComplete = amendmentSections.TryGetValue(FellingLicenceApplicationSection.TreeHealthIssues, out var ticked8) && ticked8 ? false : null,
        };

        var consultationPr = await _getFellingLicenceApplicationService
            .RetrievePublicRegisterForRemoval(applicationId, cancellationToken)
            .ConfigureAwait(false);

        var request = new ReturnToApplicantRequest(
            applicationId, userAccess.Value, internalUser.UserAccountId!.Value, 
            internalUser.AccountType is AccountTypeInternal.AccountAdministrator,  sectionsRequiringAttention, returnReason);

        var updateResult = await _updateApplications
            .ReturnToApplicantAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (updateResult.IsFailure)
        {
            await LogAndAuditAssignToApplicantAsync(updateResult.Error, internalUser.UserAccountId!.Value, applicationId, cancellationToken, applicantId)
                .ConfigureAwait(false);
            return Result.Failure("Could not assign the application back to the applicant");
        }

        var getDetailsResult = await _getFellingLicenceApplicationService
            .RetrieveApplicationNotificationDetailsAsync(applicationId, new UserAccessModel {IsFcUser = true, UserAccountId = internalUser.UserAccountId!.Value }, cancellationToken)
            .ConfigureAwait(false);
        if (getDetailsResult.IsFailure)
        {
            await LogAndAuditAssignToApplicantAsync(getDetailsResult.Error, internalUser.UserAccountId!.Value, applicationId, cancellationToken, applicantId)
                .ConfigureAwait(false);
            return Result.Failure("Could not look up application reference");
        }

        var larchCheckDetails = await _larchCheckService.GetLarchCheckDetailsAsync(applicationId, cancellationToken);
        if (larchCheckDetails.HasValue && 
            (RecommendSplitApplicationEnum)larchCheckDetails.Value.RecommendSplitApplicationDue != RecommendSplitApplicationEnum.DontReturnApplication)
        {
            var applicationSummary = await GetApplicationSummary(applicationId, cancellationToken);
            var notifyApplicantLarchResult = await NotifyApplicantOfLarchSplitAsync(
                    applicationId, applicantId, getDetailsResult.Value, larchCheckDetails.Value, applicationSummary.Value, cancellationToken)
                .ConfigureAwait(false);
            if (notifyApplicantLarchResult.IsFailure)
            {
                await LogAndAuditAssignToApplicantAsync(notifyApplicantLarchResult.Error, internalUser.UserAccountId!.Value, applicationId, cancellationToken, applicantId)
                    .ConfigureAwait(false);
                return Result.Failure("Could not send notification to applicant");
            }

        }
        else {
            var notifyApplicantResult = await NotifyApplicantAsync(applicationId, applicantId, returnReason, getDetailsResult.Value, cancellationToken)
                .ConfigureAwait(false);
            if (notifyApplicantResult.IsFailure)
            {
                await LogAndAuditAssignToApplicantAsync(notifyApplicantResult.Error, internalUser.UserAccountId!.Value, applicationId, cancellationToken, applicantId)
                    .ConfigureAwait(false);
                return Result.Failure("Could not send notification to applicant");
            }
        }

        var notifyUsersResult = await NotifyInternalUsersAsync(
            applicationId, updateResult.Value, viewFLAUrl, returnReason, getDetailsResult.Value, cancellationToken)
            .ConfigureAwait(false);

        if (notifyUsersResult.IsFailure)
        {
            await LogAndAuditAssignToApplicantAsync(notifyUsersResult.Error, internalUser.UserAccountId!.Value, applicationId, cancellationToken, applicantId)
                .ConfigureAwait(false);
            return Result.Failure("Could not send notifications to assigned FC users");
        }

        if (consultationPr.HasValue &&
            consultationPr.Value.PublicRegister.ShouldApplicationBeRemovedFromConsultationPublicRegister())
        {
            await RemoveFromConsultationPublicRegisterAsync(consultationPr.Value, viewFLAUrl, internalUser.UserAccountId!.Value, cancellationToken).ConfigureAwait(false);
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(AuditEvents.AssignFellingLicenceApplicationToApplicant, applicationId, internalUser.UserAccountId!.Value, _requestContext, new { applicantId =  applicantId }), cancellationToken);
        return Result.Success();
    }

    private async Task RemoveFromConsultationPublicRegisterAsync(
        PublicRegisterPeriodEndModel prModel,
        string viewFLAUrlBase,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var gisResult = await _publicRegister.RemoveCaseFromConsultationRegisterAsync(
            prModel.PublicRegister.EsriId!.Value,
            prModel.ApplicationReference,
            now,
            cancellationToken).ConfigureAwait(false);

        if (gisResult.IsFailure)
        {
            _logger.LogError(
                "Unable to remove the Felling Application with reference {CaseReference} from the Consultation Public Register, Error was {Error}",
                prModel.ApplicationReference,
                gisResult.Error);

            var notificationsSent = await NotifyUsersOfFailureToRemoveFromCpr(prModel, viewFLAUrlBase, cancellationToken).ConfigureAwait(false);

            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.ConsultationPublicRegisterApplicationRemovalNotification,
                    prModel.PublicRegister!.FellingLicenceApplicationId,
                    performingUserId,
                    _requestContext,
                    new
                    {
                        PublicRegisterPeriodEndDate = prModel.PublicRegister.ConsultationPublicRegisterExpiryTimestamp,
                        NumberOfFcStaffNotificationRecipients = notificationsSent,
                        ApplicationRemovalSuccess = false
                    }), cancellationToken);

            return;
        }

        _logger.LogDebug(
            "Successfully removed the Felling Application with reference {CaseReference} from the Consultation Public Register", 
            prModel.ApplicationReference);

        await _updateApplications
            .SetRemovalDateOnConsultationPublicRegisterEntryAsync(prModel.PublicRegister.FellingLicenceApplicationId, now, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task AuditErrorAsync(
        Guid? userId,
        Guid? applicationId,
        string error,
        CancellationToken cancellationToken,
        Object? auditData = null)
    {
         await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.AssignFellingLicenceApplicationToApplicantFailure,
                applicationId,
                userId,
                _requestContext,
                auditData ?? new
                {
                    Error = error
                }),
            cancellationToken);
    }

    private async Task<Result> LogAndAuditAssignToApplicantAsync(
        string error, 
        Guid? userId, 
        Guid? appId, 
        CancellationToken cancellation, 
        Guid? arg = null)
    {
        _logger.LogError(error, arg);

        var shortenedError = error.Contains("{") ? error[..(error.IndexOf("{", StringComparison.Ordinal) - 1)] : error;

        object auditData = arg is null ?
            new
            {
                error = shortenedError
            } :
            new
            {
                error = shortenedError,
                secondaryId = arg
            };

        await AuditErrorAsync(userId,
            appId,
            shortenedError,
            cancellation,
            auditData);

        return Result.Failure(shortenedError);
    }

    private async Task<Result> NotifyApplicantAsync(
        Guid applicationId,
        Guid applicantId,
        string caseNoteContent,
        ApplicationNotificationDetails applicationDetails,
        CancellationToken cancellationToken)
    {
        var externalViewURL = $"{_settings.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={applicationId}";

        var applicant = await ExternalUserAccountService
            .RetrieveUserAccountByIdAsync(applicantId, cancellationToken)
            .ConfigureAwait(false);
        if (applicant.IsFailure)
        {
            return Result.Failure("Could not send notification to applicant");
        }

        var adminHubFooter = await GetAdminHubAddressDetailsAsync(applicationDetails.AdminHubName, cancellationToken);

        var informApplicantModel = new InformApplicantOfReturnedApplicationDataModel
        {
            ApplicationReference = applicationDetails.ApplicationReference,
            PropertyName = applicationDetails.PropertyName,
            CaseNoteContent = caseNoteContent,
            ViewApplicationURL = externalViewURL,
            Name = applicant.Value.FullName,
            AdminHubFooter = adminHubFooter,
            ApplicationId = applicationId
        };

        var applicantResult = await _notificationsService.SendNotificationAsync(
                informApplicantModel,
                NotificationType.InformApplicantOfReturnedApplication,
                new NotificationRecipient(applicant.Value.Email, applicant.Value.FullName),
                cancellationToken: cancellationToken);

        if (applicantResult.IsFailure)
        {
            return Result.Failure("Could not send notification to applicant");
        }

        return Result.Success();
    }

    private async Task<Result> NotifyInternalUsersAsync(
        Guid applicationId,
        List<Guid> assignedStaffMemberIds,
        string viewFLAUrlBase,
        string caseNoteContent,
        ApplicationNotificationDetails applicationDetails,
        CancellationToken cancellationToken)
    {
        var internalViewURL = CreateViewFLAUrl(applicationId, viewFLAUrlBase);

        var staffNotificationFailures = false;

        var adminHubFooter = await GetAdminHubAddressDetailsAsync(applicationDetails.AdminHubName, cancellationToken);

        foreach (var assignee in assignedStaffMemberIds)
        {
            var user = await InternalUserAccountService.GetUserAccountAsync(assignee, cancellationToken);
            if (user.HasNoValue)
            {
                staffNotificationFailures = true;
                continue;
            }

            var informFCModel = new InformFCStaffOfReturnedApplicationDataModel
            {
                ApplicationReference = applicationDetails.ApplicationReference,
                PropertyName = applicationDetails.PropertyName,
                CaseNoteContent = caseNoteContent,
                ViewApplicationURL = internalViewURL,
                Name = user.Value.FullName(),
                AdminHubFooter = adminHubFooter,
                ApplicationId = applicationId
            };

            var fcStaffResult = await _notificationsService.SendNotificationAsync(
                informFCModel, 
                NotificationType.InformFCStaffOfReturnedApplication, 
                new NotificationRecipient(user.Value.Email, user.Value.FullName()),
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (fcStaffResult.IsFailure)
            {
                staffNotificationFailures = true;
            }
        }

        if (staffNotificationFailures)
        {
            return Result.Failure("Could not send notifications to one or more assigned FC users");
        }

        return Result.Success();
    }

    private async Task<int> NotifyUsersOfFailureToRemoveFromCpr(
        PublicRegisterPeriodEndModel prModel,
        string viewFlaUrlBase,
        CancellationToken cancellationToken)
    {
        var notificationsSent = 0;

        var internalViewUrl = CreateViewFLAUrl(prModel.PublicRegister!.FellingLicenceApplicationId, viewFlaUrlBase);
        
        var adminHubFooter = await GetAdminHubAddressDetailsAsync(prModel.AdminHubName, cancellationToken);

        foreach (var assignedUserId in prModel.AssignedUserIds ?? Array.Empty<Guid>())
        {
            var user = await InternalUserAccountService.GetUserAccountAsync(assignedUserId, cancellationToken);

            if (user.HasNoValue)
            {
                _logger.LogError("Unable to retrieve internal user account for user {id}", assignedUserId);
                continue;
            }

            var recipient = new NotificationRecipient(user.Value.Email, user.Value.FullName());

            var notificationModel =
                new InformFCStaffOfDecisionPublicRegisterAutomaticRemovalOnExpiryDataModel
                {
                    ApplicationReference = prModel.ApplicationReference!,
                    Name = user.Value.FullName(),
                    DecisionPublicRegisterExpiryDate =
                        DateTimeDisplay.GetDateDisplayString(
                            prModel.PublicRegister!.ConsultationPublicRegisterExpiryTimestamp!.Value),
                    ViewApplicationURL = internalViewUrl,
                    PropertyName = prModel.PropertyName,
                    AdminHubFooter = adminHubFooter,
                    PublishDate = DateTimeDisplay.GetDateDisplayString(
                        prModel.PublicRegister.ConsultationPublicRegisterPublicationTimestamp!.Value),
                    RegisterName = "Consultation",
                    ApplicationId = prModel.PublicRegister.FellingLicenceApplicationId
                };

            var notificationResult =
                await _notificationsService.SendNotificationAsync(
                        notificationModel,
                        NotificationType.InformFcStaffOfApplicationRemovedFromDecisionPublicRegisterFailure,
                        recipient,
                        cancellationToken: cancellationToken)
                    .Map(() => notificationsSent++);

            if (notificationResult.IsFailure)
            {
                _logger.LogError(
                    "Unable to send notification for removal of application from consultation public register period end to {address} for application {id}", 
                    recipient.Address, 
                    prModel.PublicRegister.FellingLicenceApplicationId);
            }
        }

        return notificationsSent;
    }

    public async Task<Result> NotifyApplicantOfLarchSplitAsync(
        Guid applicationId,
        Guid applicantId,
        ApplicationNotificationDetails applicationDetails,
        LarchCheckDetailsModel larchDetails,
        FellingLicenceApplicationSummaryModel applicationSummary,
        CancellationToken cancellationToken)
    {
        var externalViewURL = $"{_settings.BaseUrl}FellingLicenceApplication/ApplicationTaskList/{applicationId}";

        var applicant = await ExternalUserAccountService
            .RetrieveUserAccountByIdAsync(applicantId, cancellationToken)
            .ConfigureAwait(false);
        if (applicant.IsFailure)
        {
            return Result.Failure("Could not send notification to applicant");
        }

        var adminHubFooter = await GetAdminHubAddressDetailsAsync(applicationDetails.AdminHubName, cancellationToken);

        var informApplicantModel = new InformApplicantOfReturnedLarchApplicationDataModel
        {
            ApplicationReference = applicationDetails.ApplicationReference,
            PropertyName = applicationDetails.PropertyName,
			AdminHubFooter = adminHubFooter,
            SubmissionDate = applicationSummary.DateReceived!.Value.ToString("dd/MM/yyyy"),
            FinalActionDate = applicationSummary.FadLarchExtension(_larchOptions).ToString("dd/MM/yyyy"),
            InitialFinalActionDate = applicationSummary.FinalActionDate!.Value.ToString("dd/MM/yyyy"),
            MoratoriumDates = MoratoriumDatesToString(_larchOptions),
            IdentifiedSpeciesList = applicationSummary.AllSpeciesLarchFirst.Select(species => species.SpeciesName).ToList(),
            IdentifiedCompartmentsList = applicationSummary.DetailsList
                .Where(detail => detail.Zone1 || detail.Zone2 || detail.Zone3)
                .Select(detail => $"{detail.CompartmentName} - {(detail.Zone1 ? "Zone 1" : detail.Zone2 ? "Zone 2" : "Zone 3")}")
                .ToList(),
            ViewApplicationURL = externalViewURL,
            Name = applicant.Value.FullName,
            ApplicationId = applicationId
        };

        var notificationType = larchDetails.RecommendSplitApplicationDue switch
        {
            (int)RecommendSplitApplicationEnum.MixLarchZone1 => NotificationType.InformApplicantOfReturnedApplicationMixLarchZone1,
            (int)RecommendSplitApplicationEnum.LarchOnlyMixZone => NotificationType.InformApplicantOfReturnedApplicationLarchOnlyMixZone,
            (int)RecommendSplitApplicationEnum.MixLarchMixZone => NotificationType.InformApplicantOfReturnedApplicationMixLarchMixZone,
            _ => throw new InvalidOperationException("Invalid recommendation for split application due.")
        };

        var applicantResult = await _notificationsService.SendNotificationAsync(
                informApplicantModel,
                notificationType,
                new NotificationRecipient(applicant.Value.Email, applicant.Value.FullName),
                cancellationToken: cancellationToken);

        if (applicantResult.IsFailure)
        {

            return Result.Failure("Could not send notification to applicant");
        }


        return Result.Success();
    }

    private async Task<Result<FellingLicenceApplicationSummaryModel>> GetApplicationSummary(Guid applicationId, CancellationToken cancellationToken)
    {
        var fellingLicenceApplicationResult = await _getFellingLicenceApplicationService.GetApplicationByIdAsync(applicationId, cancellationToken);
        if (fellingLicenceApplicationResult.IsFailure)
        {
            return Result.Failure<FellingLicenceApplicationSummaryModel>("Could not retrieve felling licence application");
        }
        var fellingLicence = fellingLicenceApplicationResult.Value;

        var applicationSummary = await ExtractApplicationSummaryAsync(fellingLicence, cancellationToken);
        if (applicationSummary.IsFailure)
        {
            return Result.Failure<FellingLicenceApplicationSummaryModel>("Unable to retrieve application summary");
        }

        return Result.Success(applicationSummary.Value);
    }

    private string MoratoriumDatesToString(LarchOptions o)
    {
        var endDate = new DateTime(1, o.FlyoverPeriodStartMonth, o.FlyoverPeriodStartDay).AddDays(-1);
        var startDate = new DateTime(1, o.FlyoverPeriodEndMonth, o.FlyoverPeriodEndDay).AddDays(1);

        return $"{startDate:dd MMMM} to {endDate:dd MMMM}";
    }

}


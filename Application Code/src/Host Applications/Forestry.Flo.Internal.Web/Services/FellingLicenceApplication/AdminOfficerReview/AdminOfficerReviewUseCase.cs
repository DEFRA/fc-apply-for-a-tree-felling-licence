using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Extensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Internal.Web.Services.MassTransit.Messages;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using NodaTime;
using UserAccount = Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;

public class AdminOfficerReviewUseCase : AdminOfficerReviewUseCaseBase, IAdminOfficerReviewUseCase
{
    private readonly IClock _clock;
    private readonly ISendNotifications _emailService;
    private readonly ILogger<AdminOfficerReviewUseCase> _logger;
    private readonly IActivityFeedItemProvider _activityFeedItemProvider;
    private readonly IUpdateFellingLicenceApplication _updateFellingLicenceApplication;
    private readonly IGetAdminOfficerReview _getAdminOfficerReview;
    private readonly ExternalApplicantSiteOptions _options;
    private readonly LarchOptions _larchOptions;
    private readonly ILarchCheckService _larchCheckService;
    private readonly IUpdateConfirmedFellingAndRestockingDetailsService _updateConfirmedFellingAndRestockingDetailsService;
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly ICalculateConditions _calculateConditionsService;
    private readonly IBus _bus;

    public AdminOfficerReviewUseCase(
        ISendNotifications emailService,
        IAuditService<AdminOfficerReviewUseCaseBase> auditService,
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        ILogger<AdminOfficerReviewUseCase> logger,
        RequestContext requestContext,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IViewCaseNotesService viewCaseNotesService,
        IActivityFeedItemProvider activityFeedItemProvider,
        IUpdateAdminOfficerReviewService updateAdminOfficerReviewService,
        IClock clock,
        IUpdateFellingLicenceApplication updateFellingLicenceApplication,
        IGetAdminOfficerReview getAdminOfficerReview,
        IAgentAuthorityService agentAuthorityService,
        IOptions<ExternalApplicantSiteOptions> options,
        IOptions<LarchOptions> larchOptions,
        IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplication,
        ILarchCheckService larchCheckService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IUpdateConfirmedFellingAndRestockingDetailsService updateConfirmedFellingAndRestockingDetailsService,
        IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
        IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
        ICalculateConditions calculateConditionsService,
        IBus bus)
        : base(internalUserAccountService,
            externalUserAccountService,
            logger,
            fellingLicenceApplicationInternalRepository,
            woodlandOwnerService,
            updateAdminOfficerReviewService,
            getFellingLicenceApplication,
            auditService,
            agentAuthorityService,
            getConfiguredFcAreasService,
            woodlandOfficerReviewSubStatusService,
            requestContext)
    {
        ArgumentNullException.ThrowIfNull(updateFellingLicenceApplication);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(updateAdminOfficerReviewService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(emailService);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(viewCaseNotesService);
        ArgumentNullException.ThrowIfNull(activityFeedItemProvider);
        ArgumentNullException.ThrowIfNull(getAdminOfficerReview);
        ArgumentNullException.ThrowIfNull(updateConfirmedFellingAndRestockingDetailsService);
        ArgumentNullException.ThrowIfNull(calculateConditionsService);
        ArgumentNullException.ThrowIfNull(updateWoodlandOfficerReviewService);
        ArgumentNullException.ThrowIfNull(bus);

        _updateFellingLicenceApplication = updateFellingLicenceApplication;
        _getAdminOfficerReview = getAdminOfficerReview;
        _options = Guard.Against.Null(options).Value;
        _larchOptions = Guard.Against.Null(larchOptions).Value;
        _clock = clock;
        _emailService = emailService;
        _logger = logger;
        _activityFeedItemProvider = activityFeedItemProvider;
        _larchCheckService = larchCheckService;
        _updateConfirmedFellingAndRestockingDetailsService = updateConfirmedFellingAndRestockingDetailsService;
        _updateWoodlandOfficerReviewService = updateWoodlandOfficerReviewService;
        _calculateConditionsService = calculateConditionsService;
        _bus = bus;
    }

    /// <inheritdoc />
    public async Task<Result<AdminOfficerReviewModel>> GetAdminOfficerReviewAsync(
        Guid applicationId,
        InternalUser user,
        string hostingPage,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(applicationId);
        Guard.Against.Null(user);
        var (_, isFailure, summaryModel) = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Unable to retrieve felling licence with id {id}", applicationId);
            return Result.Failure<AdminOfficerReviewModel>("Unable to retrieve felling licence with specified id");
        }

        var providerModel = new ActivityFeedItemProviderModel()
        {
            FellingLicenceId = applicationId,
            FellingLicenceReference = summaryModel.ApplicationReference,
            ItemTypes = new[] { ActivityFeedItemType.AdminOfficerReviewComment },
        };

        var activityFeedItems = await _activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            cancellationToken);

        if (activityFeedItems.IsFailure)
        {
            _logger.LogError("Unable to retrieve activity feed items with id {id} due to: {error}", applicationId, activityFeedItems.Error);
            return activityFeedItems.ConvertFailure<AdminOfficerReviewModel>();
        }

        var requireWOReview = await
            _getAdminOfficerReview.GetCBWReviewStatusAsync(
                applicationId,
                cancellationToken) ?? true;

        var assignedWoodlandOfficer =
            summaryModel.AssigneeHistories.FirstOrDefault(x =>
                x.Role is AssignedUserRole.WoodlandOfficer
                && x.TimestampUnassigned.HasValue is false);
        var assignedApprover =
            summaryModel.AssigneeHistories.FirstOrDefault(x =>
                x.Role is AssignedUserRole.FieldManager
                && x.TimestampUnassigned.HasValue is false);

        var editable = summaryModel.AssigneeHistories.Any(x =>
                           x.Role is AssignedUserRole.AdminOfficer
                           && x.UserAccount?.Id == user.UserAccountId
                           && x.TimestampUnassigned.HasValue is false)
                       && summaryModel.Status is FellingLicenceStatus.AdminOfficerReview;

        var isAgencyApplication = summaryModel.AgentOrAgencyName is not null;

        var eiaModel =
            await GetFellingLicenceApplication.GetEnvironmentalImpactAssessmentAsync(applicationId, cancellationToken);

        var isNextUserAssigned = requireWOReview
            ? assignedWoodlandOfficer != null
            : assignedApprover != null;

        var adminOfficerReviewStatus = await
            _getAdminOfficerReview.GetAdminOfficerReviewStatusAsync(
                applicationId,
                isAgencyApplication,
                summaryModel.AreAnyLarchSpecies && summaryModel.DetailsList.Any(x => x.Zone1),
                isNextUserAssigned,
                summaryModel.IsCBWApplication,
                eiaModel.IsSuccess,
                summaryModel.HasTreeHealthIssue,
                cancellationToken);

        var result = new AdminOfficerReviewModel
        {
            FellingLicenceApplicationSummary = summaryModel,
            ApplicationId = applicationId,
            AdminOfficerReviewCommentsFeed = new ActivityFeedModel
            {
                ApplicationId = applicationId,
                NewCaseNoteType = CaseNoteType.AdminOfficerReviewComment,
                DefaultCaseNoteFilter = CaseNoteType.AdminOfficerReviewComment,
                ActivityFeedItemModels = activityFeedItems.Value,
                HostingPage = hostingPage,
                ShowFilters = false,
                ActivityFeedTitle = "Operations Admin Officer Review Comments"
            },
            DateReceived = summaryModel.DateReceived.HasValue
                ? new DatePart(summaryModel.DateReceived.Value.ToLocalTime(), "date-received")
                : null,
            ApplicationSource = summaryModel.Source,
            AssignedWoodlandOfficer = requireWOReview 
                ? assignedWoodlandOfficer?.UserAccount?.FullName 
                : assignedApprover?.UserAccount?.FullName,
            Editable = editable,
            AdminOfficerReviewTaskListStates = adminOfficerReviewStatus.AdminOfficerReviewTaskListStates,
            RequireWOReview = requireWOReview,
            AgentApplication = isAgencyApplication,
        };

        result.AdminOfficerReviewCommentsFeed.ShowAddCaseNote = editable;

        SetBreadcrumbs(result, "Operations Admin Officer Review");

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> ConfirmAdminOfficerReview(
        Guid applicationId,
        InternalUser user,
        string internalLinkToApplication,
        DateTime dateReceived,
        bool isAgentApplication,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(applicationId);
        Guard.Against.Null(user);

        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var updateDateReceivedResult =
            await _updateFellingLicenceApplication.UpdateDateReceivedAsync(
                applicationId,
                dateReceived,
                cancellationToken);

        if (updateDateReceivedResult.IsFailure)
        {
            _logger.LogError("Unable to update date received for application");
            await AppendAuditFailure(
                applicationId,
                user.UserAccountId!.Value,
                new
                {
                    updateDateReceivedResult.Error
                }, cancellationToken);
            return Result.Failure("Unable to update date received for application");
        }

        var hasExtendedFad = false;
        var skippedWoForCbw = false;

        // if larch in zone 1 and not being returned to applicant, need to update FAD and notify applicant of extension due to larch
        var larchCheckDetails = await _larchCheckService.GetLarchCheckDetailsAsync(applicationId, cancellationToken);
        if (larchCheckDetails.HasValue &&
            larchCheckDetails.Value.RecommendSplitApplicationDue == (int)RecommendSplitApplicationEnum.DontReturnApplication
            && larchCheckDetails.Value.Zone1)
        {
            _logger.LogDebug("Application {ApplicationId} requires a Larch FAD extension", applicationId);
            var notifyApplicantLarchResult = await LarchFadExtensionUpdateAsync(applicationId, cancellationToken)
                .ConfigureAwait(false);
            
            if (notifyApplicantLarchResult.IsFailure)
            {
                _logger.LogError("Could not update application FAD/send notification to applicant for Larch");
                await AppendAuditFailure(
                    applicationId,
                    user.UserAccountId!.Value,
                    new
                    {
                        notifyApplicantLarchResult.Error
                    }, cancellationToken);
                return Result.Failure("Could not update application FAD/send notification to applicant for Larch");
            }

            hasExtendedFad = true;
        }
        else
        {
            _logger.LogDebug("Application {ApplicationId} does not require a Larch FAD extension", applicationId);
        }

        // CBWChecked - Complete CFR and Conditions to be able to Approve directly
        var CBWChecked = await _getAdminOfficerReview.GetCBWReviewStatusAsync(applicationId, cancellationToken);
        var isSkippingWoReviewForCbw = CBWChecked == false;     // CBW in non-sensitive area
        if (isSkippingWoReviewForCbw)
        {
            var updateWoReviewResult = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                user.UserAccountId!.Value,
                true,
                cancellationToken,
                isSkippingWoReviewForCbw);

            if (updateWoReviewResult.IsFailure)
            {
                _logger.LogError("Unable to flag completed Confirmed F&R for application {ApplicationId}, error {Error}", applicationId, updateWoReviewResult.Error);
                await AppendAuditFailure(
                    applicationId,
                    user.UserAccountId!.Value,
                    new
                    {
                        updateWoReviewResult.Error
                    }, cancellationToken);
                return Result.Failure($"Unable to flag completed Confirmed F&R for application {applicationId}, error {updateWoReviewResult.Error}");
            }

            var conditionsStatusModel = new ConditionsStatusModel
            {
                IsConditional = true
            };
            var updateConditionalResult = await _updateWoodlandOfficerReviewService.UpdateConditionalStatusAsync(
                applicationId, conditionsStatusModel, user.UserAccountId!.Value, cancellationToken, true);
            
            if (updateConditionalResult.IsFailure)
            {
                _logger.LogError("Unable to update conditional status for application {ApplicationId}, error {Error}", applicationId, updateConditionalResult.Error);
                await AppendAuditFailure(
                    applicationId,
                    user.UserAccountId!.Value,
                    new
                    {
                        updateConditionalResult.Error
                    }, cancellationToken);
                return Result.Failure($"Unable to update conditional status for application {applicationId}, error {updateConditionalResult.Error}");
            }

            var generateConditionsResult = await GenerateConditionsAsync(
                applicationId,
                user,
                cancellationToken);

            if (generateConditionsResult.IsFailure)
            {
                // already audited and logged
                return generateConditionsResult;
            }

            // set as CPR exempt due to being a CBW application in a non-sensitive area, so no need to show on public register
            await _updateWoodlandOfficerReviewService.SetPublicRegisterExemptAsync(
                    applicationId, user.UserAccountId!.Value, true, "Cricket bat willow expedited application", cancellationToken, true);

            skippedWoForCbw = true;
        }

        var updateResult = await UpdateAdminOfficerReviewService.CompleteAdminOfficerReviewAsync(
            applicationId,
            user.UserAccountId!.Value,
            now,
            isAgentApplication,
            isSkippingWoReviewForCbw,
            cancellationToken);

        if (updateResult.IsFailure)
        {
            _logger.LogError("Unable to update application to complete admin officer review for application {ApplicationId}, error: {Error}", applicationId, updateResult.Error);
            await AppendAuditFailure(
                applicationId,
                user.UserAccountId.Value,
                new
                {
                    hasExtendedFad,
                    skippedWoForCbw,
                    updateResult.Error
                }, cancellationToken);
            return Result.Failure("Unable to update application");
        }

        await _bus.Publish(
            new GenerateSubmittedPdfPreviewMessage(
                user.UserAccountId!.Value,
                applicationId),
            cancellationToken);

        var applicant = await ExternalUserAccountService.RetrieveUserAccountEntityByIdAsync(updateResult.Value.ApplicantId, cancellationToken);

        if (applicant.IsFailure)
        {
            _logger.LogError("Unable to determine applicant for notification");
            await AppendAuditFailure(
                applicationId,
                user.UserAccountId.Value,
                new
                {
                    hasExtendedFad,
                    skippedWoForCbw,
                    Error = applicant.Error
                }, cancellationToken);
            return Result.Failure("Unable to determine applicant for notification");
        }

        var woodlandOfficer = await InternalUserAccountService.GetUserAccountAsync(updateResult.Value.WoodlandOfficerId, cancellationToken);
        if (woodlandOfficer.HasNoValue)
        {
            _logger.LogError("Unable to find a user with the id of {Id}", updateResult.Value.WoodlandOfficerId);
            await AppendAuditFailure(
                applicationId,
                user.UserAccountId.Value,
                new
                {
                    hasExtendedFad,
                    skippedWoForCbw,
                    Error = "Unable to find woodland officer to notify"
                }, cancellationToken);
            return Result.Failure("Unable to find woodland officer to notify");
        }

        var adminOfficer = await InternalUserAccountService.GetUserAccountAsync(user.UserAccountId.Value, cancellationToken);
        if (adminOfficer.HasNoValue)
        {
            _logger.LogError("Unable to find a user with the id of {Id}", user.UserAccountId.Value);
            await AppendAuditFailure(
                applicationId,
                user.UserAccountId.Value,
                new
                {
                    hasExtendedFad,
                    skippedWoForCbw,
                    Error = "Unable to find admin officer to notify"
                }, cancellationToken);
            return Result.Failure("Unable to find admin officer to notify");
        }

        var result = await SendReviewConfirmationNotifications(
            updateResult.Value.ApplicationReference, applicant.Value, woodlandOfficer.Value, adminOfficer.Value,
            applicationId, internalLinkToApplication, updateResult.Value.AdminHubName, cancellationToken);

        if (result.IsSuccess)
        {
            await AuditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.ConfirmAdminOfficerReview,
                    applicationId,
                    user.UserAccountId.Value,
                    RequestContext,
                    new
                    {
                        hasExtendedFad,
                        skippedWoForCbw
                    }),
                cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> CompleteLarchCheckAsync(
        Guid applicationId,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        var result = await UpdateAdminOfficerReviewService.SetLarchCheckCompletionAsync(
            applicationId,
            false,
            performingUserId,
            true,
            cancellationToken);

        if (result.IsFailure)
        {
            await AuditAdminOfficerReviewUpdateFailureAsync(
                applicationId,
                result.Error,
                performingUserId,
                cancellationToken);

            return result;
        }

        await AuditAdminOfficerReviewUpdateAsync(
            applicationId,
            true,
            performingUserId,
            cancellationToken);

        return result;
    }

    private async Task AppendAuditFailure(Guid entityGuid, Guid userGuid, object? auditData = null, CancellationToken cancellationToken = default)
    {
        await AuditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConfirmAdminOfficerReviewFailure,
            entityGuid,
            userGuid,
            RequestContext,
            auditData
        ), cancellationToken);
    }

    private async Task AppendNotificationAuditFailure(Guid entityGuid, Guid userGuid, object? auditData = null, CancellationToken cancellationToken = default)
    {
        await AuditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConfirmAdminOfficerReviewNotificationFailure,
            entityGuid,
            userGuid,
            RequestContext,
            auditData
        ), cancellationToken);
    }

    private async Task<Result> SendReviewConfirmationNotifications(
        string applicationReference,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount applicant,
        UserAccount woodlandOfficer,
        UserAccount adminOfficer,
        Guid applicationId,
        string internalLinkToApplication,
        string adminHubName,
        CancellationToken cancellationToken)
    {
        var adminHubFooter = await GetConfiguredFcAreasService.TryGetAdminHubAddress(adminHubName, cancellationToken);

        var woodlandOfficerModel = new InformAssignedUserOfApplicationStatusTransitionDataModel
        {
            Name = woodlandOfficer.FullName(),
            ApplicationReference = applicationReference,
            PreviousAssignedUserName = adminOfficer.FullName(),
            PreviousAssignedEmailAddress = adminOfficer.Email,
            ViewApplicationURL = internalLinkToApplication,
            AdminHubFooter = adminHubFooter,
            ApplicationId = applicationId
        };

        var result1 = await _emailService.SendNotificationAsync(
            woodlandOfficerModel,
            NotificationType.InformWoodlandOfficerOfAdminOfficerReviewCompletion,
            new NotificationRecipient(woodlandOfficer.Email, woodlandOfficer.FullName()),
            cancellationToken: cancellationToken);

        if (result1.IsFailure)
        {
            _logger.LogError("Unable to send admin review confirmation notification to woodland officer {id}", woodlandOfficer.Id);
            await AppendNotificationAuditFailure(
                applicationId, adminOfficer.Id, new { error = "Failed to send notification to woodland officer" }, cancellationToken);
            return Result.Failure("Unable to send admin officer review confirmation notification to woodland officer");
        }

        await AuditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConfirmAdminOfficerReviewNotificationSent,
            applicationId,
            adminOfficer.Id,
            RequestContext,
            new
            {
                recipient = "Woodland Officer",
                recipientId = woodlandOfficer.Id
            }
        ), cancellationToken);

        await AuditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConfirmAdminOfficerReviewNotificationSent,
            applicationId,
            adminOfficer.Id,
            RequestContext,
            new
            {
                recipient = "Applicant",
                recipientId = applicant.Id
            }
        ), cancellationToken);

        return Result.Success();
    }

    private async Task<Result> LarchFadExtensionUpdateAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating application FAD for larch and notifying applicant");

        var externalViewURL = $"{_options.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={applicationId}";

        var fellingLicenceApplicationResult = await GetFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);
        if (fellingLicenceApplicationResult.IsFailure)
        {
            _logger.LogError("Could not retrieve application {ApplicationId} to extend FAD for larch", applicationId);
            return Result.Failure("Could not retrieve felling licence application to update FAD for larch");
        }
        var fellingLicence = fellingLicenceApplicationResult.Value;

        var applicationSummary = await ExtractApplicationSummaryAsync(fellingLicence, cancellationToken);

        if (applicationSummary.IsFailure)
        {
            _logger.LogError("Unable to retrieve application summary for application {ApplicationId} in order to calculate new FAD for larch, error: {Error}",
                applicationId, applicationSummary.Error);
            return Result.Failure("Unable to retrieve application summary to update FAD for larch");
        }

        var initialFadDate = applicationSummary.Value.FinalActionDate;

        var newFadResult = applicationSummary.Value.FadLarchExtension(_larchOptions);

        if (newFadResult.IsFailure)
        {
            _logger.LogError("Could not calculate new FAD date for Larch application {ApplicationId} as it is missing a submitted date", applicationId);
            return Result.Failure("Could not calculate new FAD date for Larch application as it is missing a submitted date");
        }

        if (initialFadDate.HasNoValue() || (initialFadDate.HasValue && initialFadDate.Value < newFadResult.Value))
        {
            _logger.LogDebug("Application with id {ApplicationId} existing FAD ({ExistingFAD}) is missing or earlier than new FAD for larch ({NewFAD})",
                applicationId, initialFadDate, newFadResult.Value);

            var updateFADResult = await _updateFellingLicenceApplication.UpdateFinalActionDateAsync(
                applicationId,
                newFadResult.Value,
                cancellationToken);

            if (updateFADResult.IsFailure)
            {
                _logger.LogError("Failed to update FAD for application {ApplicationId}, error: {Error}", applicationId, updateFADResult.Error);
                return Result.Failure("Failed to update application FAD value");
            }

            var applicant = await ExternalUserAccountService.RetrieveUserAccountEntityByIdAsync(applicationSummary.Value.CreatedById, cancellationToken);
            if (applicant.IsFailure)
            {
                _logger.LogError("Could not retrieve applicant details for application {ApplicationId} in order to send notification about larch FAD extension, error: {Error}",
                    applicationId, applicant.Error);
                return Result.Failure("Could not retrieve applicant details for application in order to send notification about larch FAD extension");
            }

            var adminHubFooter = await GetConfiguredFcAreasService.TryGetAdminHubAddress(fellingLicence.AdministrativeRegion, cancellationToken);

            var informApplicantModel = new InformApplicantOfReturnedLarchApplicationDataModel
            {
                ApplicationReference = applicationSummary.Value.ApplicationReference,
                PropertyName = applicationSummary.Value.PropertyName,
                IdentifiedSpeciesList = applicationSummary.Value.AllLarchOnlySpecies.Select(species => species.SpeciesName).ToList(),
                IdentifiedCompartmentsList = applicationSummary.Value.DetailsList
                    .Where(detail => detail.Zone1 || detail.Zone2 || detail.Zone3)
                    .Select(detail => $"{detail.CompartmentName} - {(detail.Zone1 ? "Zone 1" : detail.Zone2 ? "Zone 2" : "Zone 3")}")
                    .ToList(),
                ViewApplicationURL = externalViewURL,
                AdminHubFooter = adminHubFooter,
                Name = $"{applicant.Value.FirstName} {applicant.Value.LastName}".Trim().Replace("  ", " "),
                ApplicationId = applicationId,
                InitialFinalActionDate = initialFadDate.HasValue ? initialFadDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                FinalActionDate = newFadResult.Value.ToString("dd/MM/yyyy")
            };

            var applicantResult = await _emailService.SendNotificationAsync(
                    informApplicantModel,
                    NotificationType.InformApplicantOfLarchOnlyApplicationFADextension,
                    new NotificationRecipient(applicant.Value.Email, informApplicantModel.Name),
                    cancellationToken: cancellationToken);

            if (applicantResult.IsFailure)
            {
                _logger.LogError("Failed to send notification to applicant about larch FAD extension for application {ApplicationId}, error: {Error}", applicationId, applicantResult.Error);
                return Result.Failure("Could not send notification to applicant for larch FAD extension");
            }
        }
        else
        {
            _logger.LogDebug("Application with id {ApplicationId} existing FAD ({ExistingFAD}) is not earlier than calculated FAD for larch ({NewFAD}), no changes/notification is required",
                applicationId, initialFadDate!.Value, newFadResult.Value);
        }

        return Result.Success();
    }

    private async Task<Result> GenerateConditionsAsync(Guid applicationId, InternalUser user, CancellationToken cancellationToken)
    {
        var fellingAndRestocking = await _updateConfirmedFellingAndRestockingDetailsService.
            RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, cancellationToken);

        if (fellingAndRestocking.IsFailure)
        {
            _logger.LogError("Could not retrieve felling and restocking details in order to generate conditions for application with id {ApplicationId}", applicationId);
            await AppendAuditFailure(
                applicationId,
                user.UserAccountId!.Value,
                new
                {
                    fellingAndRestocking.Error
                }, cancellationToken);
            return Result.Failure($"Unable to retrieve felling and restocking details for application {applicationId}, error {fellingAndRestocking.Error}");
        }

        var calculateConditionsRequest = fellingAndRestocking.Value.ConfirmedFellingAndRestockingDetailModels
            .GenerateCalculateConditionsRequest(applicationId);

        var calculateConditionsResult = await _calculateConditionsService.CalculateConditionsAsync(calculateConditionsRequest, user.UserAccountId!.Value, true, cancellationToken);

        if (calculateConditionsResult.IsFailure)
        {
            _logger.LogError("Could not generate conditions for application with id {ApplicationId}", applicationId);

            await AppendAuditFailure(
                applicationId,
                user.UserAccountId!.Value,
                new
                {
                    calculateConditionsResult.Error
                }, cancellationToken);
            return Result.Failure($"Unable to generate conditions for application {applicationId}, error {calculateConditionsResult.Error}");
        }

        return Result.Success();
    }
}
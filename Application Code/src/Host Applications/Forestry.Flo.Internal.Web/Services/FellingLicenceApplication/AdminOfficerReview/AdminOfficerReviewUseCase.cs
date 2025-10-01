using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Extensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;
using UserAccount = Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;

public class AdminOfficerReviewUseCase : AdminOfficerReviewUseCaseBase
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
        ICalculateConditions calculateConditionsService)
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
    }

    /// <summary>
    /// Creates an <see cref="AdminOfficerReviewModel"/> to populate the admin officer review page.
    /// </summary>
    /// <param name="applicationId">The identifier for the application to review.</param>
    /// <param name="user">The user requesting the page.</param>
    /// <param name="hostingPage">The page to return to after adding an activity feed comment.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="AdminOfficerReviewModel"/> to display to the user.</returns>
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
                && x.TimestampUnassigned.HasValue is false)?.UserAccount?.FullName;
        var assignedApprover =
            summaryModel.AssigneeHistories.FirstOrDefault(x =>
                x.Role is AssignedUserRole.FieldManager
                && x.TimestampUnassigned.HasValue is false)?.UserAccount?.FullName;

        var editable = summaryModel.AssigneeHistories.Any(x =>
                           x.Role is AssignedUserRole.AdminOfficer
                           && x.UserAccount?.Id == user.UserAccountId
                           && x.TimestampUnassigned.HasValue is false)
                       && summaryModel.Status is FellingLicenceStatus.AdminOfficerReview;

        var isAgencyApplication = summaryModel.AgentOrAgencyName is not null;

        var eiaModel =
            await GetFellingLicenceApplication.GetEnvironmentalImpactAssessmentAsync(applicationId, cancellationToken);

        var adminOfficerReviewStatus = await
            _getAdminOfficerReview.GetAdminOfficerReviewStatusAsync(
                applicationId,
                isAgencyApplication,
                summaryModel.AreAnyLarchSpecies && summaryModel.DetailsList.Any(x => x.Zone1),
                assignedWoodlandOfficer != null || assignedApprover != null,
                summaryModel.IsCBWapplication,
                eiaModel.IsSuccess,
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
            AssignedWoodlandOfficer = requireWOReview ? assignedWoodlandOfficer : assignedApprover,
            Editable = editable,
            AdminOfficerReviewTaskListStates = adminOfficerReviewStatus.AdminOfficerReviewTaskListStates,
            RequireWOReview = requireWOReview,
            AgentApplication = isAgencyApplication,
        };

        result.AdminOfficerReviewCommentsFeed.ShowAddCaseNote = editable;

        SetBreadcrumbs(result, "Operations Admin Officer Review");

        return Result.Success(result);
    }

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

        var larchCheckDetails = await _larchCheckService.GetLarchCheckDetailsAsync(applicationId, cancellationToken);
        if (larchCheckDetails.HasValue &&
            larchCheckDetails.Value.RecommendSplitApplicationDue == (int)RecommendSplitApplicationEnum.DontReturnApplication
            && larchCheckDetails.Value.Zone1)
        {
            var notifyApplicantLarchResult = await LarchFadExtensionUpdateAsync(
                    applicationId, larchCheckDetails.Value, _larchOptions, cancellationToken)
                .ConfigureAwait(false);
            if (notifyApplicantLarchResult.IsFailure)
            {
                _logger.LogError("Could not send notification to applicant");
                await AppendAuditFailure(
                    applicationId,
                    user.UserAccountId!.Value,
                    new
                    {
                        notifyApplicantLarchResult.Error
                    }, cancellationToken);
                return Result.Failure("Could not send notification to applicant");
            }
        }

        // CBW - copy the proposed felling and restocking details to be able to Approve directly
        var CBWrequireWOReview = await _getAdminOfficerReview.GetCBWReviewStatusAsync( applicationId, cancellationToken) ?? true;
        if (CBWrequireWOReview == false)
        {
            var updateWoReviewResult = await _updateWoodlandOfficerReviewService.HandleConfirmedFellingAndRestockingChangesAsync(
                applicationId,
                user.UserAccountId!.Value,
                true,
                cancellationToken);

            if (updateWoReviewResult.IsFailure)
            {
                _logger.LogError($"Unable to flag completed Confirmed F&R for application {applicationId}, error {updateWoReviewResult.Error}");
                await AppendAuditFailure(
                    applicationId,
                    user.UserAccountId!.Value,
                    new
                    {
                        updateWoReviewResult.Error
                    }, cancellationToken);
                return Result.Failure($"Unable to flag completed Confirmed F&R for application {applicationId}, error {updateWoReviewResult.Error}");
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
        }

        var updateResult = await UpdateAdminOfficerReviewService.CompleteAdminOfficerReviewAsync(
            applicationId,
            user.UserAccountId!.Value,
            now,
            isAgentApplication,
            CBWrequireWOReview,
            cancellationToken);

        if (updateResult.IsFailure)
        {
            _logger.LogError("Unable to update application to complete admin officer review");
            await AppendAuditFailure(
                applicationId,
                user.UserAccountId.Value,
                new
                {
                    updateResult.Error
                }, cancellationToken);
            return Result.Failure("Unable to update application");
        }

        var applicant = await ExternalUserAccountService.RetrieveUserAccountEntityByIdAsync(updateResult.Value.ApplicantId, cancellationToken);

        if (applicant.IsFailure)
        {
            _logger.LogError("Unable to determine applicant for notification");
            await AppendAuditFailure(
                applicationId,
                user.UserAccountId.Value,
                new
                {
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
                    RequestContext),
                cancellationToken);
        }

        return result;
    }


    /// <summary>
    /// Completes the larch check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the larch check has been updated successfully.</returns>
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

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model, string task)
    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new("Open applications", "Home", "Index", null),
            new(model.FellingLicenceApplicationSummary?.ApplicationReference!, "FellingLicenceApplication", "ApplicationSummary", model.FellingLicenceApplicationSummary?.Id.ToString()),
        };

        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = task
        };
    }

    private async Task<Result> LarchFadExtensionUpdateAsync(
        Guid applicationId,
        LarchCheckDetailsModel larchDetails,
        LarchOptions larchOptions,
        CancellationToken cancellationToken)
    {
        var externalViewURL = $"{_options.BaseUrl}FellingLicenceApplication/ApplicationTaskList/{applicationId}";

        var fellingLicenceApplicationResult = await GetFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);
        if (fellingLicenceApplicationResult.IsFailure)
        {
            return Result.Failure("Could not retrieve felling licence application");
        }
        var fellingLicence = fellingLicenceApplicationResult.Value;

        var applicationSummary = await ExtractApplicationSummaryAsync(fellingLicence, cancellationToken);

        if (applicationSummary.IsFailure)
        {
            return Result.Failure<LarchCheckModel>("Unable to retrieve application summary");
        }

        var submissionDate = applicationSummary.Value.DateReceived;
        if (!submissionDate.HasValue)
        {
            return Result.Failure("Submission date is not available");
        }

        DateTime newFAD = applicationSummary.Value.FadLarchExtension(_larchOptions);

        if (applicationSummary.Value.FinalActionDate.HasValue && applicationSummary.Value.FinalActionDate.Value < newFAD)
        {

            var updateFADResult = await _updateFellingLicenceApplication.UpdateFinalActionDateAsync(
                applicationId,
                newFAD,
                cancellationToken);
            var applicant = await ExternalUserAccountService.RetrieveUserAccountEntityByIdAsync(applicationSummary.Value.CreatedById, cancellationToken);

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
                ApplicationId = applicationId
            };

            var applicantResult = await _emailService.SendNotificationAsync(
                    informApplicantModel,
                    NotificationType.InformApplicantOfLarchOnlyApplicationFADextension,
                    new NotificationRecipient(applicant.Value.Email, informApplicantModel.Name),
                    cancellationToken: cancellationToken);

            if (applicantResult.IsFailure)
            {
                return Result.Failure("Could not send notification to applicant");
            }
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

        var calculateConditionsResult = await _calculateConditionsService.CalculateConditionsAsync(calculateConditionsRequest, user.UserAccountId!.Value, cancellationToken);

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
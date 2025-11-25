using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Extensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

public class ConditionsUseCase : FellingLicenceApplicationUseCaseBase, IConditionsUseCase
{
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly ICalculateConditions _conditionsService;
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly IAuditService<ConditionsUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly IUpdateConfirmedFellingAndRestockingDetailsService _fellingAndRestockingService;
    private readonly ExternalApplicantSiteOptions _settings;
    private readonly ISendNotifications _notificationsService;
    private readonly IClock _clock;
    private readonly ILogger<ConditionsUseCase> _logger;

    public ConditionsUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
        IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
        IAuditService<ConditionsUseCase> auditService,
        RequestContext requestContext,
        ICalculateConditions conditionsService,
        IUpdateConfirmedFellingAndRestockingDetailsService fellingAndRestockingService,
        ISendNotifications notificationsService,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IClock clock,
        IOptions<ExternalApplicantSiteOptions> settings,
        IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
        ILogger<ConditionsUseCase> logger)
        : base(internalUserAccountService,
            externalUserAccountService,
            fellingLicenceApplicationInternalRepository, 
            woodlandOwnerService,
            agentAuthorityService,
            getConfiguredFcAreasService, 
            woodlandOfficerReviewSubStatusService)
    {
        _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
        _conditionsService = Guard.Against.Null(conditionsService);
        _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _fellingAndRestockingService = Guard.Against.Null(fellingAndRestockingService);
        _settings = Guard.Against.Null(settings.Value);
        _notificationsService = Guard.Against.Null(notificationsService);
        _clock = Guard.Against.Null(clock);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ConditionsViewModel>> GetConditionsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve conditions and conditional status for application with id {ApplicationId}", applicationId);

        var conditionsStatus = await _getWoodlandOfficerReviewService.GetConditionsStatusAsync(applicationId, cancellationToken);

        if (conditionsStatus.IsFailure)
        {
            _logger.LogError("Could not retrieve conditional status for application with id {ApplicationId}, error {Error}", applicationId, conditionsStatus.Error);
            return conditionsStatus.ConvertFailure<ConditionsViewModel>();
        }

        var conditions = await _conditionsService.RetrieveExistingConditionsAsync(applicationId, cancellationToken);

        var reviewTaskListStates = await _getWoodlandOfficerReviewService.GetWoodlandOfficerReviewStatusAsync(applicationId, cancellationToken);
        if (reviewTaskListStates.IsFailure)
        {
            _logger.LogError("Could not retrieve WO review status for application with id {ApplicationId}, error {Error}", applicationId, reviewTaskListStates.Error);
            return reviewTaskListStates.ConvertFailure<ConditionsViewModel>();
        }

        var confirmedFellingAndRestockingComplete =
            reviewTaskListStates.Value.WoodlandOfficerReviewTaskListStates.FellingAndRestockingStepStatus == InternalReviewStepStatus.Completed;

        var application = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (application.IsFailure)
        {
            _logger.LogError("Failed to retrieve application summary with error {Error}", application.Error);
            return application.ConvertFailure<ConditionsViewModel>();
        }

        var model = new ConditionsViewModel
        {
            FellingLicenceApplicationSummary = application.Value,
            ApplicationId = applicationId,
            Conditions = conditions.Conditions,
            ConditionsStatus = conditionsStatus.Value,
            ConfirmedFellingAndRestockingComplete = confirmedFellingAndRestockingComplete
        };

        var breadCrumbs = new List<BreadCrumb>
        {
            new BreadCrumb("Home", "Home", "Index", null),
            new BreadCrumb(model.FellingLicenceApplicationSummary.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", applicationId.ToString()),
            new BreadCrumb("Woodland Officer Review", "WoodlandOfficerReview", "Index", applicationId.ToString())
        };
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = "Conditions"
        };

        return Result.Success(model);
    }

    /// <inheritdoc />
    public async Task<Result> SaveConditionStatusAsync(
        Guid applicationId,
        InternalUser user,
        ConditionsStatusModel model,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to store updated conditional status for application with id {ApplicationId}", applicationId);

        var updateResult = await _updateWoodlandOfficerReviewService.UpdateConditionalStatusAsync(applicationId, model, user.UserAccountId!.Value, cancellationToken);
        if (updateResult.IsFailure)
        {
            _logger.LogError("Failed to update application conditional status with error {Error}", updateResult.Error);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Conditions", updateResult.Error, cancellationToken);
            return Result.Failure(updateResult.Error);
        }

        if (model.IsConditional is false)
        {
            var request = new StoreConditionsRequest
            {
                FellingLicenceApplicationId = applicationId,
                Conditions = new List<CalculatedCondition>(0)
            };
            var removeConditionsResult = await _conditionsService.StoreConditionsAsync(request, user.UserAccountId!.Value, cancellationToken);
            if (removeConditionsResult.IsFailure)
            {
                _logger.LogError("Failed to clear application conditions (conditional status = False) with error {Error}", removeConditionsResult.Error);
                await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Conditions", removeConditionsResult.Error, cancellationToken);
                return Result.Failure(removeConditionsResult.Error);
            }
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReview,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { Section = "Conditions" }),
            cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ConditionsResponse>> GenerateConditionsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to generate conditions for application with id {ApplicationId}", applicationId);

        var fellingAndRestocking = await _fellingAndRestockingService.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, cancellationToken);
        if (fellingAndRestocking.IsFailure)
        {
            _logger.LogError("Could not retrieve felling and restocking details in order to generate conditions for application with id {ApplicationId}", applicationId);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Conditions", fellingAndRestocking.Error, cancellationToken);
            return Result.Failure<ConditionsResponse>(fellingAndRestocking.Error);
        }

        var request = fellingAndRestocking.Value.ConfirmedFellingAndRestockingDetailModels
            .GenerateCalculateConditionsRequest(applicationId);

        var result = await _conditionsService.CalculateConditionsAsync(request, user.UserAccountId!.Value, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Could not generate conditions for application with id {ApplicationId}", applicationId);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Conditions", result.Error, cancellationToken);
            return Result.Failure<ConditionsResponse>(result.Error);
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReview,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { Section = "Conditions" }),
            cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> SaveConditionsAsync(
        Guid applicationId,
        InternalUser user,
        List<CalculatedCondition> conditions,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to save conditions for application with id {ApplicationId}", applicationId);

        var request = new StoreConditionsRequest
        {
            FellingLicenceApplicationId = applicationId,
            Conditions = conditions
        };

        var result = await _conditionsService.StoreConditionsAsync(request, user.UserAccountId!.Value, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogError("Could not save conditions for application with id {ApplicationId}", applicationId);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Conditions", result.Error, cancellationToken);
            return Result.Failure(result.Error);
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReview,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { Section = "Conditions" }),
            cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> SendConditionsToApplicantAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to send conditions for application with id {ApplicationId} to the applicant", applicationId);
        
        var getDetails = 
            await _getWoodlandOfficerReviewService.GetDetailsForConditionsNotificationAsync(applicationId, cancellationToken);

        if (getDetails.IsFailure)
        {
            _logger.LogError("Could not retrieve application details to send the conditions for application with id {ApplicationId} to the applicant", applicationId);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Conditions", getDetails.Error, cancellationToken);
            return Result.Failure(getDetails.Error);
        }

        var applicant = await ExternalUserAccountService.RetrieveUserAccountEntityByIdAsync(getDetails.Value.ApplicationAuthorId, cancellationToken);
        if (applicant.IsFailure)
        {
            _logger.LogError("Could not retrieve applicant details to send the conditions for application with id {ApplicationId} to the applicant", applicationId);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Conditions", applicant.Error, cancellationToken);
            return Result.Failure(applicant.Error.ToString());
        }

        var woodlandOwner = await WoodlandOwnerService.RetrieveWoodlandOwnerByIdAsync(getDetails.Value.WoodlandOwnerId, UserAccessModel.SystemUserAccessModel, cancellationToken);
        if (woodlandOwner.IsFailure)
        {
            _logger.LogError("Could not retrieve woodland owner details to send the conditions for application with id {ApplicationId} to the applicant", applicationId);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Conditions", woodlandOwner.Error, cancellationToken);
            return Result.Failure(woodlandOwner.Error);
        }

        var conditions = await _conditionsService.RetrieveExistingConditionsAsync(applicationId, cancellationToken);
        var conditionsText = new List<string>();
        foreach (var condition in conditions.Conditions)
        {
            conditionsText.AddRange(condition.ToFormattedArray());
            conditionsText.Add(string.Empty);
        }

        if (conditions.Conditions.NotAny())
        {
            conditionsText = ["No conditions apply to the application"];
        }

        var externalViewURL = $"{_settings.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={applicationId}";
        
        var adminHubFooter = await GetAdminHubAddressDetailsAsync(getDetails.Value.AdministrativeRegion, cancellationToken)
                .ConfigureAwait(false);
        
        var sendConditionsToApplicantNotificationModel = new ConditionsToApplicantDataModel
        {
            Name = applicant.Value.FullName(),
            WoodlandOwnerName = woodlandOwner.Value.ContactName,
            ApplicationReference = getDetails.Value.ApplicationReference,
            ViewApplicationURL = externalViewURL,
            ConditionsText = string.Join(Environment.NewLine, conditionsText),
            PropertyName = getDetails.Value.PropertyName,
            SenderName = user.FullName,
            SenderEmail = user.EmailAddress,
            AdminHubFooter = adminHubFooter,
            ApplicationId = applicationId
        };

        var notificationResult = await _notificationsService.SendNotificationAsync(
            sendConditionsToApplicantNotificationModel,
            NotificationType.ConditionsToApplicant,
            new NotificationRecipient(applicant.Value.Email, applicant.Value.FullName()),
            cancellationToken: cancellationToken);
        if (notificationResult.IsFailure)
        {
            _logger.LogError("Could not send the conditions for application with id {ApplicationId} to the applicant", applicationId);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Conditions", notificationResult.Error, cancellationToken);
            return Result.Failure(notificationResult.Error);
        }

        var conditionsStatus = new ConditionsStatusModel
        {
            ConditionsToApplicantDate = _clock.GetCurrentInstant().ToDateTimeUtc(),
            IsConditional = true
        };
        var updateResult = await _updateWoodlandOfficerReviewService.UpdateConditionalStatusAsync(
            applicationId, conditionsStatus, user.UserAccountId!.Value, cancellationToken);
        if (updateResult.IsFailure)
        {
            _logger.LogError("Could not update the conditions status for application with id {ApplicationId}", applicationId);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, "Conditions", updateResult.Error, cancellationToken);
            return Result.Failure(updateResult.Error);
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReview,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { Section = "Conditions" }),
            cancellationToken);

        return Result.Success();
    }

    private async Task AuditWoodlandOfficerReviewFailureEvent(
        Guid applicationId,
        InternalUser user,
        string section,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.UpdateWoodlandOfficerReviewFailure,
            applicationId,
            user.UserAccountId,
            _requestContext,
            new { Section = section, Error = error }), cancellationToken);
    }
}
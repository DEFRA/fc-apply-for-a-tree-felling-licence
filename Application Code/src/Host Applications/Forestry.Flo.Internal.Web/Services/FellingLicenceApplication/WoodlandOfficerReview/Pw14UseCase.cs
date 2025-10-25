using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

public class Pw14UseCase(
    IUserAccountService internalUserAccountService,
    IRetrieveUserAccountsService externalUserAccountService,
    IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
    IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
    IAuditService<Pw14UseCase> auditService,
    IAgentAuthorityService agentAuthorityService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    RequestContext requestContext,
    IActivityFeedItemProvider activityFeedItemProvider,
    IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
    ILogger<Pw14UseCase> logger)
    : FellingLicenceApplicationUseCaseBase(internalUserAccountService,
        externalUserAccountService,
        fellingLicenceApplicationInternalRepository,
        woodlandOwnerService,
        agentAuthorityService,
        getConfiguredFcAreasService, 
        woodlandOfficerReviewSubStatusService), IPw14UseCase
{
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    private readonly IAuditService<Pw14UseCase> _auditService = Guard.Against.Null(auditService);
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);

    public async Task<Result<Pw14ChecksViewModel>> GetPw14CheckDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Attempting to retrieve PW14 checks details for application with id {ApplicationId}", applicationId);

        var pw14Checks = await _getWoodlandOfficerReviewService.GetPw14ChecksAsync(applicationId, cancellationToken);

        if (pw14Checks.IsFailure)
        {
            logger.LogError("Failed to retrieve PW14 checks details with error {Error}", pw14Checks.Error);
            return pw14Checks.ConvertFailure<Pw14ChecksViewModel>();
        }

        var application = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (application.IsFailure)
        {
            logger.LogError("Failed to retrieve application summary with error {Error}", application.Error);
            return application.ConvertFailure<Pw14ChecksViewModel>();
        }

        var activityFeedItems = await activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            new ActivityFeedItemProviderModel
            {
                FellingLicenceId = applicationId,
                FellingLicenceReference = application.Value.ApplicationReference,
                ItemTypes = [ActivityFeedItemType.WoodlandOfficerReviewComment],
            },
            ActorType.InternalUser,
            cancellationToken);

        if (activityFeedItems.IsFailure)
        {
            logger.LogError("Failed to retrieve activity feed items with error {Error}", activityFeedItems.Error);
            return Result.Failure<Pw14ChecksViewModel>(activityFeedItems.Error);
        }

        var result = new Pw14ChecksViewModel
        {
            FellingLicenceApplicationSummary = application.Value,
            ApplicationId = applicationId,
            Pw14Checks = pw14Checks.Value.HasValue ? pw14Checks.Value.Value : new Pw14ChecksModel(),
            ActivityFeed = new ActivityFeedModel
            {
                ApplicationId = applicationId,
                ShowFilters = false,
                ShowAddCaseNote = false,
                NewCaseNoteType = CaseNoteType.WoodlandOfficerReviewComment,
                ActivityFeedItemModels = activityFeedItems.Value
            },
            FormLevelCaseNote = new FormLevelCaseNote()
        };

        SetBreadcrumbs(result);

        return Result.Success(result);
    }

    public async Task<Result> SavePw14ChecksAsync(
        Pw14ChecksViewModel model,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Attempting to store updated PW14 checks details for application with id {ApplicationId}", model.ApplicationId);

        var updateResult = await _updateWoodlandOfficerReviewService.UpdatePw14ChecksAsync(
            model.ApplicationId,
            model.Pw14Checks,
            user.UserAccountId.Value,
            cancellationToken);

        if (updateResult.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    model.ApplicationId,
                    user.UserAccountId,
                    _requestContext,
                    new { Section = "PW14 Checks" }),
                cancellationToken);
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdatePw14Checks,
                    model.ApplicationId,
                    user.UserAccountId,
                    _requestContext,
                    new { model.Pw14Checks.Pw14ChecksComplete }),
                cancellationToken);

            return Result.Success();
        }

        logger.LogError("Failed to update application PW14 checks with error {Error}", updateResult.Error);
        await AuditWoodlandOfficerReviewFailureEvent(model.ApplicationId, user, "PW14 Checks", updateResult.Error, cancellationToken);
        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdatePw14ChecksFailure,
                model.ApplicationId,
                user.UserAccountId,
                _requestContext,
                new { updateResult.Error }),
            cancellationToken);

        return Result.Failure(updateResult.Error);
    }

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model)
    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new BreadCrumb("Home", "Home", "Index", null),
            new BreadCrumb(model.FellingLicenceApplicationSummary.ApplicationReference, "FellingLicenceApplication", "ApplicationSummary", model.FellingLicenceApplicationSummary.Id.ToString()),
            new BreadCrumb("Woodland Officer Review", "WoodlandOfficerReview", "Index", model.FellingLicenceApplicationSummary.Id.ToString())
        };
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = "PW14 Checks"
        };
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
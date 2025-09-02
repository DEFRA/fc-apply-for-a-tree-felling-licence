using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
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
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

/// <summary>
/// Usecase class for managing site visits in the woodland officer review process of a felling licence application.
/// </summary>
public class SiteVisitUseCase : FellingLicenceApplicationUseCaseBase
{
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly IActivityFeedItemProvider _activityFeedItemProvider;
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly IAuditService<SiteVisitUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ILogger<SiteVisitUseCase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SiteVisitUseCase"/> class.
    /// </summary>
    /// <param name="internalUserAccountService">A <see cref="IUserAccountService"/> to deal with internal user accounts.</param>
    /// <param name="externalUserAccountService">A <see cref="IRetrieveUserAccountsService"/> to retrieve external user accounts.</param>
    /// <param name="fellingLicenceApplicationInternalRepository">A <see cref="IFellingLicenceApplicationInternalRepository"/> to retrieve applications from the repository.</param>
    /// <param name="woodlandOwnerService">A <see cref="IRetrieveWoodlandOwners"/> to retrieve woodland owner details.</param>
    /// <param name="getWoodlandOfficerReviewService">A <see cref="IGetWoodlandOfficerReviewService"/> to retrieve woodland officer review details.</param>
    /// <param name="updateWoodlandOfficerReviewService">A <see cref="IUpdateWoodlandOfficerReviewService"/> to update woodland officer review details.</param>
    /// <param name="activityFeedItemProvider">A <see cref="IActivityFeedItemProvider"/> to retrieve site visit comment case notes.</param>
    /// <param name="auditService">A <see cref="IAuditService{T}"/> to raise audit events.</param>
    /// <param name="agentAuthorityService">A <see cref="IAgentAuthorityService"/> to retrieve AAF details.</param>
    /// <param name="getConfiguredFcAreasService">A <see cref="IGetConfiguredFcAreas"/> to get FC area config.</param>
    /// <param name="requestContext">The <see cref="RequestContext"/> for the current operation.</param>
    /// <param name="logger">A <see cref="ILogger{T}"/> logging implementation.</param>
    public SiteVisitUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
        IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
        IActivityFeedItemProvider activityFeedItemProvider,
        IAuditService<SiteVisitUseCase> auditService,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        RequestContext requestContext,
        ILogger<SiteVisitUseCase> logger) 
        : base(internalUserAccountService,
            externalUserAccountService, 
            fellingLicenceApplicationInternalRepository, 
            woodlandOwnerService,
            agentAuthorityService, 
            getConfiguredFcAreasService)
    {
        _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
        _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
        _activityFeedItemProvider = Guard.Against.Null(activityFeedItemProvider);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the site visit details for a felling licence application, including comments and summary information.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve details for.</param>
    /// <param name="hostingPage">The hosting page.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="SiteVisitViewModel"/> representing the current state of the site visit.</returns>
    public async Task<Result<SiteVisitViewModel>> GetSiteVisitDetailsAsync(
        Guid applicationId,
        string hostingPage,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve site visit details for application with id {ApplicationId}", applicationId);

        var siteVisitModel = await _getWoodlandOfficerReviewService.GetSiteVisitDetailsAsync(applicationId, cancellationToken);

        if (siteVisitModel.IsFailure)
        {
            _logger.LogError("Failed to retrieve site visit details with error {Error}", siteVisitModel.Error);
            return siteVisitModel.ConvertFailure<SiteVisitViewModel>();
        }

        var application = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (application.IsFailure)
        {
            _logger.LogError("Failed to retrieve application summary with error {Error}", application.Error);
            return application.ConvertFailure<SiteVisitViewModel>();
        }

        var siteVisit = siteVisitModel.Value.HasValue
            ? siteVisitModel.Value.Value
            : new SiteVisitModel { SiteVisitComments = new List<CaseNoteModel>(0) };

        var providerModel = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            FellingLicenceReference = application.Value.ApplicationReference,
            ItemTypes = [ActivityFeedItemType.SiteVisitComment],
        };

        var activityFeedItems = await _activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            cancellationToken);

        if (activityFeedItems.IsFailure)
        {
            _logger.LogError("Failed to retrieve activity feed items with error {Error}", activityFeedItems.Error);
            return activityFeedItems.ConvertFailure<SiteVisitViewModel>();
        }

        var result = new SiteVisitViewModel
        {
            FellingLicenceApplicationSummary = application.Value,
            ApplicationId = applicationId,
            ApplicationReference = application.Value.ApplicationReference,
            SiteVisitNeeded = siteVisit.SiteVisitNeeded,
            SiteVisitArrangementsMade = siteVisit.SiteVisitArrangementsMade,
            SiteVisitComplete = siteVisit.SiteVisitComplete,
            SiteVisitNotNeededReason = new FormLevelCaseNote
            {
                InsetTextHeading = "Explain why a site visit is not needed for this application"
            },
            SiteVisitArrangementNotes = new FormLevelCaseNote
            {
                InsetTextHeading = "Describe any site visit arrangements, or give a reason why none are required"
            },
            SiteVisitComments = new ActivityFeedModel
            {
                ApplicationId = applicationId,
                DefaultCaseNoteFilter = CaseNoteType.SiteVisitComment,
                ActivityFeedItemModels = activityFeedItems.Value,
                HostingPage = hostingPage,
                ShowFilters = false,
                ActivityFeedTitle = "Site visit comments",
                ShowAddCaseNote = false
            }
        };

        SetBreadcrumbs(result);

        return Result.Success(result);
    }

    /// <summary>
    /// Updates the site visit status for a felling licence application, indicating that a site visit is not needed.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update site visit details for.</param>
    /// <param name="user">The user making the update.</param>
    /// <param name="siteVisitNotNeededReason">The reason for not needing a site visit, to be stored as a case note.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public async Task<Result> SiteVisitIsNotNeededAsync(
        Guid applicationId,
        InternalUser user,
        FormLevelCaseNote siteVisitNotNeededReason,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to update site visit not needed for application with id {ApplicationId}", applicationId);

        var updateResult = await _updateWoodlandOfficerReviewService.SetSiteVisitNotNeededAsync(
            applicationId,
            user.UserAccountId!.Value,
            siteVisitNotNeededReason,
            cancellationToken);

        if (updateResult.IsSuccess)
        {
            if (updateResult.Value)
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateWoodlandOfficerReview,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new { Section = "Site Visit" }),
                    cancellationToken);

                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateSiteVisit,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new { NotNeededReason = siteVisitNotNeededReason }),
                    cancellationToken);
            }

            return Result.Success();
        }

        _logger.LogError("Failed to update site visit not needed with error {Error}", updateResult.Error);
        await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, updateResult.Error, cancellationToken);
        await AuditSiteVisitFailureEvent(applicationId, user, updateResult.Error, cancellationToken);

        return Result.Failure(updateResult.Error);
    }

    /// <summary>
    /// Updates the site visit arrangements for a felling licence application, including whether arrangements have been made and any notes about the arrangements.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update.</param>
    /// <param name="user">The user making the update.</param>
    /// <param name="siteVisitArrangementsMade">A flag indicating if any arrangements have been made.</param>
    /// <param name="siteVisitArrangements">Details of the arrangements, to be stored as a case note.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public async Task<Result> SetSiteVisitArrangementsAsync(
        Guid applicationId,
        InternalUser user,
        bool? siteVisitArrangementsMade,
        FormLevelCaseNote siteVisitArrangements,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to update site visit arrangements for application with id {ApplicationId}", applicationId);

        var updateResult = await _updateWoodlandOfficerReviewService.SaveSiteVisitArrangementsAsync(
            applicationId,
            user.UserAccountId!.Value,
            siteVisitArrangementsMade,
            siteVisitArrangements,
            cancellationToken);

        if (updateResult.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new { Section = "Site Visit" }),
                cancellationToken);

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateSiteVisit,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        ArrangementsMade = siteVisitArrangementsMade,
                        ArrangementsNotes = siteVisitArrangements.CaseNote
                    }),
                cancellationToken);

            return Result.Success();
        }

        _logger.LogError("Failed to update site visit arrangements with error {Error}", updateResult.Error);
        await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, updateResult.Error, cancellationToken);
        await AuditSiteVisitFailureEvent(applicationId, user, updateResult.Error, cancellationToken);

        return Result.Failure(updateResult.Error);
    }

    /// <summary>
    /// Retrieves the site visit summary for a felling licence application, including comments and summary information.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve a summary for.</param>
    /// <param name="hostingPage">The hosting page.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="SiteVisitSummaryModel"/> representing the application details required for a site visit summary document.</returns>
    public async Task<Result<SiteVisitSummaryModel>> GetSiteVisitSummaryAsync(
        Guid applicationId,
        string hostingPage,
        CancellationToken cancellationToken)
    {
        var summaryInfo = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (summaryInfo.IsFailure)
        {
            _logger.LogError("Failed to retrieve felling licence application summary with error {Error}", summaryInfo.Error);
            return summaryInfo.ConvertFailure<SiteVisitSummaryModel>();
        }

        var providerModel = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            FellingLicenceReference = summaryInfo.Value.ApplicationReference,
            ItemTypes = [ActivityFeedItemType.SiteVisitComment],
        };

        var activityFeedItems = await _activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            cancellationToken);

        if (activityFeedItems.IsFailure)
        {
            _logger.LogError("Failed to retrieve activity feed items with error {Error}", activityFeedItems.Error);
            return activityFeedItems.ConvertFailure<SiteVisitSummaryModel>();
        }

        var result = new SiteVisitSummaryModel
        {
            FellingLicenceApplicationSummary = summaryInfo.Value,
            SiteVisitComments = new ActivityFeedModel
            {
                ApplicationId = applicationId,
                DefaultCaseNoteFilter = CaseNoteType.SiteVisitComment,
                ActivityFeedItemModels = activityFeedItems.Value,
                HostingPage = hostingPage,
                ShowFilters = false,
                ActivityFeedTitle = "Site visit comments",
                ShowAddCaseNote = false
            }
        };

        SetBreadcrumbs(result, "Site Visit Summary");

        return Result.Success(result);
    }

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model, string currentPage = "Site Visit")
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
            CurrentPage = "Site Visit"
        };
    }

    private async Task AuditWoodlandOfficerReviewFailureEvent(
        Guid applicationId,
        InternalUser user,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.UpdateWoodlandOfficerReviewFailure,
            applicationId,
            user.UserAccountId,
            _requestContext,
            new { Section = "Site Visit", Error = error }), cancellationToken);
    }

    private async Task AuditSiteVisitFailureEvent(
        Guid applicationId,
        InternalUser user,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.UpdateSiteVisitFailure,
            applicationId,
            user.UserAccountId,
            _requestContext,
            new { Error = error }), cancellationToken);
    }
}
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

public class SiteVisitUseCase : FellingLicenceApplicationUseCaseBase
{
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly IActivityFeedItemProvider _activityFeedItemProvider;
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly IAuditService<SiteVisitUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly IForestryServices _forestryServices;
    private readonly IForesterServices _iForesterServices;
    private readonly IClock _clock;
    private readonly ILogger<SiteVisitUseCase> _logger;

    public SiteVisitUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
        IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
        IActivityFeedItemProvider activityFeedItemProvider,
        IForestryServices forestryServices,
        IForesterServices iForesterServices,
        IAuditService<SiteVisitUseCase> auditService,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        RequestContext requestContext,
        IClock clock,
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
        _forestryServices = Guard.Against.Null(forestryServices);
        _clock = Guard.Against.Null(clock);
        _logger = logger;
    }

    public async Task<Result<SiteVisitViewModel>> GetSiteVisitDetailsAsync(
        Guid applicationId,
        InternalUser user,
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

        var providerModel = new ActivityFeedItemProviderModel()
        {
            FellingLicenceId = applicationId,
            FellingLicenceReference = application.Value.ApplicationReference,
            ItemTypes = new[] { ActivityFeedItemType.SiteVisitComment },
        };

        var activityFeedItems = await _activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            cancellationToken);

        if (activityFeedItems.IsFailure)
        {
            _logger.LogError("Failed to retrieve activity feed items with error {Error}", application.Error);
            return activityFeedItems.ConvertFailure<SiteVisitViewModel>();
        }

        var result = new SiteVisitViewModel
        {
            FellingLicenceApplicationSummary = application.Value,
            ApplicationId = applicationId,
            ApplicationReference = application.Value.ApplicationReference,
            SiteVisitNotNeeded = siteVisit.SiteVisitNotNeeded,
            SiteVisitArtefactsCreated = siteVisit.SiteVisitArtefactsCreated,
            SiteVisitNotesRetrieved = siteVisit.SiteVisitNotesRetrieved,
            ApplicationDocumentHasBeenGenerated = application.Value.MostRecentApplicationDocument.HasValue,
            SiteVisitComments = new ActivityFeedModel
            {
                ApplicationId = applicationId,
                NewCaseNoteType = CaseNoteType.SiteVisitComment,
                DefaultCaseNoteFilter = CaseNoteType.SiteVisitComment,
                ActivityFeedItemModels = activityFeedItems.Value,
                HostingPage = hostingPage,
                ShowFilters = false,
                ActivityFeedTitle = "Site visit comments"
            }
        };
        result.SiteVisitComments.ShowAddCaseNote = result.Editable(user);

        SetBreadcrumbs(result);

        return Result.Success(result);
    }

    public async Task<Result> SiteVisitIsNotNeededAsync(
        Guid applicationId,
        InternalUser user,
        string siteVisitNotNeededReason,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to update site visit not needed for application with id {ApplicationId}", applicationId);

        var updateResult = await _updateWoodlandOfficerReviewService.SetSiteVisitNotNeededAsync(
            applicationId,
            user.UserAccountId.Value,
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

    public async Task<Result> GenerateSiteVisitArtefactsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to generate site visit artefacts for application with id {ApplicationId}", applicationId);

        // Publish application to mobile apps data layer
        var applicationDetails = await _getWoodlandOfficerReviewService.GetApplicationDetailsForSiteVisitMobileLayersAsync(
            applicationId, cancellationToken);

        if (applicationDetails.IsFailure)
        {
            _logger.LogError("Could not get application details for mobile layers for application with id {ApplicationId} with error {Error}", applicationId, applicationDetails.Error);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, applicationDetails.Error, cancellationToken);
            await AuditSiteVisitFailureEvent(applicationId, user, applicationDetails.Error, cancellationToken);
            return Result.Failure(applicationDetails.Error);
        }

        var publishResult = await _forestryServices.SavesCaseToMobileLayersAsync(
            applicationDetails.Value.CaseReference,
            applicationDetails.Value.Compartments,
            cancellationToken);

        if (publishResult.IsFailure)
        {
            _logger.LogError("Could not publish application to mobile layers for application with id {ApplicationId} with error {Error}", applicationId, publishResult.Error);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, publishResult.Error, cancellationToken);
            await AuditSiteVisitFailureEvent(applicationId, user, publishResult.Error, cancellationToken);
            return Result.Failure(publishResult.Error);
        }

        // update woodland officer review entity
        var now = _clock.GetCurrentInstant().ToDateTimeUtc();
        var updateResult = await _updateWoodlandOfficerReviewService.PublishedToSiteVisitMobileLayersAsync(
            applicationId, user.UserAccountId.Value, now, cancellationToken);
        if (updateResult.IsFailure)
        {
            _logger.LogError("Could not update woodland officer review for published application to mobile layers for application with id {ApplicationId} with error {Error}", applicationId, updateResult.Error);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, updateResult.Error, cancellationToken);
            await AuditSiteVisitFailureEvent(applicationId, user, updateResult.Error, cancellationToken);
            return Result.Failure(updateResult.Error);
        }

        //audit successful completion
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
                new { ProcessStartedDate = now }),
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RetrieveSiteVisitNotesAsync(
        Guid applicationId,
        string applicationReference,
        InternalUser user,
        CancellationToken cancellationToken)
    {

        _logger.LogDebug("Attempting to retrieve site visit notes and complete process for application with id {ApplicationId}", applicationId);

        var now = _clock.GetCurrentInstant().ToDateTimeUtc();
        var notesResult = await _forestryServices.GetVisitNotesAsync(applicationReference, cancellationToken);

        if (notesResult.IsFailure)
        {
            _logger.LogError("Could not retrieve site visit notes from mobile layers for application with id {ApplicationId} with error {Error}", applicationId, notesResult.Error);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, notesResult.Error, cancellationToken);
            await AuditSiteVisitFailureEvent(applicationId, user, notesResult.Error, cancellationToken);
            return Result.Failure(notesResult.Error);
        }

        var updateResult = await _updateWoodlandOfficerReviewService.SiteVisitNotesRetrievedAsync(
            applicationId,
            user.UserAccountId.Value,
            now,
            notesResult.Value,
            cancellationToken);

        if (updateResult.IsFailure)
        {
            _logger.LogError("Could not update woodland officer review for retrieval of site visit notes for application with id {ApplicationId} with error {Error}", applicationId, updateResult.Error);
            await AuditWoodlandOfficerReviewFailureEvent(applicationId, user, updateResult.Error, cancellationToken);
            await AuditSiteVisitFailureEvent(applicationId, user, updateResult.Error, cancellationToken);
            return updateResult;
        }
        
        //audit successful completion
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
                new { ProcessCompletedDate = now }),
            cancellationToken);

        return Result.Success();
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
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Services;
using Newtonsoft.Json;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

/// <summary>
/// Usecase class for managing site visits in the woodland officer review process of a felling licence application.
/// </summary>
public class SiteVisitUseCase : FellingLicenceApplicationUseCaseBase, ISiteVisitUseCase
{
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly IActivityFeedItemProvider _activityFeedItemProvider;
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly IAuditService<SiteVisitUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly IAddDocumentService _addDocumentService;
    private readonly IRemoveDocumentService _removeDocumentService;
    private readonly IForesterServices _foresterServices;
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
    /// <param name="addDocumentService">A <see cref="IAddDocumentService"/> to store site visit documents.</param>
    /// <param name="removeDocumentService">A <see cref="IRemoveDocumentService"/> to clear up files in event of an error.</param>
    /// <param name="foresterServices">A <see cref="IForesterServices"/> to generate compartment map images for the site visit summary.</param>
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
        IAddDocumentService addDocumentService,
        IRemoveDocumentService removeDocumentService,
        IForesterServices foresterServices,
        RequestContext requestContext,
        IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
        ILogger<SiteVisitUseCase> logger) 
        : base(internalUserAccountService,
            externalUserAccountService, 
            fellingLicenceApplicationInternalRepository, 
            woodlandOwnerService,
            agentAuthorityService, 
            getConfiguredFcAreasService,
            woodlandOfficerReviewSubStatusService)
    {
        _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
        _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
        _activityFeedItemProvider = Guard.Against.Null(activityFeedItemProvider);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _addDocumentService = Guard.Against.Null(addDocumentService);
        _removeDocumentService = Guard.Against.Null(removeDocumentService);
        _foresterServices = Guard.Against.Null(foresterServices);
        _logger = logger;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

        var woodlandOwner =
            await WoodlandOwnerService.RetrieveWoodlandOwnerByIdAsync(summaryInfo.Value.WoodlandOwnerId!.Value, cancellationToken);
        if (woodlandOwner.IsFailure)
        {
            _logger.LogError("Application woodland owner not found, application id: {ApplicationId}, woodland owner id: {WoodlandOwnerId}, error: {Error}",
                applicationId, summaryInfo.Value.WoodlandOwnerId!.Value, woodlandOwner.Error);
            return woodlandOwner.ConvertFailure<SiteVisitSummaryModel>();
        }

        var agencyForWoodlandOwner =
            await AgentAuthorityService.GetAgencyForWoodlandOwnerAsync(woodlandOwner.Value.Id!.Value, cancellationToken);

        var fellingImage = await GetImageForFellingAndRestockingCompartments(
            applicationId,
            summaryInfo.Value.DetailsList.DistinctBy(x => x.CompartmentId).Select(f => new InternalCompartmentDetails<BaseShape>
                {
                    CompartmentLabel = f.CompartmentName,
                    CompartmentNumber = f.CompartmentName,
                    ShapeGeometry = JsonConvert.DeserializeObject<Polygon>(f.GISData!)!
                }).ToList(),
            cancellationToken);

        if (fellingImage.IsFailure)
        {
            return fellingImage.ConvertFailure<SiteVisitSummaryModel>();
        }

        var restockingImage = await GetImageForFellingAndRestockingCompartments(
            applicationId,
            summaryInfo.Value.DetailsList.SelectMany(x => x.RestockingDetail).DistinctBy(x => x.RestockingCompartmentId)
                .Select(r => new InternalCompartmentDetails<BaseShape>
                {
                    CompartmentLabel = r.RestockingCompartmentName,
                    CompartmentNumber = r.RestockingCompartmentName,
                    ShapeGeometry = JsonConvert.DeserializeObject<Polygon>(r.GISData!)!
                }).ToList(),
            cancellationToken);

        if (restockingImage.IsFailure)
        {
            return restockingImage.ConvertFailure<SiteVisitSummaryModel>();
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
            },
            FellingAndRestockingDetail = new FellingAndRestockingDetails
            {
                ApplicationId = applicationId,
                ApplicationReference = summaryInfo.Value.ApplicationReference,
                ConfirmedFellingAndRestockingCompleted = false, // this field is only used to render a link to the confirmed f&r
                DetailsList = summaryInfo.Value.DetailsList
            },
            FellingMapBase64 = fellingImage.Value,
            RestockingMapBase64 = restockingImage.Value,
            ApplicationOwner = new ApplicationOwnerModel
            {
                WoodlandOwner = ModelMapping.ToWoodlandOwnerModel(woodlandOwner.Value),
                Agency = agencyForWoodlandOwner.HasNoValue ? null : agencyForWoodlandOwner.Value,
                AgentAuthorityForm = null  // not required for site visit summary view
            }
        };

        SetBreadcrumbs(result, "Site Visit Summary");

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<AddSiteVisitEvidenceModel>> GetSiteVisitEvidenceModelAsync(
        Guid applicationId,
        string hostingPage,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve site visit details to add evidence for application with id {ApplicationId}", applicationId);

        var siteVisitModel = await _getWoodlandOfficerReviewService.GetSiteVisitDetailsAsync(applicationId, cancellationToken);

        if (siteVisitModel.IsFailure)
        {
            _logger.LogError("Failed to retrieve site visit details with error {Error}", siteVisitModel.Error);
            return siteVisitModel.ConvertFailure<AddSiteVisitEvidenceModel>();
        }

        if (siteVisitModel.Value.HasNoValue)
        {
            _logger.LogError("Failed to retrieve site visit details, none exist yet for application id {ApplicationId}", applicationId);
            return Result.Failure<AddSiteVisitEvidenceModel>("No site visit details exist yet for application id " + applicationId);
        }

        var application = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (application.IsFailure)
        {
            _logger.LogError("Failed to retrieve application summary with error {Error}", application.Error);
            return application.ConvertFailure<AddSiteVisitEvidenceModel>();
        }

        var siteVisit = siteVisitModel.Value.Value;

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
            return activityFeedItems.ConvertFailure<AddSiteVisitEvidenceModel>();
        }

        var result = new AddSiteVisitEvidenceModel
        {
            FellingLicenceApplicationSummary = application.Value,
            ApplicationId = applicationId,
            SiteVisitComplete = siteVisit.SiteVisitComplete,
            SiteVisitComment = new FormLevelCaseNote(),
            SiteVisitComments = new ActivityFeedModel
            {
                ApplicationId = applicationId,
                DefaultCaseNoteFilter = CaseNoteType.SiteVisitComment,
                ActivityFeedItemModels = activityFeedItems.Value,
                HostingPage = hostingPage,
                ShowFilters = false,
                ActivityFeedTitle = "Final site visit observations",
                ShowAddCaseNote = false
            },
            SiteVisitEvidenceMetadata = siteVisit.SiteVisitAttachments.Select(a => new SiteVisitEvidenceMetadataModel
            {
                FileName = a.FileName,
                SupportingDocumentId = a.DocumentId,
                Comment = a.Comment,
                Label = a.Label,
                VisibleToConsultees = a.VisibleToConsultee,
                VisibleToApplicants = a.VisibleToApplicant
            }).ToArray()
        };

        SetBreadcrumbs(result);

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> AddSiteVisitEvidenceAsync(
        AddSiteVisitEvidenceModel model,
        FormFileCollection siteVisitAttachmentFiles,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(model);

        _logger.LogDebug("Attempting to add site visit evidence for application with id {ApplicationId}", model.ApplicationId);

        var metadata = model.SiteVisitEvidenceMetadata;
        var newFileIds = new List<Guid>();

        try
        {
            foreach (var siteVisitAttachment in siteVisitAttachmentFiles)
            {
                var matchingMetadata = metadata.FirstOrDefault(m =>
                    m.FileName == siteVisitAttachment.FileName
                    && m.SupportingDocumentId.HasNoValue()
                    && m.MarkedForDeletion == false);

                if (matchingMetadata != null)
                {
                    var fileModel = ModelMapping.ToFileToStoreModel([siteVisitAttachment]);
                    var addDocRequest = new AddDocumentsRequest
                    {
                        ActorType = ActorType.InternalUser,
                        ApplicationDocumentCount = 0,
                        DocumentPurpose = DocumentPurpose.SiteVisitAttachment,
                        FellingApplicationId = model.ApplicationId,
                        FileToStoreModels = fileModel,
                        ReceivedByApi = false,
                        UserAccountId = user.UserAccountId!.Value,
                        VisibleToApplicant = matchingMetadata.VisibleToApplicants,
                        VisibleToConsultee = matchingMetadata.VisibleToConsultees
                    };
                    var addDocsResult =
                        await _addDocumentService.AddDocumentsAsInternalUserAsync(addDocRequest, cancellationToken);

                    if (addDocsResult.IsFailure)
                    {
                        _logger.LogError("Attempt to store site visit evidence file failed: {Error}",
                            addDocsResult.Error);
                        await HandleErrorUpdatingSiteVisitAttachments(
                            string.Join(", ", addDocsResult.Error.UserFacingFailureMessages),
                            model.ApplicationId,
                            user.UserAccountId!.Value,
                            newFileIds,
                            cancellationToken);
                        return Result.Failure("Failed to update site visit attachments");
                    }

                    newFileIds.AddRange(addDocsResult.Value.DocumentIds);
                    matchingMetadata.SupportingDocumentId = addDocsResult.Value.DocumentIds.FirstOrDefault();
                }
            }

            var updateResult = await _updateWoodlandOfficerReviewService.UpdateSiteVisitEvidenceAsync(
                model.ApplicationId,
                user.UserAccountId!.Value,
                metadata.Where(x => x is { MarkedForDeletion: false, SupportingDocumentId: not null })
                    .Select(x => new SiteVisitEvidenceDocument
                    {
                        FileName = x.FileName,
                        DocumentId = x.SupportingDocumentId!.Value,
                        VisibleToApplicant = x.VisibleToApplicants,
                        VisibleToConsultee = x.VisibleToConsultees,
                        Comment = x.Comment,
                        Label = x.Label
                    }).ToArray(),
                model.SiteVisitComment,
                model.SiteVisitComplete,
                cancellationToken);

            if (updateResult.IsFailure)
            {
                _logger.LogError("Attempt to store site visit evidence file failed: {Error}", updateResult.Error);
                await HandleErrorUpdatingSiteVisitAttachments(
                    updateResult.Error,
                    model.ApplicationId,
                    user.UserAccountId!.Value,
                    newFileIds,
                    cancellationToken);
                return Result.Failure("Failed to update site visit attachments");
            }

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    model.ApplicationId,
                    user.UserAccountId,
                    _requestContext,
                    new { Section = "Site Visit" }),
                cancellationToken);
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateSiteVisit,
                    model.ApplicationId,
                    user.UserAccountId!.Value,
                    _requestContext,
                    new
                    {
                        SiteVisitEvidenceCount = metadata.Count(x => !x.MarkedForDeletion),
                        SiteVisitComment = model.SiteVisitComment.CaseNote,
                        model.SiteVisitComplete
                    }),
                cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught updating site evidence for application id {ApplicationId}", model.ApplicationId);
            await HandleErrorUpdatingSiteVisitAttachments(
                ex.Message,
                model.ApplicationId,
                user.UserAccountId!.Value,
                newFileIds,
                cancellationToken);
            return Result.Failure("Failed to update site visit attachments");
        }
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
            CurrentPage = currentPage
        };
    }

    private async Task HandleErrorUpdatingSiteVisitAttachments(
        string error,
        Guid applicationId,
        Guid userId,
        List<Guid> alreadyStoredFiles,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.UpdateSiteVisitFailure,
            applicationId,
            userId,
            _requestContext,
            new
            {
                Error = "Failed to update site visit with evidence: " + error
            }), cancellationToken);

        foreach (var fileId in alreadyStoredFiles)
        {
            var cleanupResult = await _removeDocumentService.PermanentlyRemoveDocumentAsync(applicationId, fileId, cancellationToken);
            if (cleanupResult.IsFailure)
            {
                _logger.LogError("Failed to clean up site visit attachment with id {AttachmentId} after site visit update: {Error}",
                    fileId, cleanupResult.Error);
            }
        }
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

    private async Task<Result<string>> GetImageForFellingAndRestockingCompartments(
        Guid applicationId,
        List<InternalCompartmentDetails<BaseShape>> shapes, 
        CancellationToken cancellationToken)
    {
        var generatedMainFellingMap = await _foresterServices.GenerateImage_MultipleCompartmentsAsync(shapes, cancellationToken, 3000);
        if (generatedMainFellingMap.IsFailure)
        {
            _logger.LogError("Unable to retrieve generated map image of application with id {ApplicationId}, error: {Error}", applicationId, generatedMainFellingMap.Error);
            return generatedMainFellingMap.ConvertFailure<string>();
        }

        if (!generatedMainFellingMap.Value.CanRead)
        {
            _logger.LogError("Generated map image stream is not readable for application with id {ApplicationId}", applicationId);
            return Result.Failure<string>("Generated map image stream is not readable");
        }

        return Result.Success(Convert.ToBase64String(generatedMainFellingMap.Value.ConvertStreamToBytes()));
    }
}
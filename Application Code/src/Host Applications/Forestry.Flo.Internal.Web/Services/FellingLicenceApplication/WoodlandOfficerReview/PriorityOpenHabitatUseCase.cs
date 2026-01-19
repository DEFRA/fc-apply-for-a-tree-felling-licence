using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

public class PriorityOpenHabitatUseCase : FellingLicenceApplicationUseCaseBase
{
    private readonly IHabitatRestorationService _habitatService;
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly IAuditService<PriorityOpenHabitatUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ILogger<PriorityOpenHabitatUseCase> _logger;

    public PriorityOpenHabitatUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IHabitatRestorationService habitatRestorationService,
        IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
        IAuditService<PriorityOpenHabitatUseCase> auditService,
        RequestContext requestContext,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
        ILogger<PriorityOpenHabitatUseCase> logger)
        : base(
            internalUserAccountService,
            externalUserAccountService,
            fellingLicenceApplicationInternalRepository,
            woodlandOwnerService,
            agentAuthorityService,
            getConfiguredFcAreasService,
            woodlandOfficerReviewSubStatusService)
    {
        _habitatService = Guard.Against.Null(habitatRestorationService);
        _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
    }

    public async Task<Result<PriorityOpenHabitatsViewModel>> GetPriorityOpenHabitatsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve habitat restorations for application {ApplicationId}", applicationId);

        var (_, getSummaryFailure, summaryModel, getSummaryError) = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);
        if (getSummaryFailure)
        {
            _logger.LogError("Failed to retrieve application summary for habitat restorations with error {Error}", getSummaryError);
            return Result.Failure<PriorityOpenHabitatsViewModel>(getSummaryError);
        }

        var restorations = await _habitatService.GetHabitatRestorationModelsAsync(applicationId, cancellationToken);

        var ordered = restorations
            .OrderBy(r => r.PropertyProfileCompartmentId)
            .ToList();

        var list = new List<PriorityOpenHabitatViewModel>(ordered.Count);
        foreach (var r in ordered)
        {
            var vm = new PriorityOpenHabitatViewModel
            {
                FellingLicenceApplicationSummary = summaryModel,
                PropertyProfileCompartmentId = r.PropertyProfileCompartmentId,
                HabitatType = r.HabitatType,
                OtherHabitatDescription = r.OtherHabitatDescription,
                WoodlandSpeciesType = r.WoodlandSpeciesType,
                NativeBroadleaf = r.NativeBroadleaf,
                ProductiveWoodland = r.ProductiveWoodland,
                FelledEarly = r.FelledEarly,
                Completed = r.Completed
            };
            list.Add(vm);
        }

        var woReview = await FellingLicenceRepository.GetWoodlandOfficerReviewAsync(applicationId, cancellationToken);
        bool? areDetailsCorrect = null;
        areDetailsCorrect = woReview.Value.PriorityOpenHabitatComplete;

        var viewModel = new PriorityOpenHabitatsViewModel
        {
            ApplicationId = applicationId,
            FellingLicenceApplicationSummary = summaryModel,
            Habitats = list,
            AreDetailsCorrect = areDetailsCorrect
        };

        SetBreadcrumbs(viewModel);

        return Result.Success(viewModel);
    }

    public async Task<Result> CompletePriorityOpenHabitatAsync(
        Guid applicationId,
        InternalUser user,
        bool isComplete,
        CancellationToken cancellationToken)
    {
        var result = await _updateWoodlandOfficerReviewService.CompletePriorityOpenHabitatAsync(
            applicationId,
            user.UserAccountId!.Value,
            isComplete,
            cancellationToken);

        if (result.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReviewFailure,
                    applicationId,
                    user.UserAccountId!.Value,
                    _requestContext,
                    new
                    {
                        Section = "Priority Open Habitat",
                        Error = result.Error
                    }),
                cancellationToken);

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.WoodlandOfficerReviewPriorityOpenHabitatFailure,
                    applicationId,
                    user.UserAccountId!.Value,
                    _requestContext,
                    new
                    {
                        Error = result.Error
                    }),
                cancellationToken);

            return result;
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReview,
                applicationId,
                user.UserAccountId!.Value,
                _requestContext,
                new
                {
                    Section = "Priority Open Habitat"
                }),
            cancellationToken);


        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.WoodlandOfficerReviewPriorityOpenHabitat,
                applicationId,
                user.UserAccountId!.Value,
                _requestContext,
                new
                {
                    IsComplete = isComplete
                }),
            cancellationToken);

        return result;
    }

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model, string currentPage = "Habitat restoration")
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
}

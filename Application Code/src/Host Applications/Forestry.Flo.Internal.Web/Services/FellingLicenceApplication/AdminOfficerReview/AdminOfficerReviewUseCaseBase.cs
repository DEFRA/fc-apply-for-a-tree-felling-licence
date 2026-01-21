using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;

public class AdminOfficerReviewUseCaseBase : FellingLicenceApplicationUseCaseBase
{
    protected readonly IUpdateAdminOfficerReviewService UpdateAdminOfficerReviewService;
    protected readonly IGetFellingLicenceApplicationForInternalUsers GetFellingLicenceApplication;
    protected readonly IAuditService<AdminOfficerReviewUseCaseBase> AuditService;
    protected readonly ILogger<AdminOfficerReviewUseCaseBase> Logger;
    protected readonly RequestContext RequestContext;

    public AdminOfficerReviewUseCaseBase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        ILogger<AdminOfficerReviewUseCaseBase> logger,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IUpdateAdminOfficerReviewService updateAdminOfficerReviewService,
        IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplication,
        IAuditService<AdminOfficerReviewUseCaseBase> auditService,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
        RequestContext requestContext)
        : base(
            internalUserAccountService, 
            externalUserAccountService, 
            fellingLicenceApplicationInternalRepository, 
            woodlandOwnerService, 
            agentAuthorityService,
            getConfiguredFcAreasService, 
            woodlandOfficerReviewSubStatusService)
    {
        ArgumentNullException.ThrowIfNull(updateAdminOfficerReviewService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(getFellingLicenceApplication);

        UpdateAdminOfficerReviewService = updateAdminOfficerReviewService;
        Logger = logger;
        AuditService = auditService;
        RequestContext = requestContext;
        GetFellingLicenceApplication = getFellingLicenceApplication;
    }
    
    protected async Task AuditAdminOfficerReviewUpdateAsync(
        Guid applicationId,
        bool reviewCompleted,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        await AuditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAdminOfficerReview,
                    applicationId,
                    performingUserId,
                    RequestContext,
                    new
                    {
                        ReviewCompleted = reviewCompleted,
                        PerformingUserId = performingUserId
                    }),
                cancellationToken)
            .ConfigureAwait(false);
    }

    protected async Task AuditAdminOfficerReviewUpdateFailureAsync(
        Guid applicationId,
        string? error,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        await AuditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAdminOfficerReviewFailure,
                    applicationId,
                    performingUserId,
                    RequestContext,
                    new
                    {
                        Error = error,
                        PerformingUserId = performingUserId
                    }), 
                cancellationToken)
            .ConfigureAwait(false);
    }

    protected void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model, string currentTask, bool isChildTask = false) 

    {
        var breadCrumbs = new List<BreadCrumb>
        {
            new("Open applications", "Home", "Index", null),
            new(model.FellingLicenceApplicationSummary?.ApplicationReference!, "FellingLicenceApplication",
                "ApplicationSummary", model.FellingLicenceApplicationSummary?.Id.ToString()),
        };

        if (isChildTask)
        {
            breadCrumbs.Add(new("Operations Admin Officer Review", "AdminOfficerReview", nameof(AdminOfficerReviewController.Index), model.FellingLicenceApplicationSummary?.Id.ToString()));
        }

        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = breadCrumbs,
            CurrentPage = currentTask
        };
    }
}
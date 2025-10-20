using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Extensions;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

/// <summary>
/// Use case class for handling operations related to the designations task in the woodland officer review.
/// </summary>
public class DesignationsUseCase : FellingLicenceApplicationUseCaseBase, IDesignationsUseCase
{
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly IUpdateWoodlandOfficerReviewService _updateWoodlandOfficerReviewService;
    private readonly RequestContext _requestContext;
    private readonly IAuditService<DesignationsUseCase> _auditService;
    private readonly ILogger<DesignationsUseCase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DesignationsUseCase"/> class.
    /// </summary>
    /// <param name="internalUserAccountService">A <see cref="IUserAccountService"/> to deal with internal user accounts.</param>
    /// <param name="externalUserAccountService">A <see cref="IRetrieveUserAccountsService"/> to retrieve external user accounts.</param>
    /// <param name="fellingLicenceApplicationInternalRepository">A <see cref="IFellingLicenceApplicationInternalRepository"/> to retrieve applications from the repository.</param>
    /// <param name="woodlandOwnerService">A <see cref="IRetrieveWoodlandOwners"/> to retrieve woodland owner details.</param>
    /// <param name="getWoodlandOfficerReviewService">A <see cref="IGetWoodlandOfficerReviewService"/> to retrieve woodland officer review details.</param>
    /// <param name="updateWoodlandOfficerReviewService">A <see cref="IUpdateWoodlandOfficerReviewService"/> to update woodland officer review details.</param>
    /// <param name="agentAuthorityService">A <see cref="IAgentAuthorityService"/> to retrieve AAF details.</param>
    /// <param name="getConfiguredFcAreasService">A <see cref="IGetConfiguredFcAreas"/> to get FC area config.</param>
    /// <param name="auditService">A <see cref="IAuditService{T}"/> to raise audit events.</param>
    /// <param name="requestContext">The <see cref="RequestContext"/> for the current operation.</param>
    /// <param name="logger">A <see cref="ILogger{T}"/> logging implementation.</param>
    public DesignationsUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
        IUpdateWoodlandOfficerReviewService updateWoodlandOfficerReviewService,
        IAgentAuthorityService agentAuthorityService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        IAuditService<DesignationsUseCase> auditService,
        RequestContext requestContext,
        IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
        ILogger<DesignationsUseCase> logger)
        : base(internalUserAccountService,
            externalUserAccountService,
            fellingLicenceApplicationInternalRepository,
            woodlandOwnerService,
            agentAuthorityService,
            getConfiguredFcAreasService, 
            woodlandOfficerReviewSubStatusService)
    {
        ArgumentNullException.ThrowIfNull(getWoodlandOfficerReviewService);
        ArgumentNullException.ThrowIfNull(updateWoodlandOfficerReviewService);
        ArgumentNullException.ThrowIfNull(requestContext);

        _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
        _updateWoodlandOfficerReviewService = Guard.Against.Null(updateWoodlandOfficerReviewService);
        _requestContext = Guard.Against.Null(requestContext);
        _auditService = Guard.Against.Null(auditService);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<DesignationsViewModel>> GetApplicationDesignationsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve application compartment designations for application {ApplicationId}", applicationId);

        var (_, getSummaryFailure, summaryModel, getSummaryError) = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (getSummaryFailure)
        {
            _logger.LogError("Failed to retrieve application summary for designations with error {Error}", getSummaryError);
            return Result.Failure<DesignationsViewModel>(getSummaryError);
        }

        var (_, getDesignationsFailure, designationsModel, getDesignationsError) =
            await _getWoodlandOfficerReviewService.GetCompartmentDesignationsAsync(applicationId, cancellationToken);

        if (getDesignationsFailure)
        {
            _logger.LogError("Failed to retrieve designations with error {Error}", getDesignationsError);
            return Result.Failure<DesignationsViewModel>(getDesignationsError);
        }

        // order the designations by compartment name for the UI
        designationsModel.CompartmentDesignations =
            designationsModel.CompartmentDesignations.OrderByNameNumericOrAlpha().ToList();

        if (getDesignationsFailure)
        {
            _logger.LogError("Failed to retrieve designations with error {Error}", getDesignationsError);
            return Result.Failure<DesignationsViewModel>(getDesignationsError);
        }

        var result = new DesignationsViewModel
        {
            ApplicationId = applicationId,
            FellingLicenceApplicationSummary = summaryModel,
            CompartmentDesignations = designationsModel
        };

        SetBreadcrumbs(result);

        return Result.Success(result);
    }

    /// <inheritdoc/>
    public async Task<Result<UpdateDesignationsViewModel>> GetUpdateDesignationsModelAsync(
        Guid applicationId,
        Guid submittedCompartmentId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve application compartment designations for application {ApplicationId} and compartment {CompartmentId}", 
            applicationId, submittedCompartmentId);

        var (_, getSummaryFailure, summaryModel, getSummaryError) = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (getSummaryFailure)
        {
            _logger.LogError("Failed to retrieve application summary for designations with error {Error}", getSummaryError);
            return Result.Failure<UpdateDesignationsViewModel>(getSummaryError);
        }

        var (_, getDesignationsFailure, designationsModel, getDesignationsError) =
            await _getWoodlandOfficerReviewService.GetCompartmentDesignationsAsync(applicationId, cancellationToken);

        if (getDesignationsFailure)
        {
            _logger.LogError("Failed to retrieve designations with error {Error}", getDesignationsError);
            return Result.Failure<UpdateDesignationsViewModel>(getDesignationsError);
        }

        var compartmentDesignations = designationsModel.CompartmentDesignations
            .FirstOrDefault(cd => cd.SubmittedFlaCompartmentId == submittedCompartmentId);

        if (compartmentDesignations == null)
        {
            _logger.LogError("Failed to find designations for compartment id {CompartmentId}", submittedCompartmentId);
            return Result.Failure<UpdateDesignationsViewModel>("Failed to find designations for the specified compartment id");
        }

        // order the designations by compartment name in order to identify the next compartment
        var orderedList = designationsModel.CompartmentDesignations
            .OrderByNameNumericOrAlpha()
            .ToList();
        var currentIndex = orderedList.IndexOf(compartmentDesignations);
        var nextCompartmentId = currentIndex < orderedList.Count - 1
            ? orderedList[currentIndex + 1].SubmittedFlaCompartmentId
            : (Guid?)null;

        var result = new UpdateDesignationsViewModel
        {
            ApplicationId = applicationId,
            FellingLicenceApplicationSummary = summaryModel,
            CompartmentDesignations = compartmentDesignations,
            NextCompartmentId = nextCompartmentId,
            TotalCompartments = designationsModel.CompartmentDesignations.Count,
            CompartmentsReviewed = designationsModel.CompartmentDesignations.Count(cd => cd.HasCompletedDesignations)
        };

        SetBreadcrumbs(result, $"Designations - {compartmentDesignations.CompartmentName}");
        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateCompartmentDesignationsAsync(
        Guid applicationId,
        SubmittedCompartmentDesignationsModel model,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to update compartment designations for application {ApplicationId} and compartment {CompartmentId} by user {UserId}",
            applicationId, model.SubmittedFlaCompartmentId, user.UserAccountId);

        var result = await _updateWoodlandOfficerReviewService.UpdateCompartmentDesignationsAsync(
            applicationId,
            user.UserAccountId!.Value,
            model,
            cancellationToken);

        if (result.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateDesignations,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        CompartmentId = model.SubmittedFlaCompartmentId,
                    }),
                cancellationToken);

            return result;
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateDesignationsFailure,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    CompartmentId = model.SubmittedFlaCompartmentId,
                    Error = result.Error
                }),
            cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> UpdateCompartmentDesignationsCompletionAsync(
        Guid applicationId,
        InternalUser user,
        bool isComplete,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to update compartment designations completion status to {IsComplete} for application {ApplicationId} by user {UserId}",
            isComplete, applicationId, user.UserAccountId);

        var result = await _updateWoodlandOfficerReviewService.UpdateApplicationCompartmentDesignationsCompletedAsync(
            applicationId,
            user.UserAccountId!.Value,
            isComplete,
            cancellationToken);

        if (result.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateWoodlandOfficerReview,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        Section = "Compartment designations",
                    }),
                cancellationToken);

            return result;
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.UpdateWoodlandOfficerReviewFailure,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    Section = "Compartment designations",
                    Error = result.Error
                }),
            cancellationToken);

        return result;
    }

    private void SetBreadcrumbs(FellingLicenceApplicationPageViewModel model, string currentPage = "Designations")
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
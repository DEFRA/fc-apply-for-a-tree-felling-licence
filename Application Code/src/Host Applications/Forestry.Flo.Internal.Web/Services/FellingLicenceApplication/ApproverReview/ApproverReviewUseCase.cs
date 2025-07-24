using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using ActivityFeedItemType = Forestry.Flo.Services.Common.Models.ActivityFeedItemType;
using FellingLicenceStatus = Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Use case for handling approver reviews of felling licence applications.
/// </summary>
public class ApproverReviewUseCase : FellingLicenceApplicationUseCaseBase
{
    private readonly IAgentAuthorityInternalService _agentAuthorityInternalService;
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly IPropertyProfileRepository _propertyProfileRepository;
    private readonly ILogger<FellingLicenceApplicationUseCase> _logger;
    private readonly Flo.Services.InternalUsers.Repositories.IUserAccountRepository _userAccountRepository;
    private readonly IActivityFeedItemProvider _activityFeedItemProvider;
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly IApproverReviewService _approverReviewService;
    private readonly IAuditService<FellingLicenceApplicationUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly FellingLicenceApplicationOptions _fellingLicenceApplicationOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApproverReviewUseCase"/> class.
    /// </summary>
    /// <param name="internalUserAccountService">The internal user account service.</param>
    /// <param name="externalUserAccountService">The external user account service.</param>
    /// <param name="fellingLicenceApplicationInternalRepository">The felling licence application internal repository.</param>
    /// <param name="propertyProfileRepository">The property profile repository.</param>
    /// <param name="woodlandOwnerService">The woodland owner service.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="userAccountRepository">The user account repository.</param>
    /// <param name="activityFeedItemProvider">The activity feed item provider.</param>
    /// <param name="agentAuthorityService">The agent authority service.</param>
    /// <param name="agentAuthorityInternalService">The agent authority internal service.</param>
    /// <param name="getWoodlandOfficerReviewService">The get woodland officer review service.</param>
    /// <param name="approverReviewService">The approver review service.</param>
    /// <param name="requestContext">The request context.</param>
    /// <param name="logger">The logger.</param>
    public ApproverReviewUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IPropertyProfileRepository propertyProfileRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IAuditService<FellingLicenceApplicationUseCase> auditService,
        Flo.Services.InternalUsers.Repositories.IUserAccountRepository userAccountRepository,
        IActivityFeedItemProvider activityFeedItemProvider,
        IAgentAuthorityService agentAuthorityService,
        IAgentAuthorityInternalService agentAuthorityInternalService,
        IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
        IApproverReviewService approverReviewService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        RequestContext requestContext,
        IOptions<FellingLicenceApplicationOptions> fellingLicenceApplicationOptions,
        ILogger<FellingLicenceApplicationUseCase> logger)
        : base(internalUserAccountService,
            externalUserAccountService,
            fellingLicenceApplicationInternalRepository,
            woodlandOwnerService,
            agentAuthorityService,
            getConfiguredFcAreasService)
    {
        _agentAuthorityInternalService = Guard.Against.Null(agentAuthorityInternalService);
        Guard.Against.Null(internalUserAccountService);
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _propertyProfileRepository = Guard.Against.Null(propertyProfileRepository);
        _logger = Guard.Against.Null(logger);
        _userAccountRepository = Guard.Against.Null(userAccountRepository);
        _activityFeedItemProvider = Guard.Against.Null(activityFeedItemProvider);
        _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
        _approverReviewService = Guard.Against.Null(approverReviewService);
        _fellingLicenceApplicationOptions = fellingLicenceApplicationOptions.Value;
    }

    /// <summary>
    /// Gets the post-submitted statuses.
    /// </summary>
    public static List<FellingLicenceStatus> PostSubmittedStatuses => new()
    {
        FellingLicenceStatus.Submitted,
        FellingLicenceStatus.AdminOfficerReview,
        FellingLicenceStatus.WithApplicant,
        FellingLicenceStatus.WoodlandOfficerReview,
        FellingLicenceStatus.SentForApproval,
        FellingLicenceStatus.Approved,
        FellingLicenceStatus.Refused,
        FellingLicenceStatus.Withdrawn,
        FellingLicenceStatus.ReturnedToApplicant,
        FellingLicenceStatus.ReferredToLocalAuthority
    };

    /// <summary>
    /// Creates and returns a felling licence application review summary model from an application received by the application id.
    /// </summary>
    /// <param name="applicationId">The application Id.</param>
    /// <param name="viewingUser">The logged in internal user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Maybe{ApproverReviewSummaryModel}"/>.</returns>
    public async Task<Maybe<ApproverReviewSummaryModel>> RetrieveApproverReviewAsync(
        Guid applicationId,
        InternalUser viewingUser,
        CancellationToken cancellationToken)
    {
        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (!application.HasValue)
        {
            _logger.LogError("Felling licence application not found, application id: {ApplicationId}", applicationId);
            return Maybe<ApproverReviewSummaryModel>.None;
        }

        var woodlandOwner =
            await WoodlandOwnerService.RetrieveWoodlandOwnerByIdAsync(application.Value.WoodlandOwnerId, cancellationToken);
        if (woodlandOwner.IsFailure)
        {
            _logger.LogError("Application woodland owner not found, application id: {ApplicationId}, woodland owner id: {WoodlandOwnerId}, error: {Error}",
                applicationId, application.Value.WoodlandOwnerId, woodlandOwner.Error);
            return Maybe<ApproverReviewSummaryModel>.None;
        }

        var agencyForWoodlandOwner =
            await AgentAuthorityService.GetAgencyForWoodlandOwnerAsync(woodlandOwner.Value.Id!.Value, cancellationToken);

        AgentAuthorityFormViewModel? agentAuthorityForm = null;
        if (agencyForWoodlandOwner.HasValue)
        {
            agentAuthorityForm = new AgentAuthorityFormViewModel();

            var request = new GetAgentAuthorityFormRequest
            {
                AgencyId = agencyForWoodlandOwner.Value.AgencyId.Value,
                PointInTime = application.Value.StatusHistories
                .OrderByDescending(x => x.Created)
                .FirstOrDefault(x => x.Status == FellingLicenceStatus.Submitted)?.Created,
                WoodlandOwnerId = woodlandOwner.Value.Id!.Value
            };

            var getAgentAuthorityFormResult =
                await _agentAuthorityInternalService.GetAgentAuthorityFormAsync(request, cancellationToken);
            if (getAgentAuthorityFormResult.IsSuccess)
            {
                agentAuthorityForm.AgentAuthorityId = getAgentAuthorityFormResult.Value.AgentAuthorityId;
                agentAuthorityForm.SpecificTimestampAgentAuthorityForm =
                    getAgentAuthorityFormResult.Value.SpecificTimestampAgentAuthorityForm;
                agentAuthorityForm.CurrentAgentAuthorityForm =
                    getAgentAuthorityFormResult.Value.CurrentAgentAuthorityForm;
                agentAuthorityForm.CouldRetrieveAgentAuthorityFormDetails = true;
            }
        }

        var providerModel = new ActivityFeedItemProviderModel()
        {
            FellingLicenceId = applicationId,
            FellingLicenceReference = application.Value.ApplicationReference,
            ItemTypes = Enum.GetValues(typeof(ActivityFeedItemType)).Cast<ActivityFeedItemType>().ToArray(),
        };

        var activityFeedNotes = await _activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            cancellationToken);

        if (activityFeedNotes.IsFailure)
        {
            _logger.LogError("Unable to retrieve activity feed items for FLA {id}, error: {Error}",
               applicationId, woodlandOwner.Error);
            return Maybe<ApproverReviewSummaryModel>.None;
        }
        var approverReview = await _approverReviewService.GetApproverReviewAsync(applicationId, cancellationToken);

        var applicationReviewModel = new ApproverReviewSummaryModel
        {
            Documents = ModelMapping.ToDocumentModelList(application.Value.Documents?
                    .Where(x => x.DeletionTimestamp is null)
                    .ToList())
                .OrderByDescending(x => x.CreatedTimestamp),

            Id = application.Value.Id,
            ApplicationDocument =
                application.Value.Documents?.FirstOrDefault(d => d.Purpose == DocumentPurpose.ApplicationDocument),
            ActivityFeedItems = activityFeedNotes.Value,
            ApplicationOwner = new ApplicationOwnerModel
            {
                WoodlandOwner = ModelMapping.ToWoodlandOwnerModel(woodlandOwner.Value),
                Agency = agencyForWoodlandOwner.HasNoValue ? null : agencyForWoodlandOwner.Value,
                AgentAuthorityForm = agentAuthorityForm
            },
            OperationDetailsModel = CreateOperationDetailsModel(application.Value),
            ViewingUser = viewingUser,
            IsReadonly = false,
            ApproverReview = approverReview.HasValue ? approverReview.Value : new ApproverReviewModel()
        };

        var summary = await ExtractApplicationSummaryAsync(application.Value, cancellationToken);
        if (summary.IsFailure)
        {
            _logger.LogError("Application summary cannot be extracted, application id: {ApplicationId}, error {Error}", application.Value.Id, summary.Error);
            return Maybe<ApproverReviewSummaryModel>.None;
        }
        applicationReviewModel.FellingLicenceApplicationSummary = summary.Value;

        var creator = await GetSubmittingUserAsync(application.Value.CreatedById, cancellationToken);
        if (creator.IsFailure)
        {
            _logger.LogError("Unable to retrieve the details of the external user who submitted the application, application id: {ApplicationId}, external user: {createdById} , error {Error}", application.Value.Id, application.Value.CreatedById, creator.Error);
            return Maybe<ApproverReviewSummaryModel>.None;
        }

        var woodlandOfficerReview = await _getWoodlandOfficerReviewService.GetWoodlandOfficerReviewStatusAsync(application.Value.Id,
        cancellationToken);

        applicationReviewModel.IsWOReviewed = woodlandOfficerReview.Value.RecommendedLicenceDuration != null;

        var defaultRecommendedLicenceDuration = Enum.IsDefined(typeof(RecommendedLicenceDuration), _fellingLicenceApplicationOptions.DefaultLicenseDuration)
            ? (RecommendedLicenceDuration)_fellingLicenceApplicationOptions.DefaultLicenseDuration
            : RecommendedLicenceDuration.None; // or another fallback if None is not appropriate
        applicationReviewModel.RecommendedLicenceDuration = woodlandOfficerReview.Value.RecommendedLicenceDuration ?? defaultRecommendedLicenceDuration;

        applicationReviewModel.RecommendedLicenceDurations = Enum.GetValues(typeof(RecommendedLicenceDuration))
            .Cast<RecommendedLicenceDuration>()
            .Where(e => (int)e != 0) // Exclude the "No recommendation" value
            .Select(e => new SelectListItem
            {
                Value = ((int)e).ToString(),
                Text = e == woodlandOfficerReview.Value.RecommendedLicenceDuration ? 
                    $"{e.GetDisplayName()} (recommended by Woodland Officer)" : 
                    e == defaultRecommendedLicenceDuration ? 
                        $"{e.GetDisplayName()} (default)" : 
                        e.GetDisplayName()
            }).ToList();
        
        applicationReviewModel.ApproverReview.ApprovedLicenceDuration = applicationReviewModel.ApproverReview.ApprovedLicenceDuration 
            ?? woodlandOfficerReview.Value.RecommendedLicenceDuration
            ?? defaultRecommendedLicenceDuration;

        applicationReviewModel.IsReadonly = !(
            applicationReviewModel.FellingLicenceApplicationSummary!.StatusHistories.MaxBy(x => x.Created)?.Status is FellingLicenceStatus.SentForApproval
            && applicationReviewModel.FellingLicenceApplicationSummary.AssigneeHistories.Any(x =>
                x.Role is AssignedUserRole.FieldManager
                && x.UserAccount?.Id == applicationReviewModel.ViewingUser?.UserAccountId
                && x.TimestampUnassigned == null)
            && viewingUser.CanApproveApplications
            && creator.Value.Email != viewingUser.EmailAddress
            );

        return Maybe<ApproverReviewSummaryModel>.From(applicationReviewModel);
    }

    /// <summary>
    /// Saves the approver review asynchronously.
    /// </summary>
    /// <param name="model">The approver review model.</param>
    /// <param name="user">The internal user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Result"/>.</returns>
    public async Task<Result> SaveApproverReviewAsync(
        ApproverReviewModel model,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to store updated PW14 checks details for application with id {ApplicationId}", model.ApplicationId);

        var updateResult = await _approverReviewService.SaveApproverReviewAsync(
            model.ApplicationId,
            model,
            user.UserAccountId!.Value,
            cancellationToken);

        if (updateResult.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.SaveApproverReview,
                    model.ApplicationId,
                    user.UserAccountId,
                    _requestContext,
                    new { Section = "Approver Review" }),
                cancellationToken);

            return Result.Success();
        }

        _logger.LogError("Failed to update application ApproverReview with error {Error}", updateResult.Error);

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.SaveApproverReviewFailure,
                model.ApplicationId,
                user.UserAccountId,
                _requestContext,
                new { updateResult.Error }),
            cancellationToken);

        return Result.Failure(updateResult.Error);
    }

    /// <summary>
    /// Creates the operation details model.
    /// </summary>
    /// <param name="application">The felling licence application.</param>
    /// <returns>An instance of <see cref="OperationDetailsModel"/>.</returns>
    private static OperationDetailsModel CreateOperationDetailsModel(Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication application)
    {
        return new OperationDetailsModel
        {
            ApplicationId = application.Id,
            ApplicationReference = application.ApplicationReference,
            ProposedFellingStart = application.ProposedFellingStart != null
                ? new DatePart(application.ProposedFellingStart.Value.ToLocalTime(), "felling-start")
                : null,
            ProposedFellingEnd = application.ProposedFellingEnd != null
                ? new DatePart(application.ProposedFellingEnd.Value.ToLocalTime(), "felling-end")
                : null,
            Measures = application.Measures
        };
    }
}


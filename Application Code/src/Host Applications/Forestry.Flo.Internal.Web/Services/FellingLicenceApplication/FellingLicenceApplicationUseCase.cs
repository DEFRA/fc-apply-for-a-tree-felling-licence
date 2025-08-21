using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using ActivityFeedItemType = Forestry.Flo.Services.Common.Models.ActivityFeedItemType;
using FellingLicenceStatus = Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus;
using AutoMapper;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

public class FellingLicenceApplicationUseCase : FellingLicenceApplicationUseCaseBase
{
    private readonly IAgentAuthorityInternalService _agentAuthorityInternalService;
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly IPropertyProfileRepository _propertyProfileRepository;
    private readonly ILogger<FellingLicenceApplicationUseCase> _logger;
    private readonly Forestry.Flo.Services.InternalUsers.Repositories.IUserAccountRepository _userAccountRepository;
    private readonly IActivityFeedItemProvider _activityFeedItemProvider;
    protected readonly IAuditService<ExtendApplicationsUseCase> _auditExtendApplications;

    public FellingLicenceApplicationUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IPropertyProfileRepository propertyProfileRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IAuditService<ExtendApplicationsUseCase> auditExtendApplications,
        Forestry.Flo.Services.InternalUsers.Repositories.IUserAccountRepository userAccountRepository,
        IActivityFeedItemProvider activityFeedItemProvider,
        IAgentAuthorityService agentAuthorityService,
        IAgentAuthorityInternalService agentAuthorityInternalService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
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
        Guard.Against.Null(auditExtendApplications);
        _propertyProfileRepository = Guard.Against.Null(propertyProfileRepository);
        _logger = Guard.Against.Null(logger);
        _userAccountRepository = Guard.Against.Null(userAccountRepository);
        _activityFeedItemProvider = Guard.Against.Null(activityFeedItemProvider);
        _auditExtendApplications = Guard.Against.Null(auditExtendApplications);
    }

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
    /// Retrieves a filtered collection of applications to display on the dashboard.
    /// </summary>
    /// <param name="assignedToUserAccountIdOnly">A flag indicating whether only applications assigned to the logged-in user should be retrieved.</param>
    /// <param name="assignedToUserAccountId">The identifier for the logged-in user account.</param>
    /// <param name="includeFellingLicenceStatuses">A collection of <see cref="FellingLicenceStatus"/> to filter applications by.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="FellingLicenceApplicationAssignmentListModel"/> containing a filtered collection of applications, or an error if unsuccessful.</returns>
    public async Task<Result<FellingLicenceApplicationAssignmentListModel>> GetFellingLicenceApplicationAssignmentListModelAsync(
        bool assignedToUserAccountIdOnly,
        Guid assignedToUserAccountId,
        IList<FellingLicenceStatus> includeFellingLicenceStatuses,
        CancellationToken cancellationToken)
    {
        if (!includeFellingLicenceStatuses.Any())
        {
            // If no FellingLicenceStatus selected, allow all from post-submitted status list

            includeFellingLicenceStatuses = PostSubmittedStatuses;
        }
        else
        {
            // Calculate the intersection of the user requested statuses and list of post submitted status FellingLicenceStatus.
            // This is to prevent the request parameters from being tampered with to allow for other statuses

            includeFellingLicenceStatuses = includeFellingLicenceStatuses.Where(x => PostSubmittedStatuses.Contains(x)).ToList();
        }

        var fellingLicenceApplications = await _fellingLicenceApplicationInternalRepository.ListByIncludedStatus(assignedToUserAccountIdOnly, assignedToUserAccountId, includeFellingLicenceStatuses, cancellationToken);

        var linkedPropertyProfilesIdList = fellingLicenceApplications.Select(x => x.LinkedPropertyProfile.PropertyProfileId).ToList();

        var propertyProfiles = await _propertyProfileRepository.ListAsync(linkedPropertyProfilesIdList, cancellationToken);

        // Filter to last assignee histories (current assignees)

        var currentAssigneeHistories = fellingLicenceApplications.SelectMany(x => x.AssigneeHistories.Where(y => !y.TimestampUnassigned.HasValue));

        var briefAssigneeHistories = await base.GetBriefAssigneeHistory(currentAssigneeHistories, cancellationToken);

        IList<FellingLicenceAssignmentListItemModel> fellingLicenceAssignmentListItemModels =

            fellingLicenceApplications.Select(x => new FellingLicenceAssignmentListItemModel
            {
                AssignedUserIds = briefAssigneeHistories.Value.Where(y => y.FellingLicenceApplicationId == x.Id).DistinctBy(y => y.UserId).Select(y => y.UserId).ToList(),
                Reference = x.ApplicationReference,
                FellingLicenceApplicationId = x.Id,
                Deadline = x.FinalActionDate,
                FellingLicenceStatus = x.StatusHistories.OrderBy(y => y.Created).Last().Status,
                Property = propertyProfiles.SingleOrDefault(y => y.Id == x.LinkedPropertyProfile?.PropertyProfileId)?.Name,
                UserFirstLastNames = briefAssigneeHistories.Value.Where(y => y.FellingLicenceApplicationId == x.Id).DistinctBy(y => y.UserId).Select(y => y.UserName).ToList(),
                SubmittedDate = x.StatusHistories.Any(sh => sh.Status == FellingLicenceStatus.Submitted) ? x.StatusHistories.Where(sh => sh.Status == FellingLicenceStatus.Submitted).Max(x => x.Created) : null,
                CitizensCharterDate = x.CitizensCharterDate
            }).OrderBy(x => x.Deadline).ToList();

        var model = new FellingLicenceApplicationAssignmentListModel
        {
            AssignedToUserCount = fellingLicenceAssignmentListItemModels.Count(x => x.AssignedUserIds != null && x.AssignedUserIds.Contains(assignedToUserAccountId)),
            AssignedFellingLicenceApplicationModels = fellingLicenceAssignmentListItemModels,
            FellingLicenceStatusCount = fellingLicenceAssignmentListItemModels.GroupBy(x => x.FellingLicenceStatus)
                                                                              .Select(x => new FellingLicenceStatusCount
                                                                              {
                                                                                  FellingLicenceStatus = x.Key,
                                                                                  Count = x.Count()
                                                                              }).ToList()
        };

        return Result.Success(model);
    }

    /// <summary>
    /// Creates and returns a felling licence application review summary model from an application received by the application id
    /// </summary>
    /// <param name="applicationId">The application Id</param>
    /// <param name="viewingUser">The logged in internal user</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public async Task<Maybe<FellingLicenceApplicationReviewSummaryModel>>
        RetrieveFellingLicenceApplicationReviewSummaryAsync(
            Guid applicationId,
            InternalUser viewingUser,
            CancellationToken cancellationToken)
    {
        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (!application.HasValue)
        {
            _logger.LogError("Felling licence application not found, application id: {ApplicationId}", applicationId);
            return Maybe<FellingLicenceApplicationReviewSummaryModel>.None;
        }

        var woodlandOwner =
            await WoodlandOwnerService.RetrieveWoodlandOwnerByIdAsync(application.Value.WoodlandOwnerId, cancellationToken);
        if (woodlandOwner.IsFailure)
        {
            _logger.LogError("Application woodland owner not found, application id: {ApplicationId}, woodland owner id: {WoodlandOwnerId}, error: {Error}",
                applicationId, application.Value.WoodlandOwnerId, woodlandOwner.Error);
            return Maybe<FellingLicenceApplicationReviewSummaryModel>.None;
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
            return Maybe<FellingLicenceApplicationReviewSummaryModel>.None;
        }

        var applicationReviewModel = new FellingLicenceApplicationReviewSummaryModel
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
            FellingAndRestockingDetail = new FellingAndRestockingDetails
            {
                ApplicationId = application.Value.Id,
                ApplicationReference = application.Value.ApplicationReference,
                ConfirmedFellingAndRestockingCompleted = (application.Value.WoodlandOfficerReview != null && application.Value.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete),
                DetailsList = ModelMapping.RetrieveFellingAndRestockingDetails(application.Value).ToList()
            },
            ViewingUser = viewingUser,
            UserCanApproveRefuseReferApplication = false
        };

        var summary = await ExtractApplicationSummaryAsync(application.Value, cancellationToken);
        if (summary.IsFailure)
        {
            _logger.LogError("Application summary cannot be extracted, application id: {ApplicationId}, error {Error}", application.Value.Id, summary.Error);
            return Maybe<FellingLicenceApplicationReviewSummaryModel>.None;
        }
        applicationReviewModel.FellingLicenceApplicationSummary = summary.Value;

        var creator = await GetSubmittingUserAsync(application.Value.CreatedById, cancellationToken);
        if (creator.IsFailure)
        {
            _logger.LogError("Unable to retrieve the details of the external user who submitted the application, application id: {ApplicationId}, external user: {createdById} , error {Error}", application.Value.Id, application.Value.CreatedById, creator.Error);
            return Maybe<FellingLicenceApplicationReviewSummaryModel>.None;
        }

        applicationReviewModel.UserCanApproveRefuseReferApplication =
            applicationReviewModel.FellingLicenceApplicationSummary!.StatusHistories.MaxBy(x => x.Created)?.Status is FellingLicenceStatus.SentForApproval
            && applicationReviewModel.FellingLicenceApplicationSummary.AssigneeHistories.Any(x =>
                x.Role is AssignedUserRole.FieldManager
                && x.UserAccount?.Id == applicationReviewModel.ViewingUser?.UserAccountId)
            && applicationReviewModel.IsEditable
            && viewingUser.CanApproveApplications
            && creator.Value.Email != viewingUser.EmailAddress;

        //var firstApplicationExtension = await _auditExtendApplications.GetFirstEventAuditDataAsync(
        //    AuditEvents.ApplicationExtensionNotification, applicationId, cancellationToken) as dynamic;
        //DateTime? previousActionDate = firstApplicationExtension?.GetProperty("PreviousActionDate")?.GetDateTime();

        //applicationReviewModel.FellingLicenceApplicationSummary.OriginalActionDate = previousActionDate;
        return Maybe<FellingLicenceApplicationReviewSummaryModel>.From(applicationReviewModel);
    }


    /// <summary>
    /// Retrieves the model required to reopen a withdrawn felling licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application to be reopened.</param>
    /// <param name="hostingPage">The page to return to after adding an activity feed comment.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a <see cref="ReopenWithdrawnApplicationModel"/> if successful, 
    /// or an error message if the operation fails.
    /// </returns>
    public async Task<Result<ReopenWithdrawnApplicationModel>> RetrieveReopenWithdrawnApplicationModelAsync(
        Guid applicationId,
        string hostingPage,
        CancellationToken cancellationToken)
    {
        var application = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);

        if (application.IsFailure)
        {
            _logger.LogError("Unable to retrieve application details, application id: {ApplicationId}, error: {Error}", applicationId, application.Error);
            return Result.Failure<ReopenWithdrawnApplicationModel>(application.Error);
        }

        if (application.Value.Status is not FellingLicenceStatus.Withdrawn)
        {
            _logger.LogError("Application {id} is not in a withdrawn state, current status: {status}", applicationId, application.Value.Status);
            return Result.Failure<ReopenWithdrawnApplicationModel>("Application is not in a withdrawn state");
        }

        var providerModel = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            FellingLicenceReference = application.Value.ApplicationReference,
            ItemTypes = Enum.GetValues(typeof(ActivityFeedItemType)).Cast<ActivityFeedItemType>().ToArray(),
        };

        var (_, isFailure, activityFeedItems, error) = await _activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Unable to retrieve activity feed items for FLA {id}, error: {Error}", applicationId, error);
            return Result.Failure<ReopenWithdrawnApplicationModel>(error);
        }
        return new ReopenWithdrawnApplicationModel
        {
            ApplicationId = applicationId,
            FellingLicenceApplicationSummary = application.Value,
            ActivityFeed = new ActivityFeedModel
            {
                ApplicationId = applicationId,
                NewCaseNoteType = CaseNoteType.CaseNote,
                ActivityFeedItemModels = activityFeedItems,
                HostingPage = hostingPage,
                ShowFilters = false
            },
        };
    }

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

    private async Task<Result<Dictionary<Guid, string>>> DetermineUsernamesFromUserIds(List<Guid> userIds, CancellationToken cancellation)
    {
        Dictionary<Guid, string> usernameDict = new();
        var userAccounts = await _userAccountRepository.GetUsersWithIdsInAsync(userIds, cancellation);

        if (userAccounts.IsFailure)
        {
            _logger.LogError("Unable to find all internal accounts to determine usernames");
            return Result.Failure<Dictionary<Guid, string>>("Unable to find all internal accounts to determine usernames");
        }

        foreach (var user in userAccounts.Value) if (!usernameDict.ContainsKey(user.Id)) usernameDict.Add(user.Id, user.FullName());
        return Result.Success<Dictionary<Guid, string>>(usernameDict);
    }
}


using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Controllers;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.AgentAuthorityForm;
using Forestry.Flo.External.Web.Models.Compartment;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.TenYearLicenceApplications;
using Forestry.Flo.External.Web.Services.MassTransit.Messages;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.PropertyProfiles;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Services.PropertyProfiles.Services;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NodaTime;
using AssignedUserRole = Forestry.Flo.Services.FellingLicenceApplications.Entities.AssignedUserRole;
using FellingLicenceApplicationStepStatus =
    Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplicationStepStatus;
using IUserAccountRepository = Forestry.Flo.Services.InternalUsers.Repositories.IUserAccountRepository;
using PropertyProfileDetails = Forestry.Flo.External.Web.Models.FellingLicenceApplication.PropertyProfileDetails;
using ProposedFellingDetailModel = Forestry.Flo.External.Web.Models.FellingLicenceApplication.ProposedFellingDetailModel;
using ProposedRestockingDetailModel = Forestry.Flo.External.Web.Models.FellingLicenceApplication.ProposedRestockingDetailModel;

namespace Forestry.Flo.External.Web.Services;

public class CreateFellingLicenceApplicationUseCase(
    IRetrieveUserAccountsService retrieveUserAccountsService,
    IFellingLicenceApplicationExternalRepository fellingLicenceApplicationExternalRepository,
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationService,
    IGetCompartments getCompartmentsService,
    ICompartmentRepository compartmentRepository,
    IUserAccountRepository internalUserAccountRepository,
    IAuditService<CreateFellingLicenceApplicationUseCase> auditService,
    ILogger<CreateFellingLicenceApplicationUseCase> logger,
    ISendNotifications sendNotifications,
    IClock clock,
    RequestContext requestContext,
    IActivityFeedItemProvider activityFeedService,
    IOptions<FellingLicenceApplicationOptions> fellingLicenceApplicationOptions,
    IWithdrawFellingLicenceService withdrawFellingLicenceService,
    IDeleteFellingLicenceService deleteFellingLicenceService,
    IWoodlandOwnerRepository woodlandOwnerRepository,
    IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
    IGetPropertyProfiles getPropertyProfilesService,
    IUpdateFellingLicenceApplicationForExternalUsers updateFellingLicenceApplicationService,
    IAgentAuthorityService agentAuthorityService,
    IBus busControl,
    IForesterServices foresterServices,
    IApplicationReferenceHelper applicationHelper,
    IPublicRegister publicRegisterService,
    IOptions<EiaOptions> eiaOptions,
    IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
    IOptions<InternalUserSiteOptions> internalUserSiteOptions,
    IGetConfiguredFcAreas getConfiguredFcAreasService) 
    : ApplicationUseCaseCommon(retrieveUserAccountsService,
        retrieveWoodlandOwnersService,
        getFellingLicenceApplicationService,
        getPropertyProfilesService,
        getCompartmentsService,
        agentAuthorityService,
        logger)
{
    private readonly IBus _busControl = Guard.Against.Null(busControl);

    private readonly IUpdateFellingLicenceApplicationForExternalUsers _updateFellingLicenceApplicationService =
        Guard.Against.Null(updateFellingLicenceApplicationService);

    private readonly IUserAccountRepository _internalUserAccountRepository =
        Guard.Against.Null(internalUserAccountRepository);

    private readonly ISendNotifications _sendNotifications = Guard.Against.Null(sendNotifications);

    private readonly FellingLicenceApplicationOptions _fellingLicenceApplicationOptions =
        Guard.Against.Null(fellingLicenceApplicationOptions.Value);

    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);

    private readonly IAuditService<CreateFellingLicenceApplicationUseCase> _auditService =
        Guard.Against.Null(auditService);

    private readonly ILogger<CreateFellingLicenceApplicationUseCase> _logger = Guard.Against.Null(logger);
    private readonly IClock _clock = Guard.Against.Null(clock);

    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationRepository =
        Guard.Against.Null(fellingLicenceApplicationExternalRepository);

    private readonly ICompartmentRepository _compartmentRepository = Guard.Against.Null(compartmentRepository);
    private readonly IActivityFeedItemProvider _activityFeedService = Guard.Against.Null(activityFeedService);

    private readonly IWithdrawFellingLicenceService _withdrawFellingLicenceService =
        Guard.Against.Null(withdrawFellingLicenceService);

    private readonly IWoodlandOwnerRepository _woodlandOwnerRepository = Guard.Against.Null(woodlandOwnerRepository);

    private readonly IDeleteFellingLicenceService _deleteFellingLicenceService =
        Guard.Against.Null(deleteFellingLicenceService);

    private readonly IForesterServices _foresterServices = Guard.Against.Null(foresterServices);
    private readonly IApplicationReferenceHelper _applicationReferenceHelper = Guard.Against.Null(applicationHelper);
    private readonly IPublicRegister _publicRegisterService = Guard.Against.Null(publicRegisterService);
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);

    private readonly InternalUserSiteOptions _internalUserSiteOptions =
        Guard.Against.Null(internalUserSiteOptions?.Value);

    /// <summary>
    /// Returns a woodland owner application list
    /// </summary>
    /// <param name="woodlandOwnerId"></param>
    /// <param name="user">An external user reference</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A list of woodland owner application summary items</returns>
    public async Task<Result<IEnumerable<FellingLicenceApplicationSummary>>> GetWoodlandOwnerApplicationsAsync(
        Guid woodlandOwnerId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var userAccessModel = await GetUserAccessModelAsync(user, cancellationToken).ConfigureAwait(false);
        if (userAccessModel.IsFailure)
        {
            _logger.LogWarning("Unable to retrieve user access for user with id {UserId}", user.UserAccountId);
            return Result.Failure<IEnumerable<FellingLicenceApplicationSummary>>(
                $"Unable to verify user access to woodland owner with id {woodlandOwnerId}");
        }

        var applications = await GetFellingLicenceApplicationServiceForExternalUsers
            .GetApplicationsForWoodlandOwnerAsync(
                woodlandOwnerId, userAccessModel.Value, cancellationToken).ConfigureAwait(false);

        if (applications.IsFailure)
        {
            _logger.LogWarning("Unable to retrieve applications for user with id {UserId}, " +
                               "for the woodland owner id {WoodlandOwnerId}", user.UserAccountId, woodlandOwnerId);
            return Result.Failure<IEnumerable<FellingLicenceApplicationSummary>>(
                $"Unable to access data for woodland owner with id {woodlandOwnerId}");
        }

        if (applications.Value.Any())
        {
            var getPropertyProfilesResult = await GetPropertyProfilesByIdAsync(
                new ListPropertyProfilesQuery(woodlandOwnerId,
                    applications.Value.Where(a => a.LinkedPropertyProfile != null)
                        .Select(a => a.LinkedPropertyProfile!.PropertyProfileId).ToArray()),
                user,
                cancellationToken);

            if (getPropertyProfilesResult.IsFailure)
            {
                _logger.LogWarning("Unable to retrieve property profiles for user having id of {UserId}, " +
                                   "for the Woodland Owner Id supplied of {WoodlandOwnerId}", user.UserAccountId,
                    woodlandOwnerId);

                return Result.Failure<IEnumerable<FellingLicenceApplicationSummary>>(
                    $"Unable to access properties for user with userId of {user.UserAccountId} and woodland owner id supplied of {woodlandOwnerId}");
            }

            if (getPropertyProfilesResult.Value.Any())
            {
                var propertyProfilesDictionary = getPropertyProfilesResult.Value.ToDictionary(p => p.Id, p => p);

                var woodlandOwnerNameAndAgencyDetails =
                    await GetWoodlandOwnerNameAndAgencyForApplication(applications.Value.First(), cancellationToken);
                if (woodlandOwnerNameAndAgencyDetails.IsFailure)
                {
                    _logger.LogWarning("Unable to retrieve woodland owner name and agency details for user having id of {UserId}, " +
                                       "for the Woodland Owner Id supplied of {WoodlandOwnerId}", user.UserAccountId,
                        woodlandOwnerId);
                    return Result.Failure<IEnumerable<FellingLicenceApplicationSummary>>(
                        $"Unable to access woodland owner name and agency details for user with userId of {user.UserAccountId} and woodland owner id supplied of {woodlandOwnerId}");
                }

                var result = applications.Value.Select(a =>
                {
                    var match = propertyProfilesDictionary.TryGetValue(a.LinkedPropertyProfile!.PropertyProfileId,
                        out var profile);

                    return new FellingLicenceApplicationSummary(a.Id, a.ApplicationReference,
                        GetApplicationStatus(a.StatusHistories),
                        match
                            ? profile!.Name
                            : null,
                        a.LinkedPropertyProfile?.PropertyProfileId ?? Guid.Empty,
                        match
                            ? profile!.NameOfWood
                            : null,
                        a.WoodlandOwnerId,
                        woodlandOwnerNameAndAgencyDetails.Value.WoodlandOwnerName,
                        woodlandOwnerNameAndAgencyDetails.Value.AgencyName);
                });

                return Result.Success(result);
            }
        }

        return Result.Success(Enumerable.Empty<FellingLicenceApplicationSummary>());
    }

    /// <summary>
    /// Returns property profiles for a provided woodland owner, specified by woodland owner Id
    /// </summary>
    /// <param name="woodlandOwnerId">The Id of the woodland owner to retrieve properties for.</param>
    /// <param name="user">An application user</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result object containing a list of requested property profiles</returns>
    public async Task<Result<IEnumerable<PropertyProfileDetails>>> RetrievePropertyProfilesForWoodlandOwnerAsync(
        Guid woodlandOwnerId,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        var getPropertyProfilesResult =
            await GetPropertyProfilesByIdAsync(new ListPropertyProfilesQuery(woodlandOwnerId), user, cancellationToken);

        if (getPropertyProfilesResult.IsFailure)
        {
            _logger.LogWarning("Unable to retrieve property profiles for user having id of {UserId}, " +
                               "for the Woodland Owner Id of {WoodlandOwnerId}, error is {error}",
                user.UserAccountId, woodlandOwnerId, getPropertyProfilesResult.Error);

            return Result.Failure<IEnumerable<PropertyProfileDetails>>(
                $"Unable to access properties for user with userId of {user.UserAccountId} and woodland owner id supplied of {woodlandOwnerId}");
        }

        if (getPropertyProfilesResult.Value.Any())
        {
            var propertyModels = ModelMapping
                .ToPropertyProfileDetailsModelList(getPropertyProfilesResult.Value);

            return Result.Success(propertyModels);
        }

        return Result.Success(Enumerable.Empty<PropertyProfileDetails>());
    }

    /// <summary>
    /// Creates a felling licence application given a current user and a property profile id
    /// </summary>
    /// <param name="user">A current application user</param>
    /// <param name="propertyProfileId">A property profile id</param>
    /// <param name="woodlandOwnerId">The woodland owner's id which has the property where the application is to be created.</param>
    /// <param name="stepComplete"></param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result object containing the created application</returns>
    public async Task<Result<Guid, UserDbErrorReason>> CreateFellingLicenceApplication(
        ExternalApplicant user,
        Guid propertyProfileId,
        Guid woodlandOwnerId,
        bool? stepComplete,
        CancellationToken cancellationToken)
    {
        var application = new FellingLicenceApplication
        {
            WoodlandOwnerId = woodlandOwnerId,
            CreatedById = user.UserAccountId.GetValueOrDefault(),
            CreatedTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc(),
            LinkedPropertyProfile = new LinkedPropertyProfile
            {
                PropertyProfileId = propertyProfileId
            },
            FellingLicenceApplicationStepStatus = new FellingLicenceApplicationStepStatus
            {
                CompartmentFellingRestockingStatuses = new List<CompartmentFellingRestockingStatus>()
            }
        };
        application.StatusHistories.Add(new StatusHistory
        {
            Created = _clock.GetCurrentInstant().ToDateTimeUtc(),
            Status = Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus.Draft,
            CreatedById = user.UserAccountId
        });
        application.AssigneeHistories.Add(new AssigneeHistory()
        {
            Role = AssignedUserRole.Author,
            TimestampAssigned = _clock.GetCurrentInstant().ToDateTimeUtc(),
            AssignedUserId = user.UserAccountId.GetValueOrDefault()
        });

        application.FellingLicenceApplicationStepStatus.ApplicationDetailsStatus = stepComplete;

        var result = await _fellingLicenceApplicationRepository.CreateAndSaveAsync(application,
            _fellingLicenceApplicationOptions.PostFix, _fellingLicenceApplicationOptions.StartingOffset,
            cancellationToken);

        return await result.Map(app => app.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.CreateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { woodlandOwnerId }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.CreateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                    new { woodlandOwnerId, Error = r.GetDescription() }), cancellationToken);
                _logger.LogError(
                    "The application has not been saved due to the reason {ErrorReason} for  application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    public async Task<Result<Guid, UserDbErrorReason>> UpdateRestockingCompartmentsForFellingAsync(
        ExternalApplicant user,
        Guid applicationId,
        Guid proposedFellingDetailsId,
        List<Guid> selectedCompartmentIds,
        Guid fellingCompartmentId,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base.EnsureApplicationIsEditable(applicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}", 
                applicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        if (selectedCompartmentIds.IsNullOrEmpty())
        {
            selectedCompartmentIds = new List<Guid>();
        }

        var application = applicationResult.Value;

        var fellingDetails =
            application.LinkedPropertyProfile.ProposedFellingDetails!.First(pfd => pfd.Id == proposedFellingDetailsId);

        if (fellingDetails is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var compartmentStatus =
            application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Find(c =>
                c.CompartmentId == fellingCompartmentId);
        var fellingStatus = compartmentStatus?.FellingStatuses.Find(fs => fs.Id == proposedFellingDetailsId);

        if (fellingStatus != null)
        {
            var restockingDetailsToRemove = fellingDetails.ProposedRestockingDetails!
                .Where(prd => !selectedCompartmentIds.Contains(prd.PropertyProfileCompartmentId)).ToList();

            foreach (var restockingDetailToRemove in restockingDetailsToRemove)
            {
                fellingDetails.ProposedRestockingDetails!.Remove(restockingDetailToRemove);

                fellingStatus?.RestockingCompartmentStatuses.RemoveAll(rs =>
                    rs.CompartmentId == restockingDetailToRemove.PropertyProfileCompartmentId);
            }
        }

        foreach (var selectedCompartmentId in selectedCompartmentIds)
        {
            var existingRestocking =
                fellingDetails.ProposedRestockingDetails!.FirstOrDefault(prd =>
                    prd.PropertyProfileCompartmentId == selectedCompartmentId);

            if (existingRestocking is null)
            {
                fellingDetails.ProposedRestockingDetails.Add(new ProposedRestockingDetail
                {
                    PropertyProfileCompartmentId = selectedCompartmentId,
                    RestockingProposal = TypeOfProposal.None,
                    ProposedFellingDetailsId = proposedFellingDetailsId
                });
            }
        }

        // Update / save

        _fellingLicenceApplicationRepository.Update(application);
        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId, Section = "Select Restocking Compartments" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                    new
                    {
                        user.WoodlandOwnerId, Section = "Select Restocking Compartments", Error = r.GetDescription()
                    }), cancellationToken);
                _logger.LogError(
                    "The restocking compartments have not been updated due to the reason {ErrorReason} for application with id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    /// <summary>
    /// Selects application compartments
    /// </summary>
    /// <param name="user">An application user</param>
    /// <param name="applicationId">An application id</param>
    /// <param name="selectedCompartmentIds">A list of selected compartment ids</param>
    /// <param name="compartmentSelectionStepComplete">A nullable flag indicating whether compartment selection has been completed.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An updated application id</returns>
    public async Task<Result<Guid, UserDbErrorReason>> SelectApplicationCompartmentsAsync(
        ExternalApplicant user,
        Guid applicationId,
        List<Guid> selectedCompartmentIds,
        bool? compartmentSelectionStepComplete,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base.EnsureApplicationIsEditable(applicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                applicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        if (selectedCompartmentIds.IsNullOrEmpty())
        {
            selectedCompartmentIds = new List<Guid>();
        }

        var application = applicationResult.Value;

        application.FellingLicenceApplicationStepStatus.SelectCompartmentsStatus = compartmentSelectionStepComplete;

        var selectedCompartmentIdsDictionary = selectedCompartmentIds.ToHashSet();
        var proposedFellingDetailsDictionary = application.LinkedPropertyProfile.ProposedFellingDetails?
            .Select(d => d.PropertyProfileCompartmentId).ToHashSet() ?? new HashSet<Guid>();

        var fellingDetailsToRemove =
            application.LinkedPropertyProfile.ProposedFellingDetails!.Where(d =>
                !selectedCompartmentIdsDictionary.Contains(d.PropertyProfileCompartmentId)).ToList();
        foreach (var fellingDetail in fellingDetailsToRemove)
        {
            application.LinkedPropertyProfile.ProposedFellingDetails!.Remove(fellingDetail);
        }

        foreach (var selectedCompartmentId in selectedCompartmentIds)
        {
            if (!proposedFellingDetailsDictionary.Contains(selectedCompartmentId))
            {
                application.LinkedPropertyProfile.ProposedFellingDetails!.Add(new ProposedFellingDetail
                {
                    PropertyProfileCompartmentId = selectedCompartmentId,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>()
                });
            }
        }

        var existingCompartmentFellingRestockingStatuses =
            application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses;

        // For any compartment ids that don't exist in CompartmentFellingRestockingStatuses, add them in

        foreach (var selectedCompartmentId in selectedCompartmentIds)
        {
            if (!existingCompartmentFellingRestockingStatuses.Any(x => x.CompartmentId == selectedCompartmentId))
            {
                existingCompartmentFellingRestockingStatuses.Add(new CompartmentFellingRestockingStatus()
                {
                    CompartmentId = selectedCompartmentId
                });
                if (!application.NotRunningExternalLisReport)
                {
                    application.FellingLicenceApplicationStepStatus.ConstraintCheckStatus = null;
                }
            }
        }
        // For any CompartmentFellingRestockingStatuses that have compartment IDs that don't exist in the new selection,
        // remove them

        existingCompartmentFellingRestockingStatuses.RemoveAll(x => !selectedCompartmentIds.Contains(x.CompartmentId));

        // Update / save

        _fellingLicenceApplicationRepository.Update(application);
        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId, Section = "Select Compartments" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                        new { user.WoodlandOwnerId, Section = "Select Compartments", Error = r.GetDescription() }),
                    cancellationToken);
                _logger.LogError(
                    "The application compartments have not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }


    /// <summary>
    /// Creates initial ProposedFellingDetails for the given compartment.
    /// </summary>
    /// <param name="user">An application user</param>
    /// <param name="applicationId">An application id</param>
    /// <param name="selectedCompartmentIds">A list of selected compartment ids</param>
    /// <param name="compartmentSelectionStepComplete">A nullable flag indicating whether compartment selection has been completed.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An updated application id</returns>
    public async Task<Result<Guid, UserDbErrorReason>> CreateEmptyProposedFellingDetails(
        ExternalApplicant user,
        SelectFellingOperationTypesViewModel selectFellingOperationTypesViewModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(selectFellingOperationTypesViewModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                selectFellingOperationTypesViewModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult =
            await GetFellingLicenceApplicationAsync(selectFellingOperationTypesViewModel.ApplicationId, user,
                cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        if (!selectFellingOperationTypesViewModel.OperationTypes.Any())
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var application = applicationResult.Value;

        var thisCompartmentStatuses =
            application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Find(c =>
                c.CompartmentId == selectFellingOperationTypesViewModel.FellingCompartmentId);

        if (thisCompartmentStatuses is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var fellingDetailsToRemove =
            application.LinkedPropertyProfile.ProposedFellingDetails!.Where(pfd =>
                pfd.PropertyProfileCompartmentId == selectFellingOperationTypesViewModel.FellingCompartmentId &&
                !selectFellingOperationTypesViewModel.OperationTypes.Contains(pfd.OperationType)).ToList();

        if (fellingDetailsToRemove.Any())
        {
            foreach (var fellingDetail in fellingDetailsToRemove)
            {
                application.LinkedPropertyProfile.ProposedFellingDetails!.Remove(fellingDetail);

                thisCompartmentStatuses.FellingStatuses.RemoveAll(fs => fs.Id == fellingDetail.Id);
            }
        }

        foreach (var fellingOperationType in selectFellingOperationTypesViewModel.OperationTypes)
        {
            if (!application.LinkedPropertyProfile.ProposedFellingDetails!.Any(pfd =>
                    pfd.PropertyProfileCompartmentId == selectFellingOperationTypesViewModel.FellingCompartmentId &&
                    pfd.OperationType == fellingOperationType))
            {
                application.LinkedPropertyProfile.ProposedFellingDetails!.Add(new ProposedFellingDetail
                {
                    PropertyProfileCompartmentId = selectFellingOperationTypesViewModel.FellingCompartmentId,
                    OperationType = fellingOperationType,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>()
                });
            }
        }

        thisCompartmentStatuses.Status = true;

        // Update / save

        _fellingLicenceApplicationRepository.Update(application);
        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId, Section = "Select Felling Operation Types" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                    new
                    {
                        user.WoodlandOwnerId, Section = "Select Felling Operation Types", Error = r.GetDescription()
                    }), cancellationToken);
                _logger.LogError(
                    "The felling operation types have not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    /// <summary>
    /// Creates initial ProposedRestockingDetails for the given felling, compartment, and set of restocking options.
    /// </summary>
    /// <param name="user">An application user</param>
    /// <param name="applicationId">An application id</param>
    /// <param name="fellingCompartmentId">The felling compartment within which these restockings are taking place</param>
    /// <param name="restockingCompartmentId">The compartment for which this set of restocking options is taking place</param>
    /// <param name="proposedFellingDetailsId">The felling within which these restockings are taking place</param>
    /// <param name="restockingOptions">The restocking options to be created.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An updated application id</returns>
    public async Task<Result<Guid, UserDbErrorReason>> CreateEmptyProposedRestockingDetails(
        ExternalApplicant user,
        SelectRestockingOptionsViewModel selectRestockingOptionsViewModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(selectRestockingOptionsViewModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                selectRestockingOptionsViewModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult = await GetFellingLicenceApplicationAsync(selectRestockingOptionsViewModel.ApplicationId,
            user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        if (!selectRestockingOptionsViewModel.RestockingOptions.Any())
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var application = applicationResult.Value;

        var thisCompartmentStatuses =
            application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Find(c =>
                c.CompartmentId == selectRestockingOptionsViewModel.FellingCompartmentId);
        var thisFellingStatus = thisCompartmentStatuses.FellingStatuses.Find(fs =>
            fs.Id == selectRestockingOptionsViewModel.ProposedFellingDetailsId);
        var thisRestockingCompartmentStatus = thisFellingStatus.RestockingCompartmentStatuses.Find(rcs =>
            rcs.CompartmentId == selectRestockingOptionsViewModel.RestockingCompartmentId);

        var thisFelling = application.LinkedPropertyProfile.ProposedFellingDetails.FirstOrDefault(pfd =>
            pfd.Id == selectRestockingOptionsViewModel.ProposedFellingDetailsId);

        if (thisFelling is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var restockingDetailsToRemove =
            thisFelling.ProposedRestockingDetails!.Where(prd =>
                prd.PropertyProfileCompartmentId == selectRestockingOptionsViewModel.RestockingCompartmentId &&
                !selectRestockingOptionsViewModel.RestockingOptions.Contains(prd.RestockingProposal)).ToList();

        if (restockingDetailsToRemove.Any())
        {
            foreach (var restockingDetail in restockingDetailsToRemove)
            {
                thisFelling.ProposedRestockingDetails.Remove(restockingDetail);

                thisRestockingCompartmentStatus.RestockingStatuses.RemoveAll(rs => rs.Id == restockingDetail.Id);
            }
        }

        foreach (var restockingOption in selectRestockingOptionsViewModel.RestockingOptions)
        {
            if (!thisFelling.ProposedRestockingDetails!.Any(prd =>
                    prd.PropertyProfileCompartmentId == selectRestockingOptionsViewModel.RestockingCompartmentId &&
                    prd.RestockingProposal == restockingOption))
            {
                thisFelling.ProposedRestockingDetails!.Add(new ProposedRestockingDetail
                {
                    PropertyProfileCompartmentId = selectRestockingOptionsViewModel.RestockingCompartmentId,
                    ProposedFellingDetailsId = selectRestockingOptionsViewModel.ProposedFellingDetailsId,
                    RestockingProposal = restockingOption
                });
            }
        }

        thisRestockingCompartmentStatus.Status = true;

        // Update / save

        _fellingLicenceApplicationRepository.Update(application);
        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId, Section = "Select Restocking Options" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                        new
                        {
                            user.WoodlandOwnerId, Section = "Select Restocking Options", Error = r.GetDescription()
                        }),
                    cancellationToken);
                _logger.LogError(
                    "The restocking options have not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    public async Task<Result<Guid, UserDbErrorReason>> CreateMissingRestockingStatuses(
        ExternalApplicant user,
        Guid applicationId,
        Guid fellingCompartmentId,
        Guid proposedFellingDetailsId,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base.EnsureApplicationIsEditable(applicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                applicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;

        var thisCompartmentStatuses =
            application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Find(c =>
                c.CompartmentId == fellingCompartmentId);

        if (thisCompartmentStatuses == null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var thisFellingStatus = thisCompartmentStatuses.FellingStatuses.Find(fs => fs.Id == proposedFellingDetailsId);

        if (thisFellingStatus == null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var thisFellingDetails =
            application.LinkedPropertyProfile.ProposedFellingDetails.First(pfd => pfd.Id == proposedFellingDetailsId);

        if (thisFellingDetails == null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        foreach (var proposedRestocking in thisFellingDetails.ProposedRestockingDetails)
        {
            var restockingCompartmentStatus = thisFellingStatus.RestockingCompartmentStatuses.Find(rs =>
                rs.CompartmentId == proposedRestocking.PropertyProfileCompartmentId);

            if (restockingCompartmentStatus == null)
            {
                thisFellingStatus.RestockingCompartmentStatuses.Add(new RestockingCompartmentStatus()
                {
                    CompartmentId = proposedRestocking.PropertyProfileCompartmentId,
                    Status = false,
                    RestockingStatuses = new List<RestockingStatus>()
                    {
                        new RestockingStatus()
                        {
                            Id = proposedRestocking.Id,
                            Status = false
                        }
                    }
                });
            }
            else
            {
                if (!restockingCompartmentStatus.RestockingStatuses.Any(rs => rs.Id == proposedRestocking.Id))
                {
                    restockingCompartmentStatus.RestockingStatuses.Add(new RestockingStatus
                    {
                        Id = proposedRestocking.Id,
                        Status = false
                    });
                }
            }
        }

        // now restocking compartments are selected and the statuses created, we can set the felling status to true (complete).
        thisFellingStatus.Status = true;

        // Update / save

        _fellingLicenceApplicationRepository.Update(application);
        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId, Section = "Create restocking statuses" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                        new
                        {
                            user.WoodlandOwnerId, Section = "Create restocking statuses", Error = r.GetDescription()
                        }),
                    cancellationToken);
                _logger.LogError(
                    "The restocking statuses have not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    public async Task<Result<Guid, UserDbErrorReason>> CreateMissingFellingStatuses(
        ExternalApplicant user,
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base.EnsureApplicationIsEditable(applicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                applicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;

        var thisCompartmentStatuses =
            application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Find(c =>
                c.CompartmentId == compartmentId);

        var thisFellingDetails =
            application.LinkedPropertyProfile.ProposedFellingDetails!.Where(pfd =>
                pfd.PropertyProfileCompartmentId == compartmentId);

        foreach (var fellingDetail in thisFellingDetails)
        {
            if (!thisCompartmentStatuses!.FellingStatuses.Exists(fs => fs.Id == fellingDetail.Id))
            {
                thisCompartmentStatuses.FellingStatuses.Add(new FellingStatus
                {
                    Id = fellingDetail.Id, Status = null,
                    RestockingCompartmentStatuses = new List<RestockingCompartmentStatus>()
                });
            }
        }

        // Update / save

        _fellingLicenceApplicationRepository.Update(application);
        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId, Section = "Create felling statuses" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                        new { user.WoodlandOwnerId, Section = "Create felling statuses", Error = r.GetDescription() }),
                    cancellationToken);
                _logger.LogError(
                    "The felling statuses have not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    /// <summary>
    /// Returns a model including compartments and an application for the Select Compartments view
    /// </summary>
    /// <param name="applicationId">An application id</param>
    /// <param name="user">An Application user</param>
    /// <param name="cancellationToken">A Cancellation token</param>
    /// <returns>A SelectCompartmentsViewModel object</returns>
    public async Task<Maybe<SelectCompartmentsViewModel>> GetSelectCompartmentViewModel(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var applicationResult =
            await RetrieveFellingLicenceApplication(user, applicationId,
                cancellationToken);
        if (applicationResult.HasNoValue)
        {
            return Maybe<SelectCompartmentsViewModel>.None;
        }

        var application = applicationResult.Value;
        var compartments =
            await RetrievePropertyProfileCompartmentsAsync(
                user,
                application.ApplicationSummary.PropertyProfileId,
                application.WoodlandOwnerId,
                cancellationToken);

        return Maybe<SelectCompartmentsViewModel>.From(new SelectCompartmentsViewModel()
        {
            Compartments = compartments.HasValue ? compartments.Value : new List<CompartmentModel>(),
            Application = application
        });
    }

    public async Task<Maybe<SelectRestockingOptionsViewModel>> GetSelectRestockingOptionsViewModel(
        Guid applicationId,
        Guid fellingCompartmentId,
        Guid restockingCompartmentId,
        Guid proposedFellingDetailsId,
        bool restockAlternativeArea,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var applicationResult = await RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);
        if (applicationResult.HasNoValue)
        {
            return Maybe<SelectRestockingOptionsViewModel>.None;
        }

        var application = applicationResult.Value;
        var compartments =
            await RetrievePropertyProfileCompartmentsAsync(
                user,
                application.ApplicationSummary.PropertyProfileId,
                application.WoodlandOwnerId,
                cancellationToken);

        var selectedCompartment = compartments.Value.FirstOrDefault(c => c.Id == restockingCompartmentId);

        var gisString = string.Empty;

        if (selectedCompartment != null)
        {
            var gisCompartment = compartments.Value
                .Where(c => c.Id == restockingCompartmentId)
                .Select(c => new
                {
                    c.Id,
                    c.GISData,
                    c.DisplayName,
                    Selected = true
                });

            gisString = JsonConvert.SerializeObject(gisCompartment);
        }

        var fellingCompartment = compartments.Value.FirstOrDefault(c => c.Id == fellingCompartmentId);

        ProposedFellingDetailModel felling = null;
        var found = false;

        foreach (var detail in application.FellingAndRestockingDetails.DetailsList)
        {
            foreach (var fellingdetail in detail.FellingDetails)
            {
                if (fellingdetail.Id == proposedFellingDetailsId)
                {
                    felling = fellingdetail;
                    found = true;
                    break;
                }
            }

            if (found)
                break;
        }

        if (felling is null)
        {
            return Maybe<SelectRestockingOptionsViewModel>.None;
        }

        var fellingOperationType = felling.OperationType;

        var allowedRestockingOptions = fellingOperationType.AllowedRestockingForFellingType();

        var model = new SelectRestockingOptionsViewModel
        {
            ApplicationId = applicationId,
            FellingCompartmentId = fellingCompartmentId,
            RestockingCompartmentId = restockingCompartmentId,
            ProposedFellingDetailsId = proposedFellingDetailsId,
            FellingOperationType = fellingOperationType,
            FellingCompartmentName = fellingCompartment?.DisplayName ?? string.Empty,
            RestockingCompartmentName = selectedCompartment?.DisplayName ?? string.Empty,
            RestockAlternativeArea = restockAlternativeArea,
            IsCoppiceRegrowthAllowed = allowedRestockingOptions.Contains(TypeOfProposal.RestockWithCoppiceRegrowth),
            IsCreateOpenSpaceAllowed = allowedRestockingOptions.Contains(TypeOfProposal.CreateDesignedOpenGround),
            IsIndividualTreesAllowed = allowedRestockingOptions.Contains(TypeOfProposal.RestockWithIndividualTrees),
            IsNaturalRegenerationAllowed =
                allowedRestockingOptions.Contains(TypeOfProposal.RestockByNaturalRegeneration),
            IsReplantFelledAreaAllowed = allowedRestockingOptions.Contains(TypeOfProposal.ReplantTheFelledArea),
            IsIndividualTreesInAlternativeAreaAllowed =
                allowedRestockingOptions.Contains(TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees),
            IsPlantingInAlternativeAreaAllowed =
                allowedRestockingOptions.Contains(TypeOfProposal.PlantAnAlternativeArea),
            IsNaturalColonisationAllowed = allowedRestockingOptions.Contains(TypeOfProposal.NaturalColonisation),
            GIS = gisString,
            FellingLicenceStatus = application.ApplicationSummary.Status
        };

        var restockings =
            felling.ProposedRestockingDetails.Where(prd => prd.RestockingCompartmentId == restockingCompartmentId);

        if (restockings.Any())
        {
            foreach (var restocking in restockings)
            {
                switch (restocking.RestockingProposal)
                {
                    case TypeOfProposal.CreateDesignedOpenGround:
                        model.IsCreateOpenSpaceSelected = true;
                        break;
                    case TypeOfProposal.PlantAnAlternativeArea:
                        model.IsPlantingInAlternativeAreaSelected = true;
                        break;
                    case TypeOfProposal.NaturalColonisation:
                        model.IsNaturalColonisationSelected = true;
                        break;
                    case TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees:
                        model.IsIndividualTreesInAlternativeAreaSelected = true;
                        break;
                    case TypeOfProposal.ReplantTheFelledArea:
                        model.IsReplantFelledAreaSelected = true;
                        break;
                    case TypeOfProposal.RestockByNaturalRegeneration:
                        model.IsNaturalRegenerationSelected = true;
                        break;
                    case TypeOfProposal.RestockWithCoppiceRegrowth:
                        model.IsCoppiceRegrowthSelected = true;
                        break;
                    case TypeOfProposal.RestockWithIndividualTrees:
                        model.IsIndividualTreesSelected = true;
                        break;
                }
            }
        }

        return Maybe<SelectRestockingOptionsViewModel>.From(model);
    }

    /// <summary>
    /// Returns a model including compartments and an application for the Select Compartments view
    /// </summary>
    /// <param name="applicationId">An application id</param>
    /// <param name="user">An Application user</param>
    /// <param name="cancellationToken">A Cancellation token</param>
    /// <returns>A SelectCompartmentsViewModel object</returns>
    public async Task<Maybe<SelectFellingOperationTypesViewModel>> GetSelectFellingOperationTypesViewModel(
        Guid applicationId,
        Guid compartmentId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var applicationResult =
            await RetrieveFellingLicenceApplication(user, applicationId,
                cancellationToken);
        if (applicationResult.HasNoValue)
        {
            return Maybe<SelectFellingOperationTypesViewModel>.None;
        }

        var application = applicationResult.Value;
        var compartments =
            await RetrievePropertyProfileCompartmentsAsync(
                user,
                application.ApplicationSummary.PropertyProfileId,
                application.WoodlandOwnerId,
                cancellationToken);

        var selectedCompartment = compartments.Value.FirstOrDefault(c => c.Id == compartmentId);

        var gisString = string.Empty;

        if (selectedCompartment != null)
        {
            var gisCompartment = compartments.Value
                .Where(c => c.Id == compartmentId)
                .Select(c => new
                {
                    c.Id,
                    c.GISData,
                    c.DisplayName,
                    Selected = true
                });

            gisString = JsonConvert.SerializeObject(gisCompartment);
        }

        var model = new SelectFellingOperationTypesViewModel()
        {
            Application = application,
            ApplicationId = applicationId,
            FellingCompartmentId = compartmentId,
            Compartments = compartments.HasValue ? compartments.Value : new List<CompartmentModel>(),
            GIS = gisString,
            FellingLicenceStatus = applicationResult.Value.ApplicationSummary.Status
        };

        var compartment =
            applicationResult.Value.FellingAndRestockingDetails.DetailsList.Find(d => d.CompartmentId == compartmentId);

        if (compartment != null)
        {
            foreach (var fellingDetail in compartment.FellingDetails)
            {
                switch (fellingDetail.OperationType)
                {
                    case FellingOperationType.ClearFelling:
                        model.IsClearFellingSelected = true;
                        break;
                    case FellingOperationType.FellingOfCoppice:
                        model.IsFellingOfCoppiceSelected = true;
                        break;
                    case FellingOperationType.FellingIndividualTrees:
                        model.IsFellingIndividualTreesSelected = true;
                        break;
                    case FellingOperationType.RegenerationFelling:
                        model.IsRegenerationFellingSelected = true;
                        break;
                    case FellingOperationType.Thinning:
                        model.IsThinningSelected = true;
                        break;
                }
            }
        }

        return Maybe<SelectFellingOperationTypesViewModel>.From(model);
    }

    /// <summary>
    /// Returns a model including compartments and an application for the Select Compartments view
    /// </summary>
    /// <param name="applicationId">An application id</param>
    /// <param name="user">An Application user</param>
    /// <param name="cancellationToken">A Cancellation token</param>
    /// <returns>A SelectCompartmentsViewModel object</returns>
    public async Task<Maybe<IEnumerable<CompartmentModel>>> GetSelectedCompartmentsAsync(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var applicationResult =
            await RetrieveFellingLicenceApplication(user, applicationId,
                cancellationToken);
        if (applicationResult.HasNoValue)
        {
            return Maybe<IEnumerable<CompartmentModel>>.None;
        }

        var application = applicationResult.Value;
        var compartments =
            await RetrievePropertyProfileCompartmentsAsync(user,
                application.ApplicationSummary.PropertyProfileId,
                application.WoodlandOwnerId,
                cancellationToken);

        if (compartments.HasNoValue)
        {
            return Maybe<IEnumerable<CompartmentModel>>.None;
        }

        var models =
            compartments.Value.Where(c => application.SelectedCompartments.SelectedCompartmentIds!.Contains(c.Id));

        return Maybe<IEnumerable<CompartmentModel>>.From(models);
    }

    public async Task<Maybe<ActivityFeedViewModel>> GetCaseNotesActivityFeedForApplicationAsync(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var application = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

        if (application.IsFailure)
        {
            return Maybe<ActivityFeedViewModel>.None;
        }

        var providerModel = GetProviderModelForActivityFeed(application.Value);

        var activityFeedItems = await _activityFeedService.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.ExternalApplicant,
            cancellationToken);

        var result = new ActivityFeedViewModel
        {
            ApplicationId = applicationId,
            ActivityFeedItemModels = activityFeedItems.Value,
            ShowFilters = true,
            ActivityFeedTitle = "Activity Feed",
            StatusHistories = application.Value.StatusHistories.ToList(),
        };

        return Maybe<ActivityFeedViewModel>.From(result);
    }

    /// <summary>
    /// Returns a felling licence application for a current user by a given application id
    /// </summary>
    /// <param name="user">A current application user</param>
    /// <param name="applicationId">An application id</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public async Task<Maybe<FellingLicenceApplicationModel>> RetrieveFellingLicenceApplication(
        ExternalApplicant user,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eiaOptions.Value);



        var application = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

        if (application.IsFailure)
        {
            return Maybe<FellingLicenceApplicationModel>.None;
        }

        //do we always need all of this, when on a specific task section? e.g. only need docs for support docs page.

        var fellingLicenceStatus = GetApplicationStatus(application.Value.StatusHistories);

        var providerModel = GetProviderModelForActivityFeed(application.Value);
        var activityFeedItems = await _activityFeedService.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.ExternalApplicant,
            cancellationToken);

        var supportingDocumentsToDisplay = application.Value.Documents
            .Where(x => x.DeletionTimestamp.HasValue == false && x.VisibleToApplicant)
            .ToList();

        var applicationSummary = await GetApplicationSummaryAsync(application.Value, user, cancellationToken);
        if (applicationSummary.IsFailure)
        {
            _logger.LogError("Unable to load application summary for application with id {ApplicationId}", applicationId);
            return Maybe<FellingLicenceApplicationModel>.None;
        }

        var agency = await GetAgencyModelForWoodlandOwnerAsync(application.Value.WoodlandOwnerId, cancellationToken);

        var woodlandOwnerAafId =
            await agentAuthorityService.GetAgentAuthorityForWoodlandOwnerAsync(application.Value.WoodlandOwnerId, cancellationToken);

        var requiresEia = application.Value.ShouldApplicationRequireEia();

        var currentReview =
            await getWoodlandOfficerReviewService.GetCurrentFellingAndRestockingAmendmentReviewAsync(applicationId,
                cancellationToken);

        if (currentReview.IsFailure)
        {
            _logger.LogWarning("unable to get current review for application, error : {Error}", currentReview.Error);
            return Maybe<FellingLicenceApplicationModel>.None;
        }

        var applicationModel = new FellingLicenceApplicationModel
        {
            ApplicationId = application.Value.Id,
            WoodlandOwnerId = application.Value.WoodlandOwnerId,
            AgencyId = agency.HasValue ? agency.Value.AgencyId : null,
            HasCaseNotes = activityFeedItems.IsSuccess && !activityFeedItems.Value.IsNullOrEmpty() &&
                           activityFeedItems.Value.Any(),
            ApplicationSummary = applicationSummary.Value,
            AgentAuthorityForm = new Models.FellingLicenceApplication.AgentAuthorityFormModel
            {
                ApplicationId = application.Value.Id,
                ApplicationReference = application.Value.ApplicationReference,
                StepComplete = application.Value.FellingLicenceApplicationStepStatus.AafStepStatus,
                FellingLicenceStatus = fellingLicenceStatus,
                WoodlandOwnerAafId = woodlandOwnerAafId.HasValue ? woodlandOwnerAafId.Value.Id : null,
                StepRequiredForApplication = agency.HasValue,
            },
            SelectedCompartments = new SelectedCompartmentsModel
            {
                ApplicationId = application.Value.Id,
                ApplicationReference = application.Value.ApplicationReference,
                SelectedCompartmentIds = application.Value.LinkedPropertyProfile?.ProposedFellingDetails?
                    .Select(d => d.PropertyProfileCompartmentId).ToList(),
                StepComplete = application.Value.FellingLicenceApplicationStepStatus.SelectCompartmentsStatus,
                FellingLicenceStatus = fellingLicenceStatus
            },
            OperationDetails = new OperationDetailsModel
            {
                ApplicationId = application.Value.Id,
                ApplicationReference = application.Value.ApplicationReference,
                ProposedTiming = application.Value.ProposedTiming,
                Measures = application.Value.Measures,
                ProposedFellingStart = application.Value.ProposedFellingStart != null
                    ? new DatePart(application.Value.ProposedFellingStart.Value.ToLocalTime(), "felling-start")
                    : null,
                ProposedFellingEnd = application.Value.ProposedFellingEnd != null
                    ? new DatePart(application.Value.ProposedFellingEnd.Value.ToLocalTime(), "felling-end")
                    : null,
                StepComplete = application.Value.FellingLicenceApplicationStepStatus.OperationsStatus,
                FellingLicenceStatus = fellingLicenceStatus,
                DateReceived = application.Value.DateReceived.HasValue
                    ? new DatePart(application.Value.DateReceived.Value.ToLocalTime(), "date-received")
                    : null,
                ApplicationSource = application.Value.Source,
                DisplayDateReceived = user.IsFcUser || user.AccountType is AccountTypeExternal.FcUser,
                IsForTenYearLicence = application.Value.IsForTenYearLicence
            },
            FellingAndRestockingDetails = new FellingAndRestockingDetails
            {
                ApplicationId = application.Value.Id,
                ApplicationReference = application.Value.ApplicationReference,
                DetailsList = await CreateFellingAndRestockingDetailsList(application.Value, user, fellingLicenceStatus,
                    cancellationToken),
                FellingLicenceStatus = fellingLicenceStatus

                // StepComplete set below
            },
            SupportingDocumentation = new SupportingDocumentationModel
            {
                ApplicationId = application.Value.Id,
                ApplicationReference = application.Value.ApplicationReference,
                Documents = ModelMapping.ToDocumentsModelForApplicantView(supportingDocumentsToDisplay),
                StepComplete = application.Value.FellingLicenceApplicationStepStatus.SupportingDocumentationStatus,
                FellingLicenceStatus = fellingLicenceStatus
            },
            FlaTermsAndConditionsViewModel = new FlaTermsAndConditionsViewModel
            {
                ApplicationId = application.Value.Id,
                ApplicationReference = application.Value.ApplicationReference,
                TermsAndConditionsAccepted = application.Value.TermsAndConditionsAccepted,
                StepComplete = application.Value.FellingLicenceApplicationStepStatus.TermsAndConditionsStatus,
                FellingLicenceStatus = fellingLicenceStatus
            },
            ConstraintCheck = new ConstraintCheckModel
            {
                ApplicationId = application.Value.Id,
                ApplicationReference = application.Value.ApplicationReference,
                StepComplete = application.Value.FellingLicenceApplicationStepStatus.ConstraintCheckStatus,
                FellingLicenceStatus = fellingLicenceStatus,
                MostRecentExternalLisReport = GetMostRecentDocumentOfType(application.Value.Documents,
                    DocumentPurpose.ExternalLisConstraintReport),
                SelectCompartmentStep = application.Value.FellingLicenceApplicationStepStatus.SelectCompartmentsStatus,
                ExternalLisAccessedTimestamp = application.Value.ExternalLisAccessedTimestamp,
                NotRunningExternalLisReport = application.Value.NotRunningExternalLisReport
            },
            EnvironmentalImpactAssessment = new EnvironmentalImpactAssessmentViewModel
            {
                ApplicationId = applicationId,
                StepComplete = application.Value.FellingLicenceApplicationStepStatus.EnvironmentalImpactAssessmentStatus,
                FellingLicenceStatus = application.Value.GetCurrentStatus(),
                EiaDocuments = ModelMapping.ToDocumentsModelForApplicantView(
                    application.Value.Documents?.Where(x =>
                            x.Purpose is DocumentPurpose.EiaAttachment &&
                            x.DeletionTimestamp is null)
                        .ToList()).ToArray(),
                HasApplicationBeenCompleted = application.Value.EnvironmentalImpactAssessment?.HasApplicationBeenCompleted,
                HasApplicationBeenSent = application.Value.EnvironmentalImpactAssessment?.HasApplicationBeenSent,
                ApplicationReference = application.Value.ApplicationReference,
                EiaApplicationExternalUri = eiaOptions.Value.EiaApplicationExternalUri,
                StepRequiredForApplication = requiresEia
            },
            TenYearLicence = new TenYearLicenceApplicationViewModel
            {
                ApplicationId = applicationId,
                ApplicationReference = application.Value.ApplicationReference,
                FellingLicenceStatus = application.Value.GetCurrentStatus(),
                IsForTenYearLicence = application.Value.IsForTenYearLicence,
                WoodlandManagementPlanReference = application.Value.WoodlandManagementPlanReference,
                StepComplete = application.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus,
                StepRequiredForApplication = user.IsFcUser
            },
            CurrentReviewModel = currentReview.Value.TryGetValue(out var current)
                ? current
                : null
        };

        // In the event that SupportingDocumentation.StepComplete is null, indicating model has not been saved (which equates to NOT STARTED),
        // check if any documents have been uploaded, which would logically suggest that it is really IN PROGRESS. If that's the case, set false
        // which equates to IN PROGRESS (while true equates to COMPLETED)

        if (application.Value.FellingLicenceApplicationStepStatus.SupportingDocumentationStatus.HasValue == false &&
            application.Value.Documents != null && application.Value.Documents.Any())
        {
            applicationModel.SupportingDocumentation.StepComplete = false;
        }


        // Calculate an overall status for combined felling and restocking details

        bool? fellingAndRestockingDetailsStepComplete;

        if (application.Value.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.TrueForAll(x =>
                x.OverallCompletion() is null))
        {
            // NOT STARTED

            fellingAndRestockingDetailsStepComplete = null;
        }
        else if (application.Value.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.TrueForAll(
                     x => x.OverallCompletion() is true))
        {
            // COMPLETE

            fellingAndRestockingDetailsStepComplete = true;
        }
        else
        {
            // IN PROGRESS

            fellingAndRestockingDetailsStepComplete = false;
        }

        applicationModel.FellingAndRestockingDetails.StepComplete = fellingAndRestockingDetailsStepComplete;

        return applicationModel;
    }

    public async Task<Maybe<ProposedRestockingDetailModel>> GetRestockingDetailViewModel(
        ExternalApplicant user,
        Guid applicationId,
        Guid restockingId,
        FellingLicenceApplicationModel fellingLicenceApplicationModel,
        CancellationToken cancellationToken)
    {
        var found = false;
        ProposedRestockingDetailModel restockingModel = new ProposedRestockingDetailModel();
        foreach (var detail in fellingLicenceApplicationModel.FellingAndRestockingDetails.DetailsList)
        {
            foreach (var fellingDetail in detail.FellingDetails)
            {
                foreach (var restockingDetail in fellingDetail.ProposedRestockingDetails)
                {
                    if (restockingDetail.Id == restockingId)
                    {
                        restockingModel = restockingDetail;
                        restockingModel.OperationType = fellingDetail.OperationType;
                        found = true;
                        break;
                    }
                }

                if (found)
                    break;
            }

            if (found)
                break;
        }

        if (!found)
        {
            _logger.LogError("ProposedRestockingDetailModel not found, restocking id: {RestockingId}", restockingId);
            return Maybe<ProposedRestockingDetailModel>.None;
        }

        var compartmentId = restockingModel.RestockingCompartmentId;

        var compartment = await GetCompartmentDetails(compartmentId, user, cancellationToken);

        restockingModel.CompartmentName = compartment.HasValue ? compartment.Value.Item3 : string.Empty;
        restockingModel.CompartmentTotalHectares = compartment.HasValue ? compartment.Value.Item2 : 0D;
        restockingModel.ApplicationId = applicationId;
        restockingModel.FellingLicenceStatus = fellingLicenceApplicationModel.ApplicationSummary.Status;

        return Maybe<ProposedRestockingDetailModel>.From(restockingModel);
    }

    public async Task<Maybe<FellingAndRestockingPlaybackViewModel>> GetFellingAndRestockingDetailsPlaybackViewModel(
        Guid applicationID,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var applicationResult = await GetFellingLicenceApplicationAsync(applicationID, user, cancellationToken);

        if (applicationResult.IsFailure)
        {
            return Maybe<FellingAndRestockingPlaybackViewModel>.None;
        }

        var application = applicationResult.Value;

        var fellingCompartmentsList = application.LinkedPropertyProfile?.ProposedFellingDetails?
            .Select(d => d.PropertyProfileCompartmentId)
            .Distinct().ToList() ?? new List<Guid>();
        var restockingCompartmentsList = application.LinkedPropertyProfile?.ProposedFellingDetails?
            .Where(x => x.ProposedRestockingDetails != null)
            .SelectMany(x => x.ProposedRestockingDetails!)
            .Select(x => x.PropertyProfileCompartmentId)
            .Distinct().ToList() ?? new List<Guid>();

        var allCompartmentsList = fellingCompartmentsList.Concat(restockingCompartmentsList).Distinct().ToList();

        var compartments = await _compartmentRepository.ListAsync(allCompartmentsList, cancellationToken);

        if (compartments is null)
        {
            return Maybe<FellingAndRestockingPlaybackViewModel>.None;
        }

        var maybeSubmittedProperty = await _fellingLicenceApplicationRepository
            .GetExistingSubmittedFlaPropertyDetailAsync(applicationID, cancellationToken);

        var submittedCompartments = maybeSubmittedProperty.HasValue
            ? maybeSubmittedProperty.Value.SubmittedFlaPropertyCompartments
            : [];

        var compartmentDetailList = compartments.ToList();

        var controllerName = "FellingLicenceApplication";

        var requiresEia = application.ShouldApplicationRequireEia();

        var fellingAndRestockingPlaybackViewModel = new FellingAndRestockingPlaybackViewModel()
        {
            ApplicationId = applicationID,
            ApplicationReference = application.ApplicationReference,
            FellingCompartmentDetails = new List<FellingCompartmentPlaybackViewModel>(),
            FellingCompartmentsChangeLink = new UrlActionContext
            {
                Action = nameof(FellingLicenceApplicationController.SelectCompartments),
                Controller = controllerName,
                Values = new
                    { applicationId = application.Id, isForRestockingCompartmentSelection = false, returnToPlayback = true }
            },
            FellingLicenceStatus = application.GetCurrentStatus(),
            SaveAndContinueContext = new UrlActionContext
            {
                Action = requiresEia
                    ? nameof(FellingLicenceApplicationController.EnvironmentalImpactAssessment) 
                    : nameof(FellingLicenceApplicationController.ConstraintsCheck),
                Controller = controllerName,
                Values = new Dictionary<string, string>
                {
                    {
                        "applicationId",
                        application.Id.ToString()
                    }
                }
            }
        };

        var fellingCompartmentPlaybackViewModels = new List<FellingCompartmentPlaybackViewModel>();

        foreach (var fellingCompartmentId in fellingCompartmentsList)
        {
            var fellings = application.LinkedPropertyProfile!.ProposedFellingDetails!
                .Where(pfd => pfd.PropertyProfileCompartmentId == fellingCompartmentId).ToList();

            var newFellingCompartmentPlaybackViewModel = new FellingCompartmentPlaybackViewModel()
            {
                CompartmentId = fellingCompartmentId,
                FellingOperationsChangeLink = new UrlActionContext
                {
                    Action = nameof(FellingLicenceApplicationController.SelectFellingOperationTypes),
                    Controller = controllerName,
                    Values = new { applicationId = application.Id, fellingCompartmentId, returnToPlayback = true }
                },
                FellingDetails = new List<FellingDetailPlaybackViewModel>(),
                FellingLicenceStatus = application.GetCurrentStatus()
            };

            var compartmentDetail = compartmentDetailList.Find(c => c.Id == fellingCompartmentId);

            if (compartmentDetail != null)
            {
                newFellingCompartmentPlaybackViewModel.CompartmentName = compartmentDetail.CompartmentNumber;
            }

            if (!application.IsInApplicantEditableState())
            {
                var submittedCompartmentDetail = submittedCompartments
                    .FirstOrDefault(c => c.CompartmentId == fellingCompartmentId);
                if (submittedCompartmentDetail != null)
                {
                    newFellingCompartmentPlaybackViewModel.CompartmentName =
                        submittedCompartmentDetail.CompartmentNumber;
                }
            }

            foreach (var felling in fellings)
            {
                var newFellingDetailPlaybackViewModel = new FellingDetailPlaybackViewModel
                {
                    FellingDetail = felling,
                    FellingCompartmentName = newFellingCompartmentPlaybackViewModel.CompartmentName,
                    AreaChangeLink = BuildFellingActionLink("AreaToBeFelledLabel", application.Id, fellingCompartmentId,
                        felling.Id),
                    NoofTreesChangeLink = BuildFellingActionLink("NumberOfTreesLabel", application.Id,
                        fellingCompartmentId, felling.Id),
                    TreeMarkingChangeLink = BuildFellingActionLink("IsTreeMarkingUsedLabel", application.Id,
                        fellingCompartmentId, felling.Id),
                    SpeciesChangeLink = BuildFellingActionLink("felling-tree-species-selectLabel", application.Id,
                        fellingCompartmentId, felling.Id),
                    TPOChangeLink = BuildFellingActionLink("IsPartOfTreePreservationOrderLabel", application.Id, fellingCompartmentId,
                        felling.Id),
                    ConservationAreaChangeLink = BuildFellingActionLink("IsWithinConservationAreaLabel",
                        application.Id, fellingCompartmentId, felling.Id),
                    EstimateVolumeChangeLink = BuildFellingActionLink("EstimatedTotalFellingVolumeLabel",
                        application.Id, fellingCompartmentId, felling.Id),
                    RestockingCompartmentsChangeLink = new UrlActionContext
                    {
                        Action = nameof(FellingLicenceApplicationController.SelectCompartments),
                        Controller = controllerName,
                        Values = new
                        {
                            applicationId = application.Id,
                            isForRestockingCompartmentSelection = true,
                            fellingOperationType = felling.OperationType,
                            fellingCompartmentName = newFellingCompartmentPlaybackViewModel.CompartmentName,
                            fellingCompartmentId,
                            proposedFellingDetailsId = felling.Id,
                            returnToPlayback = true
                        }
                    },
                    WillYouRestockChangeLink = new UrlActionContext
                    {
                        Action = nameof(FellingLicenceApplicationController.DecisionToRestock),
                        Controller = controllerName,
                        Values = new
                        {
                            applicationId = application.Id,
                            fellingCompartmentId,
                            proposedFellingDetailsId = felling.Id,
                            fellingOperationType = felling.OperationType,
                            returnToPlayback = true
                        }
                    },
                    RestockingCompartmentDetails = new List<RestockingCompartmentPlaybackViewModel>(),
                    FellingLicenceStatus = application.GetCurrentStatus()
                };

                var restockingCompartmentIds = felling.ProposedRestockingDetails!
                    .Select(p => p.PropertyProfileCompartmentId).Distinct().ToList();

                var restockingCompartmentPlaybackViewModels = new List<RestockingCompartmentPlaybackViewModel>();

                foreach (var restockingCompartmentId in restockingCompartmentIds)
                {
                    var newRestockingCompartmentPlaybackViewModel = new RestockingCompartmentPlaybackViewModel
                    {
                        CompartmentId = restockingCompartmentId,
                        RestockingOptionsChangeLink = new UrlActionContext
                        {
                            Action = nameof(FellingLicenceApplicationController.SelectRestockingOptions),
                            Controller = controllerName,
                            Values = new
                            {
                                applicationId = application.Id,
                                fellingCompartmentId,
                                restockingCompartmentId,
                                proposedFellingDetailsId = felling.Id,
                                restockAlternativeArea = fellingCompartmentId != restockingCompartmentId,
                                returnToPlayback = true
                            }
                        },
                        RestockingDetails = new List<RestockingDetailPlaybackViewModel>(),
                        FellingLicenceStatus = application.GetCurrentStatus()
                    };

                    var restockingCompartmentDetail = compartmentDetailList.Find(c => c.Id == restockingCompartmentId);

                    if (restockingCompartmentDetail != null)
                    {
                        newRestockingCompartmentPlaybackViewModel.CompartmentName =
                            restockingCompartmentDetail.CompartmentNumber;
                    }

                    if (!application.IsInApplicantEditableState())
                    {
                        var submittedRestockingCompartment = submittedCompartments
                            .SingleOrDefault(x => x.CompartmentId == restockingCompartmentId);
                        if (submittedRestockingCompartment != null)
                        {
                            newRestockingCompartmentPlaybackViewModel.CompartmentName =
                                submittedRestockingCompartment.CompartmentNumber;
                        }
                    }

                    var restockings = felling.ProposedRestockingDetails
                        .Where(p => p.PropertyProfileCompartmentId == restockingCompartmentId).ToList();

                    var restockingDetailPlaybackViewModels = new List<RestockingDetailPlaybackViewModel>();

                    foreach (var restocking in restockings)
                    {
                        var newRestockingDetailPlaybackViewModel = new RestockingDetailPlaybackViewModel
                        {
                            RestockingDetail = restocking,
                            RestockingCompartmentName = newRestockingCompartmentPlaybackViewModel.CompartmentName,
                            AreaChangeLink = BuildRestockingActionLink("AreaLabel", application.Id, restocking.Id,
                                fellingCompartmentId, restockingCompartmentId, felling.Id),
                            PercentageChangeLink = BuildRestockingActionLink("PercentageOfRestockAreaLabel",
                                application.Id, restocking.Id, fellingCompartmentId, restockingCompartmentId, felling.Id),
                            DensityChangeLink = BuildRestockingActionLink("RestockingDensityLabel", application.Id,
                                restocking.Id, fellingCompartmentId, restockingCompartmentId, felling.Id),
                            NumberOfTreesChangeLink = BuildRestockingActionLink("NumberOfTreesLabel", application.Id,
                                restocking.Id, fellingCompartmentId, restockingCompartmentId, felling.Id),
                            SpeciesChangeLink = BuildRestockingActionLink("restocking-tree-species-selectLabel",
                                application.Id, restocking.Id, fellingCompartmentId, restockingCompartmentId,
                                felling.Id),
                            FellingLicenceStatus = application.GetCurrentStatus()
                        };

                        restockingDetailPlaybackViewModels.Add(newRestockingDetailPlaybackViewModel);
                    }

                    newRestockingCompartmentPlaybackViewModel.RestockingDetails = restockingDetailPlaybackViewModels
                        .OrderBy(r => r.RestockingCompartmentName).ToList();

                    restockingCompartmentPlaybackViewModels.Add(newRestockingCompartmentPlaybackViewModel);
                }

                newFellingDetailPlaybackViewModel.RestockingCompartmentDetails = restockingCompartmentPlaybackViewModels
                    .OrderBy(r => r.CompartmentName).ToList();

                newFellingCompartmentPlaybackViewModel.FellingDetails.Add(newFellingDetailPlaybackViewModel);
            }

            fellingCompartmentPlaybackViewModels.Add(newFellingCompartmentPlaybackViewModel);
        }

        fellingAndRestockingPlaybackViewModel.FellingCompartmentDetails =
            fellingCompartmentPlaybackViewModels.OrderBy(f => f.CompartmentName).ToList();

        var gisCompartment = compartments.Select(c => new
        {
            c.Id,
            c.GISData,
            DisplayName = c.CompartmentNumber,
            Selected = true
        });

        fellingAndRestockingPlaybackViewModel.GIS = JsonConvert.SerializeObject(gisCompartment);

        if (!application.IsInApplicantEditableState())
        {
            var submittedCompartmentGisDetails = submittedCompartments.Select(x => new
            {
                Id = x.CompartmentId,
                x.GISData,
                DisplayName = x.CompartmentNumber,
                Selected = true
            });
            fellingAndRestockingPlaybackViewModel.GIS = JsonConvert.SerializeObject(submittedCompartmentGisDetails);
        }

        return Maybe<FellingAndRestockingPlaybackViewModel>.From(fellingAndRestockingPlaybackViewModel);
    }

    private UrlActionContext BuildFellingActionLink(
        string fragment,
        Guid applicationId,
        Guid fellingCompartmentId,
        Guid proposedFellingDetailsId)
    {
        return new UrlActionContext
        {
            Action = nameof(FellingLicenceApplicationController.FellingDetail),
            Controller = "FellingLicenceApplication",
            Values = new { applicationId, fellingCompartmentId, proposedFellingDetailsId, returnToPlayback = true },
            Fragment = fragment
        };
    }

    public UrlActionContext BuildRestockingActionLink(
        string fragment,
        Guid applicationId,
        Guid restockingId,
        Guid fellingCompartmentId,
        Guid restockingCompartmentId,
        Guid proposedFellingDetailsId)
    {
        return new UrlActionContext
        {
            Action = nameof(FellingLicenceApplicationController.RestockingDetail),
            Controller = "FellingLicenceApplication",
            Values = new
            {
                applicationId,
                restockingId,
                fellingCompartmentId,
                restockingCompartmentId,
                proposedFellingDetailsId,
                returnToPlayback = true
            },
            Fragment = fragment
        };
    }

    public async Task<Maybe<FellingAndRestockingDetailViewModel>> RetrieveFellingLicenceApplicationCompartmentDetail(
        ExternalApplicant user,
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        var compartmentDetails = await GetCompartmentDetails(compartmentId, user, cancellationToken);

        if (compartmentDetails is null)
        {
            _logger.LogError("Compartment was not found, compartment id: {CompartmentId}", compartmentId);
            return Maybe<FellingAndRestockingDetailViewModel>.None;
        }

        var (woodlandOwnerId, totalHectares, compartmentName) = compartmentDetails.Value;

        var result = await _fellingLicenceApplicationRepository.GetApplicationCompartmentDetailAsync(
            applicationId,
            woodlandOwnerId,
            compartmentId,
            cancellationToken);

        if (result.HasNoValue)
        {
            return Maybe<FellingAndRestockingDetailViewModel>.None;
        }

        var compartments = await GetCompartments(applicationId, cancellationToken);

        var application = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

        var fellingLicenceStatus = GetApplicationStatus(application.Value.StatusHistories);

        var fellingRestockingStatus =
            application.Value.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.SingleOrDefault(
                x =>
                    x.CompartmentId == compartmentId);

        var proposedFellingDetailModels = new List<ProposedFellingDetailModel>();

        foreach (var proposedFellingDetail in result.Value.ProposedFellingDetails)
        {
            proposedFellingDetailModels.Add(
                ModelMapping.ToProposedFellingDetailModel(proposedFellingDetail, totalHectares));
        }

        var compartmentDetailModel = new FellingAndRestockingDetail
        {
            ApplicationId = result.Value.ApplicationId,
            ApplicationReference = result.Value.ApplicationReference,
            CompartmentId = compartmentId,
            CompartmentName = compartmentName,
            Compartments = compartments,
            WoodlandId = result.Value.PropertyProfileId,
            FellingDetails = proposedFellingDetailModels,
            StepComplete = fellingRestockingStatus?.OverallCompletion() ?? false,
            FellingLicenceStatus = fellingLicenceStatus,
            WoodlandOwnerId = woodlandOwnerId
        };

        var profile =
            await GetPropertyProfileByIdAsync(result.Value.PropertyProfileId, user, cancellationToken);

        if (profile.IsFailure)
        {
            _logger.LogWarning("Unable to get property for application, error : {Error}", profile.Error);
            return Maybe<FellingAndRestockingDetailViewModel>.None;
        }

        var woodlandOwnerNameAndAgencyDetails =
            await GetWoodlandOwnerNameAndAgencyForApplication(application.Value, cancellationToken);
        if (woodlandOwnerNameAndAgencyDetails.IsFailure)
        {
            _logger.LogWarning("Unable to get woodland owner details for application, error : {Error}",
                woodlandOwnerNameAndAgencyDetails.Error);
            return Maybe<FellingAndRestockingDetailViewModel>.None;
        }


        return Maybe<FellingAndRestockingDetailViewModel>.From(
            new FellingAndRestockingDetailViewModel
            {
                ApplicationId = result.Value.ApplicationId,
                ApplicationReference = result.Value.ApplicationReference,
                FellingAndRestockingDetail = compartmentDetailModel,
                ApplicationSummary = new FellingLicenceApplicationSummary(
                    result.Value.ApplicationId,
                    result.Value.ApplicationReference,
                    GetApplicationStatus(result.Value.StatusHistories),
                    profile.Value.Name,
                    result.Value.PropertyProfileId,
                    profile.Value.NameOfWood,
                    application.Value.WoodlandOwnerId,
                    woodlandOwnerNameAndAgencyDetails.Value.WoodlandOwnerName,
                    woodlandOwnerNameAndAgencyDetails.Value.AgencyName)
            });
    }

    private async Task<List<FellingAndRestockingDetail>> CreateFellingAndRestockingDetailsList(
        FellingLicenceApplication application,
        ExternalApplicant user,
        FellingLicenceStatus fellingLicenceStatus,
        CancellationToken cancellationToken
    )
    {
        if (application.LinkedPropertyProfile is null)
        {
            return new List<FellingAndRestockingDetail>();
        }

        var compartmentResult =
            await RetrievePropertyProfileCompartmentsAsync(
                user,
                application.LinkedPropertyProfile.PropertyProfileId,
                application.WoodlandOwnerId,
                cancellationToken);

        if (compartmentResult.HasNoValue)
        {
            return new List<FellingAndRestockingDetail>();
        }

        var compartmentsDictionary = compartmentResult.Value.ToDictionary(c => c.Id, c => c);

        // create a dictionary of compartment ids and the felling details that relate to them, since each FellingAndRestockingDetail
        // will need to have the subset of felling details for it's particular compartment

        var compartmentFellingDetailsDictionary = ConvertProposedFellingDetailsToModel(
            application.LinkedPropertyProfile?.ProposedFellingDetails ?? new List<ProposedFellingDetail>(),
            compartmentsDictionary);

        var fellingAndRestockingDetails = new List<FellingAndRestockingDetail>();

        foreach (var compartmentId in compartmentFellingDetailsDictionary.Keys)
        {
            if (compartmentFellingDetailsDictionary[compartmentId].Count > 0)
            {
                fellingAndRestockingDetails.Add(new FellingAndRestockingDetail
                {
                    CompartmentId = compartmentId,
                    CompartmentName = compartmentsDictionary[compartmentId].DisplayName,
                    WoodlandId = application.LinkedPropertyProfile.PropertyProfileId,
                    FellingDetails = compartmentFellingDetailsDictionary[compartmentId],
                    FellingLicenceStatus = fellingLicenceStatus,
                    StepComplete = application.FellingLicenceApplicationStepStatus
                        .CompartmentFellingRestockingStatuses
                        .SingleOrDefault(x => x.CompartmentId == compartmentId)?.OverallCompletion()
                });
            }
        }

        return fellingAndRestockingDetails;
    }

    private static Dictionary<Guid, List<ProposedFellingDetailModel>> ConvertProposedFellingDetailsToModel(
        IList<ProposedFellingDetail> proposedFellingDetails,
        Dictionary<Guid, CompartmentModel> compartmentsDictionary)
    {
        var compartmentFellingDetailsDictionary = new Dictionary<Guid, List<ProposedFellingDetailModel>>();

        foreach (var key in compartmentsDictionary.Keys)
        {
            var fellingDetails = proposedFellingDetails
                .Where(fellingDetails => fellingDetails.PropertyProfileCompartmentId == key).ToList();

            var fellingDetailModels = new List<ProposedFellingDetailModel>();
            fellingDetails.ForEach(fd =>
                fellingDetailModels.Add(
                    ModelMapping.ToProposedFellingDetailModel(fd, compartmentsDictionary[key].TotalHectares)));

            compartmentFellingDetailsDictionary.Add(key, fellingDetailModels);
        }

        return compartmentFellingDetailsDictionary;
    }


    /// <summary>
    /// Updates an application property profile and clears selected compartments
    /// </summary>
    /// <param name="user">An application user</param>
    /// <param name="selectWoodlandModel">Application details including a property profile id</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An operation result including the updated application</returns>
    public async Task<Result<Guid, UserDbErrorReason>> UpdateWoodland(
        ExternalApplicant user,
        SelectWoodlandModel selectWoodlandModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(selectWoodlandModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                selectWoodlandModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }


        var applicationResult =
            await GetFellingLicenceApplicationAsync(selectWoodlandModel.ApplicationId, user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;

        if (application.LinkedPropertyProfile.PropertyProfileId != selectWoodlandModel.PropertyProfileId)
        {
            application.LinkedPropertyProfile.PropertyProfileId = selectWoodlandModel.PropertyProfileId;
            application.LinkedPropertyProfile.ProposedFellingDetails?.Clear();
        }

        application.FellingLicenceApplicationStepStatus.ApplicationDetailsStatus = selectWoodlandModel.StepComplete;

        _fellingLicenceApplicationRepository.Update(application);
        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { application.WoodlandOwnerId, Section = "Application Details" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                        new
                        {
                            application.WoodlandOwnerId, Section = "Application Details", Error = r.GetDescription()
                        }),
                    cancellationToken);
                _logger.LogError(
                    "The application property profile has not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    /// <summary>
    /// Updates the application felling and restocking details.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="fellingDetailModel">The felling detail model.</param>
    /// <param name="restockingDetailModel">The restocking detail model.</param>
    /// <param name="applicationId">The application identifier.</param>
    /// <param name="compartmentId">The compartment identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task<Result<Guid, UserDbErrorReason>> UpdateApplicationFellingDetailsAsync(
        ExternalApplicant user,
        ProposedFellingDetailModel fellingDetailModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(fellingDetailModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                fellingDetailModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult =
            await GetFellingLicenceApplicationAsync(fellingDetailModel.ApplicationId, user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;

        // Update felling details

        var fellingDetail =
            application.LinkedPropertyProfile.ProposedFellingDetails?.FirstOrDefault(d =>
                d.Id == fellingDetailModel.Id);
        if (fellingDetail is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        fellingDetail.FellingSpecies = fellingDetailModel.Species.Values.Select(s => new FellingSpecies
        {
            Species = s.Species,
            ProposedFellingDetailsId = fellingDetail.Id,
            Id = s.Id
        }).ToList();

        fellingDetail.OperationType = fellingDetailModel.OperationType;
        fellingDetail.TreeMarking = fellingDetailModel.IsTreeMarkingUsed ?? false ? fellingDetailModel.TreeMarking : null;
        fellingDetail.NumberOfTrees = fellingDetailModel.NumberOfTrees;
        fellingDetail.AreaToBeFelled = fellingDetailModel.AreaToBeFelled;
        fellingDetail.IsWithinConservationArea = fellingDetailModel.IsWithinConservationArea!.Value;
        fellingDetail.ConservationAreaReference = fellingDetail.IsWithinConservationArea
            ? fellingDetailModel.ConservationAreaReference
            : null;
        fellingDetail.IsPartOfTreePreservationOrder = fellingDetailModel.IsPartOfTreePreservationOrder!.Value;
        fellingDetail.TreePreservationOrderReference = fellingDetail.IsPartOfTreePreservationOrder
            ? fellingDetailModel.TreePreservationOrderReference
            : null;
        fellingDetail.EstimatedTotalFellingVolume = fellingDetailModel.EstimatedTotalFellingVolume;

        // Update felling status.  At this point we've supplied felling details, but not yet the decision to restock and/or the restocking compartments, so
        // the felling status should no longer be null (not started), but false (not complete).
        // However, if we've got to the felling detail direct from the playback screen, we should return there.

        var newFellingStatus = !FellingOperationRequiresStocking(fellingDetailModel.OperationType) ||
                               fellingDetailModel.ReturnToPlayback;

        var compartmentFellingRestockingStatus =
            application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses
                .Single(x => x.CompartmentId == fellingDetailModel.FellingCompartmentId);

        var statusForThisFelling =
            compartmentFellingRestockingStatus.FellingStatuses.Find(fs => fs.Id == fellingDetailModel.Id);

        if (statusForThisFelling != null)
        {
            statusForThisFelling.Status = newFellingStatus;
        }

        _fellingLicenceApplicationRepository.Update(application);

        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { application.WoodlandOwnerId, Section = "Felling Details" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                        new { application.WoodlandOwnerId, Section = "Felling Details", Error = r.GetDescription() }),
                    cancellationToken);
                _logger.LogError(
                    "The application felling restocking details status has not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    public async Task<Result<Guid, UserDbErrorReason>> UpdateApplicationFellingDetailsWithRestockDecisionAsync(
        ExternalApplicant user,
        DecisionToRestockViewModel decisionToRestockViewModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(decisionToRestockViewModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                decisionToRestockViewModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult =
            await GetFellingLicenceApplicationAsync(decisionToRestockViewModel.ApplicationId, user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;

        // Update felling details

        var fellingDetail =
            application.LinkedPropertyProfile.ProposedFellingDetails?.FirstOrDefault(d =>
                d.Id == decisionToRestockViewModel.ProposedFellingDetailsId);
        if (fellingDetail is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        // did they previously say Yes to restocking, but are changing it to No?
        if (fellingDetail.IsRestocking.HasValue && fellingDetail.IsRestocking.Value &&
            !decisionToRestockViewModel.IsRestockSelected)
        {
            fellingDetail.ProposedRestockingDetails.Clear();

            // also remove the status for any restockings in this felling.

            var compartmentStatus =
                application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Find(c =>
                    c.CompartmentId == decisionToRestockViewModel.FellingCompartmentId);

            if (compartmentStatus != null)
            {
                var fellingStatus =
                    compartmentStatus.FellingStatuses.Find(f =>
                        f.Id == decisionToRestockViewModel.ProposedFellingDetailsId);

                if (fellingStatus != null)
                {
                    fellingStatus.RestockingCompartmentStatuses.Clear();
                }
            }
        }

        fellingDetail.IsRestocking = decisionToRestockViewModel.IsRestockSelected;
        fellingDetail.NoRestockingReason = decisionToRestockViewModel.IsRestockSelected
            ? string.Empty
            : decisionToRestockViewModel.Reason;

        // if the restock decision is No, we can set the status for this felling to True (complete).

        if (!decisionToRestockViewModel.IsRestockSelected)
        {
            var compartmentStatus =
                application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Find(c =>
                    c.CompartmentId == decisionToRestockViewModel.FellingCompartmentId);

            if (compartmentStatus != null)
            {
                var fellingStatus =
                    compartmentStatus.FellingStatuses.Find(f =>
                        f.Id == decisionToRestockViewModel.ProposedFellingDetailsId);

                if (fellingStatus != null)
                {
                    fellingStatus.Status = true;
                }
            }
        }

        _fellingLicenceApplicationRepository.Update(application);

        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { application.WoodlandOwnerId, Section = "Felling Details" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                        new { application.WoodlandOwnerId, Section = "Felling Details", Error = r.GetDescription() }),
                    cancellationToken);
                _logger.LogError(
                    "The application felling restocking details status has not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    /// <summary>
    /// Updates the application reference number.
    /// </summary>
    /// <param name="user">The user to get the application against</param>
    /// <param name="applicationId">The application Id to find</param>
    /// <param name="referenceNumber">The value to update with</param>
    /// <param name="cancellationToken">The cancelation token</param>
    /// <returns>The result</returns>
    public async Task<Result<Guid, UserDbErrorReason>> UpdateApplicationReferenceAysnc(
        ExternalApplicant user,
        Guid applicationId,
        string referenceNumber,
        CancellationToken cancellationToken)
    {
        var applicationResult = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

        if (applicationResult.IsFailure)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;

        application.ApplicationReference = referenceNumber;

        _fellingLicenceApplicationRepository.Update(application);

        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { application.WoodlandOwnerId, Section = "Application Reference" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                        new
                        {
                            application.WoodlandOwnerId, Section = "Application Reference", Error = r.GetDescription()
                        }),
                    cancellationToken);
                _logger.LogError(
                    "Failed to update the application reference with error {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    /// <summary>
    /// Updates the application felling and restocking details.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="restockingDetailModel">The restocking detail model.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task<Result<Guid, UserDbErrorReason>> UpdateApplicationRestockingDetailsAsync(
        ExternalApplicant user,
        ProposedRestockingDetailModel restockingDetailModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(restockingDetailModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                restockingDetailModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult =
            await GetFellingLicenceApplicationAsync(restockingDetailModel.ApplicationId, user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;

        // Update restocking detail

        var fellingDetail =
            application.LinkedPropertyProfile!.ProposedFellingDetails!.FirstOrDefault(pfd =>
                pfd.Id == restockingDetailModel.ProposedFellingDetailsId);

        if (fellingDetail == null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var restockingDetail =
            fellingDetail.ProposedRestockingDetails.FirstOrDefault(prd => prd.Id == restockingDetailModel.Id);

        if (restockingDetail is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        restockingDetail.RestockingSpecies = restockingDetailModel.Species.Values.Select(s => new RestockingSpecies
        {
            Species = s.Species,
            Percentage = s.Percentage,
            ProposedRestockingDetailsId = restockingDetail.Id,
            Id = s.Id
        }).ToList();

        restockingDetail.RestockingProposal = restockingDetailModel.RestockingProposal;
        restockingDetail.Area = restockingDetailModel.Area;
        restockingDetail.RestockingDensity = restockingDetailModel.RestockingDensity;
        restockingDetail.NumberOfTrees = restockingDetailModel.NumberOfTrees;
        restockingDetail.PercentageOfRestockArea = restockingDetailModel.PercentageOfRestockArea;

        // Update compartment status

        var compartmentFellingRestockingStatus =
            application.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses
                .Single(x => x.CompartmentId == restockingDetailModel.FellingCompartmentId);

        var statusForThisFelling =
            compartmentFellingRestockingStatus.FellingStatuses.Find(fs =>
                fs.Id == restockingDetailModel.ProposedFellingDetailsId);
        var restockingCompartmentStatus = statusForThisFelling.RestockingCompartmentStatuses.Find(rcs =>
            rcs.CompartmentId == restockingDetailModel.RestockingCompartmentId);

        if (statusForThisFelling != null)
        {
            var statusForThisRestocking =
                restockingCompartmentStatus.RestockingStatuses.Find(rs => rs.Id == restockingDetailModel.Id);

            if (statusForThisRestocking is null)
            {
                return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
            }

            statusForThisRestocking.Status = restockingDetailModel.StepComplete;
        }

        _fellingLicenceApplicationRepository.Update(application);

        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { application.WoodlandOwnerId, Section = "Restocking Details" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                        new
                        {
                            application.WoodlandOwnerId, Section = "Restocking Details", Error = r.GetDescription()
                        }),
                    cancellationToken);
                _logger.LogError(
                    "The application restocking details have not been updated due to reason {ErrorReason} for application id: {ApplicationId}, restocking id: {RestockingId}",
                    r.GetDescription(), application.Id, restockingDetailModel.Id);
            });
    }

    private static FellingLicenceStatus GetApplicationStatus(IList<StatusHistory> statusHistories) =>
        statusHistories.Any()
            ? (FellingLicenceStatus)statusHistories.OrderByDescending(s => s.Created).First()
                .Status
            : FellingLicenceStatus.Draft;

    private async Task<(Guid, double?, string)?> GetCompartmentDetails(
        Guid compartmentId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var result = await base.GetCompartmentByIdAsync(compartmentId, user, cancellationToken);

        return result.IsSuccess
            ? (result.Value.PropertyProfile.WoodlandOwnerId, result.Value.TotalHectares,
                ModelMapping.ToCompartmentModel(result.Value).DisplayName)
            : null;
    }


    private async Task<List<CompartmentModel>> GetCompartments(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var compartmentLookup =
            await _fellingLicenceApplicationRepository.GetApplicationComparmentIdsAsync(applicationId,
                cancellationToken);

        if (compartmentLookup.HasNoValue)
        {
            return new List<CompartmentModel>();
        }

        var compartments = await _compartmentRepository.ListAsync(compartmentLookup.Value, cancellationToken);
        return compartments.Select(ModelMapping.ToCompartmentModel).ToList();
    }

    private async Task<Maybe<IReadOnlyCollection<CompartmentModel>>> RetrievePropertyProfileCompartmentsAsync(
        ExternalApplicant user,
        Guid propertyProfileId,
        Guid woodlandOwnerId,
        CancellationToken cancellationToken = default)
    {
        var compartments =
            (await _compartmentRepository.ListAsync(propertyProfileId, woodlandOwnerId,
                cancellationToken)).ToList();

        return !compartments.Any()
            ? Maybe<IReadOnlyCollection<CompartmentModel>>.None
            : Maybe<IReadOnlyCollection<CompartmentModel>>.From(ModelMapping.ToCompartmentModelList(compartments)
                .ToList());
    }

    /// <summary>
    /// Sets application operation details
    /// </summary>
    /// <param name="user">An application user</param>
    /// <param name="operationDetailsModel">An operation details model</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result containing the application identifier if successful, or a <see cref="UserDbErrorReason"/> if unsuccessful.</returns>
    public async Task<Result<Guid, UserDbErrorReason>> SetApplicationOperationsAsync(
        ExternalApplicant user,
        OperationDetailsModel operationDetailsModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(operationDetailsModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);

        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                operationDetailsModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult =
            await GetFellingLicenceApplicationAsync(operationDetailsModel.ApplicationId, user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;

        if (operationDetailsModel is
            {
                DisplayDateReceived: true,
                DateReceived: not null
            }
            && operationDetailsModel.DateReceived!.IsPopulated()
            && (user.IsFcUser || user.AccountType is AccountTypeExternal.FcUser))
        {
            application.DateReceived = operationDetailsModel.DateReceived.CalculateDate().ToUniversalTime();
        }

        application.Measures = operationDetailsModel.Measures;
        application.Source = operationDetailsModel.ApplicationSource ?? FellingLicenceApplicationSource.ApplicantUser;
        application.ProposedTiming = operationDetailsModel.ProposedTiming;
        application.ProposedFellingStart =
            operationDetailsModel.ProposedFellingStart?.CalculateDate().ToUniversalTime();
        application.ProposedFellingEnd =
            operationDetailsModel.ProposedFellingEnd?.CalculateDate().ToUniversalTime();

        application.FellingLicenceApplicationStepStatus.OperationsStatus = operationDetailsModel.StepComplete;

        _fellingLicenceApplicationRepository.Update(application);

        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplication,
                        appId,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            WoodlandOwnerId = user.WoodlandOwnerId,
                            Section = "Operation Details"
                        }),
                    cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure,
                        null,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            WoodlandOwnerId = user.WoodlandOwnerId,
                            Section = "Operation Details",
                            Error = r.GetDescription()
                        }),
                    cancellationToken);

                _logger.LogError(
                    "The operation details have not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    /// <summary>
    /// Sets Application Constraint Check Status
    /// </summary>
    /// <param name="user">An application user</param>
    /// <param name="constraintCheckModel">An Constraints check details model</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public async Task<Result<Guid, UserDbErrorReason>> SetApplicationConstraintCheckAsync(
        ExternalApplicant user,
        ConstraintCheckModel constraintCheckModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(constraintCheckModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                constraintCheckModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult =
            await GetFellingLicenceApplicationAsync(constraintCheckModel.ApplicationId,
                user,
                cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;

        application.NotRunningExternalLisReport = constraintCheckModel.NotRunningExternalLisReport.Value;
        application.FellingLicenceApplicationStepStatus.ConstraintCheckStatus = constraintCheckModel.StepComplete;
        if (constraintCheckModel.ExternalLisReportRun != null && constraintCheckModel.ExternalLisReportRun.Value)
        {
            application.ExternalLisAccessedTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc();
        }

        _fellingLicenceApplicationRepository.Update(application);
        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId, Section = "Constraint Details" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                        new { user.WoodlandOwnerId, Section = "Constraint Details", Error = r.GetDescription() }),
                    cancellationToken);
                _logger.LogError(
                    "The Constraint details have not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    /// <summary>
    /// Updates an application property profile and clears selected compartments
    /// </summary>
    /// <param name="user">An application user</param>
    /// <param name="supportingDocumentationSaveModel">Application details including a property profile id</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An operation result including the updated application</returns>
    public async Task<Result<Guid, UserDbErrorReason>> UpdateSupportingDocumentsStatusAsync(
        ExternalApplicant user,
        SupportingDocumentationSaveModel supportingDocumentationSaveModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(supportingDocumentationSaveModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                supportingDocumentationSaveModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult =
            await GetFellingLicenceApplicationAsync(supportingDocumentationSaveModel.ApplicationId,
                user,
                cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var fellingLicenceApplication = applicationResult.Value;

        fellingLicenceApplication.FellingLicenceApplicationStepStatus.SupportingDocumentationStatus =
            supportingDocumentationSaveModel.StepComplete;

        _fellingLicenceApplicationRepository.Update(fellingLicenceApplication);

        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => fellingLicenceApplication.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId, Section = "Supporting Documents" }), cancellationToken);
                _logger.LogError(
                    "Supporting Documents status has not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), fellingLicenceApplication.Id);
            });
    }

    /// <summary>
    /// Sets application terms and conditions accepted
    /// </summary>
    /// <param name="user">An application user</param>
    /// <param name="flaTermsAndConditionsViewModel">An FLA Terms and Conditions model</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public async Task<Result<Guid, UserDbErrorReason>> SetFlaTermsAndConditionsAccepted(
        ExternalApplicant user,
        FlaTermsAndConditionsViewModel flaTermsAndConditionsViewModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(flaTermsAndConditionsViewModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                flaTermsAndConditionsViewModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult =
            await GetFellingLicenceApplicationAsync(flaTermsAndConditionsViewModel.ApplicationId,
                user,
                cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;
        application.TermsAndConditionsAccepted = flaTermsAndConditionsViewModel.TermsAndConditionsAccepted;

        application.FellingLicenceApplicationStepStatus.TermsAndConditionsStatus =
            flaTermsAndConditionsViewModel.TermsAndConditionsAccepted;

        _fellingLicenceApplicationRepository.Update(application);

        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplicationFailure, null, user.UserAccountId, _requestContext,
                    new { user.WoodlandOwnerId, Section = "Terms and Conditions" }), cancellationToken);
                _logger.LogError(
                    "Terms and conditions has not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    /// <summary>
    /// Processes the submission of an application.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="user">The <see cref="ExternalApplicant"/> submitting the application.</param>
    /// <param name="linkToApplication">A textual representation of a link to the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> containing a flag indicating whether the submission is a resubmission, or an error in the event of failure.</returns>
    public async Task<Result> SubmitFellingLicenceApplicationAsync(
        Guid applicationId,
        ExternalApplicant user,
        string linkToApplication,
        CancellationToken cancellationToken)
    {
        var isResubmission = false;
        try
        {
            // Update the final action date
            var now = _clock.GetCurrentInstant();

            var userAccess = await GetUserAccessModelAsync(user, cancellationToken).ConfigureAwait(false);
            if (userAccess.IsFailure)
            {
                await PublishFailures(applicationId, user, cancellationToken, isResubmission,
                    "Could not retrieve user access");
                return Result.Failure("Could not update Felling Licence Application to submitted state");
            }

            //TODO Move the submitted fla property compartments and convert f&r processes into this service call
            //so if they fail the whole application is not in already in a submitted state

            var updateResult = await _updateFellingLicenceApplicationService
                .SubmitFellingLicenceApplicationAsync(applicationId, userAccess.Value, cancellationToken)
                .ConfigureAwait(false);

            if (updateResult.IsFailure)
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            Section = "Submit FLA",
                            Error = updateResult.Error
                        }),
                    cancellationToken);

                await PublishFailures(applicationId, user, cancellationToken, isResubmission, updateResult.Error);
                return Result.Failure("Could not update Felling Licence Application to submitted state");
            }

            isResubmission = updateResult.Value.PreviousStatus !=
                             Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus.Draft;

            // Get property profile via linked property profile table
            var propertyProfile =
                await GetPropertyProfileByIdAsync(updateResult.Value.PropertyProfileId, user, cancellationToken);
            var propertyProfileCompartments = propertyProfile.Value.Compartments;

            // Get Selected compartments to use
            var relevantCompartments = GetRelevantCompartments(user, applicationId, cancellationToken);

            var relevantPropertyProfileCompartments = propertyProfileCompartments
                .Where(p => relevantCompartments.Result.Value.SelectedCompartmentIds.Contains(p.Id));

            // Map from property profile, compartments to submitted FLA snapshot models
            var submittedFlaPropertyDetail = ModelMapping.ToSubmittedFlaPropertyDetail(propertyProfile.Value);

            // Set the FellingLicenceApplication Id
            submittedFlaPropertyDetail.FellingLicenceApplicationId = applicationId;

            var submittedFlaPropertyCompartments =
                ModelMapping.ToSubmittedFlaPropertyCompartmentList(relevantPropertyProfileCompartments);
            submittedFlaPropertyDetail.SubmittedFlaPropertyCompartments = submittedFlaPropertyCompartments.ToList();

            var addPropertyResult = await _updateFellingLicenceApplicationService
                .AddSubmittedFellingLicenceApplicationPropertyDetailAsync(submittedFlaPropertyDetail, cancellationToken)
                .ConfigureAwait(false);

            if (addPropertyResult.IsFailure)
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            Section = "Submit FLA (Submitted Property details)",
                            Error = addPropertyResult.Error
                        }),
                    cancellationToken);

                await PublishFailures(applicationId, user, cancellationToken, isResubmission, addPropertyResult.Error);
                return Result.Failure("Could not update Felling Licence Application property details");
            }

            var convertFellingResult = await _updateFellingLicenceApplicationService
                .ConvertProposedFellingAndRestockingToConfirmedAsync(applicationId, userAccess.Value, cancellationToken)
                .ConfigureAwait(false);

            if (convertFellingResult.IsFailure)
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            Section = "Submit FLA (Confirmed felling and restocking details)",
                            Error = convertFellingResult.Error
                        }),
                    cancellationToken);

                await PublishFailures(applicationId, user, cancellationToken, isResubmission, convertFellingResult.Error);
                return Result.Failure("Could not update Felling Licence Application confirmed felling and restocking details");
            }

            var reference = updateResult.Value.ApplicationReference;

            var adminHubFooter = string.IsNullOrWhiteSpace(updateResult.Value.AdminHubName)
                ? string.Empty
                : await _getConfiguredFcAreasService
                    .TryGetAdminHubAddress(updateResult.Value.AdminHubName, cancellationToken)
                    .ConfigureAwait(false);

            var woodlandDetails =
                await _foresterServices.GetWoodlandOfficerAsync(
                    submittedFlaPropertyCompartments.Select(p => p.GISData).ToList()!, cancellationToken);

            if (!woodlandDetails.IsFailure)
            {
                reference = _applicationReferenceHelper.UpdateReferenceNumber(updateResult.Value.ApplicationReference,
                    woodlandDetails.Value.Code!);
                await UpdateApplicationReferenceAysnc(user, applicationId, reference, cancellationToken);
            }


            var submissionConfirmationModel = new ApplicationSubmissionConfirmationDataModel
            {
                ApplicationReference = reference,
                Name = user.FullName!,
                PropertyName = submittedFlaPropertyDetail.Name,
                ViewApplicationURL = linkToApplication,
                AdminHubFooter = adminHubFooter,
                ApplicationId = applicationId
            };

            // TODO this should be in a service not the repo
            var woodlandOwner =
                await _woodlandOwnerRepository.GetAsync(
                    updateResult.Value.WoodlandOwnerId,
                    cancellationToken);

            var notificationResult = await _sendNotifications.SendNotificationAsync(
                submissionConfirmationModel,
                NotificationType.ApplicationSubmissionConfirmation,
                new NotificationRecipient(
                    user.EmailAddress!,
                    user.FullName),
                copyToRecipients: GetWoodlandOwnerCopyToRecipient(
                    user.EmailAddress,
                    woodlandOwner.IsSuccess ? woodlandOwner.Value : null),
                cancellationToken: cancellationToken);

            if (notificationResult.IsSuccess)
            {
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.FellingLicenceApplicationSubmissionNotificationSent,
                        applicationId,
                        user.UserAccountId!.Value,
                        _requestContext,
                        new
                        {
                            RecipientId = user.UserAccountId!.Value,
                            RecipientName = user.FullName,
                            RecipientRole = AssignedUserRole.Author,
                            RecipientEmail = user.EmailAddress
                        }),
                    cancellationToken);
            }

            if (updateResult.Value.PreviousStatus !=
                Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus.Draft)
            {
                // Notify FC (internal) users already assigned to the application to inform them of resubmission

                // TODO this should go to a service not a repo class
                var internalUsers = await _internalUserAccountRepository
                    .GetUsersWithIdsInAsync(updateResult.Value.AssignedInternalUsers, cancellationToken)
                    .ConfigureAwait(false);

                if (internalUsers.IsSuccess)
                {
                    var internalSiteApplicationLink = 
                        $"{_internalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{applicationId}";
                    
                    foreach (var internalUser in internalUsers.Value)
                    {
                        // Send a notification

                        var recipient = new NotificationRecipient(
                            internalUser.Email,
                            internalUser.FullName(false));

                        var notificationModel = new ApplicationResubmittedDataModel
                        {
                            ApplicationReference = updateResult.Value.ApplicationReference,
                            Name = recipient.Name!,
                            PropertyName = submittedFlaPropertyDetail.Name,
                            ViewApplicationURL = internalSiteApplicationLink,
                            AdminHubFooter = adminHubFooter,
                            ApplicationId = applicationId
                        };

                        var sendNotificationResult = await _sendNotifications.SendNotificationAsync(
                            notificationModel,
                            NotificationType.ApplicationResubmitted,
                            recipient,
                            senderName: user.FullName,
                            cancellationToken: cancellationToken);

                        if (sendNotificationResult.IsFailure)
                        {
                            _logger.LogError(
                                "Could not send notification for resubmission of {ApplicationId} back to internal user id {InternalUserId} due to error: {SendNotificationError}",
                                applicationId, internalUser.Id, sendNotificationResult.Error);
                        }
                    }
                }
            }

            // Select audit event depending on whether this is a resubmission

            if (!isResubmission)
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.SubmitFellingLicenceApplication, applicationId, user.UserAccountId, _requestContext,
                    new { WoodlandOwner = user.WoodlandOwnerId }), cancellationToken);
            }
            else
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.ResubmitFellingLicenceApplication, applicationId, user.UserAccountId, _requestContext,
                    new { WoodlandOwner = user.WoodlandOwnerId }), cancellationToken);
            }

            // enqueue the asynchronous generation of a licence preview PDF

            await _busControl.Publish(
                new GeneratePdfPreviewMessage(
                    user.UserAccountId!.Value,
                    applicationId),
                cancellationToken);

            // enqueue the asynchronous Assigning of the Woodland officer

            if (!isResubmission)
            {
                var internalSiteApplicationLink =
                    $"{_internalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{applicationId}";
                await _busControl.Publish(new AssignWoodlandOfficerMessage(
                    internalSiteApplicationLink,
                    updateResult.Value.WoodlandOwnerId,
                    user.UserAccountId.Value,
                    user.IsFcUser,
                    string.IsNullOrEmpty(user.AgencyId) ? null : Guid.Parse(user.AgencyId),
                    applicationId), cancellationToken);
            }

            // enqueue the asynchronous of a larch risk zones message
            await _busControl.Publish(
                new GetLarchRiskZonesMessage(
                    submittedFlaPropertyCompartments.Select(compartment => compartment.Id),
                    user.UserAccountId.Value,
                    applicationId),
                cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await PublishFailures(applicationId, user, cancellationToken, isResubmission, ex.Message, ex);
            return Result.Failure($"Felling licence application failure, application id: {applicationId}");
        }
    }

    private async Task PublishFailures(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken,
        bool isResubmission,
        string message,
        Exception? ex = null)
    {
        if (!isResubmission)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.SubmitFellingLicenceApplicationFailure, applicationId, user.UserAccountId, _requestContext,
                new { WoodlandOwner = user.WoodlandOwnerId, Error = message }), cancellationToken);
        }
        else
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ResubmitFellingLicenceApplicationFailure, applicationId, user.UserAccountId,
                _requestContext,
                new { WoodlandOwner = user.WoodlandOwnerId, Error = message }), cancellationToken);
        }

        if (ex != null)
        {
            _logger.LogError(ex, "Felling licence application failure, application id: {FellingLicenceApplicationId}",
                applicationId);
        }
    }


    /// <summary>
    /// Withdraws a felling licence application for the specified application ID.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application to withdraw.</param>
    /// <param name="user">The external applicant requesting the withdrawal.</param>
    /// <param name="linkToApplication">A URL link to the application for reference in notifications.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the withdrawal operation.</returns>
    public async Task<Result> WithdrawFellingLicenceApplicationAsync(
        Guid applicationId,
        ExternalApplicant user,
        string linkToApplication,
        CancellationToken cancellationToken)
    {
        var userAccessModel = await GetUserAccessModelAsync(user, cancellationToken);
        if (userAccessModel.IsFailure)
        {
            _logger.LogError(
                "Could not Withdraw the Felling Licence Application with ID {ApplicationId} when requested by user with ID {UserAccountId}, user access to this application was denied",
                applicationId,
                user.UserAccountId);
            return Result.Failure(
                $"Attempt to access Felling Licence Application with id: {applicationId} by user with Id of {user.UserAccountId} resulted in access being denied");
        }

        await using var transaction = await _fellingLicenceApplicationRepository.BeginTransactionAsync(cancellationToken);

        try
        {
            var resultWithdrawal =
                await _withdrawFellingLicenceService.WithdrawApplication(
                    applicationId,
                    userAccessModel.Value,
                    cancellationToken);
            if (resultWithdrawal.IsFailure)

            {
                await transaction.RollbackAsync(cancellationToken);
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.WithdrawFellingLicenceApplicationFailure,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            user.WoodlandOwnerId,
                            Section = "Withdraw FLA",
                            resultWithdrawal.Error
                        }), cancellationToken);
                _logger.LogError(
                    "Could not withdraw the Felling Licence Application with ID {ApplicationId} when requested by user with ID {UserAccountId}",
                    applicationId,
                    user.UserAccountId);
                return Result.Failure($"Could not withdraw the {nameof(FellingLicenceApplication)}");
            }

            var (_, isFailure, fellingLicenceApplication) =
                await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

            if (isFailure || fellingLicenceApplication.LinkedPropertyProfile is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.FellingLicenceApplicationWithdrawFailure,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            WoodlandOwner = user.WoodlandOwnerId,
                            Error = $"Failed to get {nameof(FellingLicenceApplication)} with ID {applicationId}"
                        }), cancellationToken);

                _logger.LogError(
                    "Failed to get Felling Licence Application with ID {ApplicationId}",
                    applicationId);

                return Result.Failure($"Failed to get {nameof(FellingLicenceApplication)}");
            }

            if (fellingLicenceApplication.PublicRegister.ShouldApplicationBeRemovedFromConsultationPublicRegister())
            {
                var publicRegisterRemovalResult = await _publicRegisterService.RemoveCaseFromConsultationRegisterAsync(
                    fellingLicenceApplication.PublicRegister!.EsriId!.Value,
                    fellingLicenceApplication.ApplicationReference,
                    _clock.GetCurrentInstant().ToDateTimeUtc(),
                    cancellationToken);

                if (publicRegisterRemovalResult.IsFailure)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    await _auditService.PublishAuditEventAsync(
                        new AuditEvent(
                            AuditEvents.WithdrawFellingLicenceApplicationFailure,
                            applicationId,
                            user.UserAccountId,
                            _requestContext,
                            new
                            {
                                user.WoodlandOwnerId,
                                Section = "Withdraw FLA",
                                publicRegisterRemovalResult.Error
                            }), cancellationToken);
                    _logger.LogError(
                        "Could not remove the Felling Licence Application with ID {ApplicationId} from the public register when requested by user with ID {UserAccountId}",
                        applicationId,
                        user.UserAccountId);
                    return Result.Failure(
                        $"Could not remove the {nameof(FellingLicenceApplication)} from the public register");
                }

                var timestamp = _clock.GetCurrentInstant().ToDateTimeUtc();

                var updateResult = await withdrawFellingLicenceService.UpdatePublicRegisterEntityToRemovedAsync(
                    applicationId,
                    user.UserAccountId,
                    timestamp,
                    cancellationToken);

                if (updateResult.IsFailure)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    await _auditService.PublishAuditEventAsync(
                        new AuditEvent(
                            AuditEvents.WithdrawFellingLicenceApplicationFailure,
                            applicationId,
                            user.UserAccountId,
                            _requestContext,
                            new
                            {
                                WoodlandOwner = user.WoodlandOwnerId,
                                Section = "Withdraw FLA",
                                updateResult.Error
                            }), cancellationToken);
                    _logger.LogError(
                        "Could not update the public register data for Felling Licence Application with ID {ApplicationId} requested by user with ID {UserAccountId}",
                        applicationId,
                        user.UserAccountId);
                    return Result.Failure(
                        $"Could not update the {nameof(FellingLicenceApplication)} public register data");
                }
            }

            await transaction.CommitAsync(cancellationToken);

            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.WithdrawFellingLicenceApplication,
                    applicationId,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        WoodlandOwner = user.WoodlandOwnerId
                    }), cancellationToken);

            if (resultWithdrawal.Value.Count > 0)
            {
                var resultRemovedOfficers = await _withdrawFellingLicenceService.RemoveAssignedWoodlandOfficerAsync(
                    applicationId,
                    resultWithdrawal.Value,
                    cancellationToken);
                if (resultRemovedOfficers.IsFailure)
                {
                    _logger.LogError(
                        "Could not remove the assignment of all the users linked to the Felling Licence Application with ID {ApplicationId} when requested by user with ID {UserAccountId}",
                        applicationId,
                        user.UserAccountId);
                }
            }

            var propertyResult = await GetPropertyProfileByIdAsync(
                    fellingLicenceApplication.LinkedPropertyProfile.PropertyProfileId, user, cancellationToken)
                .ConfigureAwait(false);
            if (propertyResult.IsFailure)
            {
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.FellingLicenceApplicationWithdrawFailure,
                        applicationId,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            WoodlandOwner = user.WoodlandOwnerId,
                            Error =
                                $"Failed to get {nameof(PropertyProfile)} with ID {fellingLicenceApplication.LinkedPropertyProfile.PropertyProfileId}"
                        }), cancellationToken);

                _logger.LogError(
                    "Failed to get Property Profile with ID {PropertyProfileId}",
                    fellingLicenceApplication.LinkedPropertyProfile.PropertyProfileId);

                return Result.Failure($"Failed to get {nameof(PropertyProfile)}");
            }

            var adminHubFooter = string.IsNullOrWhiteSpace(fellingLicenceApplication.AdministrativeRegion)
                ? string.Empty
                : await _getConfiguredFcAreasService
                    .TryGetAdminHubAddress(fellingLicenceApplication.AdministrativeRegion, cancellationToken)
                    .ConfigureAwait(false);

            var applicationWithdrawnModel = new ApplicationWithdrawnConfirmationDataModel
            {
                ApplicationReference = fellingLicenceApplication.ApplicationReference,
                PropertyName = propertyResult.Value.Name,
                Name = user.FullName!,
                ViewApplicationURL = linkToApplication,
                AdminHubFooter = adminHubFooter,
                ApplicationId = applicationId
            };

            var woodlandOwner =
                await _woodlandOwnerRepository.GetAsync(
                    fellingLicenceApplication.WoodlandOwnerId,
                    cancellationToken);

            var notificationResult = await _sendNotifications.SendNotificationAsync(
                applicationWithdrawnModel,
                NotificationType.ApplicationWithdrawnConfirmation,
                new NotificationRecipient(
                    user.EmailAddress!,
                    user.FullName),
                copyToRecipients: GetWoodlandOwnerCopyToRecipient(user.EmailAddress,
                    woodlandOwner.IsSuccess ? woodlandOwner.Value : null),
                cancellationToken: cancellationToken);

            if (notificationResult.IsSuccess)
            {
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.FellingLicenceApplicationWithdrawNotificationSent,
                        fellingLicenceApplication.Id,
                        user.UserAccountId!.Value,
                        _requestContext,
                        new
                        {
                            RecipientId = user.UserAccountId!.Value,
                            RecipientName = user.FullName,
                            RecipientEmail = user.EmailAddress,
                            RecipientRole = AssignedUserRole.Author,
                        }),
                    cancellationToken);
            }
            else
            {
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.FellingLicenceApplicationWithdrawNotificationSentFailed,
                        fellingLicenceApplication.Id,
                        user.UserAccountId!.Value,
                        _requestContext,
                        new
                        {
                            RecipientId = user.UserAccountId!.Value,
                            RecipientName = user.FullName,
                            RecipientEmail = user.EmailAddress,
                            RecipientRole = AssignedUserRole.Author,
                            ErrorDetails = notificationResult.Error
                        }),
                    cancellationToken);
            }

            // Notify FC (internal) users already assigned to the application to inform them of the withdrawal
            var internalUsers =
                await _internalUserAccountRepository.GetUsersWithIdsInAsync(resultWithdrawal.Value, cancellationToken);
            if (internalUsers.IsSuccess)
            {
                foreach (var internalUser in internalUsers.Value)
                {
                    var recipient = new NotificationRecipient(
                        internalUser.Email,
                        internalUser.FullName(false));

                    var internalSiteApplicationLink =
                        $"{_internalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{applicationId}";

                    var notificationModel = new ApplicationWithdrawnConfirmationDataModel
                    {
                        ApplicationReference = fellingLicenceApplication.ApplicationReference,
                        Name = recipient.Name!,
                        PropertyName = propertyResult.Value.Name,
                        ViewApplicationURL = internalSiteApplicationLink,
                        AdminHubFooter = adminHubFooter,
                        ApplicationId = applicationId
                    };

                    var sendNotificationResult = await _sendNotifications.SendNotificationAsync(
                        notificationModel,
                        NotificationType.ApplicationWithdrawn,
                        recipient,
                        senderName: user.FullName,
                        cancellationToken: cancellationToken);

                    if (sendNotificationResult.IsFailure)
                    {
                        _logger.LogError(
                            "Could not send notification for withdrawal of {ApplicationId} back to internal user (Id {InternalUserId}): {Error}",
                            internalUser.Id, 
                            applicationId, 
                            sendNotificationResult.Error);
                    }
                }
            }

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.FellingLicenceApplicationWithdrawComplete, applicationId, user.UserAccountId,
                _requestContext,
                new { WoodlandOwner = user.WoodlandOwnerId }), cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing application id {ApplicationId}", applicationId);
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.WithdrawFellingLicenceApplicationFailure, applicationId, user.UserAccountId,
                _requestContext,
                new { WoodlandOwner = user.WoodlandOwnerId, Error = ex.Message }), cancellationToken);
            return Result.Failure(
                $"Withdrawal failure, application id: {applicationId}, error: {ex.Message}");
        }
    }

    public async Task<Result<Guid>> DeleteDraftFellingLicenceApplicationAsync(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.DeleteDraftFellingLicenceApplication, applicationId, user.UserAccountId, _requestContext,
            new { user.WoodlandOwnerId }), cancellationToken);

        var applicationResult = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.DeleteDraftFellingLicenceApplicationFailure, applicationId, user.UserAccountId,
                _requestContext,
                new
                {
                    user.WoodlandOwnerId,
                    Error = $"Failed to get {nameof(FellingLicenceApplication)} with ID {applicationId}"
                }), cancellationToken);

            _logger.LogError("Failed to get application with ID {ApplicationId}", applicationId);

            return Result.Failure<Guid>($"Failed to get {nameof(FellingLicenceApplication)}");
        }

        var fellingLicenceApplication = applicationResult.Value;
        var woodlandOwnerId = fellingLicenceApplication.WoodlandOwnerId;

        var userAccessModel = await GetUserAccessModelAsync(user, cancellationToken);

        if (userAccessModel.IsFailure)
        {
            _logger.LogError(
                "Could not Delete the application with ID {ApplicationId} when requested by user with id {UserId}, user access to this application was denied",
                applicationId, user.UserAccountId);
            return Result.Failure<Guid>(
                $"Attempt to access Felling Licence Application with id: {applicationId} by user with Id of {user.UserAccountId} resulted in access being denied");
        }

        var resultDelete =
            await _deleteFellingLicenceService.DeleteDraftApplicationAsync(
                applicationId,
                userAccessModel.Value,
                cancellationToken);
        if (resultDelete.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.DeleteDraftFellingLicenceApplicationFailure, applicationId, user.UserAccountId,
                _requestContext,
                new { WoodlandOwner = user.WoodlandOwnerId, resultDelete.Error }), cancellationToken);

            _logger.LogError(
                "Could not Delete the application with ID {ApplicationId} when requested by user with id {UserId}",
                applicationId, user.UserAccountId);

            return Result.Failure<Guid>($"Could not Delete the {nameof(FellingLicenceApplication)}");
        }

        var documents = fellingLicenceApplication.Documents;
        foreach (var document in documents)
        {
            var resultFileDelete =
                await _deleteFellingLicenceService.PermanentlyRemoveDocumentAsync(applicationId, document,
                    cancellationToken);
            if (resultFileDelete.IsFailure)
            {
                _logger.LogError(
                    "Could not Delete the document with ID {DocumentId} from application with ID {ApplicationId} when requested by user with id {UserId}",
                    document.Id, applicationId, user.UserAccountId);
            }
        }

        return Result.Success(woodlandOwnerId);
    }

    public async Task<Result<FellingLicenceApplicationSummaryViewModel>> GetApplicationSummaryViewModel(
        ExternalApplicant user,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await RetrieveFellingLicenceApplication(user, applicationId, cancellationToken);

        if (application.HasNoValue)
        {
            _logger.LogWarning("Could not retrieve application with id {ApplicationId}", applicationId);
            return Result.Failure<FellingLicenceApplicationSummaryViewModel>(
                "Could not find application with given id");
        }

        var result = new FellingLicenceApplicationSummaryViewModel
        {
            Application = application.Value
        };

        // we will redirect back to the tasklist page if the application is incomplete so don't need to go retrieve the rest of the data
        if (!application.Value.IsComplete)
        {
            _logger.LogWarning("Application with ID {ApplicationId} is not complete, returning", applicationId);
            return Result.Success(result);
        }

        var woodlandOwner = await GetWoodlandOwnerByIdAsync(
            application.Value.WoodlandOwnerId,
            cancellationToken).ConfigureAwait(false);

        if (woodlandOwner.IsFailure)
        {
            _logger.LogError("Failed to retrieve woodland owner with id {WoodlandOwnerId}",
                application.Value.WoodlandOwnerId);
            return Result.Failure<FellingLicenceApplicationSummaryViewModel>(
                "Could not retrieve woodland owner for application");
        }

        var property = await GetPropertyProfileByIdAsync(
            application.Value.ApplicationSummary.PropertyProfileId,
            user,
            cancellationToken).ConfigureAwait(false);

        if (property.IsFailure)
        {
            _logger.LogError("Failed to retrieve property profile with id {PropertyProfileId}",
                application.Value.ApplicationSummary.PropertyProfileId);
            return Result.Failure<FellingLicenceApplicationSummaryViewModel>(
                "Could not retrieve property profile for application");
        }

        var fellingAndRestocking = await GetFellingAndRestockingDetailsPlaybackViewModel(
                applicationId, user, cancellationToken)
            .ConfigureAwait(false);

        if (fellingAndRestocking.HasNoValue)
        {
            _logger.LogError(
                "Failed to retrieve felling and restocking playback for application with id {ApplicationId}",
                applicationId);
            return Result.Failure<FellingLicenceApplicationSummaryViewModel>(
                "Could not retrieve felling and restocking for application");
        }

        var agency = await GetAgencyModelForWoodlandOwnerAsync(
            application.Value.ApplicationSummary.WoodlandOwnerId!.Value,
            cancellationToken).ConfigureAwait(false);

        result.WoodlandOwner = woodlandOwner.Value;
        result.PropertyProfile = property.Value;
        result.Agency = agency.HasValue ? agency.Value : null;
        result.FellingAndRestocking = fellingAndRestocking.Value;

        return Result.Success(result);
    }

    private async Task<Maybe<SelectedCompartmentsModel>> GetRelevantCompartments(
        ExternalApplicant user,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);

        if (application.IsFailure)
        {
            return Maybe<SelectedCompartmentsModel>.None;
        }

        var fellingLicenceStatus = GetApplicationStatus(application.Value.StatusHistories);

        var fellingCompartmentsList = application.Value.LinkedPropertyProfile?.ProposedFellingDetails?
            .Select(d => d.PropertyProfileCompartmentId)
            .ToList() ?? new List<Guid>();
        var restockingCompartmentsList = application.Value.LinkedPropertyProfile?.ProposedFellingDetails?
            .Where(x => x.ProposedRestockingDetails != null)
            .SelectMany(x => x.ProposedRestockingDetails!)
            .Select(x => x.PropertyProfileCompartmentId)
            .ToList() ?? new List<Guid>();

        var allCompartmentsList = fellingCompartmentsList.Concat(restockingCompartmentsList).Distinct().ToList();

        var selectedCompartments = new SelectedCompartmentsModel
        {
            ApplicationId = application.Value.Id,
            ApplicationReference = application.Value.ApplicationReference,
            SelectedCompartmentIds = allCompartmentsList,
            StepComplete = application.Value.FellingLicenceApplicationStepStatus.SelectCompartmentsStatus,
            FellingLicenceStatus = fellingLicenceStatus
        };

        return selectedCompartments.AsMaybe();
    }

    private static Maybe<DocumentModel> GetMostRecentDocumentOfType(
        IList<Document>? documents,
        DocumentPurpose purpose)
    {
        var documentFound = documents?.OrderByDescending(x => x.CreatedTimestamp)
            .FirstOrDefault(x => x.Purpose == purpose);

        if (documentFound == null)
        {
            return Maybe<DocumentModel>.None;
        }

        var documentModel = ModelMapping.ToDocumentModel(documentFound);
        return Maybe<DocumentModel>.From(documentModel);
    }

    private static ActivityFeedItemProviderModel GetProviderModelForActivityFeed(FellingLicenceApplication application)
    {
        //we currently display all case notes that are visible to applicants, and status/assignee histories for applications that have been submitted.

        var totalItemTypes = Enum.GetValues(typeof(ActivityFeedItemType)).Cast<ActivityFeedItemType>().ToArray();

        totalItemTypes = totalItemTypes.Where(x =>
                x.GetActivityFeedItemTypeAttribute() != ActivityFeedItemCategory.OutgoingNotification
                && x is not (ActivityFeedItemType.PublicRegisterComment or ActivityFeedItemType.ConsulteeComment))
            .ToArray();

        var selectedItemTypes = application.StatusHistories.Any(x =>
            x.Status == Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus.Submitted)
            ? totalItemTypes
            : totalItemTypes.Where(x =>
                    x is not (ActivityFeedItemType.AssigneeHistoryNotification
                        or ActivityFeedItemType.StatusHistoryNotification))
                .ToArray();

        return new ActivityFeedItemProviderModel
        {
            FellingLicenceId = application.Id,
            FellingLicenceReference = application.ApplicationReference,
            ItemTypes = selectedItemTypes,
            VisibleToApplicant = true
        };
    }

    private static NotificationRecipient[]? GetWoodlandOwnerCopyToRecipient(
        string? mainToEmailAddress,
        WoodlandOwner? woodlandOwner)
    {
        if (!string.IsNullOrWhiteSpace(woodlandOwner?.ContactEmail)
            && !woodlandOwner.ContactEmail.Equals(mainToEmailAddress, StringComparison.CurrentCultureIgnoreCase))
        {
            return new[]
            {
                new NotificationRecipient(woodlandOwner.ContactEmail, woodlandOwner.ContactName)
            };
        }

        return null;
    }

    public async Task<FellingLicenceApplicationStepStatus> GetApplicationStepStatus(Guid applicationId, CancellationToken cancellationToken)
    {
        return await _fellingLicenceApplicationRepository.GetApplicationStepStatus(applicationId, cancellationToken);
    }

    public bool FellingOperationRequiresStocking(FellingOperationType fellingOperationType)
    {
        return !fellingOperationType.AllowedRestockingForFellingType(false).IsNullOrEmpty();
    }

    /// <summary>
    /// Updates the zones for submitted FLA property compartments based on their GIS data.
    /// </summary>
    /// <param name="submittedFlaPropertyCompartmentIds">A collection of IDs for the submitted FLA property compartments.</param>
    /// <param name="userId">The ID of the user performing the update.</param>
    /// <param name="applicationId">The ID of the application associated with the compartments.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>An awaitable task.</returns>
    public async Task UpdateSubmittedFlaPropertyCompartmentZonesAsync(
        IEnumerable<Guid> submittedFlaPropertyCompartmentIds,
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        foreach (var compartmentId in submittedFlaPropertyCompartmentIds)
        {
            // Retrieve the submitted compartment by its ID
            var submittedCompartment =
                await _updateFellingLicenceApplicationService.GetSubmittedFlaPropertyCompartmentByIdAsync(compartmentId,
                    cancellationToken);

            // Log and audit if retrieval fails
            if (submittedCompartment.IsFailure)
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure,
                        applicationId,
                        userId,
                        _requestContext,
                        new
                        {
                            Section = "Submit FLA (Submitted Property details)",
                            Error = submittedCompartment.Error
                        }),
                    cancellationToken);
            }

            var compartment = submittedCompartment.Value;

            // Skip processing if the compartment is null or lacks GIS data
            if (compartment == null || string.IsNullOrEmpty(compartment.GISData))
            {
                _logger.LogWarning("Compartment with ID {CompartmentId} could not be found or has no GIS data.", compartmentId);
                continue;
            }

            // Deserialize GIS data into a polygon shape
            var shape = JsonConvert.DeserializeObject<Polygon>(compartment.GISData!);

            // Retrieve risk zones for the shape
            var zones = await _foresterServices.GetPhytophthoraRamorumRiskZonesAsync(shape, cancellationToken);

            // Determine if the shape intersects with specific zones
            var zone1 = zones.Value.Any(x => x.ZoneName == "Zone 1");
            var zone2 = zones.Value.Any(x => x.ZoneName == "Zone 2");
            var zone3 = zones.Value.Any(x => x.ZoneName == "Zone 3");

            // Update the zones for the submitted compartment
            await _updateFellingLicenceApplicationService.UpdateSubmittedFlaPropertyCompartmentZonesAsync(compartmentId,
                zone1, zone2, zone3, cancellationToken);
        }
    }
}
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.PawsDesignations;
using Forestry.Flo.External.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Implementation of <see cref="ICollectPawsDataUseCase"/>.
/// </summary>
/// <param name="retrieveUserAccountsService">A service to retrieve user accounts.</param>
/// <param name="retrieveWoodlandOwnersService">A service to retrieve woodland owners.</param>
/// <param name="getFellingLicenceApplicationServiceForExternalUsers">A service to retrieve felling licence applications.</param>
/// <param name="getPropertyProfilesService">A service to retrieve property profiles.</param>
/// <param name="getCompartmentsService">A service to retrieve compartment details.</param>
/// <param name="agentAuthorityService">A service to retrieve agent authority details.</param>
/// <param name="updateFellingLicenceApplicationService">A service to update felling licence applications.</param>
/// <param name="auditService">A service to save audit records.</param>
/// <param name="requestContext">The request context.</param>
/// <param name="logger">A logging instance.</param>
public class CollectPawsDataUseCase(
    IRetrieveUserAccountsService retrieveUserAccountsService,
    IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers,
    IGetPropertyProfiles getPropertyProfilesService,
    IGetCompartments getCompartmentsService,
    IAgentAuthorityService agentAuthorityService,
    IUpdateFellingLicenceApplicationForExternalUsers updateFellingLicenceApplicationService,
    IAuditService<CollectPawsDataUseCase> auditService,
    RequestContext requestContext,
    ILogger<CollectPawsDataUseCase> logger)
    : ApplicationUseCaseCommon(
        retrieveUserAccountsService,
        retrieveWoodlandOwnersService,
        getFellingLicenceApplicationServiceForExternalUsers,
        getPropertyProfilesService,
        getCompartmentsService,
        agentAuthorityService,
        logger
        ), 
        ICollectPawsDataUseCase
{
    private readonly ILogger<CollectPawsDataUseCase> _logger = logger ?? new NullLogger<CollectPawsDataUseCase>();
    private readonly IUpdateFellingLicenceApplicationForExternalUsers _updateFellingLicenceApplicationService = Guard.Against.Null(updateFellingLicenceApplicationService);
    private readonly IAuditService<CollectPawsDataUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);

    /// <inheritdoc />
    public async Task<Result<PawsDesignationsViewModel>> GetPawsDesignationsViewModelAsync(
        ExternalApplicant user, 
        Guid applicationId, 
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);
        if (userAccess.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            return userAccess.ConvertFailure<PawsDesignationsViewModel>();
        }

        var applicationResult = await GetFellingLicenceApplicationServiceForExternalUsers
            .GetApplicationByIdAsync(applicationId, userAccess.Value, cancellationToken);

        if (applicationResult.IsFailure)
        {
            _logger.LogError("Unable to retrieve application with id {ApplicationId}", applicationId);
            return applicationResult.ConvertFailure<PawsDesignationsViewModel>();
        }

        if ((applicationResult.Value.LinkedPropertyProfile?.ProposedCompartmentDesignations ?? []).Count == 0)
        {
            _logger.LogError("No compartment designations found on application with ID {ApplicationId}", applicationId);
            return Result.Failure<PawsDesignationsViewModel>("No compartment designations found on application");

        }

        var propertyProfile = await GetPropertyProfileByIdAsync(
            applicationResult.Value.LinkedPropertyProfile!.PropertyProfileId,
            user,
            cancellationToken);

        if (propertyProfile.IsFailure)
        {
            _logger.LogDebug("Unable to retrieve property profile id {PropertyProfileId} for application id {ApplicationId}",
                applicationResult.Value.LinkedPropertyProfile.PropertyProfileId, applicationId);

            return propertyProfile.ConvertFailure<PawsDesignationsViewModel>();
        }

        var applicationSummary = await GetApplicationSummaryAsync(applicationResult.Value, user, cancellationToken);
        if (applicationSummary.IsFailure)
        {
            _logger.LogError("Unable to retrieve application summary for application with id {ApplicationId}", applicationId);
            return applicationSummary.ConvertFailure<PawsDesignationsViewModel>();
        }

        var currentStatus = applicationResult.Value.GetCurrentStatus();

        var pawsStatuses = applicationResult.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses;

        PawsCompartmentDesignationsModel? compartmentDesignation = null;

        // if we already know which one we're looking at, find that one
        if (currentId.HasValue)
        {
            var pawsDesignationsEntity =
                applicationResult.Value.LinkedPropertyProfile?.ProposedCompartmentDesignations.SingleOrDefault(x =>
                    x.Id == currentId
                    && applicationResult.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Any(c => c.CompartmentId == x.PropertyProfileCompartmentId));

            if (pawsDesignationsEntity != null
                && pawsStatuses.Any(x => x.CompartmentId == pawsDesignationsEntity.PropertyProfileCompartmentId))
            {
                compartmentDesignation = new PawsCompartmentDesignationsModel
                {
                    Id = pawsDesignationsEntity.Id,
                    PropertyProfileCompartmentId = pawsDesignationsEntity.PropertyProfileCompartmentId,
                    PropertyProfileCompartmentName = propertyProfile.Value.Compartments.FirstOrDefault(
                        c => c.Id == pawsDesignationsEntity.PropertyProfileCompartmentId)?.CompartmentNumber ?? "Unknown Compartment",
                    CrossesPawsZones = pawsDesignationsEntity.CrossesPawsZones,
                    ProportionBeforeFelling = pawsDesignationsEntity.ProportionBeforeFelling,
                    ProportionAfterFelling = pawsDesignationsEntity.ProportionAfterFelling,
                    IsRestoringCompartment = pawsDesignationsEntity.IsRestoringCompartment,
                    RestorationDetails = pawsDesignationsEntity.RestorationDetails
                };
            }
        }

        // if we don't have one yet, get the first one
        if (compartmentDesignation == null)
        {
            compartmentDesignation = GetNextByCompartmentName(
                applicationResult.Value.LinkedPropertyProfile?.ProposedCompartmentDesignations,
                applicationResult.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses,
                null,
                propertyProfile.Value!);

            // if we still don't have one, something has gone wrong
            if (compartmentDesignation == null)
            {
                _logger.LogDebug("No matching PAWS compartment designations found for application id {ApplicationId}", applicationId);
                return Result.Failure<PawsDesignationsViewModel>("No PAWS compartment designations found");
            }
        }

        var designationStatuses =
            applicationResult.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses;
        bool? stepComplete = designationStatuses.Count == 0 || designationStatuses.TrueForAll(x => x.Status is null)
            ? null
            : designationStatuses.TrueForAll(x => x.Status is true);

        var result = new PawsDesignationsViewModel
        {
            ApplicationId = applicationId,
            ApplicationReference = applicationResult.Value.ApplicationReference,
            ApplicationSummary = applicationSummary.Value,
            FellingLicenceStatus = currentStatus,
            StepRequiredForApplication = designationStatuses.Count != 0,
            StepComplete = stepComplete,
            CompartmentDesignation = compartmentDesignation
        };

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> UpdatePawsDesignationsForCompartmentAsync(
        ExternalApplicant user, 
        Guid applicationId,
        PawsCompartmentDesignationsModel pawsModel, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to update PAWS designations data for compartment id {CompartmentId} on application id {ApplicationId}",
            pawsModel.PropertyProfileCompartmentId, applicationId);

        var uam = await GetUserAccessModelAsync(user, cancellationToken);

        if (uam.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            
            await AuditForUpdateFailure(applicationId, user, pawsModel.PropertyProfileCompartmentId, uam.Error, cancellationToken);
            
            return uam.ConvertFailure();
        }

        var updateResult = await _updateFellingLicenceApplicationService
            .UpdateApplicationPawsDesignationsDataAsync(applicationId, uam.Value, pawsModel, cancellationToken);

        if (updateResult.IsFailure)
        {
            _logger.LogError("Failed to update PAWS designations data for compartment id {CompartmentId} on application id {ApplicationId}. Error: {Error}",
                pawsModel.PropertyProfileCompartmentId, applicationId, updateResult.Error);

            await AuditForUpdateFailure(applicationId, user, pawsModel.PropertyProfileCompartmentId, updateResult.Error, cancellationToken);

            return updateResult;
        }

        _logger.LogDebug("Successfully updated PAWS designations data for compartment id {CompartmentId} on application id {ApplicationId}",
            pawsModel.PropertyProfileCompartmentId, applicationId);

        await AuditForUpdateSuccess(applicationId, user, pawsModel.PropertyProfileCompartmentId, cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<PawsRedirectResult>> GetNextCompartmentDesignationsIdAsync(
        ExternalApplicant user, 
        Guid applicationId, 
        Guid currentId,
        CancellationToken cancellationToken)
    {
        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);
        if (userAccess.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            return userAccess.ConvertFailure<PawsRedirectResult>();
        }

        var applicationResult = await GetFellingLicenceApplicationServiceForExternalUsers
            .GetApplicationByIdAsync(applicationId, userAccess.Value, cancellationToken);

        if (applicationResult.IsFailure)
        {
            _logger.LogError("Unable to retrieve application with id {ApplicationId}", applicationId);
            return applicationResult.ConvertFailure<PawsRedirectResult>();
        }

        if ((applicationResult.Value.LinkedPropertyProfile?.ProposedCompartmentDesignations ?? []).Count == 0)
        {
            _logger.LogError("No compartment designations found on application with ID {ApplicationId}", applicationId);
            return Result.Failure<PawsRedirectResult>("No compartment designations found on application");

        }

        var propertyProfile = await GetPropertyProfileByIdAsync(
            applicationResult.Value.LinkedPropertyProfile!.PropertyProfileId,
            user,
            cancellationToken);

        if (propertyProfile.IsFailure)
        {
            _logger.LogDebug("Unable to retrieve property profile id {PropertyProfileId} for application id {ApplicationId}",
                applicationResult.Value.LinkedPropertyProfile.PropertyProfileId, applicationId);

            return propertyProfile.ConvertFailure<PawsRedirectResult>();
        }

        var nextCompartmentDesignation = GetNextByCompartmentName(
            applicationResult.Value.LinkedPropertyProfile?.ProposedCompartmentDesignations,
            applicationResult.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses,
            currentId,
            propertyProfile.Value!);

        return Result.Success(new PawsRedirectResult(
            nextCompartmentDesignation?.Id,
            applicationResult.Value.ShouldApplicationRequireEia()));
    }

    private Task AuditForUpdateSuccess(
        Guid applicationId,
        ExternalApplicant user,
        Guid compartmentId,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.PawsDesignationsUpdate,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { compartmentId }),
            cancellationToken);

    private Task AuditForUpdateFailure(
        Guid applicationId,
        ExternalApplicant user,
        Guid compartmentId,
        string error,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.PawsDesignationsUpdateFailure,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { compartmentId, error }),
            cancellationToken);

    private PawsCompartmentDesignationsModel? GetNextByCompartmentName(
        IList<ProposedCompartmentDesignations>? proposedDesignations,
        IList<CompartmentDesignationStatus>? statuses,
        Guid? currentCompartmentDesignationsId,
        PropertyProfile propertyProfile)
    {
        if ((proposedDesignations ?? []).Count == 0
            || (statuses ?? []).Count == 0)
        {
            _logger.LogWarning("No proposed compartment designations provided to GetNextByCompartmentName");
            return null;
        }

        var compartmentDesignations = proposedDesignations!
            .Where(x => statuses!.Any(s => s.CompartmentId == x.PropertyProfileCompartmentId))
            .Select(x => new PawsCompartmentDesignationsModel
            {
                Id = x.Id,
                PropertyProfileCompartmentId = x.PropertyProfileCompartmentId,
                PropertyProfileCompartmentName = propertyProfile.Compartments.FirstOrDefault(
                    c => c.Id == x.PropertyProfileCompartmentId)?.CompartmentNumber ?? "Unknown Compartment",
                CrossesPawsZones = x.CrossesPawsZones,
                ProportionBeforeFelling = x.ProportionBeforeFelling,
                ProportionAfterFelling = x.ProportionAfterFelling,
                IsRestoringCompartment = x.IsRestoringCompartment,
                RestorationDetails = x.RestorationDetails
            })
            .ToList();

        compartmentDesignations = compartmentDesignations
            .OrderByNameNumericOrAlpha()
            .ToList();

        // if we aren't looking for a specific compartment, return the first one
        if (!currentCompartmentDesignationsId.HasValue)
        {
            return compartmentDesignations.First();
        }

        // find the index of the current compartment
        var currentIndex = compartmentDesignations
            .FindIndex(x => x.Id == currentCompartmentDesignationsId.Value);

        // return the next one in the list, if there is one
        if (currentIndex >= 0 && currentIndex < compartmentDesignations.Count - 1)
        {
            return compartmentDesignations[currentIndex + 1];
        }

        return null;
    }
}
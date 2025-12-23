using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using LinqKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class UpdateFellingLicenceApplicationForExternalUsersService : IUpdateFellingLicenceApplicationForExternalUsers
{
    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationRepository;
    private readonly IClock _clock;
    private readonly RequestContext _requestContext;
    private readonly FellingLicenceApplicationOptions _fellingLicenceApplicationOptions;
    private readonly ILogger<UpdateFellingLicenceApplicationForExternalUsersService> _logger;

    private static readonly List<FellingLicenceStatus> _statusesToSubmitFrom = new List<FellingLicenceStatus>
    {
        FellingLicenceStatus.Draft,
        FellingLicenceStatus.ReturnedToApplicant,
        FellingLicenceStatus.WithApplicant
    };

    public UpdateFellingLicenceApplicationForExternalUsersService(
        IFellingLicenceApplicationExternalRepository fellingLicenceApplicationRepository,
        IClock clock,
        IOptions<FellingLicenceApplicationOptions> fellingLicenceApplicationOptions,
        ILogger<UpdateFellingLicenceApplicationForExternalUsersService> logger)
    {
        ArgumentNullException.ThrowIfNull(fellingLicenceApplicationRepository);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(fellingLicenceApplicationOptions);

        _fellingLicenceApplicationRepository = fellingLicenceApplicationRepository;
        _clock = clock;
        _fellingLicenceApplicationOptions = fellingLicenceApplicationOptions.Value;
        _logger = logger ?? new NullLogger<UpdateFellingLicenceApplicationForExternalUsersService>();
    }

    /// <inheritdoc />
    public async Task<Result<SubmitFellingLicenceApplicationResponse>> SubmitFellingLicenceApplicationAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        //get current time
        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        // get entity
        var application = await _fellingLicenceApplicationRepository
            .GetAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        if (application.HasNoValue)
        {
            _logger.LogWarning("Could not locate application with id {ApplicationId} in the repository", applicationId);
            return Result.Failure<SubmitFellingLicenceApplicationResponse>("Could not submit the application");
        }

        // check the application has a property profile, no idea how we got this far if it does not!
        var applicationPropertyProfileId = application.Value.LinkedPropertyProfile?.PropertyProfileId;
        if (applicationPropertyProfileId == null)
        {
            _logger.LogWarning("Application with id {ApplicationId} has no linked property profile, cannot be submitted", applicationId);
            return Result.Failure<SubmitFellingLicenceApplicationResponse>("Could not submit the application");
        }

        // verify user access to the application
        var applicationWoodlandOwnerId = application.Value.WoodlandOwnerId;
        if (userAccessModel.CanManageWoodlandOwner(applicationWoodlandOwnerId) == false)
        {
            _logger.LogWarning(
                "User with id {UserId} does not have access to woodland owner with id {WoodlandOwnerId} and so cannot submit the application with id {ApplicationId}",
                userAccessModel.UserAccountId, application.Value.WoodlandOwnerId, applicationId);
            return Result.Failure<SubmitFellingLicenceApplicationResponse>("Could not submit the application");
        }

        // get current status, check we can submit from it
        var currentStatus = application.Value.StatusHistories.MaxBy(x => x.Created)?.Status;

        if (currentStatus == null || _statusesToSubmitFrom.Contains(currentStatus.Value) == false)
        {
            _logger.LogWarning("Cannot submit application with id {ApplicationId} as it is currently in state {Status}",
                applicationId, currentStatus);
            return Result.Failure<SubmitFellingLicenceApplicationResponse>("Could not submit the application");
        }

        // add new status history, based on current status.  always add submitted so we know when the resubmission occurred
        // even if it then goes straight on to woodland officer review
        application.Value.StatusHistories.Add(new StatusHistory
        {
            Status = FellingLicenceStatus.Submitted,
            Created = now,
            CreatedById = userAccessModel.UserAccountId
        });

        if (currentStatus == FellingLicenceStatus.WithApplicant)
        {
            application.Value.StatusHistories.Add(new StatusHistory
            {
                Status = FellingLicenceStatus.WoodlandOfficerReview,
                Created = now.AddMilliseconds(1),  // todo - this is to ensure that this status is picked up as the latest one but is a bit "hacky" so better approach would be good
                CreatedById = userAccessModel.UserAccountId
            });
        }

        // work out new Received, Citizens Charter and Final Action dates if appropriate
        if (currentStatus is not FellingLicenceStatus.WithApplicant)
        {
            if (currentStatus is not FellingLicenceStatus.ReturnedToApplicant || application.Value.DateReceived is null)
            {
                application.Value.DateReceived = now;
            }

            application.Value.FinalActionDate = now.AddDays(_fellingLicenceApplicationOptions.FinalActionDateDaysFromSubmission);
            application.Value.CitizensCharterDate = now.Add(_fellingLicenceApplicationOptions.CitizensCharterDateLength);
        }

        // update application reference
        var areaCode = string.IsNullOrWhiteSpace(application.Value.AreaCode)
            ? "---"
            : application.Value.AreaCode;
        var currentReference = application.Value.ApplicationReference;
        var newReference = $"{areaCode}{application.Value.ApplicationReference.Substring(currentReference.IndexOf('/'))}";
        application.Value.ApplicationReference = newReference;

        // unassign any current author/applicant role assignees
        foreach (var assignedApplicant in application.Value.AssigneeHistories.Where(r =>
                     r.Role is AssignedUserRole.Applicant or AssignedUserRole.Author
                     && r.TimestampUnassigned.HasNoValue()))
        {
            assignedApplicant.TimestampUnassigned = now;
        }

        // get list of assigned internal users to return in response for sending notifications
        var notificationRecipientIds = application.Value.AssigneeHistories
            .Where(x => x.Role != AssignedUserRole.Applicant && x.Role != AssignedUserRole.Applicant && x.TimestampUnassigned.HasNoValue())
            .Select(x => x.AssignedUserId)
            .ToList();

        // save changes to application
        var saveResult = await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning("Could not save updates to application with id {ApplicationId}, error: {Error}", applicationId, saveResult.Error);
            return Result.Failure<SubmitFellingLicenceApplicationResponse>("Could not submit the application");
        }

        // if submission from ReturnedToApplicant - clear down any AdminOfficerReview record
        if (currentStatus is FellingLicenceStatus.ReturnedToApplicant)
        {
            await _fellingLicenceApplicationRepository
                .DeleteAdminOfficerReviewForApplicationAsync(applicationId, cancellationToken)
                .ConfigureAwait(false);
        }

        // clear down any existing SubmittedFlaPropertyDetails - the usecase code calling this method must then generate the new version!
        await _fellingLicenceApplicationRepository
            .DeleteSubmittedFlaPropertyDetailForApplicationAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        // clear down woodland officer review task flags for tasks that will need to be redone/checked again due to resubmission
        if (currentStatus != FellingLicenceStatus.Draft)
        {
            await _fellingLicenceApplicationRepository
                .UpdateExistingWoodlandOfficerReviewFlagsForResubmission(applicationId, now, cancellationToken)
                .ConfigureAwait(false);
        }

        // return response details required by the usecase for notifications
        var result = new SubmitFellingLicenceApplicationResponse(
            newReference,
            applicationWoodlandOwnerId,
            applicationPropertyProfileId.Value,
            currentStatus.Value,
            notificationRecipientIds,
            application.Value.AdministrativeRegion);

        return Result.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result> AddSubmittedFellingLicenceApplicationPropertyDetailAsync(
        SubmittedFlaPropertyDetail propertyDetail,
        CancellationToken cancellationToken)
    {
        try
        {
            await _fellingLicenceApplicationRepository
                .AddSubmittedFlaPropertyDetailAsync(propertyDetail, cancellationToken)
                .ConfigureAwait(false);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not add property detail to submitted FLA");
            return Result.Failure("Could not add property details to submitted FLA");
        }
    }

    /// <inheritdoc />
    public async Task<Result<SubmittedFlaPropertyCompartment>> GetSubmittedFlaPropertyCompartmentByIdAsync(
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var compartment = await _fellingLicenceApplicationRepository
                .GetSubmittedFlaPropertyCompartmentByIdAsync(compartmentId, cancellationToken)
                .ConfigureAwait(false);

            if (compartment == null)
            {
                _logger.LogWarning("No SubmittedFlaPropertyCompartment found with id {CompartmentId}", compartmentId);
                return Result.Failure<SubmittedFlaPropertyCompartment>("Compartment not found");
            }

            return Result.Success(compartment.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving SubmittedFlaPropertyCompartment with id {CompartmentId}", compartmentId);
            return Result.Failure<SubmittedFlaPropertyCompartment>("Error retrieving compartment");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateSubmittedFlaPropertyCompartmentZonesAsync(
        Guid compartmentId,
        bool zone1,
        bool zone2,
        bool zone3,
        CancellationToken cancellationToken)
    {
        try
        {
            await _fellingLicenceApplicationRepository
                .UpdateSubmittedFlaPropertyCompartmentZonesAsync(compartmentId, zone1, zone2, zone3, cancellationToken)
                .ConfigureAwait(false);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating zones for SubmittedFlaPropertyCompartment with id {CompartmentId}", compartmentId);
            return Result.Failure("Error updating compartment zones");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ConvertProposedFellingAndRestockingToConfirmedAsync(
        Guid applicationId, 
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        // get entity
        var application = await _fellingLicenceApplicationRepository
            .GetAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        if (application.HasNoValue)
        {
            _logger.LogWarning("Could not locate application with id {ApplicationId} in the repository", applicationId);
            return Result.Failure("Could not convert proposed to confirmed felling and restocking for the application");
        }

        // verify user access to the application
        var applicationWoodlandOwnerId = application.Value.WoodlandOwnerId;
        if (userAccessModel.CanManageWoodlandOwner(applicationWoodlandOwnerId) == false)
        {
            _logger.LogWarning(
                "User with id {UserId} does not have access to woodland owner with id {WoodlandOwnerId} and so cannot convert the felling and restocking details for the application with id {ApplicationId}",
                userAccessModel.UserAccountId, application.Value.WoodlandOwnerId, applicationId);
            return Result.Failure("Could not convert proposed to confirmed felling and restocking for the application");
        }

        var fellingResult = ConvertProposedFellingDetailsToConfirmedAsync(application.Value);

        if (fellingResult.IsFailure)
        {
            _logger.LogError(
                "Error {Error} encountered in ConvertProposedFellingAndRestockingToConfirmedAsync, application id {ApplicationId}",
                fellingResult.Error, applicationId);
            return fellingResult;
        }

        _fellingLicenceApplicationRepository.Update(application.Value);

        try
        {
            var dbResult = await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (dbResult.IsFailure)
            {
                _logger.LogError(
                    "Error {Error} encountered in ConvertProposedFellingAndRestockingToConfirmedAsync, application id {ApplicationId}", 
                    dbResult.Error, applicationId);
                return Result.Failure(dbResult.Error + $" in ConvertProposedFellingAndRestockingToConfirmedAsync, application id {applicationId}");
            }

            _logger.LogDebug("Successfully imported proposed felling and restocking to confirmed for application with id {ApplicationId}", applicationId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in ConvertProposedFellingAndRestockingToConfirmedAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ConvertProposedCompartmentDesignationsToSubmittedAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        // get entity
        var application = await _fellingLicenceApplicationRepository
            .GetAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        if (application.HasNoValue)
        {
            _logger.LogWarning("Could not locate application with id {ApplicationId} in the repository", applicationId);
            return Result.Failure("Could not convert proposed designations to submitted designations for the application");
        }

        if (application.Value.SubmittedFlaPropertyDetail == null)
        {
            _logger.LogWarning("Application with id {ApplicationId} has no submitted FLA property detail to convert designations into", applicationId);
            return Result.Failure("Could not convert proposed designations to submitted designations for the application");
        }

        // verify user access to the application
        var applicationWoodlandOwnerId = application.Value.WoodlandOwnerId;
        if (userAccessModel.CanManageWoodlandOwner(applicationWoodlandOwnerId) == false)
        {
            _logger.LogWarning(
                "User with id {UserId} does not have access to woodland owner with id {WoodlandOwnerId} and so cannot convert the compartment designation details for the application with id {ApplicationId}",
                userAccessModel.UserAccountId, application.Value.WoodlandOwnerId, applicationId);
            return Result.Failure("Could not convert proposed designations to submitted designations for the application");
        }

        var designations = application.Value.LinkedPropertyProfile?.ProposedCompartmentDesignations ?? [];

        // add designations to each submitted compartment
        foreach (var submittedCompartment in application.Value.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments ?? [])
        {
            var designation = designations
                .FirstOrDefault(x => x.PropertyProfileCompartmentId == submittedCompartment.CompartmentId);

            if (designation == null)
            {
                _logger.LogWarning(
                    "No proposed compartment designations found for compartment id {CompartmentId} on application {ApplicationId}",
                    submittedCompartment.CompartmentId,
                    applicationId);

                continue;
            }

            var submittedDesignation = new SubmittedCompartmentDesignations
            {
                ProposedCompartmentDesignationsId = designation?.Id,
                Paws = designation?.CrossesPawsZones.Any() ?? false,
                ProportionBeforeFelling = designation.ProportionBeforeFelling,
                ProportionAfterFelling = designation.ProportionAfterFelling,
                SubmittedFlaPropertyCompartment = submittedCompartment,
                SubmittedFlaPropertyCompartmentId = submittedCompartment.Id
            };

            submittedCompartment.SubmittedCompartmentDesignations = submittedDesignation;
        }

        //_fellingLicenceApplicationRepository.Update(application.Value);

        try
        {
            var dbResult = await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (dbResult.IsFailure)
            {
                _logger.LogError(
                    "Error {Error} encountered in ConvertProposedCompartmentDesignationsToSubmittedAsync, application id {ApplicationId}",
                    dbResult.Error, applicationId);
                return Result.Failure(dbResult.Error + $" in ConvertProposedCompartmentDesignationsToSubmittedAsync, application id {applicationId}");
            }

            _logger.LogDebug("Successfully converted proposed compartment designations to submitted for application with id {ApplicationId}", applicationId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in ConvertProposedCompartmentDesignationsToSubmittedAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateTenYearLicenceStatusAsync(
        Guid applicationId, 
        UserAccessModel userAccess, 
        bool isForTenYearLicence,
        string? woodlandManagementPlanReference,
        CancellationToken cancellationToken)
    {
        try
        {
            // get entity
            var application = await _fellingLicenceApplicationRepository
                .GetAsync(applicationId, cancellationToken)
                .ConfigureAwait(false);

            if (application.HasNoValue)
            {
                _logger.LogWarning("Could not locate application with id {ApplicationId} in the repository", applicationId);
                return Result.Failure("Could not update ten-year licence status for the application");
            }

            // verify user access to the application
            var applicationWoodlandOwnerId = application.Value.WoodlandOwnerId;
            if (userAccess.CanManageWoodlandOwner(applicationWoodlandOwnerId) == false)
            {
                _logger.LogWarning(
                    "User with id {UserId} does not have access to woodland owner with id {WoodlandOwnerId} and so cannot update ten-year licence status for the application with id {ApplicationId}",
                    userAccess.UserAccountId, application.Value.WoodlandOwnerId, applicationId);
                return Result.Failure("Could not update ten-year licence status for the application");
            }

            application.Value.IsForTenYearLicence = isForTenYearLicence;
            application.Value.WoodlandManagementPlanReference = isForTenYearLicence ? woodlandManagementPlanReference : null;

            // update step  status - if isForTenYearLicence is false then we don't need docs so set step status to true,
            // otherwise if there is no value set to false, if there is a value then only change it (to false) if there are no WMP docs yet
            var wmpDocumentsCount = application.Value.Documents!.Count(d =>
                d.Purpose == DocumentPurpose.WmpDocument && d.DeletionTimestamp.HasNoValue());
            var existingStepStatus = application.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus;
            var newStepStatus = !isForTenYearLicence
                ? true
                : existingStepStatus.HasValue && wmpDocumentsCount == 0
                    ? false
                    : existingStepStatus;
            application.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus = newStepStatus;

            _fellingLicenceApplicationRepository.Update(application.Value);

            var dbResult = await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (dbResult.IsFailure)
            {
                _logger.LogError(
                    "Error {Error} encountered in UpdateTenYearLicenceStatusAsync, application id {ApplicationId}",
                    dbResult.Error, applicationId);
                return Result.Failure(dbResult.Error + $" in UpdateTenYearLicenceStatusAsync, application id {applicationId}");
            }

            _logger.LogDebug("Successfully updated ten-year licence status for application with id {ApplicationId}", applicationId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in UpdateTenYearLicenceStatusAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> CompleteTenYearLicenceStepAsync(
        Guid applicationId, 
        UserAccessModel userAccess,
        CancellationToken cancellationToken)
    {
        try
        {
            // get entity
            var application = await _fellingLicenceApplicationRepository
                .GetAsync(applicationId, cancellationToken)
                .ConfigureAwait(false);

            if (application.HasNoValue)
            {
                _logger.LogWarning("Could not locate application with id {ApplicationId} in the repository", applicationId);
                return Result.Failure("Could not update ten-year licence step status for the application");
            }

            // verify user access to the application
            var applicationWoodlandOwnerId = application.Value.WoodlandOwnerId;
            if (userAccess.CanManageWoodlandOwner(applicationWoodlandOwnerId) == false)
            {
                _logger.LogWarning(
                    "User with id {UserId} does not have access to woodland owner with id {WoodlandOwnerId} and so cannot update ten-year licence status for the application with id {ApplicationId}",
                    userAccess.UserAccountId, application.Value.WoodlandOwnerId, applicationId);
                return Result.Failure("Could not update ten-year licence step status for the application");
            }

            application.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus = true;   // true = step complete

            _fellingLicenceApplicationRepository.Update(application.Value);

            var dbResult = await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (dbResult.IsFailure)
            {
                _logger.LogError(
                    "Error {Error} encountered in CompleteTenYearLicenceStepAsync, application id {ApplicationId}",
                    dbResult.Error, applicationId);
                return Result.Failure(dbResult.Error + $" in CompleteTenYearLicenceStepAsync, application id {applicationId}");
            }

            _logger.LogDebug("Successfully updated ten-year licence step status for application with id {ApplicationId}", applicationId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in CompleteTenYearLicenceStepAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAafStepAsync(
        Guid applicationId,
        UserAccessModel userAccess,
        bool aafStepStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            // get entity
            var application = await _fellingLicenceApplicationRepository
                .GetAsync(applicationId, cancellationToken)
                .ConfigureAwait(false);

            if (application.HasNoValue)
            {
                _logger.LogWarning("Could not locate application with id {ApplicationId} in the repository", applicationId);
                return Result.Failure("Could not update AAF step status for the application");
            }

            // verify user access to the application
            var applicationWoodlandOwnerId = application.Value.WoodlandOwnerId;
            if (userAccess.CanManageWoodlandOwner(applicationWoodlandOwnerId) == false)
            {
                _logger.LogWarning(
                    "User with id {UserId} does not have access to woodland owner with id {WoodlandOwnerId} and so cannot update AAF step status for the application with id {ApplicationId}",
                    userAccess.UserAccountId, application.Value.WoodlandOwnerId, applicationId);
                return Result.Failure("Could not update AAF step status for the application");
            }

            // set step status as requested
            application.Value.FellingLicenceApplicationStepStatus.AafStepStatus = aafStepStatus;

            _fellingLicenceApplicationRepository.Update(application.Value);

            var dbResult = await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (dbResult.IsFailure)
            {
                _logger.LogError(
                    "Error {Error} encountered in UpdateAafStepAsync, application id {ApplicationId}",
                    dbResult.Error, applicationId);
                return Result.Failure(dbResult.Error + $" in UpdateAafStepAsync, application id {applicationId}");
            }

            _logger.LogDebug("Successfully updated AAF step status for application with id {ApplicationId}", applicationId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in UpdateAafStepAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateApplicationPawsDesignationsDataAsync(
        Guid applicationId, 
        UserAccessModel userAccess,
        PawsCompartmentDesignationsModel pawsDesignationsData, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Updating application {ApplicationId} PAWS designations data for compartment id {CompartmentId}",
                applicationId,
                pawsDesignationsData.PropertyProfileCompartmentId);

            // get entity
            var application = await _fellingLicenceApplicationRepository
                .GetAsync(applicationId, cancellationToken)
                .ConfigureAwait(false);

            if (application.HasNoValue)
            {
                _logger.LogError("Could not locate application with id {ApplicationId} in the repository", applicationId);
                return Result.Failure("Could not update PAWS designations data for the application");
            }

            // verify user access to the application
            var applicationWoodlandOwnerId = application.Value.WoodlandOwnerId;
            if (userAccess.CanManageWoodlandOwner(applicationWoodlandOwnerId) == false)
            {
                _logger.LogError(
                    "User with id {UserId} does not have access to woodland owner with id {WoodlandOwnerId} and so cannot update ten-year licence status for the application with id {ApplicationId}",
                    userAccess.UserAccountId, application.Value.WoodlandOwnerId, applicationId);
                return Result.Failure("Could not update PAWS designations data for the application");
            }

            var pawsDesignations = application.Value.LinkedPropertyProfile
                .ProposedCompartmentDesignations
                .SingleOrDefault(x => x.Id == pawsDesignationsData.Id);

            if (pawsDesignations == null)
            {
                _logger.LogError(
                    "Could not locate compartment designations entity with id {DesignationsId} on application with id {ApplicationId}",
                    pawsDesignationsData.Id,
                    applicationId);
                return Result.Failure("Could not update PAWS designations data for the application");
            }

            pawsDesignations.ProportionBeforeFelling = pawsDesignationsData.ProportionBeforeFelling;
            pawsDesignations.ProportionAfterFelling = pawsDesignationsData.ProportionAfterFelling;
            pawsDesignations.IsRestoringCompartment = pawsDesignationsData.IsRestoringCompartment;
            pawsDesignations.RestorationDetails = pawsDesignationsData.IsRestoringCompartment is true
                ? pawsDesignationsData.RestorationDetails
                : null;

            var compartmentDesignationsStatus = application.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses
                .SingleOrDefault(x => x.CompartmentId == pawsDesignationsData.PropertyProfileCompartmentId);
            if (compartmentDesignationsStatus == null)
            {
                _logger.LogError("No compartment designations status found for the given compartment id");
                return Result.Failure("Could not update PAWS designations data for the application");
            }

            compartmentDesignationsStatus.Status = true;

            var dbResult = await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (dbResult.IsFailure)
            {
                _logger.LogError(
                    "Error {Error} encountered in UpdateApplicationPawsDesignationsDataAsync, application id {ApplicationId}",
                    dbResult.Error, applicationId);
                return Result.Failure(dbResult.Error + $" in UpdateApplicationPawsDesignationsDataAsync, application id {applicationId}");
            }

            _logger.LogDebug("Successfully updated PAWS designations data for application with id {ApplicationId}", applicationId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in UpdateApplicationPawsDesignationsDataAsync");
            return Result.Failure(ex.Message);
        }
        

    }

    private Result ConvertProposedFellingDetailsToConfirmedAsync(FellingLicenceApplication fla)
    {
        try
        {
            var propertyProfile = fla.LinkedPropertyProfile;

            if (propertyProfile is null)
            {
                _logger.LogError("Unable to retrieve property profile in ConvertProposedFellingDetailsToConfirmed, id {id}", fla.Id);
                return Result.Failure($"Unable to retrieve property profile in ConvertProposedFellingDetailsToConfirmed, id {fla.Id}");
            }
            // Clear any previously stored fellings before converting the new proposed list
            fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments.ForEach(f => f.ConfirmedFellingDetails.Clear());
            foreach (var propFelling in propertyProfile.ProposedFellingDetails!)
            {
                var submittedFlaPropertyCompartment =
                    fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.FirstOrDefault(x =>
                        x.CompartmentId == propFelling.PropertyProfileCompartmentId);

                if (submittedFlaPropertyCompartment is null)
                {
                    _logger.LogError("Unable to determine submitted FLA property compartment with id {compartmentId}, FLA id {flaId}", propertyProfile.PropertyProfileId, fla.Id);
                    return Result.Failure($"Unable to determine submitted FLA property compartment with id {propFelling.PropertyProfileCompartmentId}, FLA id {fla.Id}");
                }

                var confirmedFellingDetail = new ConfirmedFellingDetail()
                {
                    AreaToBeFelled = propFelling.AreaToBeFelled,
                    IsPartOfTreePreservationOrder = propFelling.IsPartOfTreePreservationOrder,
                    TreePreservationOrderReference = propFelling.TreePreservationOrderReference,
                    IsWithinConservationArea = propFelling.IsWithinConservationArea,
                    ConservationAreaReference = propFelling.ConservationAreaReference,
                    NumberOfTrees = propFelling.NumberOfTrees,
                    OperationType = propFelling.OperationType,
                    SubmittedFlaPropertyCompartment = submittedFlaPropertyCompartment,
                    TreeMarking = propFelling.TreeMarking,
                    SubmittedFlaPropertyCompartmentId = submittedFlaPropertyCompartment.Id,
                    EstimatedTotalFellingVolume = propFelling.EstimatedTotalFellingVolume,
                    IsRestocking = propFelling.IsRestocking,
                    NoRestockingReason = propFelling.NoRestockingReason,
                    ProposedFellingDetailId = propFelling.Id
                };

                var confirmedSpecies = confirmedFellingDetail.ConfirmedFellingSpecies;
                confirmedSpecies.Clear();

                foreach (var species in propFelling.FellingSpecies!)
                {
                    confirmedSpecies.Add(new ConfirmedFellingSpecies()
                    {
                        Species = species.Species,
                        ConfirmedFellingDetail = confirmedFellingDetail,
                        ConfirmedFellingDetailId = confirmedFellingDetail.Id
                    });
                }
                foreach (var proposedRestockingDetail in propFelling.ProposedRestockingDetails)
                {
                    var restockSubmittedFlaPropertyCompartment =
                        fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.FirstOrDefault(x =>
                            x.CompartmentId == proposedRestockingDetail.PropertyProfileCompartmentId);
                    var confirmedRestockingDetail = new ConfirmedRestockingDetail
                    {
                        Area = proposedRestockingDetail.Area,
                        SubmittedFlaPropertyCompartmentId = restockSubmittedFlaPropertyCompartment?.Id ?? submittedFlaPropertyCompartment.Id,
                        PercentageOfRestockArea = proposedRestockingDetail.PercentageOfRestockArea,
                        RestockingDensity = proposedRestockingDetail.RestockingDensity,
                        NumberOfTrees = proposedRestockingDetail.NumberOfTrees,
                        RestockingProposal = proposedRestockingDetail.RestockingProposal,
                        ConfirmedFellingDetail = confirmedFellingDetail,
                        ConfirmedFellingDetailId = confirmedFellingDetail.Id,
                        ProposedRestockingDetailId = proposedRestockingDetail.Id
                    };

                    foreach (var species in proposedRestockingDetail.RestockingSpecies)
                    {
                        confirmedRestockingDetail.ConfirmedRestockingSpecies.Add(new()
                        {
                            Species = species.Species,
                            Percentage = species.Percentage,
                            ConfirmedRestockingDetail = confirmedRestockingDetail,
                            ConfirmedRestockingDetailsId = confirmedRestockingDetail.Id,
                        });
                    }
                    confirmedFellingDetail.ConfirmedRestockingDetails.Add(confirmedRestockingDetail);
                }
                submittedFlaPropertyCompartment.ConfirmedFellingDetails.Add(confirmedFellingDetail);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in ConvertProposedFellingDetailsToConfirmed");
            return Result.Failure("Exception caught in ConvertProposedFellingDetailsToConfirmed");
        }
    }
}
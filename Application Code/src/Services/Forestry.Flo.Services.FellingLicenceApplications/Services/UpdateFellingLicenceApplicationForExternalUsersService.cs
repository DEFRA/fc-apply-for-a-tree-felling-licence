using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class UpdateFellingLicenceApplicationForExternalUsersService : IUpdateFellingLicenceApplicationForExternalUsers
{
    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationRepository;
    private readonly IClock _clock;
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

}
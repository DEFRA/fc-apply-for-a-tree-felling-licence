using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.PropertyProfiles.DataImports;
using MassTransit;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.DataImports;

/// <summary>
/// Implementation of <see cref="IImportApplications"/>.
/// </summary>
public class ImportApplicationsService : IImportApplications
{
    private readonly IFellingLicenceApplicationExternalRepository _repository;
    private readonly IClock _clock;
    private readonly IBus _busControl;
    private readonly IAuditService<ImportApplicationsService> _auditService;
    private readonly ILogger<ImportApplicationsService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="ImportApplicationsService"/>.
    /// </summary>
    /// <param name="repository">An <see cref="IFellingLicenceApplicationExternalRepository"/> to store the imported applications.</param>
    /// <param name="auditService">An <see cref="IAuditService{T}"/> to audit the application creation.</param>
    /// <param name="clock">A <see cref="IClock"/> instance to get the current date and time.</param>
    /// <param name="busControl">A <see cref="IBus"/> to publish the message to calculate the centre point for new applications.</param>
    /// <param name="logger">A logging instance.</param>
    public ImportApplicationsService(
        IFellingLicenceApplicationExternalRepository repository,
        IAuditService<ImportApplicationsService> auditService,
        IClock clock,
        IBus busControl,
        ILogger<ImportApplicationsService> logger)
    {
        _repository = Guard.Against.Null(repository);
        _clock = Guard.Against.Null(clock);
        _busControl = Guard.Against.Null(busControl);
        _auditService = Guard.Against.Null(auditService);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<Dictionary<Guid, string>>> RunDataImportAsync(
        ImportApplicationsRequest request,
        RequestContext requestContext,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to import a set of Applications and felling/restocking details for woodland owner {WoodlandOwnerId} by user {UserId}",
            request.WoodlandOwnerId,
            request.UserId);

        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);

        Dictionary<Guid, string> importedApplicationIds = [];
        
        foreach (var application in request.ApplicationRecords)
        {

            var (_, isFailure, applicationEntity, error) = BuildNextApplicationEntity(
                now,
                request.UserId,
                request.WoodlandOwnerId,
                application,
                request.PropertyIds,
                request.FellingRecords,
                request.RestockingRecords);

            if (isFailure)
            {
                _logger.LogError(
                    "Error parsing application with ID {ApplicationId}: {Error}",
                    application.ApplicationId, 
                    error);

                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<Dictionary<Guid, string>>($"Could not parse application with ID {application.ApplicationId}: {error}");
            }

            try
            {
                await _repository.AddAsync(applicationEntity, null, 0, cancellationToken);

                if (applicationEntity.FellingLicenceApplicationStepStatus.SelectCompartmentsStatus is true)
                {
                    importedApplicationIds.Add(applicationEntity.Id, applicationEntity.ApplicationReference);
                }

                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.CreateFellingLicenceApplication,
                        applicationEntity.Id,
                        request.UserId,
                        requestContext,
                        new { request.WoodlandOwnerId }),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught storing application entity to database");
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<Dictionary<Guid, string>>($"Error storing application with ID {application.ApplicationId}: {ex.Message}");
            }
            
        }

        var saveResult = await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        
        if (saveResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError("Error saving new application imports: " + saveResult.Error);
            return Result.Failure<Dictionary<Guid, string>>("Could not save imported Applications");
        }

        foreach (var importedApplicationId in importedApplicationIds.Keys)
        {
            try
            {
                await _busControl.Publish(
                    new CentrePointCalculationMessage(request.WoodlandOwnerId, request.UserId, importedApplicationId, request.IsFcUser, request.AgencyId),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex, "Error publishing calculate centrepoint message for application id {ApplicationId}", importedApplicationId);
                return Result.Failure<Dictionary<Guid, string>>("Could not publish calculate centrepoint message for imported application");
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(importedApplicationIds);
    }

    private Result<FellingLicenceApplication> BuildNextApplicationEntity(
        DateTime now,
        Guid userId,
        Guid woodlandOwnerId,
        ApplicationSource application,
        IEnumerable<PropertyIds> properties,
        List<ProposedFellingSource> fellingRecords,
        List<ProposedRestockingSource> restockingRecords)
    {
        try
        {
            var propertyProfile = properties
                .Single(x => x.Name.Equals(application.Flov2PropertyName, StringComparison.InvariantCultureIgnoreCase));

            var applicationEntity = ParseApplication(
                now, userId, woodlandOwnerId, application, propertyProfile);

            foreach (var felling in fellingRecords.Where(x => x.ApplicationId == application.ApplicationId))
            {
                var compartment = propertyProfile.CompartmentIds
                    .Single(x => x.CompartmentName.Equals(felling.Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase));

                var fellingEntity = ParseFellingDetail(felling, compartment);

                var restockingStatuses = new List<RestockingCompartmentStatus>();
                foreach (var restocking in restockingRecords.Where(x => x.ProposedFellingId == felling.ProposedFellingId))
                {
                    var restockingEntity = ParseRestockingDetail(restocking, propertyProfile, compartment);

                    fellingEntity.ProposedRestockingDetails!.Add(restockingEntity);

                    // track restocking statuses for the compartment
                    RestockingCompartmentStatus restockingCompartmentStatus;
                    if (restockingStatuses.Any(x => x.CompartmentId == restockingEntity.PropertyProfileCompartmentId))
                    {
                        restockingCompartmentStatus = restockingStatuses
                            .First(x => x.CompartmentId == restockingEntity.PropertyProfileCompartmentId);
                    }
                    else
                    {
                        restockingCompartmentStatus = new RestockingCompartmentStatus
                        {
                            CompartmentId = restockingEntity.PropertyProfileCompartmentId,
                            Status = true,
                            RestockingStatuses = new List<RestockingStatus>()
                        };
                        restockingStatuses.Add(restockingCompartmentStatus);
                    }

                    // add this restocking status to the compartment's restocking statuses
                    restockingCompartmentStatus.RestockingStatuses.Add(new RestockingStatus
                    {
                        Id = restockingEntity.Id,
                        Status = true
                    });
                }

                applicationEntity.LinkedPropertyProfile!.ProposedFellingDetails!.Add(fellingEntity);

                // track felling statuses for the compartment
                CompartmentFellingRestockingStatus currentCompartmentStatus;
                if (applicationEntity.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses
                    .Any(x => x.CompartmentId == compartment.Id))
                {
                    currentCompartmentStatus = applicationEntity.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses
                        .First(x => x.CompartmentId == compartment.Id);
                }
                else
                {
                    currentCompartmentStatus = new CompartmentFellingRestockingStatus
                    {
                        CompartmentId = compartment.Id,
                        Status = true,
                        FellingStatuses = []
                    };
                    applicationEntity.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Add(currentCompartmentStatus);
                }

                // add this felling status to the compartment's felling statuses
                currentCompartmentStatus.FellingStatuses.Add(new FellingStatus
                {
                    Id = fellingEntity.Id,
                    Status = true,
                    RestockingCompartmentStatuses = restockingStatuses
                });
            }

            if (applicationEntity.LinkedPropertyProfile!.ProposedFellingDetails!.NotAny())
            {
                applicationEntity.FellingLicenceApplicationStepStatus.SelectCompartmentsStatus = null;
            }

            return Result.Success(applicationEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught parsing application with id {ApplicationId} from source file", application.ApplicationId);
            return Result.Failure<FellingLicenceApplication>($"Error parsing application with ID {application.ApplicationId}: {ex.Message}");
        }
    }

    private FellingLicenceApplication ParseApplication(
        DateTime now,
        Guid userId,
        Guid woodlandOwnerId,
        ApplicationSource application,
        PropertyIds propertyProfile)
    {
        _logger.LogDebug("Parsing application from import source file with id {ApplicationId}", application.ApplicationId);

        var applicationEntity = new FellingLicenceApplication
        {
            Id = Guid.NewGuid(),
            WoodlandOwnerId = woodlandOwnerId,
            CreatedById = userId,
            CreatedTimestamp = now,
            DateReceived = now,
            ProposedFellingStart = application.ProposedFellingStart?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            ProposedFellingEnd = application.ProposedFellingEnd?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Measures = application.Measures,
            ProposedTiming = application.ProposedTiming,
            LinkedPropertyProfile = new LinkedPropertyProfile
            {
                PropertyProfileId = propertyProfile.Id,
                ProposedFellingDetails = new List<ProposedFellingDetail>()
            },
            FellingLicenceApplicationStepStatus = new FellingLicenceApplicationStepStatus
            {
                ApplicationDetailsStatus = true,
                SelectCompartmentsStatus = true,
                OperationsStatus = true,
                CompartmentFellingRestockingStatuses = new List<CompartmentFellingRestockingStatus>()
            }
        };

        applicationEntity.StatusHistories.Add(new StatusHistory
        {
            Created = now,
            Status = FellingLicenceStatus.Draft
        });
        applicationEntity.AssigneeHistories.Add(new AssigneeHistory
        {
            Role = AssignedUserRole.Author,
            TimestampAssigned = now,
            AssignedUserId = userId
        });

        return applicationEntity;
    }

    private ProposedFellingDetail ParseFellingDetail(
        ProposedFellingSource felling,
        CompartmentIds compartment)
    {
        _logger.LogDebug(
            "Parsing felling detail for compartment {CompartmentName} with ID {FellingId}",
            compartment.CompartmentName, felling.ProposedFellingId);

        var newProposedFellingDetailId = Guid.NewGuid();

        var fellingSpecies = felling.Species.Split(',');

        bool? isRestocking = felling.OperationType.AllowedRestockingForFellingType(false).Any()
            ? felling.IsRestocking
            : null;

        var fellingEntity = new ProposedFellingDetail
        {
            Id = newProposedFellingDetailId,
            PropertyProfileCompartmentId = compartment.Id,
            OperationType = felling.OperationType,
            AreaToBeFelled = felling.AreaToBeFelled,
            NumberOfTrees = felling.NumberOfTrees,
            EstimatedTotalFellingVolume = felling.EstimatedTotalFellingVolume,
            TreeMarking = felling.TreeMarking,
            IsPartOfTreePreservationOrder = felling.IsPartOfTreePreservationOrder,
            TreePreservationOrderReference = felling.IsPartOfTreePreservationOrder ? felling.TreePreservationOrderReference : null,
            IsWithinConservationArea = felling.IsWithinConservationArea,
            ConservationAreaReference = felling.IsWithinConservationArea ? felling.ConservationAreaReference : null,
            IsRestocking = isRestocking,
            NoRestockingReason = isRestocking is false ? felling.NoRestockingReason : null,
            ProposedRestockingDetails = new List<ProposedRestockingDetail>()
        };
        fellingEntity.FellingSpecies = fellingSpecies.Select(x => new FellingSpecies
        {
            Species = x.Trim()
        }).ToList();

        return fellingEntity;
    }

    private ProposedRestockingDetail ParseRestockingDetail(
        ProposedRestockingSource restocking,
        PropertyIds propertyProfile,
        CompartmentIds fellingCompartment)
    {
        _logger.LogDebug(
            "Parsing restocking detail for operation {RestockingOperation} for felling compartment {CompartmentName}",
            restocking.RestockingProposal.GetDisplayName(), fellingCompartment.CompartmentName);

        var newProposedRestockingId = Guid.NewGuid();

        var proposal = restocking.RestockingProposal;

        var restockingCompartment = proposal.IsAlternativeCompartmentRestockingType()
            ? propertyProfile.CompartmentIds
                .Single(x => x.CompartmentName.Equals(restocking.Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase))
            : fellingCompartment;

        var restockingSpecies = new List<RestockingSpecies>();

        if (proposal != TypeOfProposal.CreateDesignedOpenGround)
        {
            var restockingSpeciesStringList = restocking.SpeciesAndPercentages.Split(',');
            var i = 0;

            while (i < restockingSpeciesStringList.Length)
            {
                restockingSpecies.Add(new RestockingSpecies
                {
                    Species = restockingSpeciesStringList[i].Trim(),
                    Percentage = double.Parse(restockingSpeciesStringList[i + 1].Trim())
                });
                i += 2; // Skip the next item which is the percentage
            }
        }

        double? percentOfRestockArea = restockingCompartment.Area.HasValue
            ? Math.Round(restocking.AreaToBeRestocked / restockingCompartment.Area.Value * 100, 2)
            : null;

        return new ProposedRestockingDetail
        {
            Id = newProposedRestockingId,
            PropertyProfileCompartmentId = restockingCompartment.Id,
            RestockingProposal = proposal,
            Area = restocking.AreaToBeRestocked,
            PercentageOfRestockArea = percentOfRestockArea,
            RestockingDensity = proposal.IsNumberOfTreesRestockingType() || proposal == TypeOfProposal.CreateDesignedOpenGround
                ? null
                : restocking.RestockingDensity,
            NumberOfTrees = proposal.IsNumberOfTreesRestockingType()
                ? restocking.NumberOfTrees
                : null,
            RestockingSpecies = restockingSpecies
        };
    }
}
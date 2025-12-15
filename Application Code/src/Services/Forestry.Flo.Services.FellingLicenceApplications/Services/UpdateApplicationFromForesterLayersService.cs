using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Concrete implementation of <see cref="IUpdateApplicationFromForesterLayers"/>
/// </summary>
public class UpdateApplicationFromForesterLayersService(
    IFellingLicenceApplicationExternalRepository repository,
    IForesterServices foresterServices,
    IOptions<DesignationsOptions> designationsOptions,
    ILogger<UpdateApplicationFromForesterLayersService> logger)
    : IUpdateApplicationFromForesterLayers
{
    private readonly IFellingLicenceApplicationExternalRepository _repository = Guard.Against.Null(repository);
    private readonly IForesterServices _foresterServices = Guard.Against.Null(foresterServices);
    private readonly DesignationsOptions _designationsOptions = Guard.Against.Null(designationsOptions?.Value);
    private readonly ILogger<UpdateApplicationFromForesterLayersService> _logger = logger 
        ?? new NullLogger<UpdateApplicationFromForesterLayersService>();

    /// <inheritdoc />
    public async Task<Result> UpdateForPawsLayersAsync(
        Guid applicationId, 
        Dictionary<Guid, string> selectedCompartments,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating application {ApplicationId} for PAWS layers", applicationId);

        try
        {
            var application = await _repository.GetAsync(applicationId, cancellationToken);

            if (application.HasNoValue)
            {
                _logger.LogError("Could not retrieve application with id {ApplicationId} to update PAWS zones", applicationId);
                return Result.Failure($"Could not retrieve application with id {applicationId}");
            }

            ClearAnyCompartmentsNoLongerSelected(application.Value, selectedCompartments.Select(x => x.Key));

            foreach (var compartment in selectedCompartments)
            {
                var updateResult = await UpdateApplicationCompartmentForPaws(
                    application.Value,
                    compartment.Key,
                    compartment.Value,
                    cancellationToken);

                if (updateResult.IsFailure)
                {
                    _logger.LogError(
                        "Failed to update application {ApplicationId} for PAWS data on compartment {CompartmentId}: {Error}",
                        applicationId,
                        compartment.Key,
                        updateResult.Error);

                    return Result.Failure(
                        $"Failed to update application {applicationId} for PAWS data on compartment {compartment.Key}: {updateResult.Error}");
                }
            }

            var saveResult = await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (saveResult.IsFailure)
            {
                _logger.LogError("Failed to save application {ApplicationId} after updating for PAWS zones: {Error}",
                    applicationId,
                    saveResult.Error);
                return Result.Failure($"Failed to save application {applicationId} after updating for PAWS zones: {saveResult.Error}");
            }

            _logger.LogInformation("Successfully updated application {ApplicationId} for PAWS zones", applicationId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught attempting to update application for PAWS zones");
            return Result.Failure("Failed to update application for PAWS zones");
        }
    }

    private void ClearAnyCompartmentsNoLongerSelected(
        FellingLicenceApplication application,
        IEnumerable<Guid> compartmentIdsWithGisData)
    {
        // get the selected compartment ids, in case the user has updated the application before this message
        // was processed - we don't want to remove designations by mistake because the message was delayed
        var includedCompartmentIds = application.GetAllCompartmentIdsInApplication();
        var completeSetCompartmentIds = includedCompartmentIds.Concat(compartmentIdsWithGisData).Distinct();

        // remove any proposed compartment designations for compartments that are no longer selected
        var toRemoveCompartmentDesignations =
            application.LinkedPropertyProfile!.ProposedCompartmentDesignations?
                .Where(x => completeSetCompartmentIds.Contains(x.PropertyProfileCompartmentId) == false)
                .ToList();

        foreach (var designation in toRemoveCompartmentDesignations ?? [])
        {
            _logger.LogDebug(
                "Compartment with id {CompartmentId} is no longer selected in the application, removing proposed designations",
                designation.PropertyProfileCompartmentId);
            application.LinkedPropertyProfile.ProposedCompartmentDesignations!.Remove(designation);
        }

        // remove the designations step statuses for any compartments that are no longer selected
        var toRemoveStepStatuses = application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses
            .Where(x => completeSetCompartmentIds.Contains(x.CompartmentId) == false)
            .ToList();

        foreach (var status in toRemoveStepStatuses)
        {
            _logger.LogDebug(
                "Compartment with id {CompartmentId} is no longer selected in the application, removing step status",
                status.CompartmentId);
            application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Remove(status);
        }
    }

    private async Task<Result> UpdateApplicationCompartmentForPaws(
        FellingLicenceApplication application,
        Guid compartmentId,
        string gisData,
        CancellationToken cancellationToken)
    {
        // deserialize the compartment GIS data
        var shape = JsonConvert.DeserializeObject<Polygon>(gisData)!;

        // get the zones from the two Forester layers for this shape
        var zonesOnLayer1 = await _foresterServices.GetAncientWoodlandAsync(shape, cancellationToken);

        if (zonesOnLayer1.IsFailure)
        {
            _logger.LogError(
                "Failed to retrieve Ancient Woodland layer data from Forester for compartment {CompartmentId}: {Error}",
                compartmentId,
                zonesOnLayer1.Error);
            return Result.Failure(
                $"Failed to retrieve Ancient Woodland layer data from Forester for compartment {compartmentId}: {zonesOnLayer1.Error}");
        }

        var zonesOnLayer2 = await _foresterServices.GetAncientWoodlandsRevisedAsync(shape, cancellationToken);

        if (zonesOnLayer2.IsFailure)
        {
            _logger.LogError(
                "Failed to retrieve Ancient Woodlands Revised layer data from Forester for compartment {CompartmentId}: {Error}",
                compartmentId,
                zonesOnLayer2.Error);
            return Result.Failure(
                $"Failed to retrieve Ancient Woodlands Revised layer data from Forester for compartment {compartmentId}: {zonesOnLayer2.Error}");
        }

        // get list of detected zone names
        var detectedZones = zonesOnLayer1.Value.Concat(zonesOnLayer2.Value)
            .Select(z => z.Status?.ToUpper() ?? string.Empty)
            .Distinct()
            .ToList();

        // get count of how many zones match the names in configuration for the zones we are interested in
        var zonesOfInterest = detectedZones.Where(x => 
            _designationsOptions.PawsZoneNames.Select(p => p.ToUpper()).Contains(x))
            .ToList();

        _logger.LogDebug("Detected that compartment {CompartmentId} intersects {ZonesCount} PAWS zones", compartmentId, zonesOfInterest.Count);

        // update or create the proposed compartment designation entity for this compartment
        var designationEntity = application.LinkedPropertyProfile!
            .ProposedCompartmentDesignations?
            .FirstOrDefault(d => d.PropertyProfileCompartmentId == compartmentId);

        if (designationEntity == null)
        {
            designationEntity = new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                LinkedPropertyProfileId = application.LinkedPropertyProfile.Id,
                PropertyProfileCompartmentId = compartmentId
            };
            
            application.LinkedPropertyProfile.ProposedCompartmentDesignations ??= new List<ProposedCompartmentDesignations>();
            application.LinkedPropertyProfile.ProposedCompartmentDesignations.Add(designationEntity);
        }

        designationEntity.CrossesPawsZones = zonesOfInterest;

        if (zonesOfInterest.Count == 0)
        {
            designationEntity.ProportionBeforeFelling = null;
            designationEntity.ProportionAfterFelling = null;
        }

        // add or remove the designations step status for this compartment if required
        var stepStatusEntity = application.FellingLicenceApplicationStepStatus
            .CompartmentDesignationsStatuses
            .FirstOrDefault(s => s.CompartmentId == compartmentId);

        if (stepStatusEntity == null && zonesOfInterest.Count > 0)
        {
            application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Add(
                new CompartmentDesignationStatus
                {
                    CompartmentId = compartmentId
                });
        }

        if (stepStatusEntity != null && zonesOfInterest.Count == 0)
        {
            application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Remove(stepStatusEntity);
        }

        return Result.Success();
    }
}
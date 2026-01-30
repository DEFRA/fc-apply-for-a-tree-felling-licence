using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;

namespace Forestry.Flo.External.Web.Services;

public class HabitatRestorationUseCase(
    IRetrieveUserAccountsService retrieveUserAccountsService,
    IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers,
    IGetPropertyProfiles getPropertyProfilesService,
    IGetCompartments getCompartmentsService,
    IAgentAuthorityService agentAuthorityService,
    IFellingLicenceApplicationExternalRepository repository,
    IHabitatRestorationService habitatService,
    IAuditService<HabitatRestorationUseCase> audit,
    RequestContext requestContext,
    ILogger<HabitatRestorationUseCase> logger)
    : ApplicationUseCaseCommon(
        retrieveUserAccountsService,
        retrieveWoodlandOwnersService,
        getFellingLicenceApplicationServiceForExternalUsers,
        getPropertyProfilesService,
        getCompartmentsService,
        agentAuthorityService,
        logger)
{
    private readonly IFellingLicenceApplicationExternalRepository _repository = Guard.Against.Null(repository);
    private readonly IHabitatRestorationService _habitat = Guard.Against.Null(habitatService);
    private readonly IAuditService<HabitatRestorationUseCase> _audit = Guard.Against.Null(audit);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    private readonly ILogger<HabitatRestorationUseCase> _logger = Guard.Against.Null(logger);

    public async Task<Result<Guid>> SetHabitatRestorationStatus(ExternalApplicant user, Guid applicationId, bool? isPriorityOpenHabitat, CancellationToken cancellationToken)
    {
        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);
        if (userAccess.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            await AuditForUpdateFailure(applicationId, user, userAccess.Error, cancellationToken);
            return Result.Failure<Guid>(userAccess.Error);
        }

        var appResult = await _repository.GetAsync(applicationId, cancellationToken);
        if (!appResult.HasValue || appResult.Value.LinkedPropertyProfile is null)
        {
            await AuditForUpdateFailure(applicationId, user, "Application not found", cancellationToken);
            return Result.Failure<Guid>("Application not found");
        }

        var application = appResult.Value;
        application.IsPriorityOpenHabitat = isPriorityOpenHabitat;
        if (isPriorityOpenHabitat.HasValue && isPriorityOpenHabitat.Value == false)
        {
            // Remove any existing habitat restorations if the application is not priority open habitat
            await _habitat.DeleteHabitatRestorationsAsync(applicationId, cancellationToken);

            application.FellingLicenceApplicationStepStatus.HabitatRestorationStatus = true;
        }
        else if (isPriorityOpenHabitat.HasValue && isPriorityOpenHabitat.Value == true)
        {
            // When priority open habitat is selected, ensure the step status reflects whether restorations are present and complete
            var restorations = await _habitat.GetHabitatRestorationModelsAsync(applicationId, cancellationToken);
            var hasAny = restorations.Count > 0;
            var allComplete = hasAny && restorations.All(r => r.Completed.HasValue && r.Completed.Value);

            application.FellingLicenceApplicationStepStatus.HabitatRestorationStatus = allComplete;
        }

        _repository.Update(application);
        var save = await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        if (save.IsFailure)
        {
            _logger.LogError("Failed to save habitat restoration for {ApplicationId}: {Error}", applicationId, save.Error);
            await AuditForUpdateFailure(applicationId, user, save.Error.ToString(), cancellationToken);
            return Result.Failure<Guid>(save.Error.ToString());
        }

        await AuditForUpdateSuccess(applicationId, user, isPriorityOpenHabitat, cancellationToken);
        return Result.Success(applicationId);
    }

    /// <summary>
    /// Gets a dictionary of compartment Ids valid for habitat restoration in the application, alongside a flag
    /// indicating whether the compartment already has a habitat restoration record.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve data for.</param>
    /// <param name="user">The user requesting the data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A dictionary of valid compartment Ids and their habitat restoration status.</returns>
    public async Task<Result<Dictionary<Guid, bool>>> GetHabitatCompartmentIds(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var restorations = await _habitat.GetHabitatRestorationModelsAsync(applicationId, cancellationToken);
        var existingIds = restorations.Select(r => r.PropertyProfileCompartmentId).ToList();

        var applicationResult = await base.GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);
        if (applicationResult.IsFailure)
        {
            return Result.Failure<Dictionary<Guid, bool>>(applicationResult.Error);
        }
        var application = applicationResult.Value;

        var compartmentIdsValidForHabitats = application.CompartmentIdsValidForHabitatRestoration();

        var result = new Dictionary<Guid, bool>();
        foreach (var compartmentIdValidForHabitats in compartmentIdsValidForHabitats)
        {
            result.Add(compartmentIdValidForHabitats, existingIds.Contains(compartmentIdValidForHabitats));
        }

        return result;
    }

    public async Task<Result<bool>> AreAnyNewHabitats(
        Guid applicationId,
        IEnumerable<Guid>? selectedCompartmentIds,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var currentIds = await GetHabitatCompartmentIds(applicationId, user,cancellationToken);
        if (currentIds.IsFailure)
        {
            return currentIds.ConvertFailure<bool>();
        }

        var existingHabitats = currentIds.Value.Where(x => x.Value).Select(x => x.Key);
        return Result.Success((selectedCompartmentIds ?? []).Except(existingHabitats).Any());
    }

    public async Task<Result> UpdateHabitatCompartments(
        Guid applicationId,
        IEnumerable<Guid> selectedCompartmentIds,
        CancellationToken cancellationToken)
    {
        var current = await _habitat.GetHabitatRestorationModelsAsync(applicationId, cancellationToken);
        var currentIds = current.Select(c => c.PropertyProfileCompartmentId).ToHashSet();
        var selectedIds = selectedCompartmentIds?.ToHashSet() ?? new HashSet<Guid>();

        // Delete restorations for compartments no longer selected
        foreach (var toRemove in currentIds.Except(selectedIds))
        {
            var del = await _habitat.DeleteHabitatRestorationAsync(applicationId, toRemove, cancellationToken);
            if (del.IsFailure)
            {
                _logger.LogWarning("Failed to delete habitat restoration for {AppId} compartment {CompId}", applicationId, toRemove);
            }
        }

        // Add restorations for newly selected compartments
        foreach (var toAdd in selectedIds.Except(currentIds))
        {
            var add = await _habitat.AddHabitatRestorationAsync(applicationId, toAdd, cancellationToken);
            if (add.IsFailure)
            {
                _logger.LogWarning("Failed to add habitat restoration for {AppId} compartment {CompId}", applicationId, toAdd);
            }
        }

        return Result.Success();
    }

    public async Task<Result> UpdateHabitatType(
        Guid applicationId,
        Guid compartmentId,
        HabitatType habitatType,
        string? otherHabitatDescription,
        CancellationToken cancellationToken)
    {
        var maybe = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (maybe.HasNoValue)
        {
            return Result.Failure("Habitat restoration not found");
        }

        var restoration = maybe.Value;
        restoration.HabitatType = habitatType;
        restoration.OtherHabitatDescription = otherHabitatDescription;
        var save = await _habitat.UpdateHabitatRestorationModelAsync(restoration, cancellationToken);
        if (save.IsFailure)
        {
            _logger.LogError("Failed to update habitat type for {ApplicationId}/{CompartmentId}: {Error}", applicationId, compartmentId, save.Error);
            return Result.Failure("Failed to update habitat type");
        }

        return Result.Success();
    }

    public async Task<Result> UpdateWoodlandSpecies(
        Guid applicationId,
        Guid compartmentId,
        WoodlandSpeciesType speciesType,
        CancellationToken cancellationToken)
    {
        var maybe = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (maybe.HasNoValue)
        {
            return Result.Failure("Habitat restoration not found");
        }

        var restoration = maybe.Value;
        restoration.WoodlandSpeciesType = speciesType;
        // If the species type is BroadleafWoodland, clear NativeBroadleaf selection
        if (speciesType != WoodlandSpeciesType.BroadleafWoodland)
        {
            restoration.NativeBroadleaf = null;
        }
        var save = await _habitat.UpdateHabitatRestorationModelAsync(restoration, cancellationToken);
        if (save.IsFailure)
        {
            _logger.LogError("Failed to update woodland species for {ApplicationId}/{CompartmentId}: {Error}", applicationId, compartmentId, save.Error);
            return Result.Failure("Failed to update woodland species type");
        }

        return Result.Success();
    }

    public async Task<Result> UpdateNativeBroadleaf(
        Guid applicationId,
        Guid compartmentId,
        bool? isNativeBroadleaf,
        CancellationToken cancellationToken)
    {
        var maybe = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (maybe.HasNoValue)
        {
            return Result.Failure("Habitat restoration not found");
        }

        var restoration = maybe.Value;
        restoration.NativeBroadleaf = isNativeBroadleaf;
        var save = await _habitat.UpdateHabitatRestorationModelAsync(restoration, cancellationToken);
        if (save.IsFailure)
        {
            _logger.LogError("Failed to update native broadleaf for {ApplicationId}/{CompartmentId}: {Error}", applicationId, compartmentId, save.Error);
            return Result.Failure("Failed to update native broadleaf");
        }

        return Result.Success();
    }

    public async Task<Maybe<Guid>> GetHabitatNextCompartment(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var restorations = await _habitat.GetHabitatRestorationModelsAsync(applicationId, cancellationToken);
        var next = restorations
            .OrderBy(r => r.PropertyProfileCompartmentId)
            .FirstOrDefault(r => r.Completed is null || r.Completed == false);

        return next is null
            ? Maybe<Guid>.None
            : Maybe<Guid>.From(next.PropertyProfileCompartmentId);
    }

    public async Task<Result> UpdateCompleted(
        Guid applicationId,
        Guid compartmentId,
        bool completed,
        CancellationToken cancellationToken)
    {
        var maybe = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (maybe.HasNoValue)
        {
            return Result.Failure("Habitat restoration not found");
        }

        var restoration = maybe.Value;
        restoration.Completed = completed;
        var save = await _habitat.UpdateHabitatRestorationModelAsync(restoration, cancellationToken);
        if (save.IsFailure)
        {
            _logger.LogError("Failed to mark habitat restoration completed for {ApplicationId}/{CompartmentId}: {Error}", applicationId, compartmentId, save.Error);
            await _audit.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.HabitatRestorationUpdateFailure,
                    applicationId,
                    null,
                    _requestContext,
                    new { CompartmentId = compartmentId, Completed = completed, Error = save.Error.ToString() }),
                cancellationToken);
            return Result.Failure("Failed to update completed status");
        }

        await _audit.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.HabitatRestorationUpdate,
                applicationId,
                null,
                _requestContext,
                new { CompartmentId = compartmentId, Completed = completed }),
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateFelledEarly(
        Guid applicationId,
        Guid compartmentId,
        bool? felledEarly,
        CancellationToken cancellationToken)
    {
        var maybe = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (maybe.HasNoValue)
        {
            return Result.Failure("Habitat restoration not found");
        }

        var restoration = maybe.Value;
        restoration.FelledEarly = felledEarly;
        var save = await _habitat.UpdateHabitatRestorationModelAsync(restoration, cancellationToken);
        if (save.IsFailure)
        {
            _logger.LogError("Failed to update felled early for {ApplicationId}/{CompartmentId}: {Error}", applicationId, compartmentId, save.Error);
            return Result.Failure("Failed to update felled early");
        }

        return Result.Success();
    }

    public async Task<Maybe<HabitatRestorationViewModel>> GetHabitatRestoration(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        var maybe = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (maybe.HasNoValue)
        {
            return Maybe<HabitatRestorationViewModel>.None;
        }

        var r = maybe.Value;
        var model = new HabitatRestorationViewModel
        {
            Id = r.Id,
            LinkedPropertyProfileId = r.LinkedPropertyProfileId,
            PropertyProfileCompartmentId = r.PropertyProfileCompartmentId,
            HabitatType = r.HabitatType,
            OtherHabitatDescription = r.OtherHabitatDescription,
            WoodlandSpeciesType = r.WoodlandSpeciesType,
            NativeBroadleaf = r.NativeBroadleaf,
            ProductiveWoodland = r.ProductiveWoodland,
            FelledEarly = r.FelledEarly,
            Completed = r.Completed
        };

        return Maybe<HabitatRestorationViewModel>.From(model);
    }

    public async Task<Result> UpdateProductiveWoodland(
        Guid applicationId,
        Guid compartmentId,
        bool? isProductiveWoodland,
        CancellationToken cancellationToken)
    {
        var maybe = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (maybe.HasNoValue)
        {
            return Result.Failure("Habitat restoration not found");
        }

        var restoration = maybe.Value;
        restoration.ProductiveWoodland = isProductiveWoodland;
        if (isProductiveWoodland != true)
        {
            restoration.FelledEarly = null;
        }

        var save = await _habitat.UpdateHabitatRestorationModelAsync(restoration, cancellationToken);
        if (save.IsFailure)
        {
            _logger.LogError("Failed to update productive woodland for {ApplicationId}/{CompartmentId}: {Error}", applicationId, compartmentId, save.Error);
            return Result.Failure("Failed to update productive woodland");
        }

        return Result.Success();
    }

    public async Task<IReadOnlyList<HabitatRestorationViewModel>> GetHabitatRestorations(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var restorations = await _habitat.GetHabitatRestorationModelsAsync(applicationId, cancellationToken);
        var list = new List<HabitatRestorationViewModel>(restorations.Count);
        foreach (var r in restorations)
        {
            list.Add(new HabitatRestorationViewModel
            {
                Id = r.Id,
                LinkedPropertyProfileId = r.LinkedPropertyProfileId,
                PropertyProfileCompartmentId = r.PropertyProfileCompartmentId,
                HabitatType = r.HabitatType,
                OtherHabitatDescription = r.OtherHabitatDescription,
                WoodlandSpeciesType = r.WoodlandSpeciesType,
                NativeBroadleaf = r.NativeBroadleaf,
                ProductiveWoodland = r.ProductiveWoodland,
                FelledEarly = r.FelledEarly,
                Completed = r.Completed
            });
        }
        return list;
    }

    public async Task<Result<HabitatTypeModel>> GetHabitatTypeModel(
        Guid applicationId,
        Guid compartmentId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var summaryResult = await GetFlaSummaryAsync(applicationId, user, cancellationToken);
        if (summaryResult.IsFailure)
        {
            return Result.Failure<HabitatTypeModel>(summaryResult.Error);
        }

        var compartmentResult = await GetCompartmentByIdAsync(compartmentId, user, cancellationToken);
        if (compartmentResult.IsFailure)
        {
            return Result.Failure<HabitatTypeModel>(compartmentResult.Error);
        }

        var habitatResult = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (habitatResult.HasNoValue)
        {
            return Result.Failure<HabitatTypeModel>("Habitat restoration not found");
        }

        var model = new HabitatTypeModel
        {
            ApplicationId = applicationId,
            ApplicationReference = summaryResult.Value.ApplicationReference,
            CompartmentId = compartmentId,
            CompartmentName = compartmentResult.Value.CompartmentNumber,
            SelectedHabitatType = habitatResult.Value.HabitatType,
            OtherHabitatDescription = habitatResult.Value.OtherHabitatDescription,
            ApplicationSummary = summaryResult.Value
        };

        return Result.Success(model);
    }

    public async Task<Result<WoodlandSpeciesTypeModel>> GetWoodlandSpeciesTypeModel(
        Guid applicationId,
        Guid compartmentId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var summaryResult = await GetFlaSummaryAsync(applicationId, user, cancellationToken);
        if (summaryResult.IsFailure)
        {
            return Result.Failure<WoodlandSpeciesTypeModel>(summaryResult.Error);
        }

        var compartmentResult = await GetCompartmentByIdAsync(compartmentId, user, cancellationToken);
        if (compartmentResult.IsFailure)
        {
            return Result.Failure<WoodlandSpeciesTypeModel>(compartmentResult.Error);
        }

        var habitatResult = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (habitatResult.HasNoValue)
        {
            return Result.Failure<WoodlandSpeciesTypeModel>("Habitat restoration not found");
        }

        var model = new WoodlandSpeciesTypeModel
        {
            ApplicationId = applicationId,
            ApplicationReference = summaryResult.Value.ApplicationReference,
            CompartmentId = compartmentId,
            CompartmentName = compartmentResult.Value.CompartmentNumber,
            SelectedSpeciesType = habitatResult.Value.WoodlandSpeciesType,
            ApplicationSummary = summaryResult.Value
        };

        return Result.Success(model);
    }

    public async Task<Result<HabitatNativeBroadleafModel>> GetHabitatNativeBroadleafModel(
        Guid applicationId,
        Guid compartmentId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var summaryResult = await GetFlaSummaryAsync(applicationId, user, cancellationToken);
        if (summaryResult.IsFailure)
        {
            return Result.Failure<HabitatNativeBroadleafModel>(summaryResult.Error);
        }

        var compartmentResult = await GetCompartmentByIdAsync(compartmentId, user, cancellationToken);
        if (compartmentResult.IsFailure)
        {
            return Result.Failure<HabitatNativeBroadleafModel>(compartmentResult.Error);
        }

        var habitatResult = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (habitatResult.HasNoValue)
        {
            return Result.Failure<HabitatNativeBroadleafModel>("Habitat restoration not found");
        }

        var model = new HabitatNativeBroadleafModel
        {
            ApplicationId = applicationId,
            ApplicationReference = summaryResult.Value.ApplicationReference,
            CompartmentId = compartmentId,
            CompartmentName = compartmentResult.Value.CompartmentNumber,
            IsNativeBroadleaf = habitatResult.Value.NativeBroadleaf,
            ApplicationSummary = summaryResult.Value
        };

        return Result.Success(model);
    }

    public async Task<Result<HabitatFelledEarlyModel>> GetHabitatFelledEarlyModel(
        Guid applicationId,
        Guid compartmentId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var summaryResult = await GetFlaSummaryAsync(applicationId, user, cancellationToken);
        if (summaryResult.IsFailure)
        {
            return Result.Failure<HabitatFelledEarlyModel>(summaryResult.Error);
        }

        var compartmentResult = await GetCompartmentByIdAsync(compartmentId, user, cancellationToken);
        if (compartmentResult.IsFailure)
        {
            return Result.Failure<HabitatFelledEarlyModel>(compartmentResult.Error);
        }

        var habitatResult = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (habitatResult.HasNoValue)
        {
            return Result.Failure<HabitatFelledEarlyModel>("Habitat restoration not found");
        }

        var model = new HabitatFelledEarlyModel
        {
            ApplicationId = applicationId,
            ApplicationReference = summaryResult.Value.ApplicationReference,
            CompartmentId = compartmentId,
            CompartmentName = compartmentResult.Value.CompartmentNumber,
            IsFelledEarly = habitatResult.Value.FelledEarly,
            ApplicationSummary = summaryResult.Value
        };

        return Result.Success(model);
    }

    public async Task<Result<HabitatProductiveWoodlandModel>> GetHabitatProductiveWoodlandModel(
        Guid applicationId,
        Guid compartmentId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var summaryResult = await GetFlaSummaryAsync(applicationId, user, cancellationToken);
        if (summaryResult.IsFailure)
        {
            return Result.Failure<HabitatProductiveWoodlandModel>(summaryResult.Error);
        }

        var compartmentResult = await GetCompartmentByIdAsync(compartmentId, user, cancellationToken);
        if (compartmentResult.IsFailure)
        {
            return Result.Failure<HabitatProductiveWoodlandModel>(compartmentResult.Error);
        }

        var habitatResult = await _habitat.GetHabitatRestorationModelAsync(applicationId, compartmentId, cancellationToken);
        if (habitatResult.HasNoValue)
        {
            return Result.Failure<HabitatProductiveWoodlandModel>("Habitat restoration not found");
        }

        var model = new HabitatProductiveWoodlandModel
        {
            ApplicationId = applicationId,
            ApplicationReference = summaryResult.Value.ApplicationReference,
            CompartmentId = compartmentId,
            CompartmentName = compartmentResult.Value.CompartmentNumber,
            IsProductiveWoodland = habitatResult.Value.ProductiveWoodland,
            ApplicationSummary = summaryResult.Value
        };

        return Result.Success(model);
    }

    /// <summary>
    /// Ensures that existing habitat restoration records are still valid for the given application.
    /// </summary>
    /// <remarks>
    /// This method should be called after changes such as unselecting felling compartments or changing the
    /// selected felling/restocking types in a compartment. It will remove any habitat restoration records
    /// that are no longer applicable based on the current state of the application.
    /// </remarks>
    /// <param name="applicationId">The Id of the application being updated.</param>
    /// <param name="user">The user editing the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success of the operation.</returns>
    public async Task<Result> EnsureExistingHabitatRestorationRecordsAsync(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var existingHabitatRestorations = await _habitat.GetHabitatRestorationModelsAsync(applicationId, cancellationToken);

        var existingHabitatRestorationCompartmentIds =
            existingHabitatRestorations.Select(x => x.PropertyProfileCompartmentId).ToList();

        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);
        if (userAccess.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            await AuditForUpdateFailure(applicationId, user, userAccess.Error, cancellationToken);
            return Result.Failure<Guid>(userAccess.Error);
        }

        var checkAccess = await _repository.CheckUserCanAccessApplicationAsync(applicationId, userAccess.Value, cancellationToken);
        if (checkAccess.IsFailure || !checkAccess.Value)
        {
            _logger.LogError("Unable to verify user access for user with id {UserId}", user.UserAccountId.Value);
            await AuditForUpdateFailure(applicationId, user, "User cannot access application", cancellationToken);
            return Result.Failure<Guid>(userAccess.Error);
        }

        var appResult = await _repository.GetAsync(applicationId, cancellationToken);
        if (!appResult.HasValue || appResult.Value.LinkedPropertyProfile is null)
        {
            await AuditForUpdateFailure(applicationId, user, "Application not found", cancellationToken);
            return Result.Failure<Guid>("Application not found");
        }

        var application = appResult.Value;

        var validIdsForHabitatRestoration = application.CompartmentIdsValidForHabitatRestoration();

        foreach (var existingHabitatRestorationCompartmentId in existingHabitatRestorationCompartmentIds)
        {
            if (!validIdsForHabitatRestoration.Contains(existingHabitatRestorationCompartmentId))
            {
                var deleteResult = await _habitat.DeleteHabitatRestorationAsync(applicationId, existingHabitatRestorationCompartmentId, cancellationToken);
                if (deleteResult.IsFailure)
                {
                    _logger.LogError(
                        "Failed to delete invalid habitat restoration for compartment {CompartmentId} on application {ApplicationId}: {Error}", 
                        existingHabitatRestorationCompartmentId, applicationId, deleteResult.Error);
                    return Result.Failure("Failed to remove invalid habitat restoration records");
                }
            }
        }

        // if there are no longer any compartments valid for habitat restoration, clear the option from the application
        if (!application.ShouldApplicationRequireHabitatRestoration() && application.IsPriorityOpenHabitat.HasValue)
        {
            application.IsPriorityOpenHabitat = null;
            application.FellingLicenceApplicationStepStatus.HabitatRestorationStatus = null;

            _repository.Update(application);
            var save = await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (save.IsFailure)
            {
                _logger.LogError("Failed to save habitat restoration state for {ApplicationId}: {Error}", applicationId, save.Error);
                await AuditForUpdateFailure(applicationId, user, save.Error.ToString(), cancellationToken);
                return Result.Failure(save.Error.ToString());
            }

            await AuditForUpdateSuccess(applicationId, user, null, cancellationToken);
        }

        return Result.Success();
    }

    private async Task<Result<FellingLicenceApplicationSummary>> GetFlaSummaryAsync(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var applicationResult = await base.GetFellingLicenceApplicationAsync(applicationId, user, cancellationToken);
        if (applicationResult.IsFailure)
        {
            return Result.Failure<FellingLicenceApplicationSummary>(applicationResult.Error);
        }
        var application = applicationResult.Value;

        var applicationSummaryResult = await base.GetApplicationSummaryAsync(application, user, cancellationToken);
        if (applicationSummaryResult.IsFailure)
        {
            _logger.LogError("Unable to load application summary for application with id {ApplicationId}", applicationId);
            return Result.Failure<FellingLicenceApplicationSummary>("Unable to load application summary");
        }

        return applicationSummaryResult;
    }

    private Task AuditForUpdateSuccess(
        Guid applicationId,
        ExternalApplicant user,
        bool? isPriorityOpenHabitat,
        CancellationToken cancellationToken = default) =>
        _audit.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.HabitatRestorationUpdate,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { isPriorityOpenHabitat }),
            cancellationToken);

    private Task AuditForUpdateFailure(
        Guid applicationId,
        ExternalApplicant user,
        string error,
        CancellationToken cancellationToken = default) =>
        _audit.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.HabitatRestorationUpdateFailure,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { error }),
            cancellationToken);
}
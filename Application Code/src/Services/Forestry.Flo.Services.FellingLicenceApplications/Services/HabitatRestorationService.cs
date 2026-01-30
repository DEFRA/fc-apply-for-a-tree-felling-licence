using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public interface IHabitatRestorationService
{
    Task<Maybe<Models.HabitatRestorationModel>> GetHabitatRestorationModelAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Models.HabitatRestorationModel>> GetHabitatRestorationModelsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    Task<UnitResult<UserDbErrorReason>> AddHabitatRestorationAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken);

    Task<UnitResult<UserDbErrorReason>> UpdateHabitatRestorationAsync(HabitatRestoration habitatRestoration, CancellationToken cancellationToken);

    Task<UnitResult<UserDbErrorReason>> UpdateHabitatRestorationModelAsync(Models.HabitatRestorationModel model, CancellationToken cancellationToken);

    Task<UnitResult<UserDbErrorReason>> DeleteHabitatRestorationAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken);

    Task<UnitResult<UserDbErrorReason>> DeleteHabitatRestorationsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);
}

public class HabitatRestorationService : IHabitatRestorationService
{
    private readonly IFellingLicenceApplicationExternalRepository _externalRepository;

    public HabitatRestorationService(
        IFellingLicenceApplicationExternalRepository externalRepository)
    {
        _externalRepository = externalRepository;
    }

    public async Task<Maybe<Models.HabitatRestorationModel>> GetHabitatRestorationModelAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        var maybeEntity = await _externalRepository.GetHabitatRestorationAsync(applicationId, compartmentId, cancellationToken);
        if (maybeEntity.HasNoValue)
        {
            return Maybe<Models.HabitatRestorationModel>.None;
        }
        var r = maybeEntity.Value;

        var model = new Models.HabitatRestorationModel
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

        return Maybe<Models.HabitatRestorationModel>.From(model);
    }

    public async Task<IReadOnlyList<Models.HabitatRestorationModel>> GetHabitatRestorationModelsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var restorations = await _externalRepository.GetHabitatRestorationsAsync(applicationId, cancellationToken);

        var list = new List<Models.HabitatRestorationModel>(restorations.Count);
        foreach (var r in restorations)
        {
            list.Add(new Models.HabitatRestorationModel
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

    public async Task<UnitResult<UserDbErrorReason>> AddHabitatRestorationAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        return await _externalRepository.AddHabitatRestorationAsync(applicationId, compartmentId, cancellationToken);
    }

    public async Task<UnitResult<UserDbErrorReason>> UpdateHabitatRestorationAsync(
        HabitatRestoration habitatRestoration,
        CancellationToken cancellationToken)
    {
        return await _externalRepository.UpdateHabitatRestorationAsync(habitatRestoration, cancellationToken);
    }

    public Task<UnitResult<UserDbErrorReason>> UpdateHabitatRestorationModelAsync(
        Models.HabitatRestorationModel model,
        CancellationToken cancellationToken)
    {
        var entity = new HabitatRestoration
        {
            Id = model.Id,
            LinkedPropertyProfileId = model.LinkedPropertyProfileId,
            PropertyProfileCompartmentId = model.PropertyProfileCompartmentId,
            HabitatType = model.HabitatType,
            OtherHabitatDescription = model.OtherHabitatDescription,
            WoodlandSpeciesType = model.WoodlandSpeciesType,
            NativeBroadleaf = model.NativeBroadleaf,
            ProductiveWoodland = model.ProductiveWoodland,
            FelledEarly = model.FelledEarly,
            Completed = model.Completed
        };

        return UpdateHabitatRestorationAsync(entity, cancellationToken);
    }

    public async Task<UnitResult<UserDbErrorReason>> DeleteHabitatRestorationAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken)
    {
        return await _externalRepository.DeleteHabitatRestorationAsync(applicationId, compartmentId, cancellationToken);
    }

    public async Task<UnitResult<UserDbErrorReason>> DeleteHabitatRestorationsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        return await _externalRepository.DeleteHabitatRestorationsAsync(applicationId, cancellationToken);
    }
}

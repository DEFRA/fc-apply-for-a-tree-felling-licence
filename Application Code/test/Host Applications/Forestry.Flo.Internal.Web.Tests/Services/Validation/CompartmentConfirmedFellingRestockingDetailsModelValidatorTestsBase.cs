using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Validation;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Tests.Services.Validation;

public abstract class CompartmentConfirmedFellingRestockingDetailsModelValidatorTestsBase
{
    protected CompartmentConfirmedFellingRestockingDetailsModelValidator _sut;

    protected CompartmentConfirmedFellingRestockingDetailsModelValidatorTestsBase()
    {
        _sut = new CompartmentConfirmedFellingRestockingDetailsModelValidator();
    }

    public static CompartmentConfirmedFellingRestockingDetailsModel CreateValidModel(FellingOperationType operationType, TypeOfProposal restockingProposal)
    {
        var restockingRequired = restockingProposal is not (TypeOfProposal.DoNotIntendToRestock or TypeOfProposal.None);
        var fellingRequired = operationType is not FellingOperationType.None;

        var model = new CompartmentConfirmedFellingRestockingDetailsModel
        {
            CompartmentId = Guid.NewGuid(),
            CompartmentNumber = "CompartmentNumber",
            SubCompartmentName = "SubCompartmentName",
            TotalHectares = 54d,
            ConfirmedFellingDetails = new[]
            {
                new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = Guid.NewGuid(),
                    AreaToBeFelled = fellingRequired ? 53d : null,
                    OperationType = operationType,
                    NumberOfTrees = fellingRequired ? 3400 : null,
                    IsTreeMarkingUsed = fellingRequired,
                    TreeMarking = fellingRequired ? "TreeMarking" : null,
                    IsPartOfTreePreservationOrder = fellingRequired ? false : null,
                    IsWithinConservationArea = fellingRequired ? false : null,
                    EstimatedTotalFellingVolume = 54d,
                    ConfirmedFellingSpecies = fellingRequired ? new[]
                    {
                        new ConfirmedFellingSpeciesModel
                        {
                            Deleted = false,
                            Id = Guid.NewGuid(),
                            Species = "FellingSpecies" + 0,
                            SpeciesType = SpeciesType.Broadleaf
                        },
                        new ConfirmedFellingSpeciesModel
                        {
                            Deleted = false,
                            Id = Guid.NewGuid(),
                            Species = "FellingSpecies" + 1,
                            SpeciesType = SpeciesType.Broadleaf
                        },
                    } : Array.Empty<ConfirmedFellingSpeciesModel>(),
                    ConfirmedRestockingDetails = new[]
                    {
                        new ConfirmedRestockingDetailViewModel
                        {
                            ConfirmedRestockingDetailsId = Guid.NewGuid(),
                            RestockArea = restockingRequired ? 50d : null,
                            PercentOpenSpace = restockingRequired ? 10 : null,
                            RestockingProposal = restockingProposal,
                            RestockingDensity = restockingRequired ? 1020d : null,
                            PercentNaturalRegeneration = restockingRequired ? 30 : null,
                            ConfirmedRestockingSpecies = restockingRequired ? new[]
                            {
                                new ConfirmedRestockingSpeciesModel
                                {
                                    Deleted = false,
                                    Id = Guid.NewGuid(),
                                    Percentage = 50,
                                    Species = "RestockSpecies" + 0,
                                },
                                new ConfirmedRestockingSpeciesModel
                                {
                                    Deleted = false,
                                    Id = Guid.NewGuid(),
                                    Percentage = 40,
                                    Species = "RestockSpecies" + 1,
                                },
                            } : Array.Empty<ConfirmedRestockingSpeciesModel>()
                        }
                    }
                }
            },
        };
        return model;
    }
}
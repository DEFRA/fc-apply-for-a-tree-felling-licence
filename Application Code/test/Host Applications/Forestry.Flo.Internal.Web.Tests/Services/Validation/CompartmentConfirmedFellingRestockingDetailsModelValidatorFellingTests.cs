using FluentValidation.TestHelper;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Tests.Services.Validation;

public class CompartmentConfirmedFellingRestockingDetailsModelValidatorFellingTests : CompartmentConfirmedFellingRestockingDetailsModelValidatorTestsBase
{
    [Fact]
    public void ShouldValidate_WithSuccess()
    {
        var model = CreateValidModel(FellingOperationType.Thinning, TypeOfProposal.None);

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotValidate_WhenAreaToBeFelledIsNegative()
    {
        var model = CreateValidModel(FellingOperationType.Thinning, TypeOfProposal.None);
        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            confirmedFellingDetail.AreaToBeFelled = -12;

            var result = _sut.TestValidate(model);

            result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].AreaToBeFelled");
            Assert.Contains($"Compartment {model.CompartmentName} - Area to be felled must be a positive value",
                result.Errors.Select(x => x.ErrorMessage));
        }
    }

    [Fact]
    public void ShouldNotValidate_WhenFellingAndRestockingOptionIsNone()
    {
        var model = CreateValidModel(FellingOperationType.None, TypeOfProposal.None);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].RestockingProposal");
        Assert.Contains(
            $"Compartment {model.CompartmentName} - At least one of the felling or restocking operations must be selected",
            result.Errors.Select(x => x.ErrorMessage));
    }

    [Fact]
    public void ShouldNotValidate_WhenAreaToBeFelledNotProvided()
    {
        var model = CreateValidModel(FellingOperationType.Thinning, TypeOfProposal.None);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            confirmedFellingDetail.AreaToBeFelled = null;

            var result = _sut.TestValidate(model);

            result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].AreaToBeFelled");
            Assert.Contains($"Compartment {model.CompartmentName} - Area to be felled must be provided",
                result.Errors.Select(x => x.ErrorMessage));
        }
    }

    [Fact]
    public void ShouldNotValidate_WhenConfirmedFellingSpeciesNotInputted()
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            confirmedFellingDetail.ConfirmedFellingSpecies = Array.Empty<ConfirmedFellingSpeciesModel>();

            var result = _sut.TestValidate(model);

            result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedFellingSpecies");
            Assert.Contains($"Compartment {model.CompartmentName} - At least one species for felling must be selected",
                result.Errors.Select(x => x.ErrorMessage));
        }
    }

    [Fact]
    public void ShouldNotValidate_WhenTreePreservationOrderIsSelectedAndReferenceIsNotProvided()
    {
        var model = CreateValidModel(FellingOperationType.Thinning, TypeOfProposal.None);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            confirmedFellingDetail.IsPartOfTreePreservationOrder = true;
            confirmedFellingDetail.TreePreservationOrderReference = null;

            var result = _sut.TestValidate(model);

            result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].confirmedFellingDetail.TreePreservationOrderReference");
            Assert.Contains($"Compartment {model.CompartmentName} - Tree Preservation Order Reference must be provided.",
                result.Errors.Select(x => x.ErrorMessage));
        }
    }

    [Fact]
    public void ShouldNotValidate_WhenConservationAreaIsSelectedAndReferenceIsNotProvided()
    {
        var model = CreateValidModel(FellingOperationType.Thinning, TypeOfProposal.None);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            confirmedFellingDetail.IsWithinConservationArea = true;
            confirmedFellingDetail.ConservationAreaReference = null;

            var result = _sut.TestValidate(model);

            result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].confirmedFellingDetail.ConservationAreaReference");
            Assert.Contains($"Compartment {model.CompartmentName} - Conservation Area Reference must be provided.",
                result.Errors.Select(x => x.ErrorMessage));
        }
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    public void ShouldNotValidate_WhenNumberOfTreesIsNegative(int numberOfTrees)
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            confirmedFellingDetail.NumberOfTrees = numberOfTrees;

            var result = _sut.TestValidate(model);

            result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].NumberOfTrees");
            Assert.Contains($"Compartment {model.CompartmentName} - Number of trees must be greater than zero when provided",
                result.Errors.Select(x => x.ErrorMessage));
        }
    }

    [Theory]
    [InlineData(FellingOperationType.ClearFelling, new[] { TypeOfProposal.CreateDesignedOpenGround, TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.ReplantTheFelledArea, TypeOfProposal.RestockByNaturalRegeneration, TypeOfProposal.RestockWithCoppiceRegrowth, TypeOfProposal.PlantAnAlternativeArea, TypeOfProposal.NaturalColonisation })]
    [InlineData(FellingOperationType.FellingOfCoppice, new[] { TypeOfProposal.CreateDesignedOpenGround, TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.RestockWithCoppiceRegrowth })]
    [InlineData(FellingOperationType.FellingIndividualTrees, new[] { TypeOfProposal.CreateDesignedOpenGround, TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.RestockByNaturalRegeneration, TypeOfProposal.RestockWithCoppiceRegrowth, TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees, TypeOfProposal.RestockWithIndividualTrees })]
    [InlineData(FellingOperationType.RegenerationFelling, new[] { TypeOfProposal.CreateDesignedOpenGround, TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.RestockWithCoppiceRegrowth, TypeOfProposal.ReplantTheFelledArea, TypeOfProposal.RestockByNaturalRegeneration })]
    [InlineData(FellingOperationType.Thinning, new[] { TypeOfProposal.None })]
    public void ShouldValidateRestockingOptionBasedOnFellingType(
        FellingOperationType fellingType,
        TypeOfProposal[] validRestockingOptions)
    {
        foreach (var restockingType in Enum.GetValues<TypeOfProposal>())
        {
            var model = CreateValidModel(fellingType, restockingType);

            var result = _sut.TestValidate(model);

            var shouldBeValid = validRestockingOptions.Contains(restockingType);

            Assert.Equal(shouldBeValid, result.IsValid);
        }
    }
}
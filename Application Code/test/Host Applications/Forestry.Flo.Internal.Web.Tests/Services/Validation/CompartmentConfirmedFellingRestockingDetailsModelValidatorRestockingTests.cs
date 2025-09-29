using FluentValidation.TestHelper;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Tests.Services.Validation;

public class CompartmentConfirmedFellingRestockingDetailsModelValidatorRestockingTests : CompartmentConfirmedFellingRestockingDetailsModelValidatorTestsBase
{
    [Fact]
    public void ShouldValidate_WithSuccess()
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotValidate_WhenRestockAreaNotProvided()
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.RestockArea = null;

                var result = _sut.TestValidate(model);

                result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].RestockArea");
                Assert.Contains($"Compartment {model.CompartmentName} - Area to be restocked must be provided",
                    result.Errors.Select(x => x.ErrorMessage));
            }
        }
    }

    [Fact]
    public void ShouldNotValidate_WhenRestockAreaIsNegative()
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.RestockArea = -1;

                var result = _sut.TestValidate(model);

                result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].RestockArea");
                Assert.Contains($"Compartment {model.CompartmentName} - Area to be restocked must be a positive value",
                    result.Errors.Select(x => x.ErrorMessage));
            }
        }
    }

    [Fact]
    public void ShouldNotValidate_WhenRestockAreaIsLargerThanConfirmedTotalHectares()
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.RestockArea = model.ConfirmedTotalHectares + 50;

                var result = _sut.TestValidate(model);

                result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].RestockArea");
                Assert.Contains($"Compartment {model.CompartmentName} - Area to be restocked must be less than or equal to the digitised area when confirmed restock area is entered",
                    result.Errors.Select(x => x.ErrorMessage));
            }
        }
    }

    [Fact]

    public void ShouldNotValidate_WhenRestockingDensityNotProvided()
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.RestockingDensity = null;

                var result = _sut.TestValidate(model);

                result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].RestockingDensity");
                Assert.Contains($"Compartment {model.CompartmentName} - Restocking density must be provided",
                    result.Errors.Select(x => x.ErrorMessage));
            }
        }
    }

    [Fact]
    public void ShouldNotValidate_WhenRestockingDensityIsNegative()
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.RestockingDensity = -5;

                var result = _sut.TestValidate(model);

                result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].RestockingDensity");
                Assert.Contains($"Compartment {model.CompartmentName} - Restocking density must be greater than zero",
                    result.Errors.Select(x => x.ErrorMessage));
            }
        }
    }

    [Fact]
    public void ShouldNotValidate_WhenRestockingSelectedButNoRestockingSpeciesInputted()
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.ConfirmedRestockingSpecies = Array.Empty<ConfirmedRestockingSpeciesModel>();

                var result = _sut.TestValidate(model);

                result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].ConfirmedRestockingSpecies");
                Assert.Contains($"Compartment {model.CompartmentName} - At least one species for restocking must be selected",
                    result.Errors.Select(x => x.ErrorMessage));
            }
        }
    }

    [Theory]
    [InlineData(TypeOfProposal.None)]
    [InlineData(TypeOfProposal.DoNotIntendToRestock)]
    public void ShouldNotValidate_WhenRestockingNotSelectedAndRestockingSpeciesInputted(TypeOfProposal restockProposal)
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.RestockingProposal = restockProposal;

                var result = _sut.TestValidate(model);

                result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].ConfirmedRestockingSpecies");
                Assert.Contains($"Compartment {model.CompartmentName} - No restocking species should be listed with a restocking proposal of {restockProposal.GetDisplayName()}",
                    result.Errors.Select(x => x.ErrorMessage));
            }
        }
    }

    [Fact]
    public void ShouldNotValidate_WhenOpenSpaceNotProvided_PercentageRestockingSpeciesDoesNotSumTo100()
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.PercentOpenSpace = null;

                foreach (var restockingSpecies in confirmedRestockingDetails.ConfirmedRestockingSpecies)
                {
                    restockingSpecies.Percentage = 1;
                }

                var result = _sut.TestValidate(model);

                Assert.Contains($"Compartment {model.CompartmentName} - Sum of restocking area percentages across species, plus percentage of open space, must total 100%",
                    result.Errors.Select(x => x.ErrorMessage));
            }
        }
    }

    [Fact]
    public void ShouldNotValidate_WhenOpenSpaceNotProvided_PercentageRestockingSpeciesDoesSumTo100()
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.ConfirmedRestockingSpecies[0].Percentage += confirmedRestockingDetails.PercentOpenSpace;
                confirmedRestockingDetails.PercentOpenSpace = null;

                var result = _sut.TestValidate(model);

                result.ShouldNotHaveAnyValidationErrors();
            }
        }
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(102)]

    public void ShouldNotValidate_WhenOpenSpacePercentageIsOutsideOfRange(int percentage)
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.PercentOpenSpace = percentage;

                var result = _sut.TestValidate(model);

                result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].PercentOpenSpace");
                Assert.Contains($"Compartment {model.CompartmentName} - Open space must be between zero and 100%",
                    result.Errors.Select(x => x.ErrorMessage));
            }
        }
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(102)]

    public void ShouldNotValidate_WhenNaturalRegenerationPercentageIsOutsideOfRange(int percentage)
    {
        var model = CreateValidModel(FellingOperationType.ClearFelling, TypeOfProposal.PlantAnAlternativeArea);

        foreach (var confirmedFellingDetail in model.ConfirmedFellingDetails)
        {
            foreach (var confirmedRestockingDetails in confirmedFellingDetail.ConfirmedRestockingDetails)
            {
                confirmedRestockingDetails.PercentNaturalRegeneration = percentage;

                var result = _sut.TestValidate(model);

                result.ShouldHaveValidationErrorFor("ConfirmedFellingDetails[0].ConfirmedRestockingDetails[0].PercentNaturalRegeneration");
                Assert.Contains($"Compartment {model.CompartmentName} - Natural regeneration must be between zero and 100%",
                    result.Errors.Select(x => x.ErrorMessage));
            }
        }
    }
}
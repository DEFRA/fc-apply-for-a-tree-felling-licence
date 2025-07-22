using AutoFixture;
using AutoFixture.Xunit2;
using Forestry.Flo.External.Web.Services.Validation;
using FluentValidation.TestHelper;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Tests.Services.Validation;

public class FellingAndRestockingValidatorTests
{
    // left this all commented out for now - validator class will become obsolete soon, but test logic here will probably be reused.

    //private readonly FellingAndRestockingDetailValidator _sut;

    //private static readonly Fixture FixtureInstance = new();

    //public FellingAndRestockingValidatorTests()
    //{
    //    _sut = new FellingAndRestockingDetailValidator();
    //}

    //[Theory, AutoData]
    //public void ShouldValidateValidFellingAndRestockingModel_WithSuccess(FellingAndRestockingDetail fellingAndRestockingDetail)
    //{
    //    //Arrange
    //    fellingAndRestockingDetail.FellingDetail.OperationType = FellingOperationType.Thinning;
    //    fellingAndRestockingDetail.FellingDetail.AreaToBeFelled = 100;
    //    fellingAndRestockingDetail.RestockingDetail.RestockingProposal = TypeOfProposal.None;

    //    //Act
    //    var result = _sut.TestValidate(fellingAndRestockingDetail);

    //    //Assert
    //    result.ShouldNotHaveAnyValidationErrors();
    //}

    //[Theory, AutoData]
    //public void ShouldReturnError_WhenValidateFellingAndRestockingModel_GivenAreaToBeFelledAsO(FellingAndRestockingDetail fellingAndRestockingDetail)
    //{
    //    //Arrange
    //    fellingAndRestockingDetail.FellingDetail.OperationType = FellingOperationType.Thinning;
    //    fellingAndRestockingDetail.FellingDetail.AreaToBeFelled = 100;
    //    fellingAndRestockingDetail.RestockingDetail.RestockingProposal = TypeOfProposal.None;

    //    //Act
    //    var result = _sut.TestValidate(fellingAndRestockingDetail);

    //    //Assert
    //    result.ShouldNotHaveAnyValidationErrors();
    //}


    //[Theory, AutoData]
    //public void ShouldReturnError_WhenValidateFellingAndRestockingModel_GivenBothOperationsNotSelected(FellingAndRestockingDetail fellingAndRestockingDetail)
    //{
    //    //Arrange
    //    fellingAndRestockingDetail.FellingDetail.OperationType = FellingOperationType.None;
    //    fellingAndRestockingDetail.RestockingDetail.RestockingProposal = TypeOfProposal.None;
    //    fellingAndRestockingDetail.Tab = "restocking-details";

    //    //Act
    //    var result = _sut.TestValidate(fellingAndRestockingDetail);

    //    //Assert
    //    result.ShouldHaveValidationErrorFor(p => p.RestockingDetail.RestockingProposal);
    //}

    //[Theory]
    //[InlineData(FellingOperationType.ClearFelling, new[] { TypeOfProposal.PlantAnAlternativeArea, TypeOfProposal.ReplantTheFelledArea, TypeOfProposal.RestockByNaturalRegeneration, TypeOfProposal.RestockWithCoppiceRegrowth })]
    //[InlineData(FellingOperationType.Deforestation, new[] { TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.PlantAnAlternativeArea, TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees })]
    //[InlineData(FellingOperationType.FellingOfCoppice, new[] { TypeOfProposal.ReplantTheFelledArea, TypeOfProposal.RestockByNaturalRegeneration, TypeOfProposal.RestockWithCoppiceRegrowth })]
    //[InlineData(FellingOperationType.FellingIndividualTrees, new[] { TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees, TypeOfProposal.RestockByNaturalRegeneration, TypeOfProposal.RestockWithCoppiceRegrowth, TypeOfProposal.RestockWithIndividualTrees })]
    //[InlineData(FellingOperationType.FellingToCreateOpenSpace, new[] { TypeOfProposal.CreateDesignedOpenGround })]
    //[InlineData(FellingOperationType.RegenerationFelling, new[] { TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.ReplantTheFelledArea, TypeOfProposal.RestockByNaturalRegeneration, TypeOfProposal.RestockWithCoppiceRegrowth })]
    //[InlineData(FellingOperationType.Thinning, new[] { TypeOfProposal.None })]
    //public void ShouldValidateRestockingOptionBasedOnFellingType(
    //    FellingOperationType fellingType,
    //    TypeOfProposal[] validRestockingOptions)
    //{
    //    var fellingAndRestocking = FixtureInstance.Create<FellingAndRestockingDetail>();
    //    fellingAndRestocking.FellingDetail.OperationType = fellingType;
    //    fellingAndRestocking.Tab = "restocking-details";

    //    foreach (var restockingType in Enum.GetValues<TypeOfProposal>())
    //    {
    //        fellingAndRestocking.RestockingDetail.RestockingProposal = restockingType;

    //        var result = _sut.TestValidate(fellingAndRestocking);

    //        var shouldBeValid = validRestockingOptions.Contains(restockingType);

    //        Assert.Equal(shouldBeValid, result.IsValid);
    //    }
    //}
}
using AutoFixture;
using AutoFixture.Xunit2;
using FluentValidation.TestHelper;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Services.Validation;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Services.Validation;

public class ProposedFellingDetailModelValidatorTests
{
    private readonly ProposedFellingDetailModelValidator _sut;
    
    public ProposedFellingDetailModelValidatorTests()
    {
        _sut = new ProposedFellingDetailModelValidator();
    }
    
    [Theory, AutoData]
    public void ShouldValidateValidProposedFellingDetailModel_WithSuccess(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.OperationType = FellingOperationType.Thinning;
        fellingDetailModel.AreaToBeFelled = fellingDetailModel.CompartmentTotalHectares.GetValueOrDefault();
        fellingDetailModel.IsPartOfTreePreservationOrder = true;
        fellingDetailModel.TreePreservationOrderReference = "93/00768/C";
        fellingDetailModel.IsWithinConservationArea = true;
        fellingDetailModel.ConservationAreaReference = "ConsArea-9876/9";
        foreach (var speciesModel in fellingDetailModel.Species)
        {
            speciesModel.Value.Percentage = 33;
        }
        fellingDetailModel.Species.First().Value.Percentage = 34;

        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenValidateProposedFellingDetailModel_GivenAreaToBeFelledAsOAndCompartmentHasArea(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.OperationType = FellingOperationType.Thinning;
        fellingDetailModel.AreaToBeFelled = 0;
        
        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.AreaToBeFelled);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenValidateProposedFellingDetailModel_GivenAreaToBeFelledAsOAndCompartmentHasNoArea(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.OperationType = FellingOperationType.Thinning;
        fellingDetailModel.AreaToBeFelled = 0;
        fellingDetailModel.CompartmentTotalHectares = null;

        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.AreaToBeFelled);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenValidateProposedFellingDetailModel_GivenAreaToBeFelledAsNegativeValueAndCompartmentHasArea(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.OperationType = FellingOperationType.Thinning;
        fellingDetailModel.AreaToBeFelled = -1;
        
        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.AreaToBeFelled);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenValidateProposedFellingDetailModel_GivenAreaToBeFelledAsNegativeValueAndCompartmentHasNoArea(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.OperationType = FellingOperationType.Thinning;
        fellingDetailModel.AreaToBeFelled = -1;
        fellingDetailModel.CompartmentTotalHectares = null;

        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.AreaToBeFelled);
    }


    [Theory, AutoData]
    public void ShouldReturnError_WhenValidateProposedFellingDetailModel_GivenAreaToBeFelledGreaterThanCompartmentSize(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.OperationType = FellingOperationType.Thinning;
        fellingDetailModel.AreaToBeFelled = fellingDetailModel.CompartmentTotalHectares.GetValueOrDefault() + 1;
        
        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.AreaToBeFelled);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenValidateProposedFellingDetailModel_NoSpeciesProvided(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.OperationType = FellingOperationType.Thinning;
        fellingDetailModel.AreaToBeFelled = fellingDetailModel.CompartmentTotalHectares.GetValueOrDefault();
        fellingDetailModel.Species = new Dictionary<string, SpeciesModel>();

        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.Species);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenTreePreservationOrderIsSelectedAndReferenceIsNotProvided(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.IsPartOfTreePreservationOrder = true;
        fellingDetailModel.TreePreservationOrderReference = null;

        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(x => x.TreePreservationOrderReference);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenTreePreservationOrderIsSelectedAndReferenceIsProvidedButIsTooLong(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.IsPartOfTreePreservationOrder = true;
        fellingDetailModel.TreePreservationOrderReference = "Testing This Is Not Too Long For The Validator To Validate";

        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(x => x.TreePreservationOrderReference);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenConservationAreaIsSelectedAndReferenceIsNotProvided(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.IsWithinConservationArea = true;
        fellingDetailModel.ConservationAreaReference = null;

        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(x => x.ConservationAreaReference);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenConservationAreaIsSelectedAndReferenceIsProvidedButIsTooLong(ProposedFellingDetailModel fellingDetailModel)
    {
        //Arrange
        fellingDetailModel.IsWithinConservationArea = true;
        fellingDetailModel.ConservationAreaReference = "Testing This Is Not Too Long For The Validator To Validate";

        //Act
        var result = _sut.TestValidate(fellingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(x => x.ConservationAreaReference);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenTreeMarkingIsSelectedAndExplanationIsNotProvided(ProposedFellingDetailModel fellingDetailModel)
    {
        fellingDetailModel.IsTreeMarkingUsed = true;
        fellingDetailModel.OperationType = FellingOperationType.Thinning;
        fellingDetailModel.TreeMarking = null;

        var result = _sut.TestValidate(fellingDetailModel);

        result.ShouldHaveValidationErrorFor(x => x.TreeMarking);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenNumberOfTreesIsZero_ForFellingIndividualTrees(ProposedFellingDetailModel fellingDetailModel)
    {
        fellingDetailModel.OperationType = FellingOperationType.FellingIndividualTrees;
        fellingDetailModel.NumberOfTrees = 0;

        var result = _sut.TestValidate(fellingDetailModel);

        result.ShouldHaveValidationErrorFor(x => x.NumberOfTrees);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenNumberOfTreesIsNegative_ForFellingIndividualTrees(ProposedFellingDetailModel fellingDetailModel)
    {
        fellingDetailModel.OperationType = FellingOperationType.FellingIndividualTrees;
        fellingDetailModel.NumberOfTrees = -1;

        var result = _sut.TestValidate(fellingDetailModel);

        result.ShouldHaveValidationErrorFor(x => x.NumberOfTrees);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenNumberOfTreesIsNotProvided_ForFellingIndividualTrees(ProposedFellingDetailModel fellingDetailModel)
    {
        fellingDetailModel.OperationType = FellingOperationType.FellingIndividualTrees;
        fellingDetailModel.NumberOfTrees = null;

        var result = _sut.TestValidate(fellingDetailModel);

        result.ShouldHaveValidationErrorFor(x => x.NumberOfTrees);
    }

    [Theory, CombinatorialData]
    public void ShouldNotReturnError_WhenNumberOfTreesIsNotProvided_ForOtherFellingTypes(FellingOperationType fellingOperationType)
    {
        if (fellingOperationType == FellingOperationType.FellingIndividualTrees)
        {
            return;
        }

        var fixture = new Fixture();

        var fellingDetailModel = fixture.Build<ProposedFellingDetailModel>()
            .With(x => x.OperationType, fellingOperationType)
            .Without(x => x.NumberOfTrees)
            .Create();

        var result = _sut.TestValidate(fellingDetailModel);

        result.ShouldNotHaveValidationErrorFor(x => x.NumberOfTrees);
    }

    [Theory, CombinatorialData]
    public void ShouldReturnError_WhenNumberOfTreesIs0_ForOtherFellingTypes(FellingOperationType fellingOperationType)
    {
        if (fellingOperationType == FellingOperationType.FellingIndividualTrees)
        {
            return;
        }

        var fixture = new Fixture();

        var fellingDetailModel = fixture.Build<ProposedFellingDetailModel>()
            .With(x => x.OperationType, fellingOperationType)
            .With(x => x.NumberOfTrees, 0)
            .Create();

        var result = _sut.TestValidate(fellingDetailModel);

        result.ShouldHaveValidationErrorFor(x => x.NumberOfTrees);
    }

    [Theory, CombinatorialData]
    public void ShouldReturnError_WhenNumberOfTreesIsNegative_ForOtherFellingTypes(FellingOperationType fellingOperationType)
    {
        if (fellingOperationType == FellingOperationType.FellingIndividualTrees)
        {
            return;
        }

        var fixture = new Fixture();

        var fellingDetailModel = fixture.Build<ProposedFellingDetailModel>()
            .With(x => x.OperationType, fellingOperationType)
            .With(x => x.NumberOfTrees, -1)
            .Create();

        var result = _sut.TestValidate(fellingDetailModel);

        result.ShouldHaveValidationErrorFor(x => x.NumberOfTrees);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenEstimatedTotalFellingVolume_NotProvided(ProposedFellingDetailModel fellingDetailModel)
    {
        fellingDetailModel.OperationType = FellingOperationType.FellingOfCoppice;
        fellingDetailModel.EstimatedTotalFellingVolume = 0;

        var result = _sut.TestValidate(fellingDetailModel);

        result.ShouldHaveValidationErrorFor(x => x.EstimatedTotalFellingVolume);
    }

}
using AutoFixture;
using AutoFixture.Xunit2;
using FluentValidation.TestHelper;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Services.Validation;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Tests.Services.Validation;

public class ProposedRestockingDetailModelValidatorTests
{
    private readonly ProposedRestockingDetailModelValidator _sut = new();

    private readonly Fixture _fixture = new();

    [Theory, AutoData]
    public void ShouldValidateValidProposedRestockingDetailModel_WithSuccess(ProposedRestockingDetailModel restockingDetailModel)
    {
        //Arrange
        restockingDetailModel.RestockingProposal = TypeOfProposal.CreateDesignedOpenGround;
        restockingDetailModel.Area = restockingDetailModel.CompartmentTotalHectares.GetValueOrDefault();
        restockingDetailModel.PercentageOfRestockArea = 30;
        foreach (var speciesModel in restockingDetailModel.Species)
        {
            speciesModel.Value.Percentage = 33;
        }
        restockingDetailModel.Species.First().Value.Percentage = 34;

        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenValidateProposedRestockingDetailModel_GivenAreaAsO(ProposedRestockingDetailModel restockingDetailModel)
    {
        //Arrange
        restockingDetailModel.RestockingProposal = TypeOfProposal.CreateDesignedOpenGround;
        restockingDetailModel.PercentageOfRestockArea = 10;
        restockingDetailModel.Area = 0;
        
        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.Area);
    }
    
    [Theory, AutoData]
    public void ShouldReturnError_WhenValidateProposedRestockingDetailModel_GivenAreaGreaterThanCompartmentSize(ProposedRestockingDetailModel restockingDetailModel)
    {
        //Arrange
        restockingDetailModel.RestockingProposal = TypeOfProposal.CreateDesignedOpenGround;
        restockingDetailModel.PercentageOfRestockArea = 10;
        restockingDetailModel.Area = restockingDetailModel.CompartmentTotalHectares.GetValueOrDefault() + 1;
        
        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.Area);
    }

    [Theory, AutoData]
    public void ShouldNotReturnError_WhenValidateProposedRestockingDetailModel_GivenAreaCorrectTo2DecimalPlaces(ProposedRestockingDetailModel restockingDetailModel)
    {
        //Arrange
        restockingDetailModel.RestockingProposal = TypeOfProposal.CreateDesignedOpenGround;
        restockingDetailModel.CompartmentTotalHectares = 10.455;
        restockingDetailModel.PercentageOfRestockArea = 10;
        foreach (var speciesModel in restockingDetailModel.Species)
        {
            speciesModel.Value.Percentage = 33;
        }
        restockingDetailModel.Species.First().Value.Percentage = 34;
        restockingDetailModel.Area = 10.45;

        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenValidateProposedRestockingDetailModel_GivenAreaCorrectMoreThan2DecimalPlaces(ProposedRestockingDetailModel restockingDetailModel)
    {
        //Arrange
        restockingDetailModel.RestockingProposal = TypeOfProposal.CreateDesignedOpenGround;
        restockingDetailModel.CompartmentTotalHectares = 10.455;
        restockingDetailModel.Species = new Dictionary<string, SpeciesModel>();
        restockingDetailModel.PercentageOfRestockArea = 10;
        restockingDetailModel.Area = 10.46;

        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.Area);
    }

    [Theory, CombinatorialData]
    public void ShouldReturnError_WhenValidateProposedRestockingDetailModel_GivenInvalidSpeciesPercentage(TypeOfProposal restockingType)
    {
        if (restockingType is TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock or TypeOfProposal.CreateDesignedOpenGround)
        {
            return;
        }
        
        //Arrange
        var speciesModel = _fixture
            .Build<SpeciesModel>()
            .With(x => x.Percentage, 200)
            .Create();
        var species = new Dictionary<string, SpeciesModel> { { Guid.NewGuid().ToString(), speciesModel } };
        var restockingDetailModel = _fixture
            .Build<ProposedRestockingDetailModel>()
            .With(x => x.RestockingProposal, restockingType)
            .With(x => x.Species, species)
            .Create();

        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor("Species[0].Value");

        //check for the custom property name for the error message to link to the correct control
        var expectedPropertyName = $"Species_{speciesModel.Species}__Percentage";
        Assert.Contains(result.Errors, x => x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == expectedPropertyName);
    }
    
    [Theory, CombinatorialData]
    public void ShouldReturnError_WhenValidateProposedRestockingDetailModel_GivenZeroSpeciesPercentage(TypeOfProposal restockingType)
    {
        if (restockingType is TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock or TypeOfProposal.CreateDesignedOpenGround)
        {
            return;
        }

        //Arrange
        var speciesModel = _fixture
            .Build<SpeciesModel>()
            .With(x => x.Percentage, 0)
            .Create();
        var species = new Dictionary<string, SpeciesModel> { { Guid.NewGuid().ToString(), speciesModel } };
        var restockingDetailModel = _fixture
            .Build<ProposedRestockingDetailModel>()
            .With(x => x.RestockingProposal, restockingType)
            .With(x => x.Species, species)
            .Create();
        
        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor("Species[0].Value");

        //check for the custom property name for the error message to link to the correct control
        var expectedPropertyName = $"Species_{speciesModel.Species}__Percentage";
        Assert.Contains(result.Errors, x => x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == expectedPropertyName);
    }

    [Theory, CombinatorialData]
    public void ShouldNotReturnError_WhenValidateProposedRestockingDetailModel_GivenSpeciesPercentage2DecimalPlaces(TypeOfProposal restockingType)
    {
        if (restockingType is TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock or TypeOfProposal.CreateDesignedOpenGround)
        {
            return;
        }

        //Arrange
        var speciesModel1 = _fixture
            .Build<SpeciesModel>()
            .With(x => x.Percentage, 10.15)
            .Create();
        var speciesModel2 = _fixture
            .Build<SpeciesModel>()
            .With(x => x.Percentage, 89.85)
            .Create();
        var species = new Dictionary<string, SpeciesModel>
        {
            { Guid.NewGuid().ToString(), speciesModel1 },
            { Guid.NewGuid().ToString(), speciesModel2 }
        };
        var restockingDetailModel = _fixture
            .Build<ProposedRestockingDetailModel>()
            .With(x => x.RestockingProposal, restockingType)
            .With(x => x.Species, species)
            .Create();

        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldNotHaveValidationErrorFor("Species[0].Value");
        result.ShouldNotHaveValidationErrorFor("Species[1].Value");
    }

    [Theory, CombinatorialData]
    public void ShouldReturnError_WhenValidateProposedRestockingDetailModel_GivenTotalSpeciesPercentageDoesNotAddUpTo100(TypeOfProposal restockingType)
    {
        if (restockingType is TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock or TypeOfProposal.CreateDesignedOpenGround)
        {
            return;
        }

        //Arrange
        var speciesModel1 = _fixture
            .Build<SpeciesModel>()
            .With(x => x.Percentage, 10.15)
            .Create();
        var speciesModel2 = _fixture
            .Build<SpeciesModel>()
            .With(x => x.Percentage, 90.85)
            .Create();
        var species = new Dictionary<string, SpeciesModel>
        {
            { Guid.NewGuid().ToString(), speciesModel1 },
            { Guid.NewGuid().ToString(), speciesModel2 }
        };
        var restockingDetailModel = _fixture
            .Build<ProposedRestockingDetailModel>()
            .With(x => x.RestockingProposal, restockingType)
            .With(x => x.Species, species)
            .Create();

        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.Species);
    }

    [Theory, CombinatorialData]
    public void ShouldReturnError_WhenValidateProposedRestockingDetailModel_NoSpeciesProvided(TypeOfProposal restockingType)
    {
        if (restockingType is TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock or TypeOfProposal.CreateDesignedOpenGround)
        {
            return;
        }

        //Arrange
        var restockingDetailModel = _fixture
            .Build<ProposedRestockingDetailModel>()
            .With(x => x.RestockingProposal, restockingType)
            .With(x => x.Species, new Dictionary<string, SpeciesModel>())
            .Create();

        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.Species);
    }




    [Theory, AutoData]
    public void ShouldNotReturnError_WhenValidateProposedRestockingDetailModel_GivenZeroRestockingDensityForIndividualTrees(ProposedRestockingDetailModel restockingDetailModel)
    {
        //Arrange
        restockingDetailModel.RestockingProposal = TypeOfProposal.RestockWithIndividualTrees;
        restockingDetailModel.RestockingDensity = 0;
        restockingDetailModel.NumberOfTrees = 1;

        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldNotHaveValidationErrorFor(f => f.NumberOfTrees);
        result.ShouldNotHaveValidationErrorFor(f => f.RestockingDensity);
    }


    [Theory, AutoData]
    public void ShouldReturnError_WhenValidateProposedRestockingDetailModel_GivenZeroRestockingDensityForIndividualTrees(ProposedRestockingDetailModel restockingDetailModel)
    {
        //Arrange
        restockingDetailModel.RestockingProposal = TypeOfProposal.RestockWithIndividualTrees;
        restockingDetailModel.RestockingDensity = 0;
        restockingDetailModel.NumberOfTrees = 0;

        //Act
        var result = _sut.TestValidate(restockingDetailModel);

        //Assert
        result.ShouldHaveValidationErrorFor(f => f.NumberOfTrees);
        result.ShouldNotHaveValidationErrorFor(f => f.RestockingDensity);
    }

    [Theory, CombinatorialData]
    public void ShouldValidate_WhenValidateProposedRestockingDetailModel_GivenNumberOfTreesIsNull(TypeOfProposal proposalType)
    {
        var model = _fixture.Build<ProposedRestockingDetailModel>()
            .With(x => x.RestockingProposal, proposalType)
            .Without(x => x.NumberOfTrees)
            .Create();

        //Act
        var result = _sut.TestValidate(model);

        //Assert
        if (proposalType is TypeOfProposal.RestockWithIndividualTrees
            or TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees)
        {
            result.ShouldHaveValidationErrorFor(f => f.NumberOfTrees)
                .WithErrorMessage("Enter an estimated number of trees greater than zero");

            return;
        }

        result.ShouldNotHaveValidationErrorFor(f => f.NumberOfTrees);
    }

    [Theory, CombinatorialData]
    public void ShouldReturnError_WhenValidateProposedRestockingDetailModel_GivenNumberOfTreesIsZeroOrNegative(
        TypeOfProposal proposalType, 
        [CombinatorialValues(0, -1)] int numberOfTrees)
    {
        var model = _fixture.Build<ProposedRestockingDetailModel>()
            .With(x => x.RestockingProposal, proposalType)
            .With(x => x.NumberOfTrees, numberOfTrees)
            .Create();

        //Act
        var result = _sut.TestValidate(model);

        //Assert
        if (proposalType is TypeOfProposal.RestockWithIndividualTrees
            or TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees)
        {
            result.ShouldHaveValidationErrorFor(f => f.NumberOfTrees)
                .WithErrorMessage("Enter an estimated number of trees greater than zero");

            return;
        }

        result.ShouldNotHaveValidationErrorFor(f => f.NumberOfTrees);
    }
}
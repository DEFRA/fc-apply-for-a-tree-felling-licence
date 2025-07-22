using AutoFixture.Xunit2;
using Forestry.Flo.External.Web.Models.PropertyProfile;
using Forestry.Flo.External.Web.Services.Validation;
using FluentValidation.TestHelper;

namespace Forestry.Flo.External.Web.Tests.Services.Validation;

public class PropertyProfileValidatorTests
{
    private readonly PropertyProfileValidator _sut;

    public PropertyProfileValidatorTests()
    {
        _sut = new PropertyProfileValidator();
    }

    [Theory, AutoData]
    public void ShouldValidateValidPropertyProfile_WithSuccess(PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.HasWoodlandManagementPlan = true;
        propertyProfileModel.Name = "Name";
        
        //Act
        var result = _sut.TestValidate(propertyProfileModel);

        //Assert
        result. ShouldNotHaveAnyValidationErrors();
    }
    
    [Theory, AutoData]
    public void ShouldReturnError_WhenValidPropertyProfile_GivenNameIsEmpty(PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.Name = string.Empty;
        
        //Act
        var result = _sut.TestValidate(propertyProfileModel);

        //Assert
        result.ShouldHaveValidationErrorFor(p => p.Name);
    }
    
    [Theory, AutoData]
    public void ShouldReturnError_GiveHasManagementPlan_AndEmptyReference(PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.HasWoodlandManagementPlan = true;
        propertyProfileModel.WoodlandManagementPlanReference = string.Empty;
        
        //Act
        var result = _sut.TestValidate(propertyProfileModel);

        //Assert
        result.ShouldHaveValidationErrorFor(p => p.WoodlandManagementPlanReference);
    }

    [Theory, AutoData]
    public void ShouldReturnError_GiveHasCertificationScheme_AndEmptyReference(PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.IsWoodlandCertificationScheme = true;
        propertyProfileModel.WoodlandCertificationSchemeReference = string.Empty;

        //Act
        var result = _sut.TestValidate(propertyProfileModel);

        //Assert
        result.ShouldHaveValidationErrorFor(p => p.WoodlandCertificationSchemeReference);
    }

    [Theory, AutoData]
    public void ShouldNotReturnError_GiveDoesNotHaveManagementPlan_AndEmptyReference(PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        propertyProfileModel.HasWoodlandManagementPlan = false;
        propertyProfileModel.WoodlandManagementPlanReference = string.Empty;
        propertyProfileModel.Name = "Name";

        //Act
        var result = _sut.TestValidate(propertyProfileModel);

        //Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
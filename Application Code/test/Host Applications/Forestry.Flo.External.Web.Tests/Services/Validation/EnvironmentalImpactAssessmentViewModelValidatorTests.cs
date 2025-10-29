using FluentValidation.TestHelper;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;
using Forestry.Flo.External.Web.Services.Validation;

namespace Forestry.Flo.External.Web.Tests.Services.Validation;

public class EnvironmentalImpactAssessmentViewModelValidatorTests
{
    private readonly EnvironmentalImpactAssessmentViewModelValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_HasApplicationBeenCompleted_Is_Null()
    {
        var model = new EnvironmentalImpactAssessmentViewModel
        {
            HasApplicationBeenCompleted = null
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.HasApplicationBeenCompleted)
            .WithErrorMessage("Enter whether an EIA application has been completed");
    }

    [Fact]
    public void Should_Not_Have_Error_When_HasApplicationBeenCompleted_Is_True()
    {
        var model = new EnvironmentalImpactAssessmentViewModel
        {
            HasApplicationBeenCompleted = true
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.HasApplicationBeenCompleted);
    }

    [Fact]
    public void Should_Not_Have_Error_When_HasApplicationBeenCompleted_Is_False()
    {
        var model = new EnvironmentalImpactAssessmentViewModel
        {
            HasApplicationBeenCompleted = false
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.HasApplicationBeenCompleted);
    }

    [Fact]
    public void Should_Have_Error_When_HasApplicationBeenSent_Is_False_And_Completed_Is_True()
    {
        var model = new EnvironmentalImpactAssessmentViewModel
        {
            HasApplicationBeenCompleted = true,
            HasApplicationBeenSent = false
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.HasApplicationBeenSent)
            .WithErrorMessage("If you have completed an EIA application, upload it here");
    }

    [Fact]
    public void Should_Not_Have_Error_When_HasApplicationBeenSent_Is_True_And_Completed_Is_True()
    {
        var model = new EnvironmentalImpactAssessmentViewModel
        {
            HasApplicationBeenCompleted = true,
            HasApplicationBeenSent = true
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.HasApplicationBeenSent);
    }

    [Fact]
    public void Should_Not_Have_Error_When_HasApplicationBeenSent_Is_False_And_Completed_Is_False()
    {
        var model = new EnvironmentalImpactAssessmentViewModel
        {
            HasApplicationBeenCompleted = false,
            HasApplicationBeenSent = false
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.HasApplicationBeenSent);
    }
}
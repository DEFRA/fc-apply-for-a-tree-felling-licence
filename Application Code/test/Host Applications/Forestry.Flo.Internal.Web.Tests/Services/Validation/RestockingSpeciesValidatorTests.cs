﻿using FluentAssertions;
using FluentValidation.TestHelper;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Validation;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Tests.Services.Validation;

public class RestockingSpeciesValidatorTests
{
    private readonly RestockingSpeciesValidator _sut;

    private const string CompartmentName = "Comp1";

    public RestockingSpeciesValidatorTests()
    {
        _sut = new RestockingSpeciesValidator(CompartmentName);
    }

    [Fact]
    public void ShouldValidate_WithSuccess()
    {
        var model = ConstructModel();

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotValidate_WhenPercentageIsNotProvided()
    {
        var model = ConstructModel();

        model.Percentage = null;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor("Percentage");
        result.Errors.Any(x =>
                x.ErrorMessage == $"Compartment {CompartmentName} - Percentage must be provided for all restocking species")
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(102)]

    public void ShouldNotValidate_WhenPercentageIsOutsideOfRange(int percentage)
    {
        var model = ConstructModel();

        model.Percentage = percentage;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor("Percentage");
        result.Errors.Any(x =>
                x.ErrorMessage == $"Compartment {CompartmentName} - Percentage must be greater than zero and less than or equal to 100% for restocking species {model.Species}")
            .Should()
            .BeTrue();
    }

    private static ConfirmedRestockingSpeciesModel ConstructModel()
    {
        return new ConfirmedRestockingSpeciesModel
        {
            Deleted = false,
            Id = Guid.NewGuid(),
            Percentage = 50,
            Species = "tree1"
        };
    }
}
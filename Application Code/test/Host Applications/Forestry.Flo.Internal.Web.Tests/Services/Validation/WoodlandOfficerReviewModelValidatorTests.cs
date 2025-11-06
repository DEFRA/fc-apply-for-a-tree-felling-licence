using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Validation;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Tests.Services.Validation;

public class WoodlandOfficerReviewModelValidatorTests
{
    private readonly WoodlandOfficerReviewModelValidator _sut = new();

    [Fact]
    public void Should_Pass_Validation_When_All_Required_Fields_Are_Valid()
    {
        var model = new WoodlandOfficerReviewModel
        {
            RecommendedLicenceDuration = RecommendedLicenceDuration.TwoYear,
            RecommendationForDecisionPublicRegister = true,
            RecommendationForDecisionPublicRegisterReason = "Valid reason"
        };

        var result = _sut.Validate(model);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_Fail_Validation_When_RecommendedLicenceDuration_Is_Null()
    {
        var model = new WoodlandOfficerReviewModel
        {
            RecommendedLicenceDuration = null,
            RecommendationForDecisionPublicRegister = true,
            RecommendationForDecisionPublicRegisterReason = "Valid reason"
        };

        var result = _sut.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.RecommendedLicenceDuration));
    }

    [Fact]
    public void Should_Fail_Validation_When_RecommendedLicenceDuration_Is_None()
    {
        var model = new WoodlandOfficerReviewModel
        {
            RecommendedLicenceDuration = RecommendedLicenceDuration.None,
            RecommendationForDecisionPublicRegister = true,
            RecommendationForDecisionPublicRegisterReason = "Valid reason"
        };

        var result = _sut.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.RecommendedLicenceDuration));
    }

    [Fact]
    public void Should_Fail_Validation_When_RecommendationForDecisionPublicRegister_Is_Null()
    {
        var model = new WoodlandOfficerReviewModel
        {
            RecommendedLicenceDuration = RecommendedLicenceDuration.TwoYear,
            RecommendationForDecisionPublicRegister = null,
            RecommendationForDecisionPublicRegisterReason = "Valid reason"
        };

        var result = _sut.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.RecommendationForDecisionPublicRegister));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Fail_Validation_When_RecommendationForDecisionPublicRegisterReason_Is_Empty_And_RecommendationIsFalse(string reason)
    {
        var model = new WoodlandOfficerReviewModel
        {
            RecommendedLicenceDuration = RecommendedLicenceDuration.TwoYear,
            RecommendationForDecisionPublicRegister = false,
            RecommendationForDecisionPublicRegisterReason = reason
        };

        var result = _sut.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.RecommendationForDecisionPublicRegisterReason));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Pass_Validation_When_RecommendationForDecisionPublicRegisterReason_Is_Empty_And_RecommendationIsTrue(string reason)
    {
        var model = new WoodlandOfficerReviewModel
        {
            RecommendedLicenceDuration = RecommendedLicenceDuration.TwoYear,
            RecommendationForDecisionPublicRegister = true,
            RecommendationForDecisionPublicRegisterReason = reason
        };

        var result = _sut.Validate(model);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(model.RecommendationForDecisionPublicRegisterReason));
    }
}
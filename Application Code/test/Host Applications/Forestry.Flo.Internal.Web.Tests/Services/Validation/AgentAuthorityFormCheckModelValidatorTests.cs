using FluentValidation.TestHelper;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Services.Validation;

namespace Forestry.Flo.Internal.Web.Tests.Services.Validation;

public class AgentAuthorityFormCheckModelValidatorTests
{
    private readonly AgentAuthorityFormCheckModelValidator _sut = new();

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Should_Have_Error_When_CheckFailedReason_Is_Empty_And_CheckPassed_Is_False(string? reason)
    {
        // Arrange
        var model = new AgentAuthorityFormCheckModel
        {
            CheckPassed = false,
            CheckFailedReason = reason
        };

        // Act
        var result = _sut.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CheckFailedReason)
              .WithErrorMessage("Reason for failure must be provided");
    }

    [Fact]
    public void Should_Not_Have_Error_When_CheckFailedReason_Is_Provided_And_CheckPassed_Is_False()
    {
        // Arrange
        var model = new AgentAuthorityFormCheckModel
        {
            CheckPassed = false,
            CheckFailedReason = "Some reason"
        };

        // Act
        var result = _sut.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CheckFailedReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Should_Not_Have_Error_When_CheckPassed_Is_True(string? reason)
    {
        // Arrange
        var model = new AgentAuthorityFormCheckModel
        {
            CheckPassed = true,
            CheckFailedReason = reason
        };

        // Act
        var result = _sut.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CheckFailedReason);
    }
}

using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class ConstraintsCheckControllerTests
{
    private readonly ConstraintsCheckController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public ConstraintsCheckControllerTests()
    {
        _controller = new ConstraintsCheckController();
    }

    [Fact]
    public async Task Run_ReturnsValue_WhenSuccess()
    {
        // Arrange
        var useCaseMock = new Mock<IRunFcInternalUserConstraintCheckUseCase>();
        var redirectResult = new RedirectResult("test");
        _controller.PrepareControllerForTest(Guid.NewGuid());

        useCaseMock.Setup(x => x.ExecuteConstraintsCheckAsync(
            It.IsAny<InternalUser>(), _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(redirectResult);

        // Act
        var result = await _controller.Run(_applicationId, useCaseMock.Object, CancellationToken.None);

        // Assert
        Assert.Equal(redirectResult, result);
    }

    [Fact]
    public async Task Run_RedirectsToError_WhenFailure()
    {
        // Arrange
        var useCaseMock = new Mock<IRunFcInternalUserConstraintCheckUseCase>();
        _controller.PrepareControllerForTest(Guid.NewGuid());

        useCaseMock.Setup(x => x.ExecuteConstraintsCheckAsync(
            It.IsAny<InternalUser>(), _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<RedirectResult>("fail"));

        // Act
        var result = await _controller.Run(_applicationId, useCaseMock.Object, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }
}
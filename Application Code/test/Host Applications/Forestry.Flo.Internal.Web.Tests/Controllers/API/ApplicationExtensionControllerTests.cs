using Forestry.Flo.Internal.Web.Controllers.Api;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.API;

public class ApplicationExtensionControllerTests
{
    [Fact]
    public async Task ExtendApplicationFinalActionDates_CallsUseCaseAndReturnsOk()
    {
        // Arrange
        var mockUseCase = new Mock<IExtendApplicationsUseCase>();
        var controller = new ApplicationExtensionController();
        controller.PrepareControllerBaseForTest(Guid.NewGuid());

        // Setup mock to verify method call
        mockUseCase
            .Setup(x => x.ExtendApplicationFinalActionDatesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await controller.ExtendApplicationFinalActionDates(
            mockUseCase.Object,
            CancellationToken.None);

        // Assert
        mockUseCase.Verify(x => x.ExtendApplicationFinalActionDatesAsync(
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsType<OkResult>(result);
    }
}
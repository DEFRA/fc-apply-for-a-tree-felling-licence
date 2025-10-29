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
            .Setup(x => x.ExtendApplicationFinalActionDatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await controller.ExtendApplicationFinalActionDates(
            mockUseCase.Object,
            CancellationToken.None);

        // Assert
        mockUseCase.Verify(x => x.ExtendApplicationFinalActionDatesAsync(
            It.Is<string>(s => !string.IsNullOrEmpty(s)),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public void GetFellingLicenceUrlLink_ReturnsNonNullString()
    {
        // Arrange
        var controller = new ApplicationExtensionController();
        controller.PrepareControllerBaseForTest(Guid.NewGuid());

        // Act
        var url = controller.GetType()
            .GetMethod("GetFellingLicenceUrlLink", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(controller, null);

        // Assert
        Assert.NotNull(url);
        Assert.IsType<string>(url);
    }
}
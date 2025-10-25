using Forestry.Flo.Internal.Web.Controllers.Api;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.API;

public class VoluntaryWithdrawNotificationControllerTests
{
    [Fact]
    public async Task SendVoluntaryWithdrawalNotificatons_CallsUseCasesAndReturnsOk()
    {
        // Arrange
        var mockVoluntaryUseCase = new Mock<IVoluntaryWithdrawalNotificationUseCase>();
        var mockAutomaticUseCase = new Mock<IAutomaticWithdrawalNotificationUseCase>();
        var mockWithdrawService = new Mock<IWithdrawFellingLicenceService>();

        var controller = new VoluntaryWithdrawNotificationController();
        controller.PrepareControllerBaseForTest(Guid.NewGuid());

        // Setup mocks to verify calls
        mockAutomaticUseCase
            .Setup(x => x.ProcessApplicationsAsync(It.IsAny<string>(), mockWithdrawService.Object, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        mockVoluntaryUseCase
            .Setup(x => x.SendNotificationForWithdrawalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await controller.SendVoluntaryWithdrawalNotificatons(
            mockVoluntaryUseCase.Object,
            mockAutomaticUseCase.Object,
            mockWithdrawService.Object,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        mockAutomaticUseCase.Verify(x => x.ProcessApplicationsAsync(It.IsAny<string>(), mockWithdrawService.Object, It.IsAny<CancellationToken>()), Times.Once);
        mockVoluntaryUseCase.Verify(x => x.SendNotificationForWithdrawalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetFellingLicenceUrlLink_ReturnsNonNullString()
    {
        // Arrange
        var controller = new VoluntaryWithdrawNotificationController();
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
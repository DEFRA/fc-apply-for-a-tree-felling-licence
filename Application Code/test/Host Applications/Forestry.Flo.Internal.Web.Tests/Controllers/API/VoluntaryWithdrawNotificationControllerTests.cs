using Forestry.Flo.Internal.Web.Controllers.Api;
using Forestry.Flo.Internal.Web.Services.Interfaces;
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

        var controller = new VoluntaryWithdrawNotificationController();
        controller.PrepareControllerBaseForTest(Guid.NewGuid());

        // Setup mocks to verify calls
        mockAutomaticUseCase
            .Setup(x => x.ProcessApplicationsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        mockVoluntaryUseCase
            .Setup(x => x.SendNotificationForWithdrawalAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await controller.SendVoluntaryWithdrawalNotificatons(
            mockVoluntaryUseCase.Object,
            mockAutomaticUseCase.Object,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        mockAutomaticUseCase.Verify(x => x.ProcessApplicationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockVoluntaryUseCase.Verify(x => x.SendNotificationForWithdrawalAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
using Forestry.Flo.Internal.Web.Controllers.Api;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.API;

public class AmendmentResponseControllerTests
{
    [Fact]
    public async Task SendLateAmendmentResponseReminders_ReturnsOk_WithRemindersSentCount()
    {
        // Arrange
        var useCaseMock = new Mock<ILateAmendmentResponseWithdrawalUseCase>();
        useCaseMock.Setup(x => x.SendLateAmendmentResponseRemindersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var controller = new AmendmentResponseController();

        // Act
        var result = await controller.SendLateAmendmentResponseReminders(useCaseMock.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic value = okResult.Value;
        Assert.Equal(5, (int)value.remindersSent);
    }

    [Fact]
    public async Task WithdrawLateAmendmentApplications_ReturnsOk_WithWithdrawnCount()
    {
        // Arrange
        var useCaseMock = new Mock<ILateAmendmentResponseWithdrawalUseCase>();
        var withdrawServiceMock = new Mock<IWithdrawFellingLicenceService>();
        useCaseMock.Setup(x => x.WithdrawLateAmendmentApplicationsAsync(
                It.IsAny<IWithdrawFellingLicenceService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var controller = new AmendmentResponseController();

        // Act
        var result = await controller.WithdrawLateAmendmentApplications(
            useCaseMock.Object,
            withdrawServiceMock.Object,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic value = okResult.Value;
        Assert.Equal(3, (int)value.withdrawn);
    }
}

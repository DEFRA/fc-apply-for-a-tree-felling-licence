using Forestry.Flo.Internal.Web.Controllers.Api;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.API;

public class PublicRegisterExpiryControllerTests
{
    [Fact]
    public async Task RemoveApplicationsFromConsultationPublicRegisterWhenEndDateReached_CallsUseCaseAndReturnsOk()
    {
        // Arrange
        var mockUseCase = new Mock<IPublicRegisterExpiryUseCase>();
        var controller = new PublicRegisterExpiryController();
        controller.PrepareControllerBaseForTest(Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        mockUseCase
            .Setup(x => x.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(
                It.IsAny<string>(), cancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await controller.RemoveApplicationsFromConsultationPublicRegisterWhenEndDateReached(
            mockUseCase.Object, cancellationToken);

        // Assert
        mockUseCase.Verify(x => x.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(
            It.IsAny<string>(), cancellationToken), Times.Once);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveApplicationsFromDecisionPublicRegisterWhenEndDateReached_CallsUseCaseAndReturnsOk()
    {
        // Arrange
        var mockUseCase = new Mock<IRemoveApplicationsFromDecisionPublicRegisterUseCase>();
        var controller = new PublicRegisterExpiryController();
        controller.PrepareControllerBaseForTest(Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        mockUseCase
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(), cancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await controller.RemoveApplicationsFromDecisionPublicRegisterWhenEndDateReached(
            mockUseCase.Object, cancellationToken);

        // Assert
        mockUseCase.Verify(x => x.ExecuteAsync(
            It.IsAny<string>(), cancellationToken), Times.Once);
        Assert.IsType<OkResult>(result);
    }
}

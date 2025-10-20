using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.Api;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.API;

public class PublicRegisterCommentsControllerTests
{
    [Fact]
    public async Task PullNewComments_ReturnsOk_WhenUseCaseSuccess()
    {
        // Arrange
        var mockUseCase = new Mock<IPublicRegisterCommentsUseCase>();
        var expectedValue = "new comments";
        var result = Result.Success(expectedValue);
        mockUseCase.Setup(x => x.GetNewCommentsFromPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var controller = new PublicRegisterCommentsController();

        // Act
        var response = await controller.PullNewComments(mockUseCase.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(expectedValue, okResult.Value);
    }

    [Fact]
    public async Task PullNewComments_ReturnsInternalServerError_WhenUseCaseFailure()
    {
        // Arrange
        var mockUseCase = new Mock<IPublicRegisterCommentsUseCase>();
        var errorMessage = "error occurred";
        var result = Result.Failure<string>(errorMessage);
        mockUseCase.Setup(x => x.GetNewCommentsFromPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var controller = new PublicRegisterCommentsController();

        // Act
        var response = await controller.PullNewComments(mockUseCase.Object, CancellationToken.None);

        // Assert
        var errorResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, errorResult.StatusCode);
        Assert.Equal(errorMessage, errorResult.Value);
    }
}

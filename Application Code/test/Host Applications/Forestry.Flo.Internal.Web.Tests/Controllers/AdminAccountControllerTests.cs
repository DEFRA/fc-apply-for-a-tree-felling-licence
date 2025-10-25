using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers;

public class AdminAccountControllerTests
{
    private readonly AdminAccountController _controller;

    public AdminAccountControllerTests()
    {
        _controller = new AdminAccountController();
        _controller.PrepareControllerForTest(Guid.NewGuid(), true);
    }

    [Fact]
    public async Task UnconfirmedUserAccounts_ReturnsViewResultWithModel()
    {
        // Arrange
        var mockService = new Mock<IUserAccountService>();
        var accounts = new List<UserAccount>();
        mockService.Setup(x => x.ListNonConfirmedUserAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);
        // ModelMapping.ToUserAccountModels returns List<UserAccountModel>
        // Assume ModelMapping is static and works as expected

        // Act
        var result = await _controller.UnconfirmedUserAccounts(mockService.Object, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
        Assert.NotNull(_controller.ViewBag.Breadcrumbs);
    }

    [Theory]
    [InlineData(Status.Closed)]
    [InlineData(Status.Confirmed)]
    [InlineData(Status.Denied)]
    [InlineData(Status.Requested)]
    public async Task ReviewUnconfirmedUserAccount_ReturnsViewOrRedirect(Status status)
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var mockUserAccountService = new Mock<IUserAccountService>();
        var mockAzureAdService = new Mock<IAzureAdService>();
        var userAccount = new UserAccount { Status = status, Email = "test@example.com" };
        mockUserAccountService.Setup(x => x.GetUserAccountAsync(userAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);
        mockAzureAdService.Setup(x => x.UserIsInDirectoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReviewUnconfirmedUserAccount(
            userAccountId, mockUserAccountService.Object, mockAzureAdService.Object, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
        Assert.True((bool)_controller.ViewBag.UserIsInActiveDirectory);
        Assert.NotNull(_controller.ViewBag.Breadcrumbs);
    }

    [Fact]
    public async Task ReviewUnconfirmedUserAccount_RedirectsToError_WhenNoValue()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var mockUserAccountService = new Mock<IUserAccountService>();
        var mockAzureAdService = new Mock<IAzureAdService>();
        mockUserAccountService.Setup(x => x.GetUserAccountAsync(userAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.None);

        // Act
        var result = await _controller.ReviewUnconfirmedUserAccount(
            userAccountId, mockUserAccountService.Object, mockAzureAdService.Object, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ConfirmUserAccount_UpdatesStatusAndRedirects()
    {
        // Arrange
        var id = Guid.NewGuid();
        var mockUseCase = new Mock<IRegisterUserAccountUseCase>();
        mockUseCase.Setup(x => x.UpdateUserAccountStatusAsync(
            It.IsAny<InternalUser>(), id, Status.Confirmed, true, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        // Act
        var result = await _controller.ConfirmUserAccount(id, true, mockUseCase.Object, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("UnconfirmedUserAccounts", redirectResult.ActionName);
    }

    [Fact]
    public async Task ConfirmUserAccount_FailureStillRedirects()
    {
        // Arrange
        var id = Guid.NewGuid();
        var mockUseCase = new Mock<IRegisterUserAccountUseCase>();
        mockUseCase.Setup(x => x.UpdateUserAccountStatusAsync(
            It.IsAny<InternalUser>(), id, Status.Confirmed, true, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));
        // Act
        var result = await _controller.ConfirmUserAccount(id, true, mockUseCase.Object, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("UnconfirmedUserAccounts", redirectResult.ActionName);
    }

    [Fact]
    public async Task DenyUserAccount_UpdatesStatusAndRedirects()
    {
        // Arrange
        var id = Guid.NewGuid();
        var mockUseCase = new Mock<IRegisterUserAccountUseCase>();
        mockUseCase.Setup(x => x.UpdateUserAccountStatusAsync(
            It.IsAny<InternalUser>(), id, Status.Denied, null, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        // Act
        var result = await _controller.DenyUserAccount(id, mockUseCase.Object, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("UnconfirmedUserAccounts", redirectResult.ActionName);
    }

    [Fact]
    public async Task DenyUserAccount_FailureStillRedirects()
    {
        // Arrange
        var id = Guid.NewGuid();
        var mockUseCase = new Mock<IRegisterUserAccountUseCase>();
        mockUseCase.Setup(x => x.UpdateUserAccountStatusAsync(
            It.IsAny<InternalUser>(), id, Status.Denied, null, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));
        // Act
        var result = await _controller.DenyUserAccount(id, mockUseCase.Object, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("UnconfirmedUserAccounts", redirectResult.ActionName);
    }
}
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Routing;
using AuthenticationOptions = Forestry.Flo.Services.Common.Infrastructure.AuthenticationOptions;

namespace Forestry.Flo.Internal.Web.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Guid _requestContextUserId = Guid.NewGuid();
    private readonly Mock<IFellingLicenceApplicationUseCase> _fellingLicenceApplicationUseCaseMock = new();
    private readonly Mock<IUserAccountService> _userAccountServiceMock = new();
    private readonly Mock<ILogger<HomeController>> _loggerMock = new();
    private readonly HomeController _controller;
    private readonly Fixture _fixture = new();

    public HomeControllerTests()
    {
        _controller = new HomeController(
            _fellingLicenceApplicationUseCaseMock.Object,
            _userAccountServiceMock.Object,
            _loggerMock.Object);

        _controller.PrepareControllerForTest(_requestContextUserId, true);
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WhenUserAccountExists()
    {
        var assignmentListModel = new FellingLicenceApplicationAssignmentListModel();
        var resultModel = Result.Success(assignmentListModel);
        _fellingLicenceApplicationUseCaseMock.Setup(x =>
            x.GetFellingLicenceApplicationAssignmentListModelAsync(
                It.IsAny<bool>(),
                It.IsAny<Guid>(),
                It.IsAny<IList<FellingLicenceStatus>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(resultModel);

        var userAccount = _fixture
            .Build<UserAccount>()
            .With(x => x.Roles, string.Join(",", new List<Roles> { Roles.FcAdministrator }))
            .Create();
        _userAccountServiceMock.Setup(x =>
            x.GetUserAccountAsync(_requestContextUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(userAccount));

        var result = await _controller.Index(
            assignedToUserOnly: true,
            fellingLicenceStatusArray: new[] { FellingLicenceStatus.Submitted },
            cancellationToken: CancellationToken.None,
            page: 1,
            pageSize: 12,
            column: "FinalActionDate",
            dir: "asc",
            search: "test");

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<HomePageModel>(viewResult.Model);
    }

    [Fact]
    public async Task Index_RedirectsToError_WhenAssignmentListModelFailure()
    {
        _fellingLicenceApplicationUseCaseMock.Setup(x =>
            x.GetFellingLicenceApplicationAssignmentListModelAsync(
                It.IsAny<bool>(),
                It.IsAny<Guid>(),
                It.IsAny<IList<FellingLicenceStatus>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplicationAssignmentListModel>("error"));

        var userAccount = _fixture.Create<UserAccount>();
        _userAccountServiceMock.Setup(x =>
            x.GetUserAccountAsync(_requestContextUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(userAccount));

        var result = await _controller.Index(
            assignedToUserOnly: true,
            fellingLicenceStatusArray: new[] { FellingLicenceStatus.Submitted },
            cancellationToken: CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Index_RedirectsToError_WhenUserAccountHasNoValue()
    {
        var assignmentListModel = new FellingLicenceApplicationAssignmentListModel();
        var resultModel = Result.Success(assignmentListModel);
        _fellingLicenceApplicationUseCaseMock.Setup(x =>
            x.GetFellingLicenceApplicationAssignmentListModelAsync(
                It.IsAny<bool>(),
                It.IsAny<Guid>(),
                It.IsAny<IList<FellingLicenceStatus>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(resultModel);

        _userAccountServiceMock.Setup(x =>
            x.GetUserAccountAsync(_requestContextUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.None);

        var result = await _controller.Index(
            assignedToUserOnly: true,
            fellingLicenceStatusArray: new[] { FellingLicenceStatus.Submitted },
            cancellationToken: CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public void Login_ReturnsViewResult()
    {
        var result = _controller.Login();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void SignIn_RedirectsToIndex()
    {
        var result = _controller.SignIn();
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task Logout_OneLoginProvider_SignsOutAndReturnsSignOutResult()
    {
        var optionsMock = new Mock<IOptions<AuthenticationOptions>>();
        optionsMock.Setup(x => x.Value).Returns(new AuthenticationOptions
        {
            Provider = AuthenticationProvider.OneLogin
        });

        var result = await _controller.Logout(optionsMock.Object);

        Assert.IsType<SignOutResult>(result);
    }

    [Fact]
    public async Task Logout_AzureProvider_SignsOutAndReturnsSignOutResult()
    {
        var optionsMock = new Mock<IOptions<AuthenticationOptions>>();
        optionsMock.Setup(x => x.Value).Returns(new AuthenticationOptions
        {
            Provider = AuthenticationProvider.Azure
        });

        var result = await _controller.Logout(optionsMock.Object);

        Assert.IsType<SignOutResult>(result);
        Assert.True(_controller.HttpContext.Response.Headers.ContainsKey("Clear-Site-Data"));
    }

    [Fact]
    public async Task Logout_DefaultProvider_ReturnsSignOutResult()
    {
        var optionsMock = new Mock<IOptions<AuthenticationOptions>>();
        optionsMock.Setup(x => x.Value).Returns(new AuthenticationOptions
        {
            Provider = (AuthenticationProvider)999 // Unknown
        });

        var httpContextMock = new DefaultHttpContext();
        _controller.ControllerContext.HttpContext = httpContextMock;

        var result = await _controller.Logout(optionsMock.Object);

        Assert.IsType<SignOutResult>(result);
    }

    [Fact]
    public void Error_ReturnsViewResultWithErrorViewModel()
    {
        var activity = new Activity("TestActivity");
        activity.Start();

        Activity.Current = activity;

        var result = _controller.Error();
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);

        Activity.Current.Stop();
        Activity.Current = null;
    }

    [Fact]
    public async Task AccountError_ReturnsViewResultWithErrorViewModel()
    {
        var activity = new Activity("TestActivity");
        activity.Start();

        Activity.Current = activity;
        var result = await _controller.AccountError();
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ErrorViewModel>(viewResult.Model);
        Activity.Current.Stop();
        Activity.Current = null;
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        var result = _controller.Privacy();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Accessibility_ReturnsViewResult()
    {
        var result = _controller.Accessibility();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Cookies_ReturnsViewResult()
    {
        var result = _controller.Cookies();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task UserManagement_ReturnsViewResult_WhenUserAccountExists()
    {
        var userAccount = _fixture
            .Build<UserAccount>()
            .With(x => x.Roles, string.Join(",",new List<Roles>{Roles.FcAdministrator}))
            .Create();
        _userAccountServiceMock.Setup(x =>
            x.GetUserAccountAsync(_requestContextUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(userAccount));

        var result = await _controller.UserManagement(CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(_controller.ViewBag.Breadcrumbs);
        Assert.NotNull(_controller.ViewData["SignedInUserRoles"]);
    }

    [Fact]
    public async Task UserManagement_RedirectsToError_WhenUserAccountHasNoValue()
    {
        _userAccountServiceMock.Setup(x =>
            x.GetUserAccountAsync(_requestContextUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.None);

        var result = await _controller.UserManagement(CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task UserManagement_ReturnsViewResult_WhenUserAccountIdIsNull()
    {
        // Simulate no user account id in claims
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        _controller.ControllerContext.HttpContext = httpContext;

        var result = await _controller.UserManagement(CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(_controller.ViewBag.Breadcrumbs);
    }
}
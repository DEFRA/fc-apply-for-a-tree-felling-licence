using System.Security.Claims;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers;
using Forestry.Flo.Internal.Web.Infrastructure.Display;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<IValidationProvider> _validationProviderMock;
    private readonly AccountController _controller;
    private readonly ClaimsPrincipal _user;
    private readonly Fixture _fixture = new();

    public AccountControllerTests()
    {
        _validationProviderMock = new Mock<IValidationProvider>();
        _user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "Test User")
            }));

        _controller = new AccountController(_validationProviderMock.Object);
        _controller.PrepareControllerForTest(Guid.NewGuid());
    }

    [Fact]
    public async Task RegisterAccountDetails_Get_ReturnsView_WhenModelFound()
    {
        var useCaseMock = new Mock<IRegisterUserAccountUseCase>();
        var model = _fixture.Create<UserRegistrationDetailsModel>();
        useCaseMock.Setup(x => x.GetUserAccountModelAsync(It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserRegistrationDetailsModel>.From(model));

        var result = await _controller.RegisterAccountDetails(useCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
        Assert.True(model.AllowRoleChange);
        Assert.Contains(AccountTypeInternal.AccountAdministrator, model.DisallowedRoles);
        Assert.NotNull(model.Breadcrumbs);
    }

    [Fact]
    public async Task RegisterAccountDetails_Get_Redirects_WhenModelNotFound()
    {
        var useCaseMock = new Mock<IRegisterUserAccountUseCase>();
        useCaseMock.Setup(x => x.GetUserAccountModelAsync(It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserRegistrationDetailsModel>.None);

        var result = await _controller.RegisterAccountDetails(useCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task RegisterAccountDetails_Post_ReturnsView_WhenModelStateInvalid()
    {
        var useCaseMock = new Mock<IRegisterUserAccountUseCase>();
        var model = new UserRegistrationDetailsModel();
        _controller.ModelState.AddModelError("FirstName", "Required");

        _validationProviderMock.Setup(x => x.ValidateSection(model, nameof(UserRegistrationDetailsModel), _controller.ModelState))
            .Returns(new List<FluentValidation.Results.ValidationFailure>());

        var result = await _controller.RegisterAccountDetails(model, useCaseMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
        Assert.True(model.AllowRoleChange);
        Assert.Contains(AccountTypeInternal.AccountAdministrator, model.DisallowedRoles);
        Assert.NotNull(model.Breadcrumbs);
    }

    [Fact]
    public async Task RegisterAccountDetails_Post_UpdatesAndRedirects_WhenModelStateValid()
    {
        var useCaseMock = new Mock<IRegisterUserAccountUseCase>();
        var model = _fixture.Create<UserRegistrationDetailsModel>();
        var url = "http://localhost/AdminAccount/ReviewUnconfirmedUserAccount";

        _validationProviderMock.Setup(x => x.ValidateSection(model, nameof(UserRegistrationDetailsModel), _controller.ModelState))
            .Returns(new List<FluentValidation.Results.ValidationFailure>());

        useCaseMock.Setup(x => x.UpdateAccountRegistrationDetailsAsync(
            It.IsAny<InternalUser>(), model, url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.RegisterAccountDetails(model, useCaseMock.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public void AccessDenied_ReturnsView()
    {
        var result = _controller.AccessDenied();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void UserAccountTypeNotValid_ReturnsView()
    {
        var result = _controller.UserAccountTypeNotValid();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task UserAccountAwaitingConfirmation_ReturnsView()
    {
        var result = await _controller.UserAccountAwaitingConfirmation();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void SetBreadcrumbs_SetsBreadcrumbsModel()
    {
        var model = new UserRegistrationDetailsModel();
        _controller.GetType().GetMethod("SetBreadcrumbs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(_controller, new object[] { model, "Test Page" });

        Assert.NotNull(model.Breadcrumbs);
        Assert.Equal("Test Page", model.Breadcrumbs.CurrentPage);
        Assert.NotEmpty(model.Breadcrumbs.Breadcrumbs);
    }

    [Fact]
    public void ApplySectionValidationModelErrors_AddsModelErrors()
    {
        var model = new UserRegistrationDetailsModel();
        var failures = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("FirstName", "Required")
            };
        _validationProviderMock.Setup(x => x.ValidateSection(model, nameof(UserRegistrationDetailsModel), _controller.ModelState))
            .Returns(failures);

        _controller.GetType().GetMethod("ApplySectionValidationModelErrors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(_controller, new object[] { model, nameof(UserRegistrationDetailsModel) });

        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey("FirstName"));
    }
}

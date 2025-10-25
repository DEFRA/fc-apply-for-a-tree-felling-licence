using AutoFixture;
using CSharpFunctionalExtensions;
using FluentValidation.Results;
using Forestry.Flo.Internal.Web.Controllers.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.AccountAdministration;

public class AccountAdministrationControllerTests
{
    private readonly Mock<IValidationProvider> _validationProviderMock;
    private readonly AccountAdministrationController _controller;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Fixture _fixture = new();
    private const string ListActionName = "FcStaffList";

    public AccountAdministrationControllerTests()
    {
        _validationProviderMock = new Mock<IValidationProvider>();
        _controller = new AccountAdministrationController(_validationProviderMock.Object);
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.WoodlandOfficer);
    }

    [Fact]
    public async Task FcStaffList_ReturnsViewResult_WhenSuccess()
    {
        var useCase = new Mock<IGetFcStaffMembersUseCase>();
        var model = Result.Success(_fixture.Create<FcStaffListModel>());
        useCase.Setup(x => x.GetAllFcStaffMembersAsync(
            It.IsAny<InternalUser>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);

        var result = await _controller.FcStaffList(useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<FcStaffListModel>(viewResult.Model);
    }

    [Fact]
    public async Task FcStaffList_Redirects_WhenNotAccountAdministrator()
    {
        var useCase = new Mock<IGetFcStaffMembersUseCase>();

        var result = await _controller.FcStaffList(useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task FcStaffList_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IGetFcStaffMembersUseCase>();
        useCase.Setup(x => x.GetAllFcStaffMembersAsync(
            It.IsAny<InternalUser>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FcStaffListModel>("error"));

        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);

        var result = await _controller.FcStaffList(useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public void CloseInternalUserAccount_RedirectsToAmend_WhenSelf()
    {
        var model = new UpdateUserRegistrationDetailsModel { Id = _userId };

        var result = _controller.CloseInternalUserAccount(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AmendUserAccount", redirectResult.ActionName);
        Assert.Equal(model.Id, redirectResult.RouteValues["userId"]);
    }

    [Fact]
    public void CloseInternalUserAccount_RedirectsToCloseUserAccount_WhenNotSelf()
    {
        var model = new UpdateUserRegistrationDetailsModel { Id = Guid.NewGuid() };

        var result = _controller.CloseInternalUserAccount(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("CloseUserAccount", redirectResult.ActionName);
        Assert.Equal(model.Id, redirectResult.RouteValues["userId"]);
    }

    [Fact]
    public async Task AmendInternalUserAccount_RedirectsToStaffList_WhenModelStateInvalid()
    {
        var useCase = new Mock<IGetFcStaffMembersUseCase>();
        var model = new FcStaffListModel();
        _controller.ModelState.AddModelError("Test", "Error");
        useCase.Setup(x => x.GetAllFcStaffMembersAsync(
            It.IsAny<InternalUser>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<FcStaffListModel>()));

        var result = await _controller.AmendInternalUserAccount(model, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("FcStaffList", viewResult.ViewName);
    }

    [Fact]
    public async Task AmendInternalUserAccount_RedirectsToAmendUserAccount_WhenValid()
    {
        var useCase = new Mock<IGetFcStaffMembersUseCase>();
        var model = new FcStaffListModel { SelectedUserAccountId = Guid.NewGuid() };

        var result = await _controller.AmendInternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AmendUserAccount", redirectResult.ActionName);
        Assert.Equal(model.SelectedUserAccountId, redirectResult.RouteValues["userId"]);
    }

    [Fact]
    public async Task RedirectToStaffList_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IGetFcStaffMembersUseCase>();
        var model = new FcStaffListModel { ReturnUrl = "url" };
        useCase.Setup(x => x.GetAllFcStaffMembersAsync(
            It.IsAny<InternalUser>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<FcStaffListModel>()));

        var result = await _controller.RedirectToStaffList(model, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("FcStaffList", viewResult.ViewName);
    }

    [Fact]
    public async Task RedirectToStaffList_Redirects_WhenFailure()
    {
        var useCase = new Mock<IGetFcStaffMembersUseCase>();
        var model = new FcStaffListModel { ReturnUrl = "url" };
        useCase.Setup(x => x.GetAllFcStaffMembersAsync(
            It.IsAny<InternalUser>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FcStaffListModel>("error"));

        var result = await _controller.RedirectToStaffList(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("FcStaffList", redirectResult.ActionName);
    }

    [Fact]
    public async Task AmendUserAccount_Get_Redirects_WhenNotAccountAdministrator()
    {
        var useCase = new Mock<IRegisterUserAccountUseCase>();

        var result = await _controller.AmendUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AmendUserAccount_Get_Redirects_WhenFailure()
    {
        var useCase = new Mock<IRegisterUserAccountUseCase>();
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);
        useCase.Setup(x => x.GetUserAccountModelByIdAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UpdateUserRegistrationDetailsModel>("error"));

        var result = await _controller.AmendUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("FcStaffList", redirectResult.ActionName);
    }

    [Fact]
    public async Task AmendUserAccount_Get_ReturnsView_WhenSuccess()
    {
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);
        var useCase = new Mock<IRegisterUserAccountUseCase>();
        var model = _fixture.Create<UpdateUserRegistrationDetailsModel>();
        useCase.Setup(x => x.GetUserAccountModelByIdAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AmendUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task AmendUserAccount_Post_ReturnsView_WhenModelStateInvalid()
    {
        var useCase = new Mock<IRegisterUserAccountUseCase>();
        var model = new UpdateUserRegistrationDetailsModel();
        _controller.ModelState.AddModelError("Test", "Error");
        _validationProviderMock.Setup(x => x.ValidateSection(It.IsAny<UserRegistrationDetailsModel>(), It.IsAny<string>(), It.IsAny<ModelStateDictionary>()))
            .Returns(_fixture.Create<List<ValidationFailure>>());

        var result = await _controller.AmendUserAccount(model, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task AmendUserAccount_Post_Redirects_WhenNotAccountAdministrator()
    {
        var useCase = new Mock<IRegisterUserAccountUseCase>();
        var model = new UpdateUserRegistrationDetailsModel();
        _validationProviderMock.Setup(x => x.ValidateSection(It.IsAny<UpdateUserRegistrationDetailsModel>(), It.IsAny<string>(), It.IsAny<ModelStateDictionary>()))
            .Returns([]);
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AdminOfficer);

        var result = await _controller.AmendUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AmendUserAccount_Post_SetsAccountType_WhenSelf()
    {
        var useCase = new Mock<IRegisterUserAccountUseCase>();
        var model = new UpdateUserRegistrationDetailsModel { Id = _userId, RequestedAccountType = AccountTypeInternal.AdminOfficer };
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);
        useCase.Setup(x => x.UpdateAccountRegistrationDetailsByIdAsync(It.IsAny<InternalUser>(), model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _validationProviderMock.Setup(x => x.ValidateSection(It.IsAny<UpdateUserRegistrationDetailsModel>(), It.IsAny<string>(), It.IsAny<ModelStateDictionary>()))
            .Returns([]);

        var result = await _controller.AmendUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("FcStaffList", redirectResult.ActionName);
        Assert.Equal(AccountTypeInternal.AccountAdministrator, model.RequestedAccountType);
    }

    [Fact]
    public async Task AmendUserAccount_Post_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IRegisterUserAccountUseCase>();
        var model = new UpdateUserRegistrationDetailsModel();
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);
        useCase.Setup(x => x.UpdateAccountRegistrationDetailsByIdAsync(It.IsAny<InternalUser>(), model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("error"));
        _validationProviderMock.Setup(x => x.ValidateSection(It.IsAny<UpdateUserRegistrationDetailsModel>(), It.IsAny<string>(), It.IsAny<ModelStateDictionary>()))
            .Returns([]);

        var result = await _controller.AmendUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AmendUserAccount_Post_RedirectsToFcStaffList_WhenSuccess()
    {
        var useCase = new Mock<IRegisterUserAccountUseCase>();
        var model = new UpdateUserRegistrationDetailsModel();
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);
        useCase.Setup(x => x.UpdateAccountRegistrationDetailsByIdAsync(It.IsAny<InternalUser>(), model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _validationProviderMock.Setup(x => x.ValidateSection(It.IsAny<UpdateUserRegistrationDetailsModel>(), It.IsAny<string>(), It.IsAny<ModelStateDictionary>()))
            .Returns([]);

        var result = await _controller.AmendUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("FcStaffList", redirectResult.ActionName);
    }

    [Fact]
    public async Task CloseUserAccount_Get_Redirects_WhenNotAccountAdministrator()
    {
        var useCase = new Mock<ICloseFcStaffAccountUseCase>();
        var result = await _controller.CloseUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task CloseUserAccount_Get_Redirects_WhenSelf()
    {
        var useCase = new Mock<ICloseFcStaffAccountUseCase>();
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);

        var result = await _controller.CloseUserAccount(_userId, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("FcStaffList", redirectResult.ActionName);
    }

    [Fact]
    public async Task CloseUserAccount_Get_Redirects_WhenFailure()
    {
        var useCase = new Mock<ICloseFcStaffAccountUseCase>();
        useCase.Setup(x => x.RetrieveUserAccountDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CloseUserAccountModel>("fail"));
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);

        var result = await _controller.CloseUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("FcStaffList", redirectResult.ActionName);
    }

    [Fact]
    public async Task CloseUserAccount_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<ICloseFcStaffAccountUseCase>();
        var model = _fixture.Create<CloseUserAccountModel>();
        useCase.Setup(x => x.RetrieveUserAccountDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);

        var result = await _controller.CloseUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task CloseUserAccount_Post_Redirects_WhenNotAccountAdministrator()
    {
        var useCase = new Mock<ICloseFcStaffAccountUseCase>();
        var model = _fixture.Create<CloseUserAccountModel>();
        var result = await _controller.CloseUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task CloseUserAccount_Post_Redirects_WhenSelf()
    {
        var useCase = new Mock<ICloseFcStaffAccountUseCase>();
        var userAccountModel = _fixture.Build<UserAccountModel>().With(x => x.Id, _userId).Create();
        var model = _fixture.Build<CloseUserAccountModel>().With(x => x.AccountToClose, userAccountModel).Create();


        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);

        var result = await _controller.CloseUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("FcStaffList", redirectResult.ActionName);
    }

    [Fact]
    public async Task CloseUserAccount_Post_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<ICloseFcStaffAccountUseCase>();
        var model = _fixture.Create<CloseUserAccountModel>();
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);
        useCase.Setup(x => x.CloseFcStaffAccountAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("error"));

        var result = await _controller.CloseUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task CloseUserAccount_Post_RedirectsToFcStaffList_WhenSuccess()
    {
        var useCase = new Mock<ICloseFcStaffAccountUseCase>();
        var model = _fixture.Create<CloseUserAccountModel>();
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.AccountAdministrator);
        useCase.Setup(x => x.CloseFcStaffAccountAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.CloseUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("FcStaffList", redirectResult.ActionName);
    }
}
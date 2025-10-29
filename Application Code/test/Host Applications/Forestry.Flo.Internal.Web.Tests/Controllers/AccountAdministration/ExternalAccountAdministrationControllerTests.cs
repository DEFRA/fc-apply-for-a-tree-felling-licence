using AutoFixture;
using CSharpFunctionalExtensions;
using FluentValidation.Results;
using Forestry.Flo.Internal.Web.Controllers.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.AccountAdministration;

public class ExternalAccountAdministrationControllerTests
{
    private readonly Mock<IValidationProvider> _validationProviderMock;
    private readonly ExternalAccountAdministrationController _controller;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Fixture _fixture = new();
    private const string ControllerName = "ExternalAccountAdministration";
    private const string ListActionName = "ExternalUserList";


    public ExternalAccountAdministrationControllerTests()
    {
        _validationProviderMock = new Mock<IValidationProvider>();
        _controller = new ExternalAccountAdministrationController(_validationProviderMock.Object);
        _controller.PrepareControllerForTest(_userId, true, AccountTypeInternal.AccountAdministrator);
    }

    [Fact]
    public async Task ExternalUserList_ReturnsViewResult_WhenSuccess()
    {
        var useCase = new Mock<IGetApplicantUsersUseCase>();
        var model = Result.Success(new ExternalUserListModel());
        useCase.Setup(x => x.RetrieveListOfActiveExternalUsersAsync(
            It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.ExternalUserList(useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ExternalUserListModel>(viewResult.Model);
    }

    [Fact]
    public async Task ExternalUserList_Redirects_WhenNotAccountAdministrator()
    {
        var useCase = new Mock<IGetApplicantUsersUseCase>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal();
        _controller.ControllerContext.HttpContext = httpContext;

        var result = await _controller.ExternalUserList(useCase.Object, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task ExternalUserList_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IGetApplicantUsersUseCase>();
        useCase.Setup(x => x.RetrieveListOfActiveExternalUsersAsync(
            It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ExternalUserListModel>("error"));

        var result = await _controller.ExternalUserList(useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task CloseUserAccount_RedirectsToExternalList_WhenModelStateInvalid()
    {
        var useCase = new Mock<IGetApplicantUsersUseCase>();
        var model = new ExternalUserListModel();
        _controller.ModelState.AddModelError("Test", "Error");

        useCase.Setup(x =>
                x.RetrieveListOfActiveExternalUsersAsync(It.IsAny<InternalUser>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<ExternalUserListModel>());

        var result = await _controller.CloseUserAccount(model, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(ListActionName, viewResult.ViewName);
    }

    [Fact]
    public async Task CloseUserAccount_RedirectsToCloseExternalUserAccount_WhenValid()
    {
        var useCase = new Mock<IGetApplicantUsersUseCase>();
        var model = new ExternalUserListModel { SelectedUserAccountId = Guid.NewGuid() };

        var result = await _controller.CloseUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("CloseExternalUserAccount", redirectResult.ActionName);
        Assert.Equal(model.SelectedUserAccountId, redirectResult.RouteValues["userId"]);
    }

    [Fact]
    public async Task AmendUserAccount_RedirectsToExternalList_WhenModelStateInvalid()
    {
        var useCase = new Mock<IGetApplicantUsersUseCase>();
        var model = new ExternalUserListModel();
        _controller.ModelState.AddModelError("Test", "Error");
        useCase.Setup(x => x.RetrieveListOfActiveExternalUsersAsync(
                It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ExternalUserListModel()));

        var result = await _controller.AmendUserAccount(model, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(ListActionName, viewResult.ViewName);
    }

    [Fact]
    public async Task AmendUserAccount_RedirectsToAmendExternalUserAccount_WhenValid()
    {
        var useCase = new Mock<IGetApplicantUsersUseCase>();
        var model = new ExternalUserListModel { SelectedUserAccountId = Guid.NewGuid() };

        var result = await _controller.AmendUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AmendExternalUserAccount", redirectResult.ActionName);
        Assert.Equal(model.SelectedUserAccountId, redirectResult.RouteValues["userId"]);
    }

    [Fact]
    public async Task RedirectToExternalList_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IGetApplicantUsersUseCase>();
        var model = new ExternalUserListModel { ReturnUrl = "url" };
        useCase.Setup(x => x.RetrieveListOfActiveExternalUsersAsync(
            It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ExternalUserListModel()));

        var result = await _controller.RedirectToExternalList(model, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(ListActionName, viewResult.ViewName);
    }

    [Fact]
    public async Task RedirectToExternalList_Redirects_WhenFailure()
    {
        var useCase = new Mock<IGetApplicantUsersUseCase>();
        var model = new ExternalUserListModel { ReturnUrl = "url" };
        useCase.Setup(x => x.RetrieveListOfActiveExternalUsersAsync(
            It.IsAny<InternalUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ExternalUserListModel>("error"));

        var result = await _controller.RedirectToExternalList(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ListActionName, redirectResult.ActionName);
    }

    [Fact]
    public async Task AmendExternalUserAccount_Get_Redirects_WhenNotAccountAdministrator()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        var externalUser = _fixture.Create<AmendExternalUserAccountModel>();
        useCase.Setup(x => x.RetrieveExternalUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalUser);
        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.WoodlandOfficer);

        var result = await _controller.AmendExternalUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AmendExternalUserAccount_Get_Redirects_WhenFailure()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        useCase.Setup(x => x.RetrieveExternalUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AmendExternalUserAccountModel>("fail"));

        var result = await _controller.AmendExternalUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ListActionName, redirectResult.ActionName);
    }

    [Fact]
    public async Task AmendExternalUserAccount_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        useCase.Setup(x => x.RetrieveExternalUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<AmendExternalUserAccountModel>());

        var result = await _controller.AmendExternalUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task AmendExternalUserAccount_Post_ReturnsView_WhenModelStateInvalid()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        var model = new AmendExternalUserAccountModel();
        _controller.ModelState.AddModelError("Test", "Error");
        _validationProviderMock.Setup(x => x.ValidateExternalUserAccountSection(model, nameof(AmendExternalUserAccountModel), _controller.ModelState))
            .Returns(_fixture.Create<List<ValidationFailure>>());

        var result = await _controller.AmendExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task AmendExternalUserAccount_Post_Redirects_WhenNotAccountAdministrator()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        var model = new AmendExternalUserAccountModel();

        _validationProviderMock.Setup(x =>
                x.ValidateExternalUserAccountSection(It.IsAny<AmendExternalUserAccountModel>(), It.IsAny<string>(),
                    It.IsAny<ModelStateDictionary>()))
            .Returns([]);
        _controller.PrepareControllerForTest(_userId, true, AccountTypeInternal.AdminOfficer);

        var result = await _controller.AmendExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AmendExternalUserAccount_Post_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        var model = new AmendExternalUserAccountModel();
        useCase.Setup(x => x.UpdateExternalAccountDetailsAsync(It.IsAny<InternalUser>(), model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("error"));
        _validationProviderMock.Setup(x =>
                x.ValidateExternalUserAccountSection(It.IsAny<AmendExternalUserAccountModel>(), It.IsAny<string>(),
                    It.IsAny<ModelStateDictionary>()))
            .Returns([]);

        var result = await _controller.AmendExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AmendExternalUserAccount_Post_RedirectsToExternalUserList_WhenSuccess()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        var model = new AmendExternalUserAccountModel();
        useCase.Setup(x => x.UpdateExternalAccountDetailsAsync(It.IsAny<InternalUser>(), model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _validationProviderMock.Setup(x =>
                x.ValidateExternalUserAccountSection(It.IsAny<AmendExternalUserAccountModel>(), It.IsAny<string>(),
                    It.IsAny<ModelStateDictionary>()))
            .Returns([]);

        var result = await _controller.AmendExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ListActionName, redirectResult.ActionName);
    }

    [Fact]
    public async Task CloseExternalUserAccount_Get_Redirects_WhenNotAccountAdministratorOrSelf()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        useCase.Setup(x => x.RetrieveCloseExternalUserModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<CloseExternalUserModel>());

        var result = await _controller.CloseExternalUserAccount(_userId, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task CloseExternalUserAccount_Get_Redirects_WhenFailure()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        useCase.Setup(x => x.RetrieveCloseExternalUserModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CloseExternalUserModel>("fail"));

        var result = await _controller.CloseExternalUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ListActionName, redirectResult.ActionName);
    }

    [Fact]
    public async Task CloseExternalUserAccount_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        var model = _fixture.Create<CloseExternalUserModel>();
        useCase.Setup(x => x.RetrieveCloseExternalUserModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.CloseExternalUserAccount(Guid.NewGuid(), useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Theory]
    [CombinatorialData]
    public async Task CloseExternalUserAccount_Post_Redirects_WhenNotAccountAdministrator(AccountTypeInternal role)
    {
        if (role is AccountTypeInternal.AccountAdministrator) return;

        var useCase = new Mock<IAmendExternalUserUseCase>();
        var model = _fixture.Create<CloseExternalUserModel>();

        _controller.PrepareControllerForTest(_userId, role: role);
        var result = await _controller.CloseExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task CloseExternalUserAccount_Post_ValidatesWoodlandOwnerAndRedirects_WhenWoodlandOwnerIdNull()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        var externalUserModel = _fixture
            .Build<ExternalUserAccountModel>()
            .With(x => x.AccountType, AccountTypeExternal.WoodlandOwner)
            .Without(x => x.WoodlandOwnerId)
            .Create();
        var model = _fixture
            .Build<CloseExternalUserModel>()
            .With(x => x.AccountToClose, new ExternalUserModel
            {
                ExternalUser = externalUserModel
            })
            .Create();

        var result = await _controller.CloseExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Theory]
    [InlineData(AccountTypeExternal.Agent)]
    [InlineData(AccountTypeExternal.AgentAdministrator)]
    public async Task CloseExternalUserAccount_Post_ValidatesAgentAndRedirects_WhenValidationFailure(AccountTypeExternal type)
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        useCase.Setup(x => x.VerifyAgentCanBeClosedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var externalUserModel = _fixture
            .Build<ExternalUserAccountModel>()
            .With(x => x.AccountType, type)
            .Without(x => x.WoodlandOwnerId)
            .Create();
        var model = _fixture
            .Build<CloseExternalUserModel>()
            .With(x => x.AccountToClose, new ExternalUserModel
            {
                ExternalUser = externalUserModel,
                AgencyModel = _fixture.Create<AgencyModel>()
            })
            .Create();

        var result = await _controller.CloseExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ListActionName, redirectResult.ActionName);
    }

    [Theory]
    [InlineData(AccountTypeExternal.Agent)]
    [InlineData(AccountTypeExternal.AgentAdministrator)]
    public async Task CloseExternalUserAccount_Post_ValidatesAgentAndRedirects_WhenSuccess(AccountTypeExternal type)
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        useCase.Setup(x => x.VerifyAgentCanBeClosedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var externalUserModel = _fixture
            .Build<ExternalUserAccountModel>()
            .With(x => x.AccountType, type)
            .Without(x => x.WoodlandOwnerId)
            .Create();
        var model = _fixture
            .Build<CloseExternalUserModel>()
            .With(x => x.AccountToClose, new ExternalUserModel
            {
                ExternalUser = externalUserModel,
                AgencyModel = _fixture.Create<AgencyModel>()
            })
            .Create();

        var result = await _controller.CloseExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ListActionName, redirectResult.ActionName);
        useCase.Verify(x => x.VerifyAgentCanBeClosedAsync(externalUserModel.Id, model.AccountToClose.AgencyModel!.AgencyId!.Value, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(AccountTypeExternal.Agent)]
    [InlineData(AccountTypeExternal.AgentAdministrator)]
    public async Task CloseExternalUserAccount_Post_DoesNotValidateWhenAgencyModelNull(AccountTypeExternal type)
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        useCase.Setup(x => x.VerifyAgentCanBeClosedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var externalUserModel = _fixture
            .Build<ExternalUserAccountModel>()
            .With(x => x.AccountType, type)
            .Without(x => x.WoodlandOwnerId)
            .Create();
        var model = _fixture
            .Build<CloseExternalUserModel>()
            .With(x => x.AccountToClose, new ExternalUserModel
            {
                ExternalUser = externalUserModel
            })
            .Create();

        var result = await _controller.CloseExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ListActionName, redirectResult.ActionName);
        _validationProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CloseExternalUserAccount_Post_ValidatesWoodlandOwnerAndRedirects_WhenCannotBeClosed()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        var model = _fixture.Create<CloseExternalUserModel>();
        useCase.Setup(x => x.VerifyWoodlandOwnerCanBeClosedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.CloseExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ListActionName, redirectResult.ActionName);
    }

    [Fact]
    public async Task CloseExternalUserAccount_Post_RedirectsToError_WhenCloseAccountFailure()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        var externalUserModel = _fixture
            .Build<ExternalUserAccountModel>()
            .With(x => x.AccountType, AccountTypeExternal.WoodlandOwner).Create();
        var model = _fixture
            .Build<CloseExternalUserModel>()
            .With(x => x.AccountToClose, new ExternalUserModel
            {
                ExternalUser = externalUserModel
            })
            .Create();
        useCase.Setup(x => x.CloseExternalUserAccountAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("error"));
        useCase.Setup(x => x.VerifyWoodlandOwnerCanBeClosedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.CloseExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task CloseExternalUserAccount_Post_RedirectsToExternalUserList_WhenSuccess()
    {
        var useCase = new Mock<IAmendExternalUserUseCase>();
        var externalUserModel = _fixture
            .Build<ExternalUserAccountModel>()
            .With(x => x.AccountType, AccountTypeExternal.WoodlandOwner).Create();
        var model = _fixture
            .Build<CloseExternalUserModel>()
            .With(x => x.AccountToClose, new ExternalUserModel
            {
                ExternalUser = externalUserModel
            })
            .Create();
        useCase.Setup(x => x.CloseExternalUserAccountAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));


        useCase.Setup(x => x.VerifyWoodlandOwnerCanBeClosedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.CloseExternalUserAccount(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ListActionName, redirectResult.ActionName);
    }
}
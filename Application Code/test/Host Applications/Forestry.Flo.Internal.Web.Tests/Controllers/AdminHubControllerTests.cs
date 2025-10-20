using System.Security.Claims;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers;
using Forestry.Flo.Internal.Web.Models.AdminHub;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.AdminHub;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.AdminHubs.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers;

public class AdminHubControllerTests
{
    private readonly ClaimsPrincipal _user;
    private readonly ControllerContext _controllerContext;

    public AdminHubControllerTests()
    {
        _user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "TestUser") }));
        _controllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _user }
        };
    }

    private AdminHubController CreateController()
    {
        var controller = new AdminHubController();
        controller.ControllerContext = _controllerContext;
        var tempData = new TempDataDictionary(_controllerContext.HttpContext, Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;
        return controller;
    }

    [Fact]
    public async Task AdminHubSummary_ReturnsViewResult_WhenSuccess()
    {
        var mockUseCase = new Mock<IManageAdminHubUseCase>();
        var expectedModel = new ViewAdminHubModel();
        mockUseCase.Setup(x => x.RetrieveAdminHubDetailsAsync(It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedModel));

        var controller = CreateController();

        var result = await controller.AdminHubSummary(mockUseCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(expectedModel, viewResult.Model);
    }

    [Fact]
    public async Task AdminHubSummary_RedirectsToError_WhenFailure()
    {
        var mockUseCase = new Mock<IManageAdminHubUseCase>();
        mockUseCase.Setup(x => x.RetrieveAdminHubDetailsAsync(It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ViewAdminHubModel>("error"));

        var controller = CreateController();

        var result = await controller.AdminHubSummary(mockUseCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AddAdminOfficer_RedirectsWithConfirmation_WhenSuccess()
    {
        var mockUseCase = new Mock<IManageAdminHubUseCase>();
        mockUseCase.Setup(x => x.AddAdminOfficerAsync(It.IsAny<ViewAdminHubModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<ManageAdminHubOutcome>());

        var controller = CreateController();
        var model = new ViewAdminHubModel();

        var result = await controller.AddAdminOfficer(model, mockUseCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("AdminHubSummary", redirectResult.Url);
        Assert.Contains("Officer successfully added", controller.TempData["ConfirmationMessage"]?.ToString());
    }

    [Fact]
    public async Task AddAdminOfficer_RedirectsWithError_WhenFailure()
    {
        var mockUseCase = new Mock<IManageAdminHubUseCase>();
        mockUseCase.Setup(x => x.AddAdminOfficerAsync(It.IsAny<ViewAdminHubModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ManageAdminHubOutcome.InvalidAssignment);

        var controller = CreateController();
        var model = new ViewAdminHubModel();

        var result = await controller.AddAdminOfficer(model, mockUseCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("AdminHubSummary", redirectResult.Url);
        Assert.Contains("Something went wrong", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task RemoveAdminOfficer_RedirectsWithConfirmation_WhenSuccess()
    {
        var mockUseCase = new Mock<IManageAdminHubUseCase>();
        mockUseCase.Setup(x => x.RemoveAdminOfficerAsync(It.IsAny<ViewAdminHubModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<ManageAdminHubOutcome>());

        var controller = CreateController();
        var model = new ViewAdminHubModel();

        var result = await controller.RemoveAdminOfficer(model, mockUseCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("AdminHubSummary", redirectResult.Url);
        Assert.Contains("Officer removed", controller.TempData["ConfirmationMessage"]?.ToString());
    }

    [Fact]
    public async Task RemoveAdminOfficer_RedirectsWithError_WhenFailure()
    {
        var mockUseCase = new Mock<IManageAdminHubUseCase>();
        mockUseCase.Setup(x => x.RemoveAdminOfficerAsync(It.IsAny<ViewAdminHubModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ManageAdminHubOutcome.InvalidAssignment);

        var controller = CreateController();
        var model = new ViewAdminHubModel();

        var result = await controller.RemoveAdminOfficer(model, mockUseCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("AdminHubSummary", redirectResult.Url);
        Assert.Contains("Something went wrong", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task EditAdminHubDetails_RedirectsWithConfirmation_WhenSuccess()
    {
        var mockUseCase = new Mock<IManageAdminHubUseCase>();
        mockUseCase.Setup(x => x.EditAdminHub(It.IsAny<ViewAdminHubModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<ManageAdminHubOutcome>());

        var controller = CreateController();
        var model = new ViewAdminHubModel();

        var result = await controller.EditAdminHubDetails(model, mockUseCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("AdminHubSummary", redirectResult.Url);
        Assert.Contains("Admin Hub edited successful", controller.TempData["ConfirmationMessage"]?.ToString());
    }

    [Fact]
    public async Task EditAdminHubDetails_RedirectsWithError_WhenFailure_NoChangeSubmitted()
    {
        var mockUseCase = new Mock<IManageAdminHubUseCase>();
        mockUseCase.Setup(x => x.EditAdminHub(It.IsAny<ViewAdminHubModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ManageAdminHubOutcome.NoChangeSubmitted);

        var controller = CreateController();
        var model = new ViewAdminHubModel();

        var result = await controller.EditAdminHubDetails(model, mockUseCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("AdminHubSummary", redirectResult.Url);
        Assert.Contains("No changes were provided", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task EditAdminHubDetails_RedirectsWithError_WhenFailure_Other()
    {
        var mockUseCase = new Mock<IManageAdminHubUseCase>();
        mockUseCase.Setup(x => x.EditAdminHub(It.IsAny<ViewAdminHubModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ManageAdminHubOutcome.UpdateFailure);

        var controller = CreateController();
        var model = new ViewAdminHubModel();

        var result = await controller.EditAdminHubDetails(model, mockUseCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("AdminHubSummary", redirectResult.Url);
        Assert.Contains("Something went wrong", controller.TempData["ErrorMessage"]?.ToString());
    }
}
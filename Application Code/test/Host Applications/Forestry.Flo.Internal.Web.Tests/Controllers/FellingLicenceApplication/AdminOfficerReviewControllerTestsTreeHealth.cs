using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class AdminOfficerReviewControllerTestsTreeHealth
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IAdminOfficerTreeHealthCheckUseCase> _mockUseCase = new();
    private readonly AdminOfficerReviewController _controller;

    public AdminOfficerReviewControllerTestsTreeHealth()
    {
        _controller = new AdminOfficerReviewController(new Mock<IEnvironmentalImpactAssessmentAdminOfficerUseCase>().Object);
        _controller.PrepareControllerForTest(Guid.NewGuid());
    }


    [Theory, AutoMoqData]
    public async Task TreeHealthCheck_Get_ReturnsViewResultWithViewModel(
        Guid applicationId,
        CheckTreeHealthIssuesViewModel viewModel)
    {
        // Arrange
        _mockUseCase
            .Setup(x => x.GetTreeHealthCheckAdminOfficerViewModelAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(viewModel));
        // Act
        var result = await _controller.TreeHealthCheck(applicationId, _mockUseCase.Object, CancellationToken.None);
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
    }

    [Theory, AutoMoqData]
    public async Task TreeHealthCheck_Get_FailureRedirectsToError(
        Guid applicationId)
    {
        // Arrange
        _mockUseCase
            .Setup(x => x.GetTreeHealthCheckAdminOfficerViewModelAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CheckTreeHealthIssuesViewModel>("Failure"));
        // Act
        var result = await _controller.TreeHealthCheck(applicationId, _mockUseCase.Object, CancellationToken.None);
        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Theory, AutoMoqData]
    public async Task TreeHealthCheck_Post_InvalidModel_ReturnsViewResultWithViewModel(
        CheckTreeHealthIssuesViewModel model,
        CheckTreeHealthIssuesViewModel viewModel)
    {
        // Arrange
        model.Confirmed = false;
        _mockUseCase
            .Setup(x => x.GetTreeHealthCheckAdminOfficerViewModelAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(viewModel));
        // Act
        var result = await _controller.TreeHealthCheck(model, _mockUseCase.Object, CancellationToken.None);
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Theory, AutoMoqData]
    public async Task TreeHealthCheck_Post_ValidModel_RedirectsToTaskList(
        CheckTreeHealthIssuesViewModel model)
    {
        // Arrange
        model.Confirmed = true;
        _mockUseCase
            .Setup(x => x.ConfirmTreeHealthCheckedAsync(model.ApplicationId, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        // Act
        var result = await _controller.TreeHealthCheck(model, _mockUseCase.Object, CancellationToken.None);
        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("AdminOfficerReview", redirectResult.ControllerName);
    }

    [Theory, AutoMoqData]
    public async Task TreeHealthCheck_Post_ConfirmTreeHealthCheckedAsyncFailure_RedirectsToError(
        CheckTreeHealthIssuesViewModel model)
    {
        // Arrange
        model.Confirmed = true;
        _mockUseCase
            .Setup(x => x.ConfirmTreeHealthCheckedAsync(model.ApplicationId, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Failure"));
        // Act
        var result = await _controller.TreeHealthCheck(model, _mockUseCase.Object, CancellationToken.None);
        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }
}
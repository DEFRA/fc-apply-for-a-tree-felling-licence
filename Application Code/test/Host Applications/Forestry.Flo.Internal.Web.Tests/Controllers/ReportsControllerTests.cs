using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.Reports;
using Moq;
using FluentValidation;
using Forestry.Flo.Internal.Web.Controllers;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Internal.Web.Tests.Controllers;

public class ReportsControllerTests
{
    private ReportsController _controller;
    private readonly Guid _requestContextUserId = Guid.NewGuid();

    public ReportsControllerTests()
    {
        _controller = new ReportsController(new NullLogger<ReportsController>());
        _controller.PrepareControllerForTest(_requestContextUserId);
    }

    [Fact]
    public void Index_ReturnsViewResultWithBreadcrumbs()
    {
        // Act
        var result = _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult);
        Assert.NotNull(_controller.ViewBag.Breadcrumbs);
    }

    [Fact]
    public async Task FellingLicenceApplicationsDataReport_ReturnsViewResult_WhenReferenceModelSuccess()
    {
        // Arrange
        var mockUseCase = new Mock<IGenerateReportUseCase>();
        var expectedModel = new ReportRequestViewModel();
        mockUseCase.Setup(x => x.GetReferenceModelAsync(
            It.IsAny<InternalUser>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedModel));

        // Act
        var result = await _controller.FellingLicenceApplicationsDataReport(mockUseCase.Object, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(expectedModel, viewResult.Model);
    }

    [Fact]
    public async Task FellingLicenceApplicationsDataReport_RedirectsToError_WhenReferenceModelFailure()
    {
        // Arrange
        var mockUseCase = new Mock<IGenerateReportUseCase>();
        mockUseCase.Setup(x => x.GetReferenceModelAsync(
            It.IsAny<InternalUser>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ReportRequestViewModel>("error"));

        // Act
        var result = await _controller.FellingLicenceApplicationsDataReport(mockUseCase.Object, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task SubmitFellingLicenceApplicationsDataReport_ReturnsFileStreamResult_WhenReportSuccess()
    {
        // Arrange
        var mockUseCase = new Mock<IGenerateReportUseCase>();
        var mockValidator = new Mock<IValidator<ReportRequestViewModel>>();
        var referenceModel = new ReportRequestViewModel();
        mockUseCase.Setup(x => x.GetReferenceModelAsync(
            It.IsAny<InternalUser>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(referenceModel));
        mockValidator.Setup(x => x.Validate(It.IsAny<ReportRequestViewModel>()))
            .Returns(new FluentValidation.Results.ValidationResult());
        mockUseCase.Setup(x => x.GenerateReportAsync(
            It.IsAny<ReportRequestViewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileStreamResult(new MemoryStream(), "application/octet-stream"));

        var viewModel = new ReportRequestViewModel();

        // Act
        var result = await _controller.SubmitFellingLicenceApplicationsDataReport(
            viewModel, mockUseCase.Object, mockValidator.Object, CancellationToken.None);

        // Assert
        Assert.IsType<FileStreamResult>(result);
    }

    [Fact]
    public async Task SubmitFellingLicenceApplicationsDataReport_ReturnsViewWithMessage_WhenReportSuccessButNoFile()
    {
        // Arrange
        var mockUseCase = new Mock<IGenerateReportUseCase>();
        var mockValidator = new Mock<IValidator<ReportRequestViewModel>>();
        var referenceModel = new ReportRequestViewModel();
        mockUseCase.Setup(x => x.GetReferenceModelAsync(
            It.IsAny<InternalUser>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(referenceModel));
        mockValidator.Setup(x => x.Validate(It.IsAny<ReportRequestViewModel>()))
            .Returns(new FluentValidation.Results.ValidationResult());
        mockUseCase.Setup(x => x.GenerateReportAsync(
            It.IsAny<ReportRequestViewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmptyResult());

        var viewModel = new ReportRequestViewModel();

        // Act
        var result = await _controller.SubmitFellingLicenceApplicationsDataReport(
            viewModel, mockUseCase.Object, mockValidator.Object, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("FellingLicenceApplicationsDataReport", viewResult.ViewName);
        Assert.Equal(viewModel, viewResult.Model);
    }

    [Fact]
    public async Task SubmitFellingLicenceApplicationsDataReport_ReturnsViewWithError_WhenReportFailure()
    {
        // Arrange
        var mockUseCase = new Mock<IGenerateReportUseCase>();
        var mockValidator = new Mock<IValidator<ReportRequestViewModel>>();
        var referenceModel = new ReportRequestViewModel();
        mockUseCase.Setup(x => x.GetReferenceModelAsync(
            It.IsAny<InternalUser>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(referenceModel));
        mockValidator.Setup(x => x.Validate(It.IsAny<ReportRequestViewModel>()))
            .Returns(new FluentValidation.Results.ValidationResult());
        mockUseCase.Setup(x => x.GenerateReportAsync(
            It.IsAny<ReportRequestViewModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IActionResult>("failure"));

        var viewModel = new ReportRequestViewModel();

        // Act
        var result = await _controller.SubmitFellingLicenceApplicationsDataReport(
            viewModel, mockUseCase.Object, mockValidator.Object, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("FellingLicenceApplicationsDataReport", viewResult.ViewName);
        Assert.Equal(viewModel, viewResult.Model);
    }

    [Fact]
    public async Task SubmitFellingLicenceApplicationsDataReport_ReturnsView_WhenReferenceModelFailure()
    {
        // Arrange
        var mockUseCase = new Mock<IGenerateReportUseCase>();
        var mockValidator = new Mock<IValidator<ReportRequestViewModel>>();
        mockUseCase.Setup(x => x.GetReferenceModelAsync(
            It.IsAny<InternalUser>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ReportRequestViewModel>("error"));

        var viewModel = new ReportRequestViewModel();

        // Act
        var result = await _controller.SubmitFellingLicenceApplicationsDataReport(
            viewModel, mockUseCase.Object, mockValidator.Object, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task SubmitFellingLicenceApplicationsDataReport_ReturnsView_WhenModelStateInvalid()
    {
        // Arrange
        var mockUseCase = new Mock<IGenerateReportUseCase>();
        var mockValidator = new Mock<IValidator<ReportRequestViewModel>>();
        var referenceModel = new ReportRequestViewModel();
        mockUseCase.Setup(x => x.GetReferenceModelAsync(
            It.IsAny<InternalUser>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(referenceModel));
        var validationResult = new FluentValidation.Results.ValidationResult(
            [new FluentValidation.Results.ValidationFailure("Test", "Error")]);
        mockValidator.Setup(x => x.Validate(It.IsAny<ReportRequestViewModel>()))
            .Returns(validationResult);

        var viewModel = new ReportRequestViewModel();

        // Act
        var result = await _controller.SubmitFellingLicenceApplicationsDataReport(
            viewModel, mockUseCase.Object, mockValidator.Object, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("FellingLicenceApplicationsDataReport", viewResult.ViewName);
        Assert.Equal(viewModel, viewResult.Model);
        Assert.False(_controller.ModelState.IsValid);
    }
    [Fact]
    public async Task SubmitFellingLicenceApplicationsDataReport_LogsErrorAndRedirects_WhenReferenceModelFailure()
    {
        // Arrange
        var mockUseCase = new Mock<IGenerateReportUseCase>();
        var mockValidator = new Mock<IValidator<ReportRequestViewModel>>();
        var logger = new Mock<ILogger<ReportsController>>();
        var controller = new ReportsController(logger.Object);
        controller.PrepareControllerForTest(_requestContextUserId);

        mockUseCase.Setup(x => x.GetReferenceModelAsync(
            It.IsAny<InternalUser>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ReportRequestViewModel>("error"));

        var viewModel = new ReportRequestViewModel();

        // Act
        var result = await controller.SubmitFellingLicenceApplicationsDataReport(
            viewModel, mockUseCase.Object, mockValidator.Object, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Could not produce view model for report UI error is [error]")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
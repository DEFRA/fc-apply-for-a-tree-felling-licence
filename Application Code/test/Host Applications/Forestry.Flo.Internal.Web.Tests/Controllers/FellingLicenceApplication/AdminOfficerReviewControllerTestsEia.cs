using AutoFixture;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.ValueTasks;
using FluentValidation;
using FluentValidation.Results;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class AdminOfficerReviewControllerTestsEia
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IEnvironmentalImpactAssessmentAdminOfficerUseCase> _eiaUseCaseMock = new();
    private readonly Mock<IValidator<EiaWithFormsPresentViewModel>> _presentValidatorMock = new();
    private readonly Mock<IValidator<EiaWithFormsAbsentViewModel>> _absentValidatorMock = new();
    private readonly AdminOfficerReviewController _controller;

    public AdminOfficerReviewControllerTestsEia()
    {
        _controller = new AdminOfficerReviewController(_eiaUseCaseMock.Object);
        _controller.PrepareControllerForTest(Guid.NewGuid());
    }

    [Fact]
    public async Task EiaCheck_RedirectsToError_WhenFailure()
    {
        _eiaUseCaseMock.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EnvironmentalImpactAssessmentModel>("fail"));

        var result = await _controller.EiaCheck(Guid.NewGuid(), CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaCheck_CallsEiaWithFormsPresent_WhenDocumentsPresent()
    {
        var eiaModel = _fixture.Build<EnvironmentalImpactAssessmentModel>()
            .With(x => x.EiaDocuments, new List<DocumentModel> { new DocumentModel() })
            .Create();
        _eiaUseCaseMock.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(eiaModel));
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<FellingLicenceApplicationSummaryModel>()));

        var result = await _controller.EiaCheck(Guid.NewGuid(), CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("EiaWithFormsPresent", viewResult.ViewName);
        Assert.IsType<EiaWithFormsPresentViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task EiaCheck_CallsEiaWithFormsAbsent_WhenNoDocuments()
    {
        var eiaModel = _fixture.Build<EnvironmentalImpactAssessmentModel>()
            .With(x => x.EiaDocuments, new List<DocumentModel>())
            .Create();
        _eiaUseCaseMock.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(eiaModel));
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<FellingLicenceApplicationSummaryModel>()));

        var result = await _controller.EiaCheck(Guid.NewGuid(), CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("EiaWithFormsAbsent", viewResult.ViewName);
        Assert.IsType<EiaWithFormsAbsentViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task EiaWithFormsAbsent_RedirectsToError_WhenSummaryFailure()
    {
        var eiaModel = _fixture.Create<EnvironmentalImpactAssessmentModel>();
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplicationSummaryModel>("fail"));

        var result = await _controller.EiaWithFormsAbsent(eiaModel, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaWithFormsAbsent_ReturnsView_WhenSuccess()
    {
        var eiaModel = _fixture.Create<EnvironmentalImpactAssessmentModel>();
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<FellingLicenceApplicationSummaryModel>()));

        var result = await _controller.EiaWithFormsAbsent(eiaModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("EiaWithFormsAbsent", viewResult.ViewName);
        Assert.IsType<EiaWithFormsAbsentViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task EiaWithFormsPresent_RedirectsToError_WhenSummaryFailure()
    {
        var eiaModel = _fixture.Create<EnvironmentalImpactAssessmentModel>();
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplicationSummaryModel>("fail"));

        var result = await _controller.EiaWithFormsPresent(eiaModel, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaWithFormsPresent_ReturnsView_WhenSuccess()
    {
        var eiaModel = _fixture.Create<EnvironmentalImpactAssessmentModel>();
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<FellingLicenceApplicationSummaryModel>()));

        var result = await _controller.EiaWithFormsPresent(eiaModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("EiaWithFormsPresent", viewResult.ViewName);
        Assert.IsType<EiaWithFormsPresentViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task EiaWithFormsPresent_Post_ReturnsView_WhenModelStateInvalid()
    {
        var model = _fixture.Create<EiaWithFormsPresentViewModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _presentValidatorMock.Setup(x => x.Validate(model)).Returns(_fixture.Create<ValidationResult>());
        var eiaModel = _fixture.Create<EnvironmentalImpactAssessmentModel>();
        _eiaUseCaseMock.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(eiaModel));
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(eiaModel.FellingLicenceApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<FellingLicenceApplicationSummaryModel>()));

        var result = await _controller.EiaWithFormsPresent(model, _presentValidatorMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task EiaWithFormsPresent_Post_RedirectsToError_WhenEiaModelFailure()
    {
        var model = _fixture.Create<EiaWithFormsPresentViewModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _presentValidatorMock.Setup(x => x.Validate(model)).Returns(_fixture.Create<ValidationResult>());
        _eiaUseCaseMock.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EnvironmentalImpactAssessmentModel>("fail"));

        var result = await _controller.EiaWithFormsPresent(model, _presentValidatorMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaWithFormsPresent_Post_RedirectsToError_WhenSummaryFailure()
    {
        var model = _fixture.Create<EiaWithFormsPresentViewModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _presentValidatorMock.Setup(x => x.Validate(model)).Returns(_fixture.Create<ValidationResult>());
        var eiaModel = _fixture.Create<EnvironmentalImpactAssessmentModel>();
        _eiaUseCaseMock.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(eiaModel));
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(eiaModel.FellingLicenceApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplicationSummaryModel>("fail"));

        var result = await _controller.EiaWithFormsPresent(model, _presentValidatorMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaWithFormsPresent_Post_RedirectsToError_WhenConfirmFailure()
    {
        var model = _fixture.Create<EiaWithFormsPresentViewModel>();
        _presentValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _eiaUseCaseMock.Setup(x => x.ConfirmAttachedEiaFormsAreCorrectAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.EiaWithFormsPresent(model, _presentValidatorMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaWithFormsPresent_Post_RedirectsToIndex_WhenSuccess()
    {
        var model = _fixture.Create<EiaWithFormsPresentViewModel>();
        _presentValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _eiaUseCaseMock.Setup(x => x.ConfirmAttachedEiaFormsAreCorrectAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.EiaWithFormsPresent(model, _presentValidatorMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task EiaWithFormsAbsent_Post_ReturnsView_WhenModelStateInvalid()
    {
        var model = _fixture.Create<EiaWithFormsAbsentViewModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _absentValidatorMock.Setup(x => x.Validate(model)).Returns(_fixture.Create<ValidationResult>());
        var eiaModel = _fixture.Create<EnvironmentalImpactAssessmentModel>();
        _eiaUseCaseMock.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(eiaModel));
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(eiaModel.FellingLicenceApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<FellingLicenceApplicationSummaryModel>()));

        var result = await _controller.EiaWithFormsAbsent(model, _absentValidatorMock.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task EiaWithFormsAbsent_Post_RedirectsToErrorWhenRetrievalFailure()
    {
        var model = _fixture.Create<EiaWithFormsAbsentViewModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _absentValidatorMock.Setup(x => x.Validate(model)).Returns(_fixture.Create<ValidationResult>());
        var eiaModel = _fixture.Create<EnvironmentalImpactAssessmentModel>();
        _eiaUseCaseMock.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EnvironmentalImpactAssessmentModel>("fail"));
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(eiaModel.FellingLicenceApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<FellingLicenceApplicationSummaryModel>()));

        var result = await _controller.EiaWithFormsAbsent(model, _absentValidatorMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaWithFormsAbsent_Post_RedirectsToError_WhenEiaModelFailure()
    {
        var model = _fixture.Create<EiaWithFormsAbsentViewModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _absentValidatorMock.Setup(x => x.Validate(model)).Returns(_fixture.Create<ValidationResult>());
        _eiaUseCaseMock.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EnvironmentalImpactAssessmentModel>("fail"));

        var result = await _controller.EiaWithFormsAbsent(model, _absentValidatorMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaWithFormsAbsent_Post_RedirectsToError_WhenSummaryFailure()
    {
        var model = _fixture.Create<EiaWithFormsAbsentViewModel>();
        _controller.ModelState.AddModelError("Test", "Error");
        _absentValidatorMock.Setup(x => x.Validate(model)).Returns(_fixture.Create<ValidationResult>());
        var eiaModel = _fixture.Create<EnvironmentalImpactAssessmentModel>();
        _eiaUseCaseMock.Setup(x => x.GetEnvironmentalImpactAssessmentAsync(model.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(eiaModel));
        _eiaUseCaseMock.Setup(x => x.GetSummaryModel(eiaModel.FellingLicenceApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplicationSummaryModel>("fail"));

        var result = await _controller.EiaWithFormsAbsent(model, _absentValidatorMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaWithFormsAbsent_Post_RedirectsToError_WhenConfirmFailure()
    {
        var model = _fixture.Create<EiaWithFormsAbsentViewModel>();
        _absentValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _eiaUseCaseMock.Setup(x => x.ConfirmEiaFormsHaveBeenReceivedAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.EiaWithFormsAbsent(model, _absentValidatorMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task EiaWithFormsAbsent_Post_RedirectsToIndex_WhenSuccess()
    {
        var model = _fixture.Create<EiaWithFormsAbsentViewModel>();
        _absentValidatorMock.Setup(x => x.Validate(model)).Returns(new FluentValidation.Results.ValidationResult());
        _eiaUseCaseMock.Setup(x => x.ConfirmEiaFormsHaveBeenReceivedAsync(model, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.EiaWithFormsAbsent(model, _absentValidatorMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(model.ApplicationId, redirect.RouteValues["id"]);
    }
}
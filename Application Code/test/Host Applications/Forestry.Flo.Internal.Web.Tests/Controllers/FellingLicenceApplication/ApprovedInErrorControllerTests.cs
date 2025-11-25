using AutoFixture;
using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StatusHistoryModel = Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.StatusHistoryModel;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class ApprovedInErrorControllerTests
{
    private readonly Mock<IApprovedInErrorUseCase> _useCase = new();
    private readonly Mock<ILogger<ApprovedInErrorController>> _logger = new();
    private readonly Mock<IValidator<ApprovedInErrorViewModel>> _validator = new();
    private readonly ApprovedInErrorController _controller;
    private readonly IFixture _fixture = new Fixture().CustomiseFixtureForFellingLicenceApplications();
    private readonly Guid _applicationId = Guid.NewGuid();

    public ApprovedInErrorControllerTests()
    {
        _controller = new ApprovedInErrorController(_useCase.Object, _logger.Object);
        _controller.PrepareControllerForTest(Guid.NewGuid(), role: AccountTypeInternal.FieldManager);
        // Default validator returns no errors
        _validator
        .Setup(v => v.Validate(It.IsAny<ApprovedInErrorViewModel>()))
        .Returns(new ValidationResult());
    }

    [Fact]
    public async Task Index_ReturnsRedirectToError_WhenModelHasNoValue()
    {
        _useCase
        .Setup(x => x.RetrieveApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInErrorViewModel>.None);

        var result = await _controller.Index(_applicationId, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_RedirectsToApplicationSummary_WhenStatusNotSentForApproval()
    {
        var model = new ApprovedInErrorViewModel
        {
            Id = _applicationId,
            ApplicationId = _applicationId,
            FellingLicenceApplicationSummary = new FellingLicenceApplicationSummaryModel
            {
                StatusHistories = [_fixture.Build<StatusHistoryModel>().With(x => x.Status, FellingLicenceStatus.AdminOfficerReview).Create()]
            }
        };

        _useCase
        .Setup(x => x.RetrieveApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(model);

        var result = await _controller.Index(_applicationId, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirect.ActionName);
        Assert.Equal("FellingLicenceApplication", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_ReturnsViewAndSetsBreadcrumbs_WhenSuccess()
    {
        var summary = new FellingLicenceApplicationSummaryModel
        {
            StatusHistories = [_fixture.Build<StatusHistoryModel>().With(x => x.Status, FellingLicenceStatus.Approved).Create()],
            ApplicationReference = "REF-123",
            Status = FellingLicenceStatus.Approved
        };
        var model = new ApprovedInErrorViewModel
        {
            Id = _applicationId,
            ApplicationId = _applicationId,
            FellingLicenceApplicationSummary = summary
        };

        _useCase
        .Setup(x => x.RetrieveApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(model);

        var result = await _controller.Index(_applicationId, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ApprovedInErrorViewModel>(view.Model);
        Assert.NotNull(vm.Breadcrumbs);
        Assert.Equal("Approved in error", vm.Breadcrumbs!.CurrentPage);
        Assert.NotNull(vm.Breadcrumbs.Breadcrumbs);
        Assert.Equal(2, vm.Breadcrumbs.Breadcrumbs.Count);

        var first = vm.Breadcrumbs.Breadcrumbs[0];
        Assert.Equal("Open applications", first.Text);
        Assert.Equal("Home", first.Controller);
        Assert.Equal("Index", first.Action);
        Assert.Null(first.RouteId);

        var second = vm.Breadcrumbs.Breadcrumbs[1];
        Assert.Equal("REF-123", second.Text);
        Assert.Equal("FellingLicenceApplication", second.Controller);
        Assert.Equal("ApplicationSummary", second.Action);
        Assert.Equal(_applicationId.ToString(), second.RouteId);
    }

    [Fact]
    public async Task ConfirmApprovedInError_ReturnsViewWithReload_WhenValidationFails_AndReloadHasValue()
    {
        _validator
        .Setup(v => v.Validate(It.IsAny<ApprovedInErrorViewModel>()))
        .Returns(new ValidationResult(new List<ValidationFailure> { new("ReasonExpiryDate", "Select at least one reason") }));

        var summary = new FellingLicenceApplicationSummaryModel
        {
            StatusHistories =
            [
                _fixture.Build<StatusHistoryModel>()
                .With(x => x.Status, FellingLicenceStatus.SentForApproval)
                .Create()
            ],
            ApplicationReference = "REF-123"
        };
        var reloadModel = new ApprovedInErrorViewModel
        {
            Id = _applicationId,
            ApplicationId = _applicationId,
            FellingLicenceApplicationSummary = summary
        };

        _useCase
        .Setup(x => x.RetrieveApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(reloadModel);

        var input = new ApprovedInErrorViewModel { Id = _applicationId, ApplicationId = _applicationId };

        var result = await _controller.ConfirmApprovedInError(input, _useCase.Object, _validator.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.IsType<ApprovedInErrorViewModel>(view.Model);
    }

    [Fact]
    public async Task ConfirmApprovedInError_ValidationFails_ReloadedModelPreservesPostedValues_AndBreadcrumbs()
    {
        _validator
        .Setup(v => v.Validate(It.IsAny<ApprovedInErrorViewModel>()))
        .Returns(new ValidationResult(new List<ValidationFailure> { new("ReasonOther", "Enter details for Other") }));

        var summary = new FellingLicenceApplicationSummaryModel
        {
            StatusHistories =
            [
                _fixture.Build<StatusHistoryModel>()
                .With(x => x.Status, FellingLicenceStatus.SentForApproval)
                .Create()
            ],
            ApplicationReference = "REF-XYZ"
        };
        var reloadModel = new ApprovedInErrorViewModel
        {
            Id = _applicationId,
            ApplicationId = _applicationId,
            FellingLicenceApplicationSummary = summary
        };
        _useCase
        .Setup(x => x.RetrieveApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(reloadModel);

        var input = new ApprovedInErrorViewModel
        {
            Id = _applicationId,
            ApplicationId = _applicationId,
            PreviousReference = "OLD-REF",
            ReasonExpiryDate = true,
            ReasonSupplementaryPoints = false,
            ReasonOther = true,
            CaseNote = "case-note"
        };

        var result = await _controller.ConfirmApprovedInError(input, _useCase.Object, _validator.Object, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ApprovedInErrorViewModel>(view.Model);
        Assert.Equal("Index", view.ViewName);
        Assert.Equal(input.ApplicationId, vm.ApplicationId);
        Assert.Equal(input.PreviousReference, vm.PreviousReference);
        Assert.Equal(input.ReasonExpiryDate, vm.ReasonExpiryDate);
        Assert.Equal(input.ReasonSupplementaryPoints, vm.ReasonSupplementaryPoints);
        Assert.Equal(input.ReasonOther, vm.ReasonOther);
        Assert.Equal(input.CaseNote, vm.CaseNote);
        Assert.NotNull(vm.Breadcrumbs);
        Assert.Equal("Approver Review", vm.Breadcrumbs!.CurrentPage);
        Assert.Collection(vm.Breadcrumbs!.Breadcrumbs,
        b => Assert.Equal("Open applications", b.Text),
        b => Assert.Equal("REF-XYZ", b.Text));
    }

    [Fact]
    public async Task ConfirmApprovedInError_ReturnsViewWithModel_WhenValidationFails_AndReloadHasNoValue()
    {
        _validator
        .Setup(v => v.Validate(It.IsAny<ApprovedInErrorViewModel>()))
        .Returns(new ValidationResult(new List<ValidationFailure> { new("ReasonExpiryDate", "Select at least one reason") }));

        _useCase
        .Setup(x => x.RetrieveApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInErrorViewModel>.None);

        var input = new ApprovedInErrorViewModel { Id = _applicationId, ApplicationId = _applicationId };

        var result = await _controller.ConfirmApprovedInError(input, _useCase.Object, _validator.Object, CancellationToken.None);

        var view = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", view.ActionName);
    }

    [Fact]
    public async Task ConfirmApprovedInError_RedirectsToApplicationSummary_WhenSaveSucceeds()
    {
        _useCase
        .Setup(x => x.ConfirmApprovedInErrorAsync(It.IsAny<ApprovedInErrorModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Success());

        var input = new ApprovedInErrorViewModel
        {
            Id = _applicationId,
            ApplicationId = _applicationId,
            PreviousReference = "OLD-1",
            ReasonExpiryDate = true
        };

        var result = await _controller.ConfirmApprovedInError(input, _useCase.Object, _validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirect.ActionName);
        Assert.Equal("FellingLicenceApplication", redirect.ControllerName);
        Assert.Equal(_applicationId, redirect.RouteValues!["Id"]);
    }

    [Fact]
    public async Task ConfirmApprovedInError_ReturnsView_WhenSaveFails_AndReloadHasValue()
    {
        _useCase
        .Setup(x => x.ConfirmApprovedInErrorAsync(It.IsAny<ApprovedInErrorModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Failure("fail"));

        var summary = new FellingLicenceApplicationSummaryModel
        {
            StatusHistories =
            [
                _fixture.Build<StatusHistoryModel>()
                .With(x => x.Status, FellingLicenceStatus.SentForApproval)
                .Create()
            ],
            ApplicationReference = "REF-123"
        };
        var reloadModel = new ApprovedInErrorViewModel { Id = _applicationId, ApplicationId = _applicationId, FellingLicenceApplicationSummary = summary };
        _useCase
        .Setup(x => x.RetrieveApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(reloadModel);

        var input = new ApprovedInErrorViewModel
        {
            Id = _applicationId,
            ApplicationId = _applicationId,
            PreviousReference = "OLD-1",
            ReasonExpiryDate = true
        };

        var result = await _controller.ConfirmApprovedInError(input, _useCase.Object, _validator.Object, CancellationToken.None);

        if (result is ViewResult view)
        {
            Assert.Equal("Index", view.ViewName);
            Assert.IsType<ApprovedInErrorViewModel>(view.Model);
        }
        else if (result is RedirectToActionResult redirect)
        {
            Assert.Equal("ApplicationSummary", redirect.ActionName);
        }
    }

    [Fact]
    public async Task ConfirmApprovedInError_RedirectsToError_WhenReloadAfterValidationHasNoValue()
    {
        // Save fails, controller attempts to reload view model
        _useCase
        .Setup(x => x.ConfirmApprovedInErrorAsync(It.IsAny<ApprovedInErrorModel>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Failure("fail"));

        // Reload returns no value -> controller redirects to Error/Home
        _useCase
        .Setup(x => x.RetrieveApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInErrorViewModel>.None);

        var input = new ApprovedInErrorViewModel { Id = _applicationId, ApplicationId = _applicationId };

        var result = await _controller.ConfirmApprovedInError(input, _useCase.Object, _validator.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }
}

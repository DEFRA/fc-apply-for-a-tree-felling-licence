using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class ExternalConsulteeInviteControllerTests
{
    private readonly Mock<IExternalConsulteeInviteUseCase> _useCaseMock;
    private readonly ExternalConsulteeInviteController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    public ExternalConsulteeInviteControllerTests()
    {
        _useCaseMock = new Mock<IExternalConsulteeInviteUseCase>();
        _controller = new ExternalConsulteeInviteController();
        _controller.PrepareControllerForTest(Guid.NewGuid());
    }

    [Fact]
    public async Task Index_Get_ReturnsView_WhenSuccess()
    {
        var summary = new FellingLicenceApplicationSummaryModel { Id = _applicationId, ApplicationReference = "REF" };
        var viewModel = new ExternalConsulteeIndexViewModel { FellingLicenceApplicationSummary = summary };
        _useCaseMock.Setup(x => x.GetConsulteeInvitesIndexViewModelAsync(_applicationId, _cancellationToken))
            .ReturnsAsync(Result.Success(viewModel));

        var result = await _controller.Index(_applicationId, _useCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        Assert.NotNull(viewModel.Breadcrumbs);
    }

    [Fact]
    public async Task Index_Get_RedirectsToError_WhenFailure()
    {
        _useCaseMock.Setup(x => x.GetConsulteeInvitesIndexViewModelAsync(_applicationId, _cancellationToken))
            .ReturnsAsync(Result.Failure<ExternalConsulteeIndexViewModel>("fail"));

        var result = await _controller.Index(_applicationId, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_Post_ReturnsView_WhenModelStateInvalid()
    {
        var summary = new FellingLicenceApplicationSummaryModel { Id = _applicationId, ApplicationReference = "REF" };
        var viewModel = new ExternalConsulteeIndexViewModel { FellingLicenceApplicationSummary = summary };
        _controller.ModelState.AddModelError("Test", "Error");
        _useCaseMock.Setup(x => x.GetConsulteeInvitesIndexViewModelAsync(_applicationId, _cancellationToken))
            .ReturnsAsync(Result.Success(viewModel));

        var model = new ExternalConsulteeIndexViewModel { ApplicationId = _applicationId };
        var result = await _controller.Index(model, _useCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        Assert.NotNull(viewModel.Breadcrumbs);
    }

    [Fact]
    public async Task Index_Post_RedirectsToError_WhenGetModelFailure()
    {
        _controller.ModelState.AddModelError("Test", "Error");
        _useCaseMock.Setup(x => x.GetConsulteeInvitesIndexViewModelAsync(_applicationId, _cancellationToken))
            .ReturnsAsync(Result.Failure<ExternalConsulteeIndexViewModel>("fail"));

        var model = new ExternalConsulteeIndexViewModel { ApplicationId = _applicationId };
        var result = await _controller.Index(model, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_Post_RedirectsToError_WhenSetNotNeededFailure()
    {
        var model = new ExternalConsulteeIndexViewModel { ApplicationId = _applicationId, ApplicationNeedsConsultations = false };
        _useCaseMock.Setup(x => x.SetDoesNotRequireConsultationsAsync(_applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.Index(model, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task Index_Post_RedirectsToReview_WhenSetNotNeededSuccess()
    {
        var model = new ExternalConsulteeIndexViewModel { ApplicationId = _applicationId, ApplicationNeedsConsultations = false };
        _useCaseMock.Setup(x => x.SetDoesNotRequireConsultationsAsync(_applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.Index(model, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("WoodlandOfficerReview", redirect.ControllerName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task Index_Post_RedirectsToError_WhenSetCompleteFailure()
    {
        var model = new ExternalConsulteeIndexViewModel { ApplicationId = _applicationId, ApplicationNeedsConsultations = true };
        _useCaseMock.Setup(x => x.SetConsultationsCompleteAsync(_applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.Index(model, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task Index_Post_RedirectsToReview_WhenSetCompleteSuccess()
    {
        var model = new ExternalConsulteeIndexViewModel { ApplicationId = _applicationId, ApplicationNeedsConsultations = true };
        _useCaseMock.Setup(x => x.SetConsultationsCompleteAsync(_applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.Index(model, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("WoodlandOfficerReview", redirect.ControllerName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task InviteNewConsultee_Get_ReturnsView_WhenSuccess()
    {
        var summary = new FellingLicenceApplicationSummaryModel { Id = _applicationId, ApplicationReference = "REF" };
        var model = new ExternalConsulteeInviteFormModel { FellingLicenceApplicationSummary = summary };
        _useCaseMock.Setup(x => x.GetNewExternalConsulteeInviteViewModelAsync(_applicationId, _cancellationToken))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.InviteNewConsultee(_applicationId, _useCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
        Assert.NotNull(model.Breadcrumbs);
    }

    [Fact]
    public async Task InviteNewConsultee_Get_RedirectsToError_WhenFailure()
    {
        _useCaseMock.Setup(x => x.GetNewExternalConsulteeInviteViewModelAsync(_applicationId, _cancellationToken))
            .ReturnsAsync(Result.Failure<ExternalConsulteeInviteFormModel>("fail"));

        var result = await _controller.InviteNewConsultee(_applicationId, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task InviteNewConsultee_Post_ReturnsView_WhenModelStateInvalid()
    {
        var summary = new FellingLicenceApplicationSummaryModel { Id = _applicationId, ApplicationReference = "REF" };
        var reloadModel = Result.Success(new ExternalConsulteeInviteFormModel { FellingLicenceApplicationSummary = summary });
        _controller.ModelState.AddModelError("Test", "Error");
        _useCaseMock.Setup(x => x.GetNewExternalConsulteeInviteViewModelAsync(_applicationId, _cancellationToken))
            .ReturnsAsync(reloadModel);

        var model = new ExternalConsulteeInviteFormModel
        {
            ApplicationId = _applicationId,
            ConsulteeName = "Name",
            Email = "email@test.com",
            Purpose = ExternalConsulteeInvitePurpose.Mandatory,
            AreaOfFocus = "Area",
            SelectedDocumentIds = new List<Guid?> { Guid.NewGuid() },
            ExemptFromConsultationPublicRegister = true
        };

        var result = await _controller.InviteNewConsultee(model, _useCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(reloadModel.Value, viewResult.Model);
        Assert.Equal(model.ConsulteeName, reloadModel.Value.ConsulteeName);
        Assert.Equal(model.Email, reloadModel.Value.Email);
        Assert.Equal(model.Purpose, reloadModel.Value.Purpose);
        Assert.Equal(model.AreaOfFocus, reloadModel.Value.AreaOfFocus);
        Assert.Equal(model.SelectedDocumentIds, reloadModel.Value.SelectedDocumentIds);
        Assert.Equal(model.ExemptFromConsultationPublicRegister, reloadModel.Value.ExemptFromConsultationPublicRegister);
        Assert.NotNull(reloadModel.Value.Breadcrumbs);
    }

    [Fact]
    public async Task InviteNewConsultee_Post_RedirectsToError_WhenReloadModelFailure()
    {
        _controller.ModelState.AddModelError("Test", "Error");
        _useCaseMock.Setup(x => x.GetNewExternalConsulteeInviteViewModelAsync(_applicationId, _cancellationToken))
            .ReturnsAsync(Result.Failure<ExternalConsulteeInviteFormModel>("fail"));

        var model = new ExternalConsulteeInviteFormModel { ApplicationId = _applicationId };

        var result = await _controller.InviteNewConsultee(model, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task InviteNewConsultee_Post_RedirectsToIndex_WhenSuccess()
    {
        var model = new ExternalConsulteeInviteFormModel
        {
            ApplicationId = _applicationId,
            ConsulteeName = "Name",
            Email = "email@test.com",
            Purpose = ExternalConsulteeInvitePurpose.Mandatory,
            AreaOfFocus = "Area",
            SelectedDocumentIds = new List<Guid?> { Guid.NewGuid() },
            ExemptFromConsultationPublicRegister = true
        };

        _useCaseMock.Setup(x => x.InviteExternalConsulteeAsync(It.IsAny<ExternalConsulteeInviteModel>(), _applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.InviteNewConsultee(model, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task InviteNewConsultee_Post_RedirectsToError_WhenFailure()
    {
        var model = new ExternalConsulteeInviteFormModel
        {
            ApplicationId = _applicationId,
            ConsulteeName = "Name",
            Email = "email@test.com",
            Purpose = ExternalConsulteeInvitePurpose.Mandatory,
            AreaOfFocus = "Area",
            SelectedDocumentIds = new List<Guid?> { Guid.NewGuid() },
            ExemptFromConsultationPublicRegister = true
        };

        _useCaseMock.Setup(x => x.InviteExternalConsulteeAsync(It.IsAny<ExternalConsulteeInviteModel>(), _applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.InviteNewConsultee(model, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task GetReceivedComments_Get_ReturnsView_WhenSuccess()
    {
        var summary = new FellingLicenceApplicationSummaryModel { Id = _applicationId, ApplicationReference = "REF" };
        var viewModel = new ReceivedConsulteeCommentsViewModel { FellingLicenceApplicationSummary = summary };
        _useCaseMock.Setup(x => x.GetReceivedCommentsAsync(_applicationId, It.IsAny<Guid>(), _cancellationToken))
            .ReturnsAsync(Result.Success(viewModel));

        var result = await _controller.GetReceivedComments(_applicationId, Guid.NewGuid(), _useCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        Assert.NotNull(viewModel.Breadcrumbs);
    }

    [Fact]
    public async Task GetReceivedComments_Get_RedirectsToError_WhenFailure()
    {
        _useCaseMock.Setup(x => x.GetReceivedCommentsAsync(_applicationId, It.IsAny<Guid>(), _cancellationToken))
            .ReturnsAsync(Result.Failure<ReceivedConsulteeCommentsViewModel>("fail"));

        var result = await _controller.GetReceivedComments(_applicationId, Guid.NewGuid(), _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public void CreateBreadcrumbs_ReturnsBreadcrumbsModel()
    {
        var summary = new FellingLicenceApplicationSummaryModel { Id = _applicationId, ApplicationReference = "REF" };
        var result = typeof(ExternalConsulteeInviteController)
            .GetMethod("CreateBreadcrumbs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { summary, "Invite consultees" });

        var breadcrumbs = Assert.IsType<BreadcrumbsModel>(result);
        Assert.Equal("Invite consultees", breadcrumbs.CurrentPage);
        Assert.Equal(3, breadcrumbs.Breadcrumbs.Count);
    }
    }
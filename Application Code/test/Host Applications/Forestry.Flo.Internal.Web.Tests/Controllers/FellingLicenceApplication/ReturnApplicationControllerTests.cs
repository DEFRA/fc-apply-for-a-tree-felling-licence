using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class ReturnApplicationControllerTests
{
    private readonly Mock<IFellingLicenceApplicationUseCase> _applicationUseCaseMock;
    private readonly Mock<IReturnApplicationUseCase> _returnApplicationUseCaseMock;
    private readonly ReturnApplicationController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly CancellationToken _cancellationToken = CancellationToken.None;
    private readonly IFixture _fixture = new Fixture().CustomiseFixtureForFellingLicenceApplications();

    public ReturnApplicationControllerTests()
    {
        _applicationUseCaseMock = new Mock<IFellingLicenceApplicationUseCase>();
        _returnApplicationUseCaseMock = new Mock<IReturnApplicationUseCase>();
        _controller = new ReturnApplicationController();
        _controller.PrepareControllerForTest(Guid.NewGuid(), role: AccountTypeInternal.FieldManager);
    }

    [Fact]
    public async Task Index_ReturnsView_WhenSuccess()
    {
        var internalUser = new InternalUser(_controller.User);
        var summary = SetupForLoadModelSuccess(internalUser);

        _applicationUseCaseMock.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(summary);

        var result = await _controller.Index(_applicationId, _applicationUseCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ReturnApplicationModel>(viewResult.Model);
    }

    [Fact]
    public async Task Index_Redirects_WhenUserCannotApprove()
    {
        _controller.PrepareControllerForTest(Guid.NewGuid(), role: AccountTypeInternal.WoodlandOfficer);
        var result = await _controller.Index(_applicationId, _applicationUseCaseMock.Object, _cancellationToken);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal(_applicationId, redirectResult.RouteValues["id"]);
    }

    [Fact]
    public async Task Index_RedirectsToError_WhenFailure()
    {
        _applicationUseCaseMock.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Maybe.None);

        var result = await _controller.Index(_applicationId, _applicationUseCaseMock.Object, _cancellationToken);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ReturnApplication_Redirects_WhenUserCannotApprove()
    {
        var model = _fixture.Create<ReturnApplicationModel>();
        _controller.PrepareControllerForTest(Guid.NewGuid(), role: AccountTypeInternal.WoodlandOfficer);

        var result = await _controller.ReturnApplication(_applicationId, model, _applicationUseCaseMock.Object, _returnApplicationUseCaseMock.Object, _cancellationToken);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal(_applicationId, redirectResult.RouteValues["id"]);
    }

    [Fact]
    public async Task ReturnApplication_ReturnsView_WhenCaseNoteMissing()
    {
        var model = _fixture.Create<ReturnApplicationModel>();
        model.ReturnReason = model.ReturnReason with
        {
            CaseNote = null
        };

        var internalUser = new InternalUser(_controller.User);
        var summary = SetupForLoadModelSuccess(internalUser);

        _applicationUseCaseMock.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(summary);

        var result = await _controller.ReturnApplication(_applicationId, model, _applicationUseCaseMock.Object, _returnApplicationUseCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.IsType<ReturnApplicationModel>(viewResult.Model);
    }

    [Fact]
    public async Task ReturnApplication_RedirectsToError_WhenLoadViewModelFails()
    {
        var model = _fixture.Create<ReturnApplicationModel>();
        model.ReturnReason = model.ReturnReason with
        {
            CaseNote = null
        };

        var internalUser = new InternalUser(_controller.User);
        var summary = SetupForLoadModelSuccess(internalUser);
        summary.FellingLicenceApplicationSummary.Status = FellingLicenceStatus.Draft; // Invalid status for returning application

        _applicationUseCaseMock.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(
                _applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(summary);

        var result = await _controller.ReturnApplication(_applicationId, model, _applicationUseCaseMock.Object, _returnApplicationUseCaseMock.Object, _cancellationToken);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ReturnApplication_RedirectsToSummary_WhenSuccess()
    {
        var model = _fixture.Create<ReturnApplicationModel>();
        var internalUser = new InternalUser(_controller.User);
        var summary = SetupForLoadModelSuccess(internalUser);
        _applicationUseCaseMock.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(
                _applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(summary);

        _returnApplicationUseCaseMock.Setup(x => x.ReturnApplication(
            It.IsAny<InternalUser>(), _applicationId, model.ReturnReason, _cancellationToken))
            .ReturnsAsync(_fixture.Create<FinaliseFellingLicenceApplicationResult>());

        var result = await _controller.ReturnApplication(_applicationId, model, _applicationUseCaseMock.Object, _returnApplicationUseCaseMock.Object, _cancellationToken);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirectResult.ActionName);
        Assert.Equal("FellingLicenceApplication", redirectResult.ControllerName);
        Assert.Equal(_applicationId, redirectResult.RouteValues["id"]);
    }

    [Fact]
    public async Task ReturnApplication_AddsUserGuide_WhenSubProcessFailures()
    {
        var model = _fixture.Create<ReturnApplicationModel>();
        var internalUser = new InternalUser(_controller.User);
        var summary = SetupForLoadModelSuccess(internalUser);
        _applicationUseCaseMock.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(
                _applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(summary);

        var finaliseResult =
            FinaliseFellingLicenceApplicationResult.CreateSuccess(_fixture
                .Create<List<FinaliseFellingLicenceApplicationProcessOutcomes>>());

        _returnApplicationUseCaseMock.Setup(x => x.ReturnApplication(
                It.IsAny<InternalUser>(), _applicationId, model.ReturnReason, _cancellationToken))
            .ReturnsAsync(finaliseResult);

        var result = await _controller.ReturnApplication(_applicationId, model, _applicationUseCaseMock.Object, _returnApplicationUseCaseMock.Object, _cancellationToken);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirectResult.ActionName);
        Assert.Equal("FellingLicenceApplication", redirectResult.ControllerName);
        Assert.Equal(_applicationId, redirectResult.RouteValues["id"]);

        Assert.Equal("One or more issues occured", _controller.TempData[Infrastructure.ControllerExtensions.UserGuideKey]);

        var body = _controller.TempData[Infrastructure.ControllerExtensions.UserGuideBodyKey] as string;
        Assert.NotNull(body);

        foreach (var error in finaliseResult.SubProcessFailures)
        {
            Assert.Contains(error.GetDescription(), body);
        }
    }

    private FellingLicenceApplicationReviewSummaryModel SetupForLoadModelSuccess(InternalUser internalUser)
    {
        var userAccountModel = _fixture
            .Build<UserAccountModel>()
            .With(x => x.Id, internalUser.UserAccountId!.Value)
            .Create();

        var fieldManager = _fixture
            .Build<AssigneeHistoryModel>()
            .With(x => x.Role, AssignedUserRole.FieldManager)
            .With(x => x.UserAccount, userAccountModel)
            .Without(x => x.TimestampUnassigned)
            .Create();

        var summaryItem = _fixture
            .Build<FellingLicenceApplicationSummaryModel>()
            .With(x => x.Status, FellingLicenceStatus.SentForApproval)
            .With(x => x.AssigneeHistories, [fieldManager])
            .Create();

        return _fixture
            .Build<FellingLicenceApplicationReviewSummaryModel>()
            .With(x => x.ViewingUser, internalUser)
            .With(x => x.FellingLicenceApplicationSummary, summaryItem)
            .Create();
    }
}
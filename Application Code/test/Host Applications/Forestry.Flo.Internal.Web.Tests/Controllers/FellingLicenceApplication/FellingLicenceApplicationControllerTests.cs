using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class FellingLicenceApplicationControllerTests
{
    private readonly FellingLicenceApplicationController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly IFixture _fixture = new Fixture().CustomiseFixtureForFellingLicenceApplications();

    public FellingLicenceApplicationControllerTests()
    {
        _controller = new FellingLicenceApplicationController();
        var httpContext = new DefaultHttpContext();
        _controller.PrepareControllerForTest(Guid.NewGuid(), role: AccountTypeInternal.AccountAdministrator);
    }

    [Fact]
    public async Task ApplicationSummary_ReturnsViewResult_WhenSuccess()
    {
        var useCase = new Mock<IFellingLicenceApplicationUseCase>();
        var summary = _fixture.Create<FellingLicenceApplicationReviewSummaryModel>();
        useCase.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(
            _applicationId, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(summary));

        var result = await _controller.ApplicationSummary(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(summary, viewResult.Model);
        var breadcrumbs = ((FellingLicenceApplicationReviewSummaryModel)viewResult.Model).Breadcrumbs;
        Assert.NotNull(breadcrumbs);
        Assert.Equal(summary.FellingLicenceApplicationSummary.ApplicationReference, breadcrumbs.CurrentPage);
        Assert.Single(breadcrumbs.Breadcrumbs);
    }

    [Fact]
    public async Task ApplicationSummary_RedirectsToError_WhenNoValue()
    {
        var useCase = new Mock<IFellingLicenceApplicationUseCase>();
        useCase.Setup(x => x.RetrieveFellingLicenceApplicationReviewSummaryAsync(
            _applicationId, It.IsAny<InternalUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplicationReviewSummaryModel>.None);

        var result = await _controller.ApplicationSummary(_applicationId, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ReopenWithdrawnApplication_Get_Redirects_WhenNotAccountAdministrator()
    {
        var useCase = new Mock<IFellingLicenceApplicationUseCase>();

        _controller.PrepareControllerForTest(Guid.NewGuid(), true, AccountTypeInternal.AdminOfficer);

        var result = await _controller.ReopenWithdrawnApplication(_applicationId, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ReopenWithdrawnApplication_Get_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IFellingLicenceApplicationUseCase>();
        useCase.Setup(x => x.RetrieveReopenWithdrawnApplicationModelAsync(
            _applicationId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ReopenWithdrawnApplicationModel>("fail"));

        _controller.PrepareControllerForTest(Guid.NewGuid(), true, AccountTypeInternal.AccountAdministrator);

        var result = await _controller.ReopenWithdrawnApplication(_applicationId, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ReopenWithdrawnApplication_Get_ReturnsView_WhenSuccess()
    {
        var useCase = new Mock<IFellingLicenceApplicationUseCase>();
        var model = _fixture.Create<ReopenWithdrawnApplicationModel>();
        useCase.Setup(x => x.RetrieveReopenWithdrawnApplicationModelAsync(
            _applicationId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        _controller.PrepareControllerForTest(Guid.NewGuid(), true, AccountTypeInternal.AccountAdministrator);

        var result = await _controller.ReopenWithdrawnApplication(_applicationId, useCase.Object, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
        var breadcrumbs = ((ReopenWithdrawnApplicationModel)viewResult.Model).Breadcrumbs;
        Assert.NotNull(breadcrumbs);
        Assert.Equal(model.FellingLicenceApplicationSummary.ApplicationReference, breadcrumbs.CurrentPage);
    }

    [Fact]
    public async Task ReopenWithdrawnApplication_Post_Redirects_WhenNotAccountAdministrator()
    {
        var useCase = new Mock<IRevertApplicationFromWithdrawnUseCase>();
        var model = _fixture.Create<ReopenWithdrawnApplicationModel>();

        _controller.PrepareControllerForTest(Guid.NewGuid(), true, AccountTypeInternal.AdminOfficer);

        var result = await _controller.ReopenWithdrawnApplication(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ReopenWithdrawnApplication_Post_RedirectsToError_WhenFailure()
    {
        var useCase = new Mock<IRevertApplicationFromWithdrawnUseCase>();
        var model = _fixture
            .Build<ReopenWithdrawnApplicationModel>()
            .With(x => x.ApplicationId, _applicationId)
            .Create();
        useCase.Setup(x => x.RevertApplicationFromWithdrawnAsync(
            It.IsAny<InternalUser>(), _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        _controller.PrepareControllerForTest(Guid.NewGuid(), true, AccountTypeInternal.AccountAdministrator);

        var result = await _controller.ReopenWithdrawnApplication(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ReopenWithdrawnApplication_Post_RedirectsToIndex_WhenSuccess()
    {
        var useCase = new Mock<IRevertApplicationFromWithdrawnUseCase>();
        var model = _fixture.Create<ReopenWithdrawnApplicationModel>();
        useCase.Setup(x => x.RevertApplicationFromWithdrawnAsync(
            It.IsAny<InternalUser>(), _applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _controller.PrepareControllerForTest(Guid.NewGuid(), true, AccountTypeInternal.AccountAdministrator);

        var result = await _controller.ReopenWithdrawnApplication(model, useCase.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }
}
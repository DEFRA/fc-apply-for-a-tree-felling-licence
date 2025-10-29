using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class ExternalConsulteeReviewControllerTests
{
    private readonly ExternalConsulteeReviewController _controller;
    private readonly Mock<IExternalConsulteeReviewUseCase> _useCaseMock;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _accessCode = Guid.NewGuid();
    private readonly Guid _documentId = Guid.NewGuid();
    private readonly string _emailAddress = "test@example.com";
    private readonly CancellationToken _cancellationToken = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public ExternalConsulteeReviewControllerTests()
    {
        _useCaseMock = new Mock<IExternalConsulteeReviewUseCase>();
        _controller = new ExternalConsulteeReviewController();
        _controller.PrepareControllerForTest(Guid.NewGuid());
    }

    [Fact]
    public async Task Index_Get_RedirectsToLinkExpired_WhenAccessCodeInvalid()
    {
        _useCaseMock.Setup(x => x.ValidateAccessCodeAsync(_applicationId, _accessCode, _emailAddress, _cancellationToken))
            .ReturnsAsync(Result.Failure<ExternalInviteLink>("fail"));

        var result = await _controller.Index(_applicationId, _accessCode, _emailAddress, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("LinkExpired", redirect.ActionName);
    }

    [Fact]
    public async Task Index_Get_RedirectsToError_WhenGetApplicationSummaryFails()
    {
        _useCaseMock.Setup(x => x.ValidateAccessCodeAsync(_applicationId, _accessCode, _emailAddress, _cancellationToken))
            .ReturnsAsync(Result.Success(_fixture.Create<ExternalInviteLink>()));
        _useCaseMock.Setup(x => x.GetApplicationSummaryForConsulteeReviewAsync(_applicationId, It.IsAny<ExternalInviteLink>(), _accessCode, _cancellationToken))
            .ReturnsAsync(Result.Failure<ExternalConsulteeReviewViewModel>("fail"));

        var result = await _controller.Index(_applicationId, _accessCode, _emailAddress, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_Get_ReturnsView_WhenSuccess()
    {
        var summaryModel = new ExternalConsulteeReviewViewModel();
        _useCaseMock.Setup(x => x.ValidateAccessCodeAsync(_applicationId, _accessCode, _emailAddress, _cancellationToken))
            .ReturnsAsync(Result.Success(_fixture.Create<ExternalInviteLink>()));
        _useCaseMock.Setup(x => x.GetApplicationSummaryForConsulteeReviewAsync(_applicationId, It.IsAny<ExternalInviteLink>(), _accessCode, _cancellationToken))
            .ReturnsAsync(Result.Success(summaryModel));

        var result = await _controller.Index(_applicationId, _accessCode, _emailAddress, _useCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(summaryModel, viewResult.Model);
    }

    [Fact]
    public async Task Index_Post_RedirectsToError_WhenAccessCodeInvalid()
    {
        var commentModel = new AddConsulteeCommentModel
        {
            ApplicationId = _applicationId,
            AccessCode = _accessCode,
            AuthorContactEmail = _emailAddress
        };
        var files = new FormFileCollection();
        _controller.ModelState.AddModelError("Test", "Error");

        _useCaseMock.Setup(x => x.ValidateAccessCodeAsync(_applicationId, _accessCode, _emailAddress, _cancellationToken))
            .ReturnsAsync(Result.Failure<ExternalInviteLink>("fail"));

        var result = await _controller.Index(commentModel, files, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_Post_RedirectsToError_WhenGetApplicationSummaryFails()
    {
        var commentModel = new AddConsulteeCommentModel
        {
            ApplicationId = _applicationId,
            AccessCode = _accessCode,
            AuthorContactEmail = _emailAddress
        };
        var files = new FormFileCollection();
        _controller.ModelState.AddModelError("Test", "Error");

        _useCaseMock.Setup(x => x.ValidateAccessCodeAsync(_applicationId, _accessCode, _emailAddress, _cancellationToken))
            .ReturnsAsync(Result.Success(_fixture.Create<ExternalInviteLink>()));
        _useCaseMock.Setup(x => x.GetApplicationSummaryForConsulteeReviewAsync(_applicationId, It.IsAny<ExternalInviteLink>(), _accessCode, _cancellationToken))
            .ReturnsAsync(Result.Failure<ExternalConsulteeReviewViewModel>("fail"));

        var result = await _controller.Index(commentModel, files, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_Post_ReturnsView_WhenModelStateInvalidAndSuccess()
    {
        var commentModel = new AddConsulteeCommentModel
        {
            ApplicationId = _applicationId,
            AccessCode = _accessCode,
            AuthorContactEmail = _emailAddress
        };
        var files = new FormFileCollection();
        _controller.ModelState.AddModelError("Test", "Error");

        var summaryModel = new ExternalConsulteeReviewViewModel();
        _useCaseMock.Setup(x => x.ValidateAccessCodeAsync(_applicationId, _accessCode, _emailAddress, _cancellationToken))
            .ReturnsAsync(Result.Success(_fixture.Create<ExternalInviteLink>()));
        _useCaseMock.Setup(x => x.GetApplicationSummaryForConsulteeReviewAsync(_applicationId, It.IsAny<ExternalInviteLink>(), _accessCode, _cancellationToken))
            .ReturnsAsync(Result.Success(summaryModel));

        var result = await _controller.Index(commentModel, files, _useCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(summaryModel, viewResult.Model);
        Assert.Equal(commentModel, ((ExternalConsulteeReviewViewModel)viewResult.Model).AddConsulteeComment);
    }

    [Fact]
    public async Task Index_Post_RedirectsToError_WhenAddConsulteeCommentFails()
    {
        var commentModel = new AddConsulteeCommentModel
        {
            ApplicationId = _applicationId,
            AccessCode = _accessCode,
            AuthorContactEmail = _emailAddress
        };
        var files = new FormFileCollection();

        _useCaseMock.Setup(x => x.AddConsulteeCommentAsync(commentModel, files, _cancellationToken))
            .ReturnsAsync(Result.Failure<bool>("fail"));

        var result = await _controller.Index(commentModel, files, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_Post_RedirectsToIndex_WhenSuccess()
    {
        var commentModel = new AddConsulteeCommentModel
        {
            ApplicationId = _applicationId,
            AccessCode = _accessCode,
            AuthorContactEmail = _emailAddress
        };
        var files = new FormFileCollection();

        _useCaseMock.Setup(x => x.AddConsulteeCommentAsync(commentModel, files, _cancellationToken))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.Index(commentModel, files, _useCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
        Assert.Equal(_accessCode, redirect.RouteValues["accessCode"]);
        Assert.Equal(_emailAddress, redirect.RouteValues["emailAddress"]);
    }

    [Fact]
    public void LinkExpired_ReturnsView()
    {
        var result = _controller.LinkExpired();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task DownloadSupportingDocument_ReturnsFileResult_WhenSuccess()
    {
        var fileResult = new FileContentResult([], "application/pdf");
        _useCaseMock.Setup(x => x.GetSupportingDocumentAsync(_applicationId, _accessCode, _documentId, _emailAddress, _cancellationToken))
            .ReturnsAsync(fileResult);

        var result = await _controller.DownloadSupportingDocument(_useCaseMock.Object, _applicationId, _accessCode, _documentId, _emailAddress, _cancellationToken);

        Assert.Equal(fileResult, result);
    }

    [Fact]
    public async Task DownloadSupportingDocument_RedirectsToIndex_WhenFailure()
    {
        _useCaseMock.Setup(x => x.GetSupportingDocumentAsync(_applicationId, _accessCode, _documentId, _emailAddress, _cancellationToken))
            .ReturnsAsync(Result.Failure<FileContentResult>("fail"));

        var result = await _controller.DownloadSupportingDocument(_useCaseMock.Object, _applicationId, _accessCode, _documentId, _emailAddress, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
        Assert.Equal(_accessCode, redirect.RouteValues["accessCode"]);
        Assert.Equal(_emailAddress, redirect.RouteValues["emailAddress"]);
    }
}
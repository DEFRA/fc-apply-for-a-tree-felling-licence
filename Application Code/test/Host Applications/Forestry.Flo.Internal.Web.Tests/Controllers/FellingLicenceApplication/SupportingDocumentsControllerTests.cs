using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class SupportingDocumentsControllerTests
{
    private readonly Mock<IGetSupportingDocumentUseCase> _getSupportingDocumentUseCaseMock;
    private readonly Mock<IRemoveSupportingDocumentUseCase> _removeSupportingDocumentUseCaseMock;
    private readonly SupportingDocumentsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _documentId = Guid.NewGuid();

    public SupportingDocumentsControllerTests()
    {
        _getSupportingDocumentUseCaseMock = new Mock<IGetSupportingDocumentUseCase>();
        _removeSupportingDocumentUseCaseMock = new Mock<IRemoveSupportingDocumentUseCase>();
        _controller = new SupportingDocumentsController(_getSupportingDocumentUseCaseMock.Object, _removeSupportingDocumentUseCaseMock.Object);
        _controller.PrepareControllerForTest(Guid.NewGuid());
    }

    [Fact]
    public async Task GetDocument_ReturnsValue_WhenSuccess()
    {
        var expectedResult = new FileContentResult(Array.Empty<byte>(), "application/pdf");
        _getSupportingDocumentUseCaseMock
            .Setup(x => x.GetSupportingDocumentAsync(It.IsAny<InternalUser>(), _applicationId, _documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedResult));

        var result = await _controller.GetDocument(_applicationId, _documentId, CancellationToken.None);

        Assert.Same(expectedResult, result);
    }

    [Fact]
    public async Task GetDocument_RedirectsToError_WhenFailure()
    {
        _getSupportingDocumentUseCaseMock
            .Setup(x => x.GetSupportingDocumentAsync(It.IsAny<InternalUser>(), _applicationId, _documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FileContentResult>("error"));

        var result = await _controller.GetDocument(_applicationId, _documentId, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task DownloadSupportingDocument_ReturnsValue_WhenSuccess()
    {
        var expectedResult = new FileContentResult(Array.Empty<byte>(), "application/pdf");
        _getSupportingDocumentUseCaseMock
            .Setup(x => x.GetSupportingDocumentAsync(It.IsAny<InternalUser>(), _applicationId, _documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedResult));

        var result = await _controller.DownloadSupportingDocument(_applicationId, _documentId, CancellationToken.None);

        Assert.Same(expectedResult, result);
    }

    [Fact]
    public async Task DownloadSupportingDocument_RedirectsToError_WhenFailure()
    {
        _getSupportingDocumentUseCaseMock
            .Setup(x => x.GetSupportingDocumentAsync(It.IsAny<InternalUser>(), _applicationId, _documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FileContentResult>("error"));

        var result = await _controller.DownloadSupportingDocument(_applicationId, _documentId, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task AttachSupportingDocumentation_Redirects_WhenNoFiles()
    {
        var model = new AddSupportingDocumentModel { FellingLicenceApplicationId = _applicationId };
        var files = new FormFileCollection();
        var useCaseMock = new Mock<IAddSupportingDocumentsUseCase>();

        var result = await _controller.AttachSupportingDocumentation(model, files, useCaseMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("#supporting-documents-tab", redirect.Url);
    }

    [Fact]
    public async Task AttachSupportingDocumentation_AddsDocumentsAndRedirects_WhenFilesPresent()
    {
        var model = new AddSupportingDocumentModel { FellingLicenceApplicationId = _applicationId };
        var fileMock = new Mock<IFormFile>();
        var files = new FormFileCollection { fileMock.Object };
        var useCaseMock = new Mock<IAddSupportingDocumentsUseCase>();
        useCaseMock.Setup(x => x.AddDocumentsToApplicationAsync(
            It.IsAny<InternalUser>(),
            _applicationId,
            files,
            It.IsAny<ModelStateDictionary>(),
            model.AvailableToApplicant,
            model.AvailableToConsultees,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.AttachSupportingDocumentation(model, files, useCaseMock.Object, CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("#supporting-documents-tab", redirect.Url);
        useCaseMock.Verify(x => x.AddDocumentsToApplicationAsync(
            It.IsAny<InternalUser>(),
            _applicationId,
            files,
            It.IsAny<ModelStateDictionary>(),
            model.AvailableToApplicant,
            model.AvailableToConsultees,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveSupportingDocumentation_RemovesDocumentAndRedirects()
    {
        _removeSupportingDocumentUseCaseMock
            .Setup(x => x.RemoveSupportingDocumentsAsync(It.IsAny<InternalUser>(), _applicationId, _documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.RemoveSupportingDocumentation(_applicationId, _documentId, CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("#supporting-documents-tab", redirect.Url);
        _removeSupportingDocumentUseCaseMock.Verify(x => x.RemoveSupportingDocumentsAsync(
            It.IsAny<InternalUser>(), _applicationId, _documentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenUseCasesNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SupportingDocumentsController(null!, _removeSupportingDocumentUseCaseMock.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new SupportingDocumentsController(_getSupportingDocumentUseCaseMock.Object, null!));
    }
}
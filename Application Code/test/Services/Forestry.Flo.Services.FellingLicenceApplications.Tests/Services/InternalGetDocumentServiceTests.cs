using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class InternalGetDocumentServiceTests
    {
        private readonly Mock<IFileStorageService> _fileStorageService = new();
        private readonly Mock<IAuditService<GetDocumentServiceBase>> _mockGetDocumentAuditService = new();
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _mockRepository = new();
        private readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
        private readonly Guid RequestContextUserId = Guid.NewGuid();

        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Theory, AutoMoqData]
        public async Task WhenDocumentIsNotFound(
            GetDocumentRequest request)
        {
            //arrange
            var sut = CreateSut();

            _mockRepository
                .Setup(x => x.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Document>.None);

            //act
            var result = await sut.GetDocumentAsync(
                request,
                CancellationToken.None);

            Assert.True(result.IsFailure);

            //assert
            _mockGetDocumentAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.GetFellingLicenceAttachmentFailureEvent
                && y.SourceEntityId == request.ApplicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(y.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    request.DocumentId,
                    failureReason = "Document not found"
                }, _serializerOptions)
            ), It.IsAny<CancellationToken>()), Times.Once);
            _mockGetDocumentAuditService.VerifyNoOtherCalls();

            _mockRepository.Verify(x => x.GetDocumentByIdAsync(
                request.ApplicationId, request.DocumentId, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.VerifyNoOtherCalls();

            _fileStorageService.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task WhenCannotRetrieveFileFromStorage(
            GetDocumentExternalRequest request,
            Document document,
            FileAccessFailureReason reason)
        {
            //arrange
            var sut = CreateSut();

            _mockRepository
                .Setup(x => x.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Document>.From(document));
            _fileStorageService
                .Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<GetFileSuccessResult, FileAccessFailureReason>(reason));

            //act
            var result = await sut.GetDocumentAsync(
                request,
                CancellationToken.None);

            Assert.True(result.IsFailure);

            //assert
            _mockGetDocumentAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.GetFellingLicenceAttachmentFailureEvent
                && y.SourceEntityId == request.ApplicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(y.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    request.DocumentId,
                    failureReason = reason.ToString()
                }, _serializerOptions)
            ), It.IsAny<CancellationToken>()), Times.Once);
            _mockGetDocumentAuditService.VerifyNoOtherCalls();

            _mockRepository.Verify(x => x.GetDocumentByIdAsync(
                request.ApplicationId, request.DocumentId, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.VerifyNoOtherCalls();

            _fileStorageService.Verify(x => x.GetFileAsync(document.Location, It.IsAny<CancellationToken>()), Times.Once);
            _fileStorageService.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task WhenFileRetrieveSuccessfully(
            GetDocumentExternalRequest request,
            Document document,
            GetFileSuccessResult getFileSuccessResult)
        {
            //arrange
            var sut = CreateSut();

            _mockRepository
                .Setup(x => x.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Document>.From(document));
            _fileStorageService
                .Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<GetFileSuccessResult, FileAccessFailureReason>(getFileSuccessResult));

            //act
            var result = await sut.GetDocumentAsync(
                request,
                CancellationToken.None);

            Assert.True(result.IsSuccess);

            Assert.Equal(document.MimeType, result.Value.ContentType);
            Assert.Equal(getFileSuccessResult.FileBytes, result.Value.FileBytes);
            Assert.Equal(document.FileName, result.Value.FileName);

            //assert
            _mockGetDocumentAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.GetFellingLicenceAttachmentEvent
                && y.SourceEntityId == request.ApplicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(y.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    documentId = document.Id,
                    purpose = document.Purpose,
                    fileName = document.FileName,
                    location = document.Location,
                    storedFileModelName = document.FileName,
                    storedFileModelContentType = document.MimeType
                }, _serializerOptions)
            ), It.IsAny<CancellationToken>()), Times.Once);
            _mockGetDocumentAuditService.VerifyNoOtherCalls();

            _mockRepository.Verify(x => x.GetDocumentByIdAsync(
                request.ApplicationId, request.DocumentId, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.VerifyNoOtherCalls();

            _fileStorageService.Verify(x => x.GetFileAsync(document.Location, It.IsAny<CancellationToken>()), Times.Once);
            _fileStorageService.VerifyNoOtherCalls();
        }

        private InternalGetDocumentService CreateSut()
        {
            _mockGetDocumentAuditService.Reset();
            _fileStorageService.Reset();
            _mockRepository.Reset();

            var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
                localAccountId: RequestContextUserId);
            var requestContext = new RequestContext(
                RequestContextCorrelationId,
                new RequestUserModel(user));

            var getDocumentService = new InternalGetDocumentService(
                _fileStorageService.Object,
                _mockGetDocumentAuditService.Object,
                requestContext,
                new NullLogger<GetDocumentServiceBase>(),
                _mockRepository.Object);

            return getDocumentService;
        }
    }
}

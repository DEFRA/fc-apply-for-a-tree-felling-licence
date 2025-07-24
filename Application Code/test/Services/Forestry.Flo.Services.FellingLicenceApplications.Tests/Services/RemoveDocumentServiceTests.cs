using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using NodaTime;
using Xunit;
using Forestry.Flo.Services.Common.Models;
using System.Collections.Generic;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class RemoveDocumentServiceTests
    {
        private readonly Mock<IFileStorageService> _fileStorageService;
        private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationRepository;
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepository;
        private readonly Mock<IAuditService<RemoveDocumentService>> _auditService;
        private readonly Mock<IUnitOfWork> _unitOfWOrkMock;
        private readonly Mock<IClock> _clock;

        public RemoveDocumentServiceTests()
        {
            _fileStorageService = new Mock<IFileStorageService>();
            _auditService = new Mock<IAuditService<RemoveDocumentService>>();
            _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationExternalRepository>();
            _fellingLicenceApplicationInternalRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
            _unitOfWOrkMock = new Mock<IUnitOfWork>();
            _clock = new Mock<IClock>();
            _fellingLicenceApplicationRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
        }

        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };


        [Theory, AutoMoqData]
        public async Task WhenDocumentToRemoveIsNotFoundInApplication(Document document)
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationInternalRepository.Setup(r =>
                    r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Document>.None);

            var internalRequest = new RemoveDocumentRequest
            {
                ApplicationId = document.FellingLicenceApplicationId,
                DocumentId = document.Id,
                UserId = Guid.NewGuid(),
            };

            //act

            var result = await sut.RemoveDocumentAsInternalUserAsync(
                internalRequest,
                CancellationToken.None
            );

            //assert
            _fellingLicenceApplicationInternalRepository.Verify(v => v.GetDocumentByIdAsync(document.FellingLicenceApplicationId, document.Id, CancellationToken.None), Times.Once);

            _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsFailure);
        }


        [Theory, AutoMoqData]
        public async Task WhenDocumentCannotBeRemovedFromStorage(
            Guid applicationId, 
            Guid documentId,
            Document documentEntity,
            FileAccessFailureReason reason)
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationInternalRepository
                .Setup(x => x.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe.From(documentEntity));

            _fileStorageService.Setup(x => x.RemoveFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(reason));

            //act

            var result = await sut.PermanentlyRemoveDocumentAsync(
                applicationId,
                documentId,
                CancellationToken.None);

            //assert

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 purpose = documentEntity.Purpose,
                                 documentId = documentId,
                                 FailureReason = reason.ToString(),
                             }, _options)),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsFailure);

            _fellingLicenceApplicationInternalRepository
                .Verify(x => x.GetDocumentByIdAsync(applicationId, documentId, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

            _fileStorageService
                .Verify(x => x.RemoveFileAsync(documentEntity.Location, It.IsAny<CancellationToken>()), Times.Once);
            _fileStorageService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task WhenApplicationIsNotFound_ForInternalUser()
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationInternalRepository.Setup(r =>
                    r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Document>.None);

            var internalRequest = new RemoveDocumentRequest
            {
                ApplicationId = Guid.NewGuid(),
                DocumentId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
            };

            //act

            var result = await sut.RemoveDocumentAsInternalUserAsync(
                internalRequest,
                CancellationToken.None
            );

            //assert
            _fellingLicenceApplicationInternalRepository.Verify(v => v.GetDocumentByIdAsync(internalRequest.ApplicationId, internalRequest.DocumentId, CancellationToken.None), Times.Once);

            _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsFailure);
        }

        [Theory, AutoMoqData]
        public async Task WhenApplicationIsNotFound_ForExternalUser(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = application.WoodlandOwnerId,
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
            };

            var externalRequest = new RemoveDocumentExternalRequest
            {
                ApplicationId = application.Id,
                DocumentId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                UserAccessModel = userAccessModel
            };

            //act

            var result = await sut.RemoveDocumentAsExternalApplicantAsync(
                externalRequest,
                new CancellationToken()
            );

            //assert
            _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent),
                It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsFailure);
        }

        [Theory, AutoMoqData]
        public async Task WhenDocumentIsPermanentlyRemoved(
            Guid applicationId,
            Guid documentId,
            Document documentEntity)
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationInternalRepository
                .Setup(x => x.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Document>.From(documentEntity));
            _fellingLicenceApplicationInternalRepository
                .Setup(x => x.DeleteDocumentAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
            _fileStorageService
                .Setup(x => x.RemoveFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<FileAccessFailureReason>());

            //act
            var result = await sut.PermanentlyRemoveDocumentAsync(
                applicationId,
                documentId,
                CancellationToken.None);

            //assert

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentEvent),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsSuccess);
            
            _fileStorageService
                .Verify(x => x.RemoveFileAsync(documentEntity.Location, It.IsAny<CancellationToken>()), Times.Once);
            _fileStorageService.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository
                .Verify(x => x.GetDocumentByIdAsync(applicationId, documentId, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationInternalRepository
                .Verify(x => x.DeleteDocumentAsync(documentEntity, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task WhenDocumentIsSuccessfullySoftDeleted(Document document)
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationInternalRepository.Setup(r =>
                    r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

            document.AttachedByType = ActorType.InternalUser;
            document.Purpose = DocumentPurpose.Attachment;

            var internalRequest = new RemoveDocumentRequest
            {
                ApplicationId = document.FellingLicenceApplicationId,
                DocumentId = document.Id,
                UserId = Guid.NewGuid(),
            };

            //act
            var result = await sut.RemoveDocumentAsInternalUserAsync(
                internalRequest,
                CancellationToken.None
            );

            //assert
            _fellingLicenceApplicationInternalRepository.Verify(v => v.GetDocumentByIdAsync(document.FellingLicenceApplicationId, document.Id, CancellationToken.None), Times.Once);

            _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentEvent),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsSuccess);
            Assert.NotNull(document.DeletionTimestamp);
            Assert.NotNull(document.DeletedByUserId);
        }


        [Theory, AutoMoqData]
        public async Task ShouldReturnFailure_WhenActorTypeDoesNotMatch(Document document)
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationInternalRepository
                .Setup(r => r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

            document.AttachedByType = ActorType.InternalUser;
            document.Purpose = DocumentPurpose.Attachment;

            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = document.FellingLicenceApplication.WoodlandOwnerId,
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { document.FellingLicenceApplication.WoodlandOwnerId }
            };

            var externalRequest = new RemoveDocumentExternalRequest
            {
                ApplicationId = document.FellingLicenceApplicationId,
                DocumentId = document.Id,
                UserId = Guid.NewGuid(),
                UserAccessModel = userAccessModel
            };

            //act
            var result = await sut.RemoveDocumentAsExternalApplicantAsync(
                externalRequest,
                CancellationToken.None
            );

            //assert

            Assert.True(result.IsFailure);

            _fellingLicenceApplicationInternalRepository
                .Verify(r => r.GetDocumentByIdAsync(externalRequest.ApplicationId, externalRequest.DocumentId, CancellationToken.None), Times.Once);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                        JsonSerializer.Serialize(new
                        {
                            purpose = document.Purpose,
                            documentId = document.Id,
                            FailureReason = $"Document not uploaded by {ActorType.ExternalApplicant.GetDisplayName()}"
                        }, _options)),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task ShouldReturnFailure_DocumentIsNotAttachment(Document document)
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationInternalRepository
                .Setup(r => r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

            document.AttachedByType = ActorType.InternalUser;
            document.Purpose = DocumentPurpose.Correspondence;

            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = document.FellingLicenceApplication.CreatedById,
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { document.FellingLicenceApplication.WoodlandOwnerId }
            };

            var externalRequest = new RemoveDocumentExternalRequest
            {
                ApplicationId = document.FellingLicenceApplicationId,
                DocumentId = document.Id,
                UserId = Guid.NewGuid(),
                UserAccessModel = userAccessModel
            };

            //act
            var result = await sut.RemoveDocumentAsExternalApplicantAsync(
                externalRequest,
                CancellationToken.None
            );

            //assert

            Assert.True(result.IsFailure);

            _fellingLicenceApplicationInternalRepository
                .Verify(r => r.GetDocumentByIdAsync(externalRequest.ApplicationId, externalRequest.DocumentId, CancellationToken.None), Times.Once);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 purpose = document.Purpose,
                                 documentId = document.Id,
                                 FailureReason = "Only attachments may be deleted"
                             }, _options)),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task ShouldReturnFailure_WhenDeletionDataNotUpdated(Document document)
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationInternalRepository
                .Setup(r => r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

            _fellingLicenceApplicationRepository
                .Setup(r => r.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(UserDbErrorReason.General);

            document.AttachedByType = ActorType.ExternalApplicant;


            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = document.FellingLicenceApplication.CreatedById,
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { document.FellingLicenceApplication.WoodlandOwnerId }
            };


            var externalRequest = new RemoveDocumentExternalRequest
            {
                ApplicationId = document.FellingLicenceApplicationId,
                DocumentId = document.Id,
                UserId = Guid.NewGuid(),
                UserAccessModel = userAccessModel
            };

            //act
            var result = await sut.RemoveDocumentAsExternalApplicantAsync(
                externalRequest,
                CancellationToken.None
            );

            //assert
            
            _fellingLicenceApplicationInternalRepository
                .Verify(r => r.GetDocumentByIdAsync(externalRequest.ApplicationId, externalRequest.DocumentId, CancellationToken.None), Times.Once);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 purpose = document.Purpose,
                                 documentId = document.Id,
                                 FailureReason = $"Unable to update deletion data in document table for application"
                             }, _options)),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsFailure);
        }

        private RemoveDocumentService CreateSut()
        {
            _auditService.Reset();
            _unitOfWOrkMock.Reset();
            _fileStorageService.Reset();
            _clock.Reset();
            
            _fileStorageService.Setup(x => x.RemoveFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<FileAccessFailureReason>());

            _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));

            return new RemoveDocumentService(_fileStorageService.Object,
                _fellingLicenceApplicationRepository.Object,
                _fellingLicenceApplicationInternalRepository.Object,
                _auditService.Object,
                new RequestContext("test", new RequestUserModel(new ClaimsPrincipal())),
                _clock.Object,
                new NullLogger<RemoveDocumentService>());
        }
    }
}

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
using AutoFixture;

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
        private readonly IFixture _fixture = new Fixture();

        public RemoveDocumentServiceTests()
        {
            _fileStorageService = new Mock<IFileStorageService>();
            _auditService = new Mock<IAuditService<RemoveDocumentService>>();
            _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationExternalRepository>();
            _fellingLicenceApplicationInternalRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
            _unitOfWOrkMock = new Mock<IUnitOfWork>();
            _clock = new Mock<IClock>();
            _fellingLicenceApplicationRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
            _fixture.CustomiseFixtureForFellingLicenceApplications();
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

            _fellingLicenceApplicationRepository.Setup(r =>
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
            _fellingLicenceApplicationRepository.Verify(v => v.GetDocumentByIdAsync(document.FellingLicenceApplicationId, document.Id, CancellationToken.None), Times.Once);
            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();
            _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);

            _auditService.VerifyNoOtherCalls();

            Assert.True(result.IsSuccess);
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

            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
            _fileStorageService
                .Verify(x => x.RemoveFileAsync(documentEntity.Location, It.IsAny<CancellationToken>()), Times.Once);
            _fileStorageService.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task WhenDocumentCannotBeFoundToRemoveFromStorage(
            Guid applicationId,
            Guid documentId)
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationInternalRepository
                .Setup(x => x.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Document>.None);

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
                                 purpose = (string)null,
                                 documentId = documentId,
                                 FailureReason = "Could not retrieve document to delete",
                             }, _options)),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsFailure);

            _fellingLicenceApplicationInternalRepository
                .Verify(x => x.GetDocumentByIdAsync(applicationId, documentId, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
            _fileStorageService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task WhenApplicationIsNotFound_ForInternalUser()
        {
            //arrange
            var sut = CreateSut();

            _fellingLicenceApplicationRepository.Setup(r =>
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
            _fellingLicenceApplicationRepository.Verify(v => v.GetDocumentByIdAsync(internalRequest.ApplicationId, internalRequest.DocumentId, CancellationToken.None), Times.Once);
            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

            _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);

            _auditService.VerifyNoOtherCalls();

            Assert.True(result.IsSuccess);
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

            _fellingLicenceApplicationRepository.Verify(x => x.GetDocumentByIdAsync(application.Id, externalRequest.DocumentId, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

            _auditService.VerifyNoOtherCalls();

            Assert.True(result.IsSuccess);
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

            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task WhenDocumentFailsToBeRemovedFromRepository(
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
                .ReturnsAsync(UnitResult.Failure<UserDbErrorReason>(UserDbErrorReason.General));
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
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsFailure);

            _fileStorageService
                .Verify(x => x.RemoveFileAsync(documentEntity.Location, It.IsAny<CancellationToken>()), Times.Once);
            _fileStorageService.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository
                .Verify(x => x.GetDocumentByIdAsync(applicationId, documentId, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationInternalRepository
                .Verify(x => x.DeleteDocumentAsync(documentEntity, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        }

        [Theory, CombinatorialData]
        public async Task WhenDocumentIsSuccessfullySoftDeleted(
            [CombinatorialValues(DocumentPurpose.EiaAttachment, DocumentPurpose.Attachment, DocumentPurpose.WmpDocument, DocumentPurpose.TreeHealthAttachment)] DocumentPurpose purpose)
        {


            //arrange
            var sut = CreateSut();

            var document = _fixture
                .Build<Document>()
                .With(x => x.Purpose, purpose)
                .With(x => x.AttachedByType, ActorType.InternalUser)
                .Create();

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

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
            _fellingLicenceApplicationRepository.Verify(v => v.GetDocumentByIdAsync(document.FellingLicenceApplicationId, document.Id, CancellationToken.None), Times.Once);

            if (purpose == DocumentPurpose.WmpDocument)
            {
                // WMP documents require the application to be fetched to update the ten-year felling step status
                _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(document.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
            }

            _unitOfWOrkMock.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentEvent),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsSuccess);
            Assert.NotNull(document.DeletionTimestamp);
            Assert.NotNull(document.DeletedByUserId);
        }

        [Theory, AutoMoqData]
        public async Task WhenWmpDocumentIsSuccessfullySoftDeletedAndIsLastWmpDocumentOnApplication(
            FellingLicenceApplication application)
        {


            //arrange
            var sut = CreateSut();

            var document = _fixture
                .Build<Document>()
                .With(x => x.Purpose, DocumentPurpose.WmpDocument)
                .With(x => x.AttachedByType, ActorType.InternalUser)
                .Without(x => x.DeletionTimestamp)
                .Create();

            var otherDeletedWmpDocument = _fixture
                .Build<Document>()
                .With(x => x.Purpose, DocumentPurpose.WmpDocument)
                .With(x => x.AttachedByType, ActorType.InternalUser)
                .Create();

            var otherNonWmpDocument = _fixture
                .Build<Document>()
                .With(x => x.Purpose, DocumentPurpose.Attachment)
                .With(x => x.AttachedByType, ActorType.InternalUser)
                .Without(x => x.DeletionTimestamp)
                .Create();

            application.Documents = new List<Document> { document, otherDeletedWmpDocument, otherNonWmpDocument };

            application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus = true;

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

            _fellingLicenceApplicationRepository
                .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

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
            _fellingLicenceApplicationRepository.Verify(v => v.GetDocumentByIdAsync(document.FellingLicenceApplicationId, document.Id, CancellationToken.None), Times.Once);

            // WMP documents require the application to be fetched to update the ten-year felling step status
            _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(document.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);

            //assert that the ten-year licence status was reset to in progress
            Assert.False(application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus);

            _unitOfWOrkMock.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

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

            _fellingLicenceApplicationRepository
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

            _fellingLicenceApplicationRepository
                .Verify(r => r.GetDocumentByIdAsync(externalRequest.ApplicationId, externalRequest.DocumentId, CancellationToken.None), Times.Once);
            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

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

        [Theory, CombinatorialData]
        public async Task ShouldReturnFailure_DocumentIsNotAttachmentOrEiaAttachmentOrWmpDocument(DocumentPurpose purpose)
        {
            if (purpose is DocumentPurpose.EiaAttachment or DocumentPurpose.Attachment or DocumentPurpose.WmpDocument or DocumentPurpose.TreeHealthAttachment)
            {
                return;
            }

            //arrange
            var sut = CreateSut();

            var document = _fixture
                .Build<Document>()
                .With(x => x.Purpose, purpose)
                .With(x => x.AttachedByType, ActorType.InternalUser)
                .Create();

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository
                .Setup(r => r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

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

            _fellingLicenceApplicationRepository
                .Verify(r => r.GetDocumentByIdAsync(externalRequest.ApplicationId, externalRequest.DocumentId, CancellationToken.None), Times.Once);
            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 purpose = document.Purpose,
                                 documentId = document.Id,
                                 FailureReason = "Only attachments and EIA attachments may be deleted"
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

            _fellingLicenceApplicationRepository
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
            
            _fellingLicenceApplicationRepository
                .Verify(r => r.GetDocumentByIdAsync(externalRequest.ApplicationId, externalRequest.DocumentId, CancellationToken.None), Times.Once);

            if (document.Purpose == DocumentPurpose.WmpDocument)
            {
                // WMP documents require the application to be fetched to update the ten-year felling step status
                _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(document.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
            }

            _unitOfWOrkMock.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
            
            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

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

        #region HideDocumentFromApplicantAsync Tests

        [Theory, AutoMoqData]
        public async Task HideDocumentFromApplicantAsync_WhenDocumentNotFound_ReturnsFailure(
            Guid applicationId,
         Guid documentId,
            Guid userAccountId)
        {
       //arrange
        var sut = CreateSut();

   _fellingLicenceApplicationRepository
            .Setup(r => r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
   .ReturnsAsync(Maybe<Document>.None);

     //act
            var result = await sut.HideDocumentFromApplicantAsync(
      applicationId,
       documentId,
                userAccountId,
    CancellationToken.None);

        //assert
   Assert.True(result.IsFailure);
            Assert.Equal("Document not found", result.Error);

   _fellingLicenceApplicationRepository
    .Verify(r => r.GetDocumentByIdAsync(applicationId, documentId, CancellationToken.None), Times.Once);
            _fellingLicenceApplicationRepository
     .Verify(r => r.UpdateDocumentVisibleToApplicantAsync(
         It.IsAny<Guid>(), 
         It.IsAny<Guid>(), 
        It.IsAny<bool>(), 
           It.IsAny<CancellationToken>()), Times.Never);

  _auditService.Verify(s =>
      s.PublishAuditEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
   public async Task HideDocumentFromApplicantAsync_WhenUpdateFails_ReturnsFailure(Document document)
      {
     //arrange
            var sut = CreateSut();
     var userAccountId = Guid.NewGuid();

            _fellingLicenceApplicationRepository
      .Setup(r => r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

       _fellingLicenceApplicationRepository
   .Setup(r => r.UpdateDocumentVisibleToApplicantAsync(
        It.IsAny<Guid>(),
         It.IsAny<Guid>(),
         It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<UserDbErrorReason>(UserDbErrorReason.General));

            //act
    var result = await sut.HideDocumentFromApplicantAsync(
 document.FellingLicenceApplicationId,
    document.Id,
              userAccountId,
 CancellationToken.None);

     //assert
 Assert.True(result.IsFailure);
            Assert.Equal("Could not update document visibility", result.Error);

  _fellingLicenceApplicationRepository
      .Verify(r => r.GetDocumentByIdAsync(
     document.FellingLicenceApplicationId, 
          document.Id, 
              CancellationToken.None), Times.Once);
            _fellingLicenceApplicationRepository
           .Verify(r => r.UpdateDocumentVisibleToApplicantAsync(
      document.FellingLicenceApplicationId,
           document.Id,
     false,
   CancellationToken.None), Times.Once);

       _auditService.Verify(s =>
 s.PublishAuditEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
     }

        [Theory, AutoMoqData]
        public async Task HideDocumentFromApplicantAsync_WhenSuccessful_UpdatesVisibilityAndPublishesAuditEvent(Document document)
        {
  //arrange
            var sut = CreateSut();
        var userAccountId = Guid.NewGuid();

    _fellingLicenceApplicationRepository
      .Setup(r => r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(document);

            _fellingLicenceApplicationRepository
       .Setup(r => r.UpdateDocumentVisibleToApplicantAsync(
         It.IsAny<Guid>(),
         It.IsAny<Guid>(),
     It.IsAny<bool>(),
  It.IsAny<CancellationToken>()))
             .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

     //act
    var result = await sut.HideDocumentFromApplicantAsync(
        document.FellingLicenceApplicationId,
         document.Id,
                userAccountId,
                CancellationToken.None);

    //assert
            Assert.True(result.IsSuccess);

     _fellingLicenceApplicationRepository
   .Verify(r => r.GetDocumentByIdAsync(
    document.FellingLicenceApplicationId,
    document.Id,
              CancellationToken.None), Times.Once);
            _fellingLicenceApplicationRepository
         .Verify(r => r.UpdateDocumentVisibleToApplicantAsync(
          document.FellingLicenceApplicationId,
      document.Id,
   false,
             CancellationToken.None), Times.Once);

  _auditService.Verify(s =>
     s.PublishAuditEventAsync(
         It.Is<AuditEvent>(e =>
             e.EventName == AuditEvents.HideFellingLicenceDocumentEvent &&
           e.UserId == userAccountId &&
      JsonSerializer.Serialize(e.AuditData, _options) ==
          JsonSerializer.Serialize(new
         {
        documentId = document.Id,
            document.Purpose,
        document.FileName,
    document.Location
      }, _options)),
        CancellationToken.None), Times.Once);
   }

        [Theory, AutoMoqData]
 public async Task HideDocumentFromApplicantAsync_VerifiesCorrectParametersPassedToRepository(Document document)
        {
            //arrange
     var sut = CreateSut();
        var userAccountId = Guid.NewGuid();
     var applicationId = document.FellingLicenceApplicationId;
    var documentId = document.Id;

   _fellingLicenceApplicationRepository
          .Setup(r => r.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(document);

 _fellingLicenceApplicationRepository
         .Setup(r => r.UpdateDocumentVisibleToApplicantAsync(
        It.IsAny<Guid>(),
      It.IsAny<Guid>(),
        It.IsAny<bool>(),
    It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

  //act
          await sut.HideDocumentFromApplicantAsync(
             applicationId,
    documentId,
  userAccountId,
      CancellationToken.None);

  //assert
 _fellingLicenceApplicationRepository
             .Verify(r => r.UpdateDocumentVisibleToApplicantAsync(
  applicationId,
  documentId,
      false, // Verify it's setting to false to hide from applicant
          CancellationToken.None), Times.Once);
        }

        #endregion
    }
}

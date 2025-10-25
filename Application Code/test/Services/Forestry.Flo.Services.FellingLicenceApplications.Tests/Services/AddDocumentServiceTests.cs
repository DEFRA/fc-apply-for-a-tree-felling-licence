using AutoFixture;
using CSharpFunctionalExtensions;
using FileSignatures;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class AddDocumentServiceTests
    {
        private static readonly Fixture FixtureInstance = new();
        private static readonly DateTime UtcNow = DateTime.UtcNow;
        private readonly IClock _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
        private readonly Mock<IFileStorageService> _fileStorageService = new();

        private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationRepository = new();
        private readonly Mock<IFileFormatInspector> _mockFileFormatInspector = new();
        private readonly Mock<IAuditService<AddDocumentService>> _mockAddDocumentServiceAudit = new();
        private readonly Mock<IUnitOfWork> _unitOfWOrkMock = new();

        public AddDocumentServiceTests()
        {
            FixtureInstance.CustomiseFixtureForFellingLicenceApplications();
        }

        [Theory, AutoMoqData]
        public async Task WhenNoDocumentsSpecified(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut();
            var filesToStore = new List<FileToStoreModel>(0);

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 0,
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = application.WoodlandOwnerId
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                new CancellationToken());

            //assert
            _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
          
            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Never);

            Assert.True(result.IsFailure);
            Assert.Single(result.Error.UserFacingFailureMessages);
            Assert.Equal("No documents were specified.",result.Error.UserFacingFailureMessages.First());

        }

        [Theory, AutoMoqData]
        public async Task WhenApplicationNotFoundForId(Guid applicationId)
        {
            //arrange
            var sut = CreateSut();
            var filesToStore = new List<FileToStoreModel>(0);

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 0,
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = applicationId,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = Guid.NewGuid()
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                CancellationToken.None);

            //assert
            _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Never);

            Assert.True(result.IsFailure);
            Assert.Single(result.Error.UserFacingFailureMessages);
            Assert.Equal($"An application cannot be retrieved with an id of {applicationId}.", result.Error.UserFacingFailureMessages.First());

        }

        [Theory, AutoMoqData]
        public async Task WhenTooManyDocumentsToAdd(FellingLicenceApplication application)
        {
            //arrange
            const int maxNumberOfDocumentsToAdd = 2;
            var sut = CreateSut(maxNumberOfDocuments:maxNumberOfDocumentsToAdd);
            var filesToStore = CreateFilesToStore(maxNumberOfDocumentsToAdd + 1);

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 100,
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = application.WoodlandOwnerId
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                new CancellationToken());

            //assert
            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Never);

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.AddFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Once);
            
            Assert.True(result.IsFailure);
            Assert.Single(result.Error.UserFacingFailureMessages);
            Assert.Equal($"You can only upload up to {maxNumberOfDocumentsToAdd} documents.",result.Error.UserFacingFailureMessages.First());
        }

        [Theory, AutoMoqData]
        public async Task WhenGreaterThanDocumentLimitButIsAddingWmpDocument(FellingLicenceApplication application)
        {
            //arrange
            const int maxNumberOfDocumentsToAdd = 2;
            var sut = CreateSut(maxNumberOfDocuments: maxNumberOfDocumentsToAdd);
            var filesToStore = CreateFilesToStore(maxNumberOfDocumentsToAdd + 1);

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 100,
                DocumentPurpose = DocumentPurpose.WmpDocument,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = application.WoodlandOwnerId
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                new CancellationToken());

            //assert
            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Exactly(filesToStore.Count));

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.AddFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Never);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.UserFacingFailureMessages);
        }

        [Theory, AutoMoqData]
        public async Task WithSingleValidDocumentToAdd(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut(maxNumberOfDocuments:application.Documents!.Count+1);
            var filesToStore = CreateFilesToStore();

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 0,
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = application.WoodlandOwnerId
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                new CancellationToken());

            //assert

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Once);

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.AddFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Never);
            
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.UserFacingFailureMessages);
        }

        [Theory, AutoMoqData]
        public async Task WhenAddingDocumentToRepositoryFails(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut(maxNumberOfDocuments: application.Documents!.Count + 1);
            var filesToStore = CreateFilesToStore();

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 0,
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = application.WoodlandOwnerId
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository
                .Setup(x => x.AddDocumentAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                CancellationToken.None);

            //assert

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Never);

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplicationFailure),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsFailure);
        }

        [Theory]
        [InlineData(DocumentPurpose.FcLisConstraintReport)]
        [InlineData(DocumentPurpose.ExternalLisConstraintReport)]
        public async Task CanSaveWhenCorrectDocumentPurposeWhenAddedViaApi(DocumentPurpose documentPurpose)
        {
            //arrange
            var application = FixtureInstance.Create<FellingLicenceApplication>();
            var sut = CreateSut(maxNumberOfDocuments:application.Documents!.Count+1);
            var filesToStore = CreateFilesToStore();

            var addDocumentRequest = new AddDocumentsRequest
            {
                ActorType = ActorType.System,
                ApplicationDocumentCount = 0,
                DocumentPurpose = documentPurpose,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = true,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //act
            var result = await sut.AddDocumentsAsInternalUserAsync(
                addDocumentRequest,
                new CancellationToken());

            //assert
            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Once);

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.AddFellingLicenceAttachmentEvent),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.UserFacingFailureMessages);
        }

        [Theory]
        [InlineData(DocumentPurpose.Correspondence)]
        [InlineData(DocumentPurpose.Attachment)]
        [InlineData(DocumentPurpose.ApplicationDocument)]
        [InlineData(DocumentPurpose.SiteVisitAttachment)]
        public async Task DenyWhenIncorrectDocumentPurposeUsedWhenAddedViaApi(DocumentPurpose documentPurpose)
        {
            //arrange
            var application = FixtureInstance.Create<FellingLicenceApplication>();
            var sut = CreateSut(maxNumberOfDocuments:application.Documents!.Count+1);
            var filesToStore = CreateFilesToStore();

            var addDocumentRequest = new AddDocumentsRequest
            {
                ActorType = ActorType.System,
                ApplicationDocumentCount = 0,
                DocumentPurpose = documentPurpose,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = true,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //act
            var result = await sut.AddDocumentsAsInternalUserAsync(
                addDocumentRequest,
                new CancellationToken());

            //assert
            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.AddFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Once);

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Never);
            
            Assert.True(result.IsFailure);
            Assert.Single(result.Error.UserFacingFailureMessages);
            Assert.Contains($"Attempt by Api to add document with incorrect purpose type {documentPurpose}.", result.Error.UserFacingFailureMessages);
        }

        [Theory, AutoMoqData]
        public async Task WithMultipleValidDocumentsToAdd(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut(maxNumberOfDocuments:application.Documents!.Count+2);
            var filesToStore = CreateFilesToStore(2);

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 0,
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = application.WoodlandOwnerId
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                new CancellationToken());

            //assert
            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Exactly(2));

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.AddFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Never);
            
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.UserFacingFailureMessages);
        }

        [Theory, AutoMoqData]
        public async Task WithMultipleInvalidDocumentsToAdd(FellingLicenceApplication application)
        {
            //arrange
            const int numberOfDocumentsToAdd = 2;
            var sut = CreateSut(maxNumberOfDocuments:application.Documents!.Count+numberOfDocumentsToAdd);
            var filesToStore = CreateFilesToStore(numberOfDocumentsToAdd);

            _fileStorageService.SetupSequence(x => x.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SavedFileFailureResultWithFailureReason(StoreFileFailureResultReason.Unknown))
                .ReturnsAsync(SavedFileFailureResultWithFailureReason(StoreFileFailureResultReason.FailedValidation));

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 0,
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = application.WoodlandOwnerId
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                new CancellationToken());

            //assert
            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Never);

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.AddFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Exactly(numberOfDocumentsToAdd));
            
            Assert.True(result.IsFailure);
            Assert.Equal(numberOfDocumentsToAdd,result.Error.UserFacingFailureMessages.Count());
            Assert.Contains("Unable to save file at this time.", result.Error.UserFacingFailureMessages);
            Assert.Contains($"Contents of '{filesToStore.Last().FileName}' was empty.", result.Error.UserFacingFailureMessages);
        }

         [Theory, AutoMoqData]
        public async Task WithMixOfValidAndInvalidDocumentsToAdd(FellingLicenceApplication application)
        {
            //arrange
            const int numberOfDocumentsToAdd = 4;
            var sut = CreateSut(maxNumberOfDocuments:application.Documents!.Count+numberOfDocumentsToAdd);
            var filesToStore = CreateFilesToStore(numberOfDocumentsToAdd);

            _fileStorageService.SetupSequence(x => x.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SavedFileSuccessResult("testLocation",4))
                .ReturnsAsync(SavedFileFailureResultWithFailureReason(StoreFileFailureResultReason.Unknown))
                .ReturnsAsync(SavedFileSuccessResult("testLocation2",4))
                .ReturnsAsync(SavedFileFailureResultWithFailureReason(StoreFileFailureResultReason.FailedValidation));

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 0,
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = application.WoodlandOwnerId
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                new CancellationToken());

            //assert

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Exactly(2));

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.AddFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Exactly(numberOfDocumentsToAdd-2));
            
            Assert.True(result.IsFailure);
            Assert.Equal(2 ,result.Error.UserFacingFailureMessages.Count());
            Assert.Contains("Unable to save file at this time.", result.Error.UserFacingFailureMessages);
            Assert.Contains($"Contents of '{filesToStore.Last().FileName}' was empty.", result.Error.UserFacingFailureMessages);
        }

        [Theory, AutoMoqData]
        public async Task WhenWoodlandOwnerCannotBeVerified(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut();
            var filesToStore = CreateFilesToStore();

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 0,
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = application.WoodlandOwnerId
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                new CancellationToken());

            //assert

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.AddFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Once);


            Assert.True(result.IsFailure);
            Assert.Single(result.Error.UserFacingFailureMessages);
            Assert.Contains($"External applicant lacks permission to add documents to application, app id: {addDocumentRequest.FellingApplicationId}, woodland owner id: {addDocumentRequest.WoodlandOwnerId}.", result.Error.UserFacingFailureMessages);
        }

        [Theory, AutoMoqData]
        public async Task WhenUploadingFileThatFailsValidation(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut(maxNumberOfDocuments: application.Documents!.Count + 1);
            var filesToStore = FixtureInstance.CreateMany<FileToStoreModel>(1).ToList();

            var addDocumentRequest = new AddDocumentsExternalRequest()
            {
                ActorType = ActorType.ExternalApplicant,
                ApplicationDocumentCount = 0,
                DocumentPurpose = DocumentPurpose.Attachment,
                FellingApplicationId = application.Id,
                FileToStoreModels = new ReadOnlyCollection<FileToStoreModel>(filesToStore),
                ReceivedByApi = false,
                UserAccountId = Guid.NewGuid(),
                VisibleToApplicant = true,
                VisibleToConsultee = true,
                WoodlandOwnerId = application.WoodlandOwnerId
            };

            //setup

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.VerifyWoodlandOwnerIdForApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _fellingLicenceApplicationRepository.Setup(r =>
                    r.CheckApplicationExists(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //act
            var result = await sut.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Single(result.Error.UserFacingFailureMessages);
            Assert.Contains($"The type of file '{filesToStore.First().FileName}' is not permitted to be attached.", result.Error.UserFacingFailureMessages);


            //assert

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Never);

            _mockAddDocumentServiceAudit.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.AddFellingLicenceAttachmentFailureEvent),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        private AddDocumentService CreateSut(int maxNumberOfDocuments=2, int maxFileSizeBytes=1024)
        {
            _fileStorageService.Reset();
            _fileStorageService.Setup(f => f.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SavedFileSuccessResult("testLocation", 4));

            _mockFileFormatInspector.Reset();
            _fellingLicenceApplicationRepository.Reset();
            _unitOfWOrkMock.Reset();
            _fellingLicenceApplicationRepository.
                SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);

            _mockAddDocumentServiceAudit.Reset();

            var settings = new UserFileUploadOptions
            {
                MaxNumberDocuments = maxNumberOfDocuments,
                MaxFileSizeBytes = maxFileSizeBytes,
                AllowedFileTypes = new[]
                {
                    new AllowedFileType()
                    {
                        FileUploadReasons = [FileUploadReason.SupportingDocument, FileUploadReason.AgentAuthorityForm, FileUploadReason.WmpDocument, FileUploadReason.EiaDocument],
                        Description = "test",
                        Extensions = ["csv", "txt"]
                    }
                }
            };
            var apiFileUploadOptions = new ApiFileUploadOptions()
            {
                MaxFileSizeBytes = maxFileSizeBytes,
            };

            var fileValidator = new FileValidator(
                _mockFileFormatInspector.Object,
                Options.Create(settings),
                Options.Create(apiFileUploadOptions));


            return new AddDocumentService(
                _fixedTimeClock,
                _fileStorageService.Object,
                new FileTypesProvider(),
                Options.Create(settings),
                _mockAddDocumentServiceAudit.Object,
                _fellingLicenceApplicationRepository.Object,
                fileValidator,
                new RequestContext("test",new RequestUserModel(new ClaimsPrincipal())),
                new NullLogger<AddDocumentService>()
            );
        }

        private static Result<StoreFileSuccessResult, StoreFileFailureResult> SavedFileSuccessResult(string location, int fileSize)
        {
            var s = new StoreFileSuccessResult(location, fileSize);
            return Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(s);
        }

        private static Result<StoreFileSuccessResult, StoreFileFailureResult> SavedFileFailureResultWithFailureReason(StoreFileFailureResultReason reason)
        {
            return Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(new StoreFileFailureResult(reason));
        }

        private static List<FileToStoreModel> CreateFilesToStore(int count = 1)
        {
            var results = FixtureInstance.CreateMany<FileToStoreModel>(count).ToList();
            results.ForEach(f => f.FileName += ".txt");
            return results;
        }
    }
}

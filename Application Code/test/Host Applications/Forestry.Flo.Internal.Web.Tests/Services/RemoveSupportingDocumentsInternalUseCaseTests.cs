using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System.Text.Json;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Tests.Services
{
    public class RemoveSupportingDocumentsInternalUseCaseTests
    {
        private static readonly Fixture FixtureInstance = new();
        private readonly Mock<IFileStorageService> _fileStorageService;
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository;
        private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationExternalRepository;
        private readonly Mock<IAuditService<RemoveSupportingDocumentUseCase>> _mockRemoveSupportingDocumentUseCaseAuditService;
        private readonly Mock<IRemoveDocumentService> _removeDocumentService;
        private readonly InternalUser _internalUser;
        private readonly Mock<IClock> _clock;

        public RemoveSupportingDocumentsInternalUseCaseTests()
        {
            _fileStorageService = new Mock<IFileStorageService>();
            _mockRemoveSupportingDocumentUseCaseAuditService =
            new Mock<IAuditService<RemoveSupportingDocumentUseCase>>();
            _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
            _fellingLicenceApplicationExternalRepository = new Mock<IFellingLicenceApplicationExternalRepository>();
            _removeDocumentService = new Mock<IRemoveDocumentService>();
            _clock = new Mock<IClock>();

            var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<Guid>(),
                FixtureInstance.Create<string>(),
                AccountTypeInternal.AdminOfficer);
            _internalUser = new InternalUser(user);
        }



        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Theory, AutoMoqData]
        public async Task WhenDocumentServiceReturnsFailure(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut();

            _removeDocumentService.Setup(r => r.RemoveDocumentAsInternalUserAsync(It.IsAny<RemoveDocumentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure("error"));

            var document = application.Documents!.First();

            //act
            var result = await sut.RemoveSupportingDocumentsAsync(
                _internalUser,
                application.Id,
                document.Id,
                new CancellationToken()
            );

            //assert

            _mockRemoveSupportingDocumentUseCaseAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 ApplicationId = application.Id,
                                 Section = "Supporting Documentation",
                                 _internalUser.UserAccountId,
                                 result.Error
                             }, _options)),
                    CancellationToken.None), Times.Once);

            Assert.True(result.IsFailure);

            _removeDocumentService.Verify(v => v.RemoveDocumentAsInternalUserAsync(It.Is<RemoveDocumentRequest>(
                r => r.ApplicationId == application.Id
                     && r.DocumentId == document.Id
                     && r.UserId == _internalUser.UserAccountId), CancellationToken.None), Times.Once);
        }


        [Theory, AutoMoqData]
        public async Task WhenDocumentIsSoftDeleted(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut();

            _removeDocumentService.Setup(r => r.RemoveDocumentAsInternalUserAsync(It.IsAny<RemoveDocumentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success);

            var document = application.Documents!.First();

            //act
            var result = await sut.RemoveSupportingDocumentsAsync(
                _internalUser,
                application.Id,
                document.Id,
                new CancellationToken()
            );

            //assert

            _mockRemoveSupportingDocumentUseCaseAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentEvent
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             ApplicationId = application.Id,
                             _internalUser.UserAccountId,
                             Section = "Supporting Documentation",
                         }, _options)), CancellationToken.None), Times.Once);

            Assert.True(result.IsSuccess);

            _removeDocumentService.Verify(v => v.RemoveDocumentAsInternalUserAsync(It.Is<RemoveDocumentRequest>(
                r => r.ApplicationId == application.Id
                     && r.DocumentId == document.Id
                     && r.UserId == _internalUser.UserAccountId), CancellationToken.None), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenFellingLicenceDocumentIsRemoved(
            FellingLicenceApplication fla)
        {
            //arrange
            var sut = CreateSut();

            _removeDocumentService
                .Setup(s => s.PermanentlyRemoveDocumentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            //act

            var result = await sut.RemoveFellingLicenceDocument(_internalUser,
                fla.Id,
                fla.Documents.First().Id,
                CancellationToken.None);

            //assert

            _mockRemoveSupportingDocumentUseCaseAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentEvent
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             ApplicationId = fla.Id,
                             _internalUser.UserAccountId,
                             Section = "Supporting Documentation",
                         }, _options)), CancellationToken.None), Times.Once);

            Assert.True(result.IsSuccess);

            _removeDocumentService
                .Verify(x => x.PermanentlyRemoveDocumentAsync(fla.Id, fla.Documents.First().Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenFellingLicenceDocumentCannotBeRemoved(
            FellingLicenceApplication fla)
        {
            //arrange
            var sut = CreateSut();

            _removeDocumentService
                .Setup(s => s.PermanentlyRemoveDocumentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure("Could not remove file"));

            //act

            var result = await sut.RemoveFellingLicenceDocument(_internalUser,
                fla.Id,
                fla.Documents.First().Id,
                CancellationToken.None);

            //assert

            _mockRemoveSupportingDocumentUseCaseAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             ApplicationId = fla.Id,
                             _internalUser.UserAccountId,
                             Section = "Supporting Documentation",
                             error = result.Error
                         }, _options)), CancellationToken.None), Times.Once);

            Assert.True(result.IsFailure);

            _removeDocumentService
                .Verify(x => x.PermanentlyRemoveDocumentAsync(fla.Id, fla.Documents.First().Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        private RemoveSupportingDocumentUseCase CreateSut()
        {
            _fellingLicenceApplicationRepository.Reset();
            _clock.Reset();
            _removeDocumentService.Reset();
            _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));


            return new RemoveSupportingDocumentUseCase(
                _removeDocumentService.Object,
                _mockRemoveSupportingDocumentUseCaseAuditService.Object,
                new RequestContext("test", new RequestUserModel(_internalUser.Principal)),
                new NullLogger<AddSupportingDocumentsUseCase>());
        }
    }
}

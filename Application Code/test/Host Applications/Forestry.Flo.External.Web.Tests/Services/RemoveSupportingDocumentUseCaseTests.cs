using System.Text.Json;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.External.Web.Tests.Services
{
    public class RemoveSupportingDocumentUseCaseTests
    {
        private static readonly Fixture FixtureInstance = new();
        private readonly Mock<IFileStorageService> _fileStorageService;
        private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsService;
        private readonly Mock<IRetrieveWoodlandOwners> _retrieveWoodlandOwnersService;
        private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _getFellingLicenceApplicationForExternalUsersService;
        private readonly Mock<IGetPropertyProfiles> _getPropertyProfilesService;
        private readonly Mock<IGetCompartments> _getCompartmentsService;

        private readonly Mock<IAuditService<RemoveSupportingDocumentUseCase>> _mockUseCaseAuditService;
        private readonly ExternalApplicant _externalApplicant;
        private readonly WoodlandOwner _woodlandOwner;
        private readonly Mock<IUnitOfWork> _unitOfWOrkMock;
        private readonly Mock<IClock> _clock;
        private readonly Mock<IRemoveDocumentService> _removeDocumentsService;
        private readonly Mock<IAgentAuthorityService> _agentAuthorityService;

        public RemoveSupportingDocumentUseCaseTests()
        {
            _fileStorageService = new Mock<IFileStorageService>();
            _mockUseCaseAuditService = new Mock<IAuditService<RemoveSupportingDocumentUseCase>>();
            _retrieveUserAccountsService = new();
            _retrieveWoodlandOwnersService = new();

            _getFellingLicenceApplicationForExternalUsersService =
                new Mock<IGetFellingLicenceApplicationForExternalUsers>();

            _getPropertyProfilesService = new Mock<IGetPropertyProfiles>();
            _getCompartmentsService = new();

            _removeDocumentsService = new Mock<IRemoveDocumentService>();
            _unitOfWOrkMock = new Mock<IUnitOfWork>();
            _clock = new Mock<IClock>();

            _agentAuthorityService = new();

            _woodlandOwner = FixtureInstance.Build<WoodlandOwner>()
                .With(wo => wo.IsOrganisation, true)
                .Create();

            var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<Guid>(),
                _woodlandOwner.Id,
                AccountTypeExternal.WoodlandOwnerAdministrator);
            _externalApplicant = new ExternalApplicant(user);
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

            _removeDocumentsService.Setup(r => r.RemoveDocumentAsExternalApplicantAsync(It.IsAny<RemoveDocumentExternalRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure("error"));

            // Simulate application in editable state

            _getFellingLicenceApplicationForExternalUsersService.Setup(x => x.GetIsEditable(
                It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var document = application.Documents!.First();

            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = _externalApplicant.UserAccountId!.Value,
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
            };

            _retrieveUserAccountsService
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userAccessModel);

            //act
            var result = await sut.RemoveSupportingDocumentAsync(
                _externalApplicant,
                application.Id,
                document.Id,
                new CancellationToken()
            );

            //assert

            Assert.True(result.IsFailure);

            _mockUseCaseAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 ApplicationId = application.Id,
                                 _externalApplicant.WoodlandOwnerId,
                                 Section = "Supporting Documentation",
                                 Reason = "Unauthorized",
                                 result.Error
                             }, _options)),
                    CancellationToken.None), Times.Once);

            _removeDocumentsService.Verify(v => v.RemoveDocumentAsExternalApplicantAsync(It.Is<RemoveDocumentExternalRequest>(
                    r => r.UserAccessModel.IsFcUser == userAccessModel.IsFcUser
                         && r.UserAccessModel.UserAccountId == _externalApplicant.UserAccountId
                         && r.UserAccessModel.AgencyId == userAccessModel.AgencyId
                         && r.UserAccessModel.WoodlandOwnerIds.Contains(application.WoodlandOwnerId!)
                         && r.UserAccessModel.WoodlandOwnerIds.Count == 1
                         && r.ApplicationId == application.Id
                         && r.DocumentId == document.Id
                         && r.UserId == _externalApplicant.UserAccountId)
                , CancellationToken.None), Times.Once);
        }


        [Theory, AutoMoqData]
        public async Task WhenDocumentIsSoftDeleted(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut();

            _removeDocumentsService.Setup(r => r.RemoveDocumentAsExternalApplicantAsync(It.IsAny<RemoveDocumentExternalRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success);

            // Simulate application in editable state

            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = _externalApplicant.UserAccountId.Value,
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { application.WoodlandOwnerId }
            };

            _getFellingLicenceApplicationForExternalUsersService.Setup(x => x.GetIsEditable(
                It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            _retrieveUserAccountsService
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userAccessModel);

            var document = application.Documents!.First();

            //act
            var result = await sut.RemoveSupportingDocumentAsync(
                _externalApplicant,
                application.Id,
                document.Id,
                new CancellationToken()
            );

            //assert
            Assert.True(result.IsSuccess);

            _mockUseCaseAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentEvent
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             ApplicationId = application.Id,
                             _externalApplicant.UserAccountId,
                             Section = "Supporting Documentation"
                         }, _options)), CancellationToken.None), Times.Once);

           

           _removeDocumentsService.Verify(v => v.RemoveDocumentAsExternalApplicantAsync(It.Is<RemoveDocumentExternalRequest>(
               r => r.UserAccessModel.IsFcUser == userAccessModel.IsFcUser
                    && r.UserAccessModel.UserAccountId == _externalApplicant.UserAccountId
                    && r.UserAccessModel.AgencyId == userAccessModel.AgencyId
                    && r.UserAccessModel.WoodlandOwnerIds.Contains(application.WoodlandOwnerId!)
                    && r.UserAccessModel.WoodlandOwnerIds.Count == 1
                    && r.ApplicationId == application.Id
                    && r.DocumentId == document.Id
                    && r.UserId == _externalApplicant.UserAccountId)
               , CancellationToken.None), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenAttemptDocumentIsRemovedOnNonEditableFla(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut();

            _getFellingLicenceApplicationForExternalUsersService.Setup(f => f.GetApplicationByIdAsync(
                It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(application);

            // Simulate application in editable state

            _getFellingLicenceApplicationForExternalUsersService.Setup(x => x.GetIsEditable(
                It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var document = application.Documents!.First();

            //act, assert
            var result = await sut.RemoveSupportingDocumentAsync(_externalApplicant, application.Id, document.Id, new CancellationToken());
            Assert.True(result.IsFailure);

            _unitOfWOrkMock.Verify(i => i.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);

        }

        [Theory, AutoMoqData]
        public async Task WhenFellingLicenceDocumentIsRemoved(
            FellingLicenceApplication fla)
        {
            //arrange
            var sut = CreateSut();

            _removeDocumentsService
                .Setup(s => s.PermanentlyRemoveDocumentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            //act

            var result = await sut.RemoveFellingLicenceDocument(_externalApplicant,
                fla.Id,
                fla.Documents.First().Id,
                CancellationToken.None);

            //assert

            _mockUseCaseAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentEvent
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             ApplicationId = fla.Id,
                             _externalApplicant.UserAccountId,
                             Section = "Supporting Documentation",
                         }, _options)), CancellationToken.None), Times.Once);

            Assert.True(result.IsSuccess);

            _removeDocumentsService
                .Verify(x => x.PermanentlyRemoveDocumentAsync(fla.Id, fla.Documents.First().Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenFellingLicenceDocumentCannotBeRemoved(
            FellingLicenceApplication fla)
        {
            //arrange
            var sut = CreateSut();

            _removeDocumentsService
                .Setup(s => s.PermanentlyRemoveDocumentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure("Could not remove file"));

            //act

            var result = await sut.RemoveFellingLicenceDocument(_externalApplicant,
                fla.Id,
                fla.Documents.First().Id,
                CancellationToken.None);

            //assert

            _mockUseCaseAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RemoveFellingLicenceAttachmentFailureEvent
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             ApplicationId = fla.Id,
                             _externalApplicant.UserAccountId,
                             Section = "Supporting Documentation",
                             error = result.Error
                         }, _options)), CancellationToken.None), Times.Once);

            Assert.True(result.IsFailure);

            _removeDocumentsService
                .Verify(x => x.PermanentlyRemoveDocumentAsync(fla.Id, fla.Documents.First().Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        private RemoveSupportingDocumentUseCase CreateSut()
        {
            _mockUseCaseAuditService.Reset();
            _removeDocumentsService.Reset();
            _retrieveWoodlandOwnersService.Reset();
            _unitOfWOrkMock.Reset();
            _fileStorageService.Reset();
            _getFellingLicenceApplicationForExternalUsersService.Reset();
            _clock.Reset();
            _agentAuthorityService.Reset();

            _fileStorageService.Setup(x => x.RemoveFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<FileAccessFailureReason>());

            _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));

            return new RemoveSupportingDocumentUseCase(
                _removeDocumentsService.Object,
                _retrieveUserAccountsService.Object,
                _retrieveWoodlandOwnersService.Object,
                _mockUseCaseAuditService.Object,
                _getFellingLicenceApplicationForExternalUsersService.Object,
                _getPropertyProfilesService.Object,
                _getCompartmentsService.Object,
                _agentAuthorityService.Object,
                new NullLogger<RemoveSupportingDocumentUseCase>(),
                new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)));
        }
    }
}

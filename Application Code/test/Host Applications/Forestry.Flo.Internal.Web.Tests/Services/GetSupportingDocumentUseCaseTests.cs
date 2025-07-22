using System.Security.Claims;
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
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services
{
    public class GetSupportingDocumentUseCaseTests
    {
        private static readonly Fixture FixtureInstance = new();
        private readonly Mock<IFileStorageService> _fileStorageService;
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository;
        private readonly Mock<IAuditService<GetSupportingDocumentUseCase>> _mockUseCaseAuditService;
        private readonly Mock<IAuditService<GetDocumentServiceBase>> _mockGetDocumentService;
        private readonly InternalUser _internalUser;

        public GetSupportingDocumentUseCaseTests()
        {
            _fileStorageService = new Mock<IFileStorageService>();
            _mockUseCaseAuditService = new Mock<IAuditService<GetSupportingDocumentUseCase>>();
            _mockGetDocumentService = new Mock<IAuditService<GetDocumentServiceBase>>();
            _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
            
            var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<Guid>(),
                FixtureInstance.Create<string>(),
                AccountTypeInternal.AdminOfficer);
            _internalUser = new InternalUser(user);
        }

        [Theory, AutoMoqData]
        public async Task WhenApplicationIsNotFound(FellingLicenceApplication application)
        {
            //arrange
            var docBytes = new byte[] { 0x20 };
            var sut = CreateSut(docBytes);
            var document = application.Documents!.First();

            _fellingLicenceApplicationRepository.Setup(f => f.GetDocumentByIdAsync(Guid.NewGuid(), Guid.NewGuid(),
                It.IsAny<CancellationToken>())).ReturnsAsync(document);

            var testDocument = application.Documents!.First();

            //act
            var result = await sut.GetSupportingDocumentAsync(
                _internalUser,
                Guid.NewGuid(),
                testDocument.Id,
                new CancellationToken()
            );

            Assert.True(result.IsFailure);
        }

        [Theory, AutoMoqData]
        public async Task WhenDocumentToGetIsNotFoundInApplication(FellingLicenceApplication application)
        {
            //arrange
            var docBytes = new byte[] { 0x20 };
            const string contentType = "application/pdf";
            var sut = CreateSut(docBytes);
            application.Documents!.First().MimeType = contentType;
            var document = application.Documents!.First();

            _fellingLicenceApplicationRepository.Setup(f => f.GetDocumentByIdAsync(It.IsAny<Guid>(), Guid.NewGuid(),
                It.IsAny<CancellationToken>())).ReturnsAsync(document);

            //act
            var result = await sut.GetSupportingDocumentAsync(
                _internalUser,
                application.Id,
                Guid.NewGuid(),
                new CancellationToken()
            );

            //assert
            Assert.True(result.IsFailure);
        }

        [Theory, AutoMoqData]
        public async Task WhenDocumentIsGotShouldReturnFileForDownload(FellingLicenceApplication application)
        {
            //arrange
            var docBytes = new byte[] { 0x20 };
            const string contentType = "application/pdf";
            var sut = CreateSut(docBytes);
            application.Documents!.First().MimeType=contentType;

            var document = application.Documents!.First();

            _fellingLicenceApplicationRepository.Setup(f => f.GetDocumentByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(document);

            //act
            var result = await sut.GetSupportingDocumentAsync(
                _internalUser,
                application.Id,
                document.Id,
                new CancellationToken()
            );

            //assert
            _mockGetDocumentService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.GetFellingLicenceAttachmentEvent),
                    It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsSuccess);
            Assert.Equal(docBytes, result.Value.FileContents);
            Assert.Equal(contentType, result.Value.ContentType);
            Assert.IsType<FileContentResult>(result.Value);
        }

        private GetSupportingDocumentUseCase CreateSut(byte[] docBytes)
        {
            _mockUseCaseAuditService.Reset();
            _mockGetDocumentService.Reset();
            _fileStorageService.Reset();
            _fellingLicenceApplicationRepository.Reset();

            _fileStorageService.Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    Result.Success<GetFileSuccessResult, FileAccessFailureReason>(new GetFileSuccessResult(docBytes)));

            var getDocumentService = new InternalGetDocumentService(
                _fileStorageService.Object,
                _mockGetDocumentService.Object,
                new RequestContext("test", new RequestUserModel(new ClaimsPrincipal())),
                new NullLogger<GetDocumentServiceBase>(),
                _fellingLicenceApplicationRepository.Object);

            return new GetSupportingDocumentUseCase(getDocumentService,
                new NullLogger<GetSupportingDocumentUseCase>());
        }
    }
}

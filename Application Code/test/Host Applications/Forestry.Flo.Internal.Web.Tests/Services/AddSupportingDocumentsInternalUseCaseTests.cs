using AutoFixture;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System.Text;

namespace Forestry.Flo.Internal.Web.Tests.Services
{
    public class AddSupportingDocumentsInternalUseCaseTests
    {
        private static readonly Fixture FixtureInstance = new();
        private static readonly DateTime UtcNow = DateTime.UtcNow;
        private readonly IClock _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
        private readonly Mock<IFileStorageService> _fileStorageService;
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository;
        private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationExternalRepository;
        private readonly Mock<IAuditService<AddSupportingDocumentsUseCase>> _mockAddSupportingDocumentAuditService;
        private readonly Mock<IAuditService<AddDocumentService>> _mockAddDocumentAuditService;
        private readonly Mock<IAddDocumentService> _addDocumentsService;
        private readonly InternalUser _internalUser;
        private readonly Mock<IUnitOfWork> _unitOfWOrkMock;
        private FormFileCollection _formFileCollection;
        private readonly ModelStateDictionary _modelStateDictionary;

        public AddSupportingDocumentsInternalUseCaseTests()
        {
            _fileStorageService = new Mock<IFileStorageService>();
            _mockAddSupportingDocumentAuditService = new Mock<IAuditService<AddSupportingDocumentsUseCase>>();
            _mockAddDocumentAuditService = new Mock<IAuditService<AddDocumentService>>();
            _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
            _fellingLicenceApplicationExternalRepository = new Mock<IFellingLicenceApplicationExternalRepository>();
            _addDocumentsService = new Mock<IAddDocumentService>();
            _unitOfWOrkMock = new Mock<IUnitOfWork>();
            _formFileCollection = new FormFileCollection();
            _modelStateDictionary = new ModelStateDictionary();

            var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<Guid>(),
                FixtureInstance.Create<string>(),
                AccountTypeInternal.AdminOfficer);
            _internalUser = new InternalUser(user);
        }

        [Theory, AutoMoqData]

        public async Task ShouldAddDocument_WhenAllDetailsCorrect(FellingLicenceApplication fla)
        {
            // setup

            var sut = CreateSut();

            _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            _addDocumentsService.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult([Guid.NewGuid()], new List<string>())));

            AddFileToFormCollection("testFile");

            var result = await sut.AddDocumentsToApplicationAsync(
                _internalUser,
                fla.Id,
                _formFileCollection,
                _modelStateDictionary,
                true,
                true,
                CancellationToken.None);

            // assert

            result.IsSuccess.Should().BeTrue();

            // verify

            _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(
                fla.Id,
                CancellationToken.None)
                , Times.Once);

            _addDocumentsService.Verify(x => x.AddDocumentsAsInternalUserAsync(
                It.Is<AddDocumentsRequest>(r => r.UserAccountId == _internalUser.UserAccountId
                                                && r.ActorType == ActorType.InternalUser
                                                && r.FellingApplicationId == fla.Id
                                                && r.DocumentPurpose == DocumentPurpose.Attachment),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Theory, AutoMoqData]

        public async Task ShouldAddDocument_WhenNoFilesToAdd(FellingLicenceApplication fla)
        {
            // setup

            var sut = CreateSut();

            _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            var result = await sut.AddDocumentsToApplicationAsync(
                _internalUser,
                fla.Id,
                _formFileCollection,
                _modelStateDictionary,
                true,
                true,
                CancellationToken.None);

            // assert

            result.IsSuccess.Should().BeTrue();
        }


        [Fact]

        public async Task ShouldReturnFailure_WhenFLANotFound()
        {
            // setup

            var sut = CreateSut();

            _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

            AddFileToFormCollection();

            var result = await sut.AddDocumentsToApplicationAsync(
                _internalUser,
                Guid.NewGuid(),
                _formFileCollection,
                _modelStateDictionary,
                true,
                true,
                CancellationToken.None);

            // assert

            result.IsFailure.Should().BeTrue();
        }

        private AddSupportingDocumentsUseCase CreateSut(int maxNumberOfDocuments = 2, int maxFileSizeBytes = 1024)
        {
            _mockAddSupportingDocumentAuditService.Reset();
            _mockAddDocumentAuditService.Reset();
            _formFileCollection = new FormFileCollection();
            _unitOfWOrkMock.Reset();
            _fellingLicenceApplicationRepository.Reset();
            _addDocumentsService.Reset();

            _fellingLicenceApplicationRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
            _fellingLicenceApplicationExternalRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);

            var settings = new UserFileUploadOptions
            {
                MaxNumberDocuments = maxNumberOfDocuments,
                MaxFileSizeBytes = maxFileSizeBytes,
                AllowedFileTypes = new[]
                {
                    new AllowedFileType()
                    {
                        FileUploadReasons = [FileUploadReason.SupportingDocument],
                        Description = "test",
                        Extensions = new[] { "csv", "txt" }
                    }
                }
            };


            return new AddSupportingDocumentsUseCase(_addDocumentsService.Object,
                _mockAddSupportingDocumentAuditService.Object,
                _fellingLicenceApplicationRepository.Object,
                new RequestContext("test", new RequestUserModel(_internalUser.Principal)),
                new NullLogger<AddSupportingDocumentsUseCase>());
        }

        private void AddFileToFormCollection(string fileName = "test.csv", string expectedFileContents = "test", string contentType = "text/csv", bool isValid = true)
        {
            var fileBytes = !isValid ? FixtureInstance.CreateMany<byte>(100000).ToArray() : Encoding.Default.GetBytes(expectedFileContents);
            Mock<IFormFile> formFileMock = new Mock<IFormFile>();

            formFileMock.Setup(ff => ff.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((s, ct) =>
                {
                    var buffer = fileBytes;
                    s.Write(buffer, 0, buffer.Length);
                    return Task.CompletedTask;
                });

            formFileMock.Setup(ff => ff.FileName).Returns(fileName);
            formFileMock.Setup(ff => ff.Length).Returns(fileBytes.Length);
            formFileMock.Setup(ff => ff.ContentType).Returns(contentType);

            _formFileCollection.Add(formFileMock.Object);
        }

        private static Result<StoreFileSuccessResult, StoreFileFailureResult> SavedFileSuccessResult(string location, int fileSize)
        {
            var s = new StoreFileSuccessResult(location, fileSize);
            return Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(s);
        }
    }
}

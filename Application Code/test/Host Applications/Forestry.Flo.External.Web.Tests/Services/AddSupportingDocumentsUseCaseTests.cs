using System.Text;
using AutoFixture;
using AutoFixture.AutoMoq;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Tests.Services
{
    public class AddSupportingDocumentsUseCaseTests
    {
        private readonly Fixture _fixtureInstance = new();
        private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsService;
        private readonly Mock<IRetrieveWoodlandOwners> _retrieveWoodlandOwnersService;
        private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _getFellingLicenceApplicationForExternalUsersService;
        private readonly Mock<IGetPropertyProfiles> _getPropertyProfilesService;
        private readonly Mock<IGetCompartments> _getCompartmentsService;
        private readonly Mock<IAddDocumentService> _mockAddDocumentService;
        private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService;

        private readonly Mock<IAuditService<AddSupportingDocumentsUseCase>> _mockAddSupportingDocumentAuditService;
        private readonly Mock<IAuditService<AddDocumentService>> _mockAddDocumentAuditService;
        private readonly ExternalApplicant _externalApplicant;
        private FormFileCollection _formFileCollection;
        private readonly ModelStateDictionary _modelStateDictionary;

        public AddSupportingDocumentsUseCaseTests()
        {
            _fixtureInstance.CustomiseFixtureForFellingLicenceApplications();

            var fileStorageService = new Mock<IFileStorageService>();
            _mockAddSupportingDocumentAuditService = new Mock<IAuditService<AddSupportingDocumentsUseCase>>();
            _mockAddDocumentAuditService = new Mock<IAuditService<AddDocumentService>>();
            _retrieveUserAccountsService = new Mock<IRetrieveUserAccountsService>();
            _mockAddDocumentService = new Mock<IAddDocumentService>();
            _getPropertyProfilesService = new Mock<IGetPropertyProfiles>();
            _getCompartmentsService = new Mock<IGetCompartments>();
            _retrieveWoodlandOwnersService = new Mock<IRetrieveWoodlandOwners>();
            _mockAgentAuthorityService = new();

            _getFellingLicenceApplicationForExternalUsersService =
                new Mock<IGetFellingLicenceApplicationForExternalUsers>();

            _formFileCollection = new FormFileCollection();
            _modelStateDictionary = new ModelStateDictionary();

            var woodlandOwner = _fixtureInstance.Build<WoodlandOwner>()
                .With(wo => wo.IsOrganisation, true)
                .Create();

            var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
                _fixtureInstance.Create<string>(),
                _fixtureInstance.Create<string>(),
                _fixtureInstance.Create<Guid>(),
                woodlandOwner.Id,
                AccountTypeExternal.WoodlandOwnerAdministrator);
            _externalApplicant = new ExternalApplicant(user);

            fileStorageService.Setup(f => f.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SavedFileSuccessResult("testLocation",4));
        }

        [Theory, AutoMoqData]
        public async Task WhenApplicationIsNotFound(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut();
            AddFileToFormCollection("test");

            _getFellingLicenceApplicationForExternalUsersService.Setup(f => f.GetApplicationByIdAsync(
                It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(application);

            // Simulate application in editable state

            _getFellingLicenceApplicationForExternalUsersService.Setup(x => x.GetIsEditable(
                application.Id, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var addSupportingDocumentModel = CreateAddSupportingDocumentModel(application);

            _mockAddDocumentService.Setup(x =>
                    x.AddDocumentsAsExternalApplicantAsync(It.IsAny<AddDocumentsExternalRequest>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsFailureResult(
                    userFacingFailureMessages: new[] { $"An application cannot be retrieved with an id of {application.Id}." })));

            //act
            var result = await sut.AddDocumentsToApplicationAsync(
                _externalApplicant,
                addSupportingDocumentModel,
                _formFileCollection,
                _modelStateDictionary,
                new CancellationToken()
            );

            //assert
            _mockAddSupportingDocumentAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Never);

            Assert.True(result.IsFailure);
        }

        [Theory]
        [InlineData(DocumentPurpose.Attachment)]
        [InlineData(DocumentPurpose.EiaAttachment)]
        [InlineData(DocumentPurpose.WmpDocument)]
        public async Task WhenSuccess(DocumentPurpose purpose)
        {
            var application = _fixtureInstance.Create<FellingLicenceApplication>();

            //arrange
            var sut = CreateSut();
            AddFileToFormCollection("test");

            _getFellingLicenceApplicationForExternalUsersService.Setup(f => f.GetApplicationByIdAsync(
                It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(application);

            // Simulate application in editable state

            _getFellingLicenceApplicationForExternalUsersService.Setup(x => x.GetIsEditable(
                application.Id, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var addSupportingDocumentModel = CreateAddSupportingDocumentModel(application);
            addSupportingDocumentModel.Purpose = purpose;

            _mockAddDocumentService.Setup(x =>
                    x.AddDocumentsAsExternalApplicantAsync(It.IsAny<AddDocumentsExternalRequest>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult(
                    [Guid.NewGuid()], [])));

            //act
            var result = await sut.AddDocumentsToApplicationAsync(
                _externalApplicant,
                addSupportingDocumentModel,
                _formFileCollection,
                _modelStateDictionary, 
                CancellationToken.None
            );

            //assert
            _mockAddSupportingDocumentAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Never);

            _mockAddDocumentService
                .Verify(x => x.AddDocumentsAsExternalApplicantAsync(It.Is<AddDocumentsExternalRequest>(a =>
                    a.ActorType == ActorType.ExternalApplicant
                    && a.ApplicationDocumentCount == application.Documents!.Count(d => d.Purpose == DocumentPurpose.Attachment && d.DeletionTimestamp == null)
                    && a.DocumentPurpose == purpose
                    && a.FellingApplicationId == application.Id
                    && a.FileToStoreModels.Count == 1
                    && a.FileToStoreModels.Single().FileName == "test"
                    && a.ReceivedByApi == false
                    && a.UserAccountId == _externalApplicant.UserAccountId
                    && a.VisibleToApplicant == true
                    && a.VisibleToConsultee == addSupportingDocumentModel.AvailableToConsultees
                    && a.WoodlandOwnerId == application.WoodlandOwnerId
                    ), It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsSuccess);
        }

        [Theory, AutoMoqData]
        public async Task WhenEmptyFileCollection(FellingLicenceApplication application)
        {
            //arrange
            var sut = CreateSut();

            _getFellingLicenceApplicationForExternalUsersService.Setup(f => f.GetApplicationByIdAsync(
                It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(application);

            var addSupportingDocumentModel = CreateAddSupportingDocumentModel(application);

            //act
            var result = await sut.AddDocumentsToApplicationAsync(
                _externalApplicant,
                addSupportingDocumentModel,
                _formFileCollection,
                _modelStateDictionary,
                new CancellationToken()
            );

            //assert
            _mockAddSupportingDocumentAuditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(e => e.EventName == AuditEvents.UpdateFellingLicenceApplication),
                    It.IsAny<CancellationToken>()), Times.Never);

            Assert.True(result.IsSuccess);
        }

        private AddSupportingDocumentsUseCase CreateSut(int maxNumberOfDocuments=2, int maxFileSizeBytes=1024)
        {
            _mockAddSupportingDocumentAuditService.Reset();
            _mockAddDocumentAuditService.Reset();
            _formFileCollection = new FormFileCollection();
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

            return new AddSupportingDocumentsUseCase(
                _mockAddDocumentService.Object,
                _mockAddSupportingDocumentAuditService.Object,
                _getFellingLicenceApplicationForExternalUsersService.Object,
                _retrieveUserAccountsService.Object,
                _retrieveWoodlandOwnersService.Object,
                _getPropertyProfilesService.Object,
                _getCompartmentsService.Object,
                _mockAgentAuthorityService.Object,
                new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
                new NullLogger<AddSupportingDocumentsUseCase>());
        }

        private void AddFileToFormCollection(string fileName="test.csv", string expectedFileContents="test", string contentType="text/csv", bool isValid=true)
        {
            var fileBytes = !isValid ? _fixtureInstance.CreateMany<byte>(100000).ToArray() : Encoding.Default.GetBytes(expectedFileContents);
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

        private static AddSupportingDocumentModel CreateAddSupportingDocumentModel(FellingLicenceApplication application)
        {
            return new AddSupportingDocumentModel
            {
                DocumentCount = application.Documents!.Count,
                FellingLicenceApplicationId = application.Id
            };
        }

        private static Result<StoreFileSuccessResult, StoreFileFailureResult> SavedFileFailureResultWithFailureReason(StoreFileFailureResultReason reason)
        {
            return Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(new StoreFileFailureResult(reason));
        }

        private static Result<StoreFileSuccessResult, StoreFileFailureResult> SavedFileFailureResultWithInvalidFileReason(FileInvalidReason reason)
        {
            return Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(StoreFileFailureResult.CreateWithInvalidFileReason(reason));
        }
    }
}

using System.Text.Json;
using Forestry.Flo.External.Web.Services.ExternalApi;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NodaTime.Testing;
using NodaTime;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.External.Web.Tests.Services.Api
{
    public class AddDocumentFromExternalSystemUseCaseTests
    {
        private static readonly Fixture FixtureInstance = new();
        private static readonly DateTime UtcNow = DateTime.UtcNow;
        private readonly IClock _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
        private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationRepository;
        private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _fellingLicenceApplicationService;
        private readonly Mock<IAuditService<AddDocumentFromExternalSystemUseCase>> _mockAddDocumentFromExternalSystemUseCaseAuditService;
        private readonly Mock<IAuditService<AddDocumentService>> _mockAddDocumentAuditService;
        private readonly Mock<IFileStorageService> _fileStorageService;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAddDocumentService> _addDocumentService;
        private readonly WoodlandOwner _woodlandOwner;
        private readonly ExternalApplicant _externalApplicant;
        private readonly Mock<IOptions<DocumentVisibilityOptions>> _visibilityOptions;

        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AddDocumentFromExternalSystemUseCaseTests()
        {
            FixtureInstance.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => FixtureInstance.Behaviors.Remove(b));
            FixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());

            _mockAddDocumentFromExternalSystemUseCaseAuditService = new Mock<IAuditService<AddDocumentFromExternalSystemUseCase>>();
            _mockAddDocumentAuditService = new Mock<IAuditService<AddDocumentService>>();
            _fellingLicenceApplicationService = new Mock<IGetFellingLicenceApplicationForExternalUsers>();
            _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationExternalRepository>();
            _fileStorageService = new Mock<IFileStorageService>();
            _addDocumentService = new Mock<IAddDocumentService>();
            _visibilityOptions = new Mock<IOptions<DocumentVisibilityOptions>>();

            _fileStorageService.Reset();
            _fellingLicenceApplicationService.Reset();
            _fellingLicenceApplicationRepository.Reset();
            _addDocumentService.Reset();
            _visibilityOptions.Reset();   

            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _fellingLicenceApplicationRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);

            _visibilityOptions.Setup(x => x.Value).Returns(new DocumentVisibilityOptions
            {
                ApplicationDocument = new DocumentVisibilityOptions.VisibilityOptions
                {
                    VisibleToApplicant = true,
                    VisibleToConsultees = true
                },
                ExternalLisConstraintReport = new DocumentVisibilityOptions.VisibilityOptions
                {
                    VisibleToApplicant = true,
                    VisibleToConsultees = true
                },
                FcLisConstraintReport = new DocumentVisibilityOptions.VisibilityOptions
                {
                    VisibleToApplicant = false,
                    VisibleToConsultees = false
                }, 
                SiteVisitAttachment = new DocumentVisibilityOptions.VisibilityOptions
                {
                    VisibleToApplicant = true,
                    VisibleToConsultees = true
                }
            });

            _fileStorageService.Setup(f => f.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SavedFileSuccessResult("testLocation",4));

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

        [Theory]
        [InlineData(FellingLicenceStatus.Draft)]
        [InlineData(FellingLicenceStatus.WithApplicant)]
        [InlineData(FellingLicenceStatus.ReturnedToApplicant)]
        public async Task WhenFlaFoundAndIsStillWithApplicantThenCanSaveExternalLisDocument(FellingLicenceStatus status)
        {
            //arrange
            _mockAddDocumentAuditService.Reset();
            _mockAddDocumentFromExternalSystemUseCaseAuditService.Reset();
            var application = FixtureInstance.Create<FellingLicenceApplication>();

            //arrange
            application.StatusHistories.Add(
                    new StatusHistory
                    {
                        Created = DateTime.Now.AddYears(25),//make sure most recent!
                        Status = FellingLicenceStatus.Draft,
                        FellingLicenceApplication = application,
                        FellingLicenceApplicationId = application.Id
                    }
                );

            _fellingLicenceApplicationService.Setup(r => r.GetApplicationByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);
            
            _fellingLicenceApplicationRepository.Setup(f => f.GetAsync(Guid.NewGuid(),
                It.IsAny<CancellationToken>())).ReturnsAsync(application);

            _addDocumentService.Setup(r =>
                    r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(
                    new AddDocumentsSuccessResult(new List<string>())));

            var docBytes = new byte[10];
            var docName = "testdoc";
            var maxDocs = application.Documents.Count + 1;
            var sut = CreateSut(maxNumberOfDocuments:maxDocs);

            //act
            var result = await sut.AddLisConstraintReportAsync(
                application.Id, 
                docBytes, 
                docName, 
                "application/pdf",
                DocumentPurpose.ExternalLisConstraintReport, 
                new CancellationToken());

            //assert
            var sc = result as StatusCodeResult;
            Assert.NotNull(sc);
            Assert.Equal(StatusCodes.Status201Created, sc.StatusCode );

            _mockAddDocumentFromExternalSystemUseCaseAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                    y.EventName == AuditEvents.LISConstraintReportConsumedOk
                    && y.SourceEntityId == application.Id
                    && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                    && JsonSerializer.Serialize(y.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        name = docName,
                        contentType = "application/pdf",
                        size = docBytes.Length,
                        documentPurpose = DocumentPurpose.ExternalLisConstraintReport,
                    }, _options)
                )
                ,
                It.IsAny<CancellationToken>()), Times.Once);

            _addDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(
                It.Is<AddDocumentsRequest>(r =>
                                                r.ActorType == ActorType.System
                                                && r.FellingApplicationId == application.Id
                                                && r.DocumentPurpose == DocumentPurpose.ExternalLisConstraintReport),
                It.IsAny<CancellationToken>()), Times.Once());

            _fellingLicenceApplicationService.Verify(x => x.GetApplicationByIdAsync(
                application.Id, It.Is<UserAccessModel>(u => u.IsFcUser), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenFlaNotFoundDoesNotSaveDocument(FellingLicenceApplication application)
        {
            //arrange
            application.StatusHistories.Add(
                    new StatusHistory()
                    {
                        Created = DateTime.Now.AddYears(25),//make sure most recent!
                        Status = FellingLicenceStatus.Draft,
                        FellingLicenceApplication = application,
                        FellingLicenceApplicationId = application.Id
                    }
                );

            _fellingLicenceApplicationService.Setup(r => r.GetApplicationByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<FellingLicenceApplication>("error"));

            _fellingLicenceApplicationRepository.Setup(f => f.GetAsync(Guid.NewGuid(),
                It.IsAny<CancellationToken>())).ReturnsAsync( Maybe<FellingLicenceApplication>.None );

            var docBytes = new byte[10];
            var docName = "testdoc";
            var maxDocs = application.Documents.Count + 1;
            var sut = CreateSut(maxNumberOfDocuments:maxDocs);

            //act

            var result = await sut.AddLisConstraintReportAsync(
                application.Id, 
                docBytes, 
                docName, 
                "application/pdf",
                DocumentPurpose.ExternalLisConstraintReport, 
                new CancellationToken());

            var sc = result as StatusCodeResult;

            //assert

            Assert.NotNull(sc);
            Assert.Equal(sc.StatusCode, StatusCodes.Status500InternalServerError);

            _mockAddDocumentFromExternalSystemUseCaseAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                    y.EventName == AuditEvents.LISConstraintReportConsumedFailure
                    && y.SourceEntityId == application.Id
                    && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                    && JsonSerializer.Serialize(y.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        Error = "Felling Licence application not found."
                    }, _options)
                )
                ,
                It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Theory]
        [InlineData(FellingLicenceStatus.Withdrawn)]
        [InlineData(FellingLicenceStatus.Approved)]
        [InlineData(FellingLicenceStatus.Refused)]
        [InlineData(FellingLicenceStatus.Received)]
        [InlineData(FellingLicenceStatus.SentForApproval)]
        [InlineData(FellingLicenceStatus.Submitted)]
        [InlineData(FellingLicenceStatus.WoodlandOfficerReview)]
        public async Task WhenFlaHasNonApplicantStatusCanNotSaveExternalLisDocument(FellingLicenceStatus status)
        {
            //arrange
            _mockAddDocumentAuditService.Reset();
            _mockAddDocumentFromExternalSystemUseCaseAuditService.Reset();
            var application = FixtureInstance.Create<FellingLicenceApplication>();

            application.StatusHistories.Add(
                    new StatusHistory()
                    {
                        Created = DateTime.Now.AddYears(25),//make sure most recent!
                        Status = status,
                        FellingLicenceApplication = application,
                        FellingLicenceApplicationId = application.Id
                    }
                );

            _fellingLicenceApplicationService.Setup(r => r.GetApplicationByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);

            _fellingLicenceApplicationRepository.Setup(f => f.GetAsync(Guid.NewGuid(),
                It.IsAny<CancellationToken>())).ReturnsAsync( application );

            var docBytes = new byte[10];
            var docName = "testdoc";
            var maxDocs = application.Documents.Count + 1;
            var sut = CreateSut(maxNumberOfDocuments:maxDocs);

            //act

            var result = await sut.AddLisConstraintReportAsync(
                application.Id, 
                docBytes, 
                docName, 
                "application/pdf",
                DocumentPurpose.ExternalLisConstraintReport, 
                new CancellationToken());

            var sc = result as StatusCodeResult;

            //assert

            Assert.NotNull(sc);
            Assert.Equal(sc.StatusCode, StatusCodes.Status500InternalServerError);

            _mockAddDocumentFromExternalSystemUseCaseAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                    y.EventName == AuditEvents.LISConstraintReportConsumedFailure
                    && y.SourceEntityId == application.Id
                    && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                    && JsonSerializer.Serialize(y.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        Error = "Felling Licence application has incorrect state to accept document."
                    }, _options)
                )
                ,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        private AddDocumentFromExternalSystemUseCase CreateSut(int maxNumberOfDocuments=2, int maxFileSizeBytes=1024)
        {
            _mockAddDocumentFromExternalSystemUseCaseAuditService.Reset();
            _mockAddDocumentAuditService.Reset();
          
            var settings = new UserFileUploadOptions
            {
                MaxNumberDocuments = maxNumberOfDocuments,
                MaxFileSizeBytes = maxFileSizeBytes,
                AllowedFileTypes = new[]
                {
                    new AllowedFileType()
                    {
                        FileUploadReasons = [FileUploadReason.SupportingDocument, FileUploadReason.AgentAuthorityForm],
                        Description = "test",
                        Extensions = new[] { "csv", "txt" }
                    }
                }
            };

            return new AddDocumentFromExternalSystemUseCase(
                _fellingLicenceApplicationService.Object,
                _addDocumentService.Object,
                _mockAddDocumentFromExternalSystemUseCaseAuditService.Object,
                new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
                new NullLogger<AddDocumentFromExternalSystemUseCase>(),
                _visibilityOptions.Object);
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

        private static Result<StoreFileSuccessResult, StoreFileFailureResult> SavedFileFailureResultWithInvalidFileReason(FileInvalidReason reason)
        {
            return Result.Failure<StoreFileSuccessResult, StoreFileFailureResult>(StoreFileFailureResult.CreateWithInvalidFileReason(reason));
        }
    }
}

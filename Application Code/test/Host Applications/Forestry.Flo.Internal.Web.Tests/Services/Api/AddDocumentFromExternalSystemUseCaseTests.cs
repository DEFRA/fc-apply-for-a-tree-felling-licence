using System.Text.Json;
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
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NodaTime.Testing;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Tests.Services.Api
{
    public class AddDocumentFromExternalSystemUseCaseTests
    {
        private static readonly Fixture FixtureInstance = new();
        private static readonly DateTime UtcNow = DateTime.UtcNow;
        private readonly IClock _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
        private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationRepository;
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepository;
        private readonly Mock<IAuditService<AddDocumentFromExternalSystemUseCase>> _mockAddDocumentFromExternalSystemUseCaseAuditService;
        private readonly Mock<IAddDocumentService> _addDocumentService;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly InternalUser _internalUser;
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
            _fellingLicenceApplicationInternalRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
            _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationExternalRepository>();
            _visibilityOptions = new Mock<IOptions<DocumentVisibilityOptions>>();
            _addDocumentService = new Mock<IAddDocumentService>();
            _fellingLicenceApplicationInternalRepository.Reset();
            _fellingLicenceApplicationRepository.Reset();
            _visibilityOptions.Reset();;

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

            var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<Guid>(),
                FixtureInstance.Create<string>(),
                AccountTypeInternal.AccountAdministrator);

            _internalUser = new InternalUser(user);
        }

        [Theory]
        [InlineData(FellingLicenceStatus.Received)]
        [InlineData(FellingLicenceStatus.Submitted)]
        [InlineData(FellingLicenceStatus.AdminOfficerReview)]
        [InlineData(FellingLicenceStatus.WoodlandOfficerReview)]
        [InlineData(FellingLicenceStatus.SentForApproval)]
        public async Task WhenFlaFoundAndIsNotWithdrawnOrRefusedThenCanSaveDocument(FellingLicenceStatus status)
        {
            //arrange
            _mockAddDocumentFromExternalSystemUseCaseAuditService.Reset();
            var application = FixtureInstance.Create<FellingLicenceApplication>();

            _addDocumentService.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult(new List<string>())));

            application.StatusHistories.Add(
                    new StatusHistory
                    {
                        Created = DateTime.Now.AddYears(25),//make sure most recent!
                        Status = status,
                        FellingLicenceApplication = application,
                        FellingLicenceApplicationId = application.Id
                    }
                );

            _fellingLicenceApplicationInternalRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);
            
            var docBytes = new byte[10];
            var docName = "testdoc";
            var maxDocs = application.Documents.Count + 1;
            var sut = CreateSut(maxNumberOfDocuments:maxDocs);

            var result = await sut.AddLisConstraintReportAsync(
                application.Id, 
                docBytes, 
                docName, 
                "application/pdf",
                DocumentPurpose.FcLisConstraintReport, 
                new CancellationToken());

            //assert
            var sc = result as StatusCodeResult;
            Assert.NotNull(sc);
            Assert.Equal(StatusCodes.Status201Created, sc.StatusCode );

            _addDocumentService.Verify(v =>
                v.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a => 
                    a.ActorType == ActorType.System 
                    && a.ReceivedByApi == true
                    && a.UserAccountId == null
                    && a.FellingApplicationId == application.Id
                    && a.DocumentPurpose == DocumentPurpose.FcLisConstraintReport
                    && a.ApplicationDocumentCount == application.Documents.Count(x => x.DeletionTimestamp == null)),
                    CancellationToken.None),
                Times.Once);

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
                    documentPurpose = DocumentPurpose.FcLisConstraintReport
                }, _options)
                )
                ,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenFlaNotFoundDoesNotSaveDocument(FellingLicenceApplication application)
        {
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

            _fellingLicenceApplicationInternalRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( Maybe<FellingLicenceApplication>.None);

            var docBytes = new byte[10];
            var docName = "testdoc";
            var maxDocs = application.Documents.Count + 1;
            var sut = CreateSut(maxNumberOfDocuments:maxDocs);

            var result = await sut.AddLisConstraintReportAsync(
                application.Id, 
                docBytes, 
                docName, 
                "application/pdf",
                DocumentPurpose.FcLisConstraintReport, 
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
        
        [Theory, AutoMoqData]
        public async Task WhenFlaHasRefusedStatusCanNotSaveDocument(FellingLicenceApplication application)
        {
            //arrange
            application.StatusHistories.Add(
                    new StatusHistory
                    {
                        Created = DateTime.Now.AddYears(25),//make sure most recent!
                        Status = FellingLicenceStatus.Refused,
                        FellingLicenceApplication = application,
                        FellingLicenceApplicationId = application.Id
                    }
                );

            _fellingLicenceApplicationInternalRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);

           
            var docBytes = new byte[10];
            var docName = "testdoc";
            var maxDocs = application.Documents.Count + 1;
            var sut = CreateSut(maxNumberOfDocuments:maxDocs);

            var result = await sut.AddLisConstraintReportAsync(
                application.Id, 
                docBytes, 
                docName, 
                "application/pdf",
                DocumentPurpose.FcLisConstraintReport, 
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

        [Theory]
        [InlineData(FellingLicenceStatus.Draft)] 
        [InlineData(FellingLicenceStatus.WithApplicant)]
        [InlineData(FellingLicenceStatus.ReturnedToApplicant)]
        [InlineData(FellingLicenceStatus.Approved)]
        [InlineData(FellingLicenceStatus.Refused)]
        public async Task WhenFlaHasNonFcStatusCanNotSaveDocument(FellingLicenceStatus status)
        {
            _mockAddDocumentFromExternalSystemUseCaseAuditService.Reset();
            var application = FixtureInstance.Create<FellingLicenceApplication>();

            _addDocumentService.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult(new List<string>())));

            application.StatusHistories.Add(
                new StatusHistory
                {
                    Created = DateTime.Now.AddYears(25),//make sure most recent!
                    Status = status,
                    FellingLicenceApplication = application,
                    FellingLicenceApplicationId = application.Id
                }
            );

            _fellingLicenceApplicationInternalRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);


            var docBytes = new byte[10];
            var docName = "testdoc";
            var maxDocs = application.Documents.Count + 1;
            var sut = CreateSut(maxNumberOfDocuments:maxDocs);

            var result = await sut.AddLisConstraintReportAsync(
                application.Id, 
                docBytes, 
                docName, 
                "application/pdf",
                DocumentPurpose.FcLisConstraintReport, 
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

        [Theory, AutoMoqData]
        public async Task WhenErrorsDuringAdding(FellingLicenceApplication application)
        {
            //arrange
            application.StatusHistories.Add(
                    new StatusHistory()
                    {
                        Created = DateTime.Now.AddYears(25),//make sure most recent!
                        Status = FellingLicenceStatus.Submitted,
                        FellingLicenceApplication = application,
                        FellingLicenceApplicationId = application.Id
                    }
                );

            _fellingLicenceApplicationInternalRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( application);

            _addDocumentService.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsFailureResult(new List<string>())));

            var docBytes = new byte[10];
            var docName = "testdoc";

            var maxDocs = application.Documents.Count + 1;
            var sut = CreateSut(maxNumberOfDocuments:maxDocs);

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


            _addDocumentService.Verify(v =>
                    v.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a =>
                            a.ActorType == ActorType.System
                            && a.ReceivedByApi == true
                            && a.UserAccountId == null
                            && a.FellingApplicationId == application.Id
                            && a.DocumentPurpose == DocumentPurpose.ExternalLisConstraintReport
                            && a.ApplicationDocumentCount == application.Documents.Count(x => x.DeletionTimestamp == null)),
                        CancellationToken.None),
                Times.Once);

            _mockAddDocumentFromExternalSystemUseCaseAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                    y.EventName == AuditEvents.LISConstraintReportConsumedFailure
                    && y.SourceEntityId == application.Id
                    && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                )
                ,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        private AddDocumentFromExternalSystemUseCase CreateSut(int maxNumberOfDocuments=2, int maxFileSizeBytes=1024)
        {
            _mockAddDocumentFromExternalSystemUseCaseAuditService.Reset();
          
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
                _fellingLicenceApplicationInternalRepository.Object,
                _addDocumentService.Object,
                _mockAddDocumentFromExternalSystemUseCaseAuditService.Object,
                new RequestContext("test", new RequestUserModel(_internalUser.Principal)),
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
    }
}

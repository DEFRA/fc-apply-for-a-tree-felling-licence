using AutoFixture;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Migrations;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NodaTime;
using System.Text.Json;
using System.Threading;
using ExternalAccountModel = Forestry.Flo.Services.Applicants.Models.UserAccountModel;
using InternalUserAccount = Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount;
using JsonSerializer = System.Text.Json.JsonSerializer;
using WoodlandOwnerModel = Forestry.Flo.Services.Applicants.Models.WoodlandOwnerModel;

namespace Forestry.Flo.Internal.Web.Tests.Services
{
    public class GeneratePdfApplicationUseCaseTests
    {
        private static readonly Fixture FixtureInstance = new();
        private static readonly DateTime UtcNow = DateTime.UtcNow;
        private readonly Mock<IClock> _clock;
        private readonly Mock<IFileStorageService> _fileStorageService;
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepositoryMock;
        private readonly Mock<IAuditService<GeneratePdfApplicationUseCase>> _generatePdfApplicationAuditServiceMock;
        private readonly Mock<IAuditService<AddDocumentService>> _addDocumentAuditServiceMock;
        private readonly Mock<IAddDocumentService>? _addDocumentsServiceMock;

        private readonly Mock<IAuditService<CreateApplicationSnapshotDocumentService>>
            _createApplicationSnapshotDocumentAuditServiceMock;

        private readonly Mock<ICreateApplicationSnapshotDocumentService> _createApplicationSnapshotDocumentServiceMock;
        private readonly Mock<IGetWoodlandOfficerReviewService> _getWoodlandOfficerReviewServiceMock;
        private readonly Mock<IApproverReviewService> _approverReviewServiceMock;
        private readonly Mock<IRetrieveUserAccountsService> _externalAccountServiceMock;
        private readonly Mock<IUserAccountService> _internalAccountServiceMock;
        private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerServiceMock;
        private readonly Mock<IPropertyProfileRepository> _propertyProfileRepositoryMock;
        private readonly Mock<IForesterServices> _foresterAccessMock;
        private readonly Mock<ICalculateConditions> _calculateConditionsServiceMock;
        private readonly Mock<IOptions<DocumentVisibilityOptions>> _documentVisibilityOptions;
        private readonly Mock<IOptions<PDFGeneratorAPIOptions>> _licencePdfGeneratorApiOptions;
        private readonly Mock<IGetConfiguredFcAreas> _mockGetFcAreas;

        private readonly InternalUser _internalUser;
        private readonly Mock<IUnitOfWork> _unitOfWOrkMock;

        private static readonly DocumentVisibilityOptions DocumentVisibilityOptions = new() {
            ApplicationDocument = new DocumentVisibilityOptions.VisibilityOptions {
                VisibleToApplicant = true,
                VisibleToConsultees = true
            },
            ExternalLisConstraintReport = new DocumentVisibilityOptions.VisibilityOptions {
                VisibleToApplicant = true,
                VisibleToConsultees = true
            },
            FcLisConstraintReport = new DocumentVisibilityOptions.VisibilityOptions {
                VisibleToApplicant = false,
                VisibleToConsultees = false
            },
            SiteVisitAttachment = new DocumentVisibilityOptions.VisibilityOptions {
                VisibleToApplicant = true,
                VisibleToConsultees = false
            }
        };

        private static readonly PDFGeneratorAPIOptions PDFGeneratorAPIOptions = new() {
            BaseUrl = "",
            TemplateName = "FellingLicence.html",
            Version = "0.0"
        };

        public GeneratePdfApplicationUseCaseTests()
        {
            _fellingLicenceApplicationRepositoryMock = new Mock<IFellingLicenceApplicationInternalRepository>();
            _generatePdfApplicationAuditServiceMock = new Mock<IAuditService<GeneratePdfApplicationUseCase>>();
            _addDocumentAuditServiceMock = new Mock<IAuditService<AddDocumentService>>();
            _addDocumentsServiceMock = new Mock<IAddDocumentService>();
            _createApplicationSnapshotDocumentAuditServiceMock = new Mock<IAuditService<CreateApplicationSnapshotDocumentService>>();
            _createApplicationSnapshotDocumentServiceMock = new Mock<ICreateApplicationSnapshotDocumentService>();
            _getWoodlandOfficerReviewServiceMock = new Mock<IGetWoodlandOfficerReviewService>();
            _approverReviewServiceMock = new Mock<IApproverReviewService>();
            _externalAccountServiceMock = new Mock<IRetrieveUserAccountsService>();
            _internalAccountServiceMock = new Mock<IUserAccountService>();
            _woodlandOwnerServiceMock = new Mock<IRetrieveWoodlandOwners>();
            _propertyProfileRepositoryMock = new Mock<IPropertyProfileRepository>();
            _foresterAccessMock = new Mock<IForesterServices>();
            _calculateConditionsServiceMock = new Mock<ICalculateConditions>();
            _documentVisibilityOptions = new Mock<IOptions<DocumentVisibilityOptions>>();
            _licencePdfGeneratorApiOptions = new Mock<IOptions<PDFGeneratorAPIOptions>>();
            _mockGetFcAreas = new Mock<IGetConfiguredFcAreas>();

            _clock = new Mock<IClock>();
            _fileStorageService = new Mock<IFileStorageService>();
            _unitOfWOrkMock = new Mock<IUnitOfWork>();

            _documentVisibilityOptions.Reset();
            _documentVisibilityOptions.Setup(x => x.Value).Returns(DocumentVisibilityOptions);


            _licencePdfGeneratorApiOptions.Reset();
            _licencePdfGeneratorApiOptions.Setup(x => x.Value).Returns(PDFGeneratorAPIOptions);

            _fileStorageService.Setup(f => f.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SavedFileSuccessResult("testLocation", 4));

            var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<Guid>(),
                FixtureInstance.Create<string>(),
                AccountTypeInternal.AdminOfficer);
            _internalUser = new InternalUser(user);
        }

        private readonly JsonSerializerOptions _options = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Theory, AutoMoqData]
        public async Task ShouldAddPDFApplicaton_WhenNoFellingAndRestockingDetailsNotApproved(
            FellingLicenceApplication fla,
            ExternalAccountModel applicant,
            WoodlandOwnerModel woodlandOwner,
            InternalUserAccount approverAccount,
            PropertyProfile propertyProfile,
            WoodlandOfficerReviewStatusModel woodlandOfficerReviewStatus,
            ConditionsStatusModel conditionsStatus,
            ConditionsResponse restockingConditionsRetrieved,
            Stream generatedMaps,
            Polygon gisData,
            byte[] pdfGenerated,
            ApproverReviewModel approverReview)
        {
            // setup
            var sut = CreateSut();

            fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

            foreach (var comp in fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!) {
                comp.Id = propertyProfile.Compartments[0].Id;
                comp.CompartmentId = propertyProfile.Compartments[0].Id;
                foreach (var felling in comp.ConfirmedFellingDetails) {
                    if (felling.SubmittedFlaPropertyCompartment == null) {
                        felling.SubmittedFlaPropertyCompartment = new SubmittedFlaPropertyCompartment();
                    }

                    felling.SubmittedFlaPropertyCompartment.CompartmentId = propertyProfile.Compartments[0].Id;
                    felling.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments[0].Id;

                    foreach (var restocking in felling.ConfirmedRestockingDetails) {
                        restocking.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments[0].Id;
                    }
                }
            }

            foreach (var compartment in propertyProfile.Compartments) {
                compartment.GISData = JsonConvert.SerializeObject(gisData);
            }

            var currentDate = DateTime.UtcNow.Date;
            fla.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = currentDate.AddDays(-3),
                    FellingLicenceApplication = fla,
                    Status = FellingLicenceStatus.Submitted
                }
            };

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps, approverReview);

            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pdfGenerated);

            _addDocumentsServiceMock.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult(new List<string>())));


            var result = await sut.GeneratePdfApplicationAsync(_internalUser, fla.Id, false, CancellationToken.None);

            // assert

            result.IsSuccess.Should().BeTrue();

            // verify

            _createApplicationSnapshotDocumentServiceMock.Verify(v => v.CreateApplicationSnapshotAsync(
                fla.Id,
                It.IsAny<PDFGeneratorRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _fellingLicenceApplicationRepositoryMock.Verify(v => v.GetAsync(
                fla.Id,
                CancellationToken.None)
                , Times.AtLeastOnce);

            _addDocumentsServiceMock.Verify(x => x.AddDocumentsAsInternalUserAsync(
                It.Is<AddDocumentsRequest>(r => r.UserAccountId == _internalUser.UserAccountId
                                                && r.ActorType == ActorType.InternalUser
                                                && r.FellingApplicationId == fla.Id
                                                && r.DocumentPurpose == DocumentPurpose.ApplicationDocument
                                                && !r.VisibleToApplicant
                                                && !r.VisibleToConsultee),
                It.IsAny<CancellationToken>()), Times.Once());

            _generatePdfApplicationAuditServiceMock.Verify(v =>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.GeneratingPdfFellingLicence
                        && JsonSerializer.Serialize(e.AuditData, _options) ==
                        JsonSerializer.Serialize(new {
                            ApplicationId = fla.Id,
                            IsFinal = false
                        }, _options)),
                    CancellationToken.None
                    ));

            _mockGetFcAreas.Verify();
        }



        [Theory, AutoMoqData]
        public async Task ShouldAddPDFApplicaton_WhenGivenFellingAndRestockingDetailsNotApproved(
            FellingLicenceApplication fla,
            ExternalAccountModel applicant,
            WoodlandOwnerModel woodlandOwner,
            InternalUserAccount approverAccount,
            PropertyProfile propertyProfile,
            WoodlandOfficerReviewStatusModel woodlandOfficerReviewStatus,
            ConditionsStatusModel conditionsStatus,
            ConditionsResponse restockingConditionsRetrieved,
            Stream generatedMaps,
            byte[] pdfGenerated,
            Polygon gisData, ApproverReviewModel approverReview)
        {
            // setup

            var sut = CreateSut();

            foreach (var felling in fla.LinkedPropertyProfile?.ProposedFellingDetails) {
                felling.PropertyProfileCompartmentId = propertyProfile.Compartments.FirstOrDefault().Id;

                foreach (var restocking in felling.ProposedRestockingDetails) {
                    restocking.PropertyProfileCompartmentId = propertyProfile.Compartments.FirstOrDefault().Id;
                }
            }

            foreach (var comp in fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments) {
                comp.Id = propertyProfile.Compartments.FirstOrDefault().Id;
                comp.CompartmentId = propertyProfile.Compartments.FirstOrDefault().Id;
                foreach (var felling in comp.ConfirmedFellingDetails) {
                    if (felling.SubmittedFlaPropertyCompartment == null) {
                        felling.SubmittedFlaPropertyCompartment = new SubmittedFlaPropertyCompartment();
                    }

                    felling.SubmittedFlaPropertyCompartment.CompartmentId = propertyProfile.Compartments.FirstOrDefault().Id;
                    felling.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments.FirstOrDefault().Id;

                    foreach (var restocking in felling.ConfirmedRestockingDetails) {
                        restocking.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments.FirstOrDefault().Id;
                    }
                }
            }

            foreach (var compartment in propertyProfile.Compartments) {
                compartment.GISData = JsonConvert.SerializeObject(gisData);
            }

            var currentDate = DateTime.UtcNow.Date;
            fla.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = currentDate.AddDays(-3),
                    FellingLicenceApplication = fla,
                    Status = FellingLicenceStatus.Submitted
                }
            };

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps, approverReview);

            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pdfGenerated);

            _addDocumentsServiceMock.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult(new List<string>())));


            var result = await sut.GeneratePdfApplicationAsync(_internalUser, fla.Id, false, CancellationToken.None);

            // assert

            result.IsSuccess.Should().BeTrue();

            // verify

            _createApplicationSnapshotDocumentServiceMock.Verify(v => v.CreateApplicationSnapshotAsync(
                fla.Id,
                It.IsAny<PDFGeneratorRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _fellingLicenceApplicationRepositoryMock.Verify(v => v.GetAsync(
                fla.Id,
                CancellationToken.None)
                , Times.AtLeastOnce);

            _addDocumentsServiceMock.Verify(x => x.AddDocumentsAsInternalUserAsync(
                It.Is<AddDocumentsRequest>(r => r.UserAccountId == _internalUser.UserAccountId
                                                && r.ActorType == ActorType.InternalUser
                                                && r.FellingApplicationId == fla.Id
                                                && r.DocumentPurpose == DocumentPurpose.ApplicationDocument
                                                && !r.VisibleToApplicant
                                                && !r.VisibleToConsultee),
                It.IsAny<CancellationToken>()), Times.Once());

            _generatePdfApplicationAuditServiceMock.Verify(v =>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.GeneratingPdfFellingLicence
                        && JsonSerializer.Serialize(e.AuditData, _options) ==
                        JsonSerializer.Serialize(new {
                            ApplicationId = fla.Id,
                            IsFinal = false
                        }, _options)),
                    CancellationToken.None
                    ));

            _mockGetFcAreas.Verify();
        }

        [Theory, AutoMoqData]
        public async Task ShouldAddPDFApplicaton_WhenNoFellingAndRestockingDetailsApproved(
            FellingLicenceApplication fla,
            ExternalAccountModel applicant,
            WoodlandOwnerModel woodlandOwner,
            InternalUserAccount approverAccount,
            PropertyProfile propertyProfile,
            WoodlandOfficerReviewStatusModel woodlandOfficerReviewStatus,
            ConditionsStatusModel conditionsStatus,
            ConditionsResponse restockingConditionsRetrieved,
            Stream generatedMaps,
            byte[] pdfGenerated, ApproverReviewModel approverReview)
        {
            // setup
            var sut = CreateSut();

            var currentDate = DateTime.UtcNow.Date;
            fla.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = currentDate,
                    FellingLicenceApplication = fla,
                    Status = FellingLicenceStatus.SentForApproval
                }
            };
            fla.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

            foreach (var submittedFlaPropertyCompartment in fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments) {
                submittedFlaPropertyCompartment.ConfirmedFellingDetails = null;
            }

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps, approverReview);

            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pdfGenerated);

            _addDocumentsServiceMock.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult(new List<string>())));


            var result = await sut.GeneratePdfApplicationAsync(_internalUser, fla.Id, true, CancellationToken.None);

            // assert

            result.IsSuccess.Should().BeTrue();

            // verify

            _createApplicationSnapshotDocumentServiceMock.Verify(v => v.CreateApplicationSnapshotAsync(
                fla.Id,
                It.IsAny<PDFGeneratorRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _fellingLicenceApplicationRepositoryMock.Verify(v => v.GetAsync(
                fla.Id,
                CancellationToken.None)
                , Times.AtLeastOnce);

            _addDocumentsServiceMock.Verify(x => x.AddDocumentsAsInternalUserAsync(
                It.Is<AddDocumentsRequest>(r => r.UserAccountId == _internalUser.UserAccountId
                                                && r.ActorType == ActorType.InternalUser
                                                && r.FellingApplicationId == fla.Id
                                                && r.DocumentPurpose == DocumentPurpose.ApplicationDocument
                                                && r.VisibleToApplicant == DocumentVisibilityOptions.ApplicationDocument.VisibleToApplicant
                                                && r.VisibleToConsultee == DocumentVisibilityOptions.ApplicationDocument.VisibleToConsultees),
                It.IsAny<CancellationToken>()), Times.Once());

            _generatePdfApplicationAuditServiceMock.Verify(v =>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.GeneratingPdfFellingLicence
                        && JsonSerializer.Serialize(e.AuditData, _options) ==
                        JsonSerializer.Serialize(new {
                            ApplicationId = fla.Id,
                            IsFinal = true
                        }, _options)),
                    CancellationToken.None
                    ));

            _mockGetFcAreas.Verify();
        }

        [Theory, AutoMoqData]
        public async Task ShouldAddPDFApplicaton_WhenFellingAndRestockingDetailsApproved(
            FellingLicenceApplication fla,
            ExternalAccountModel applicant,
            WoodlandOwnerModel woodlandOwner,
            InternalUserAccount approverAccount,
            PropertyProfile propertyProfile,
            WoodlandOfficerReviewStatusModel woodlandOfficerReviewStatus,
            ConditionsStatusModel conditionsStatus,
            ConditionsResponse restockingConditionsRetrieved,
            Stream generatedMaps,
            byte[] pdfGenerated,
            Polygon gisData, ApproverReviewModel approverReview)
        {
            // setup
            var sut = CreateSut();

            foreach (var compartment in fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments) {
                compartment.CompartmentId = propertyProfile.Compartments.FirstOrDefault().Id;
                compartment.Id = Guid.NewGuid();
                foreach (var felling in compartment.ConfirmedFellingDetails) {
                    felling.SubmittedFlaPropertyCompartmentId = compartment.Id;
                    felling.SubmittedFlaPropertyCompartment = compartment;
                }

            }

            foreach (var felling in fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.SelectMany(x => x.ConfirmedFellingDetails)) {
                foreach (var restocking in felling.ConfirmedRestockingDetails) {
                    restocking.ConfirmedFellingDetail = felling;
                    restocking.SubmittedFlaPropertyCompartmentId = felling.SubmittedFlaPropertyCompartmentId;
                }
            }

            foreach (var compartment in propertyProfile.Compartments) {
                compartment.GISData = JsonConvert.SerializeObject(gisData);
            }

            var currentDate = DateTime.UtcNow.Date;
            fla.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = currentDate.AddDays(-3),
                    FellingLicenceApplication = fla,
                    Status = FellingLicenceStatus.SentForApproval
                }
            };
            fla.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps, approverReview);

            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pdfGenerated);

            _addDocumentsServiceMock.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult(new List<string>())));


            var result = await sut.GeneratePdfApplicationAsync(_internalUser, fla.Id, true, CancellationToken.None);

            // assert

            result.IsSuccess.Should().BeTrue();

            // verify

            _createApplicationSnapshotDocumentServiceMock.Verify(v => v.CreateApplicationSnapshotAsync(
                fla.Id,
                It.IsAny<PDFGeneratorRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _fellingLicenceApplicationRepositoryMock.Verify(v => v.GetAsync(
                fla.Id,
                CancellationToken.None)
                , Times.AtLeastOnce);

            _addDocumentsServiceMock.Verify(x => x.AddDocumentsAsInternalUserAsync(
                It.Is<AddDocumentsRequest>(r => r.UserAccountId == _internalUser.UserAccountId
                                                && r.ActorType == ActorType.InternalUser
                                                && r.FellingApplicationId == fla.Id
                                                && r.DocumentPurpose == DocumentPurpose.ApplicationDocument
                                                && r.VisibleToApplicant == DocumentVisibilityOptions.ApplicationDocument.VisibleToApplicant
                                                && r.VisibleToConsultee == DocumentVisibilityOptions.ApplicationDocument.VisibleToConsultees),
                It.IsAny<CancellationToken>()), Times.Once());

            _generatePdfApplicationAuditServiceMock.Verify(v =>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.GeneratingPdfFellingLicence
                        && JsonSerializer.Serialize(e.AuditData, _options) ==
                        JsonSerializer.Serialize(new {
                            ApplicationId = fla.Id,
                            IsFinal = true
                        }, _options)),
                    CancellationToken.None
                    ));

            _mockGetFcAreas.Verify();
        }

        [Theory, AutoMoqData]
        public async Task ShouldFail_WhenFailedToGeneratePdf(
            FellingLicenceApplication fla,
            ExternalAccountModel applicant,
            WoodlandOwnerModel woodlandOwner,
            InternalUserAccount approverAccount,
            PropertyProfile propertyProfile,
            WoodlandOfficerReviewStatusModel woodlandOfficerReviewStatus,
            ConditionsStatusModel conditionsStatus,
            ConditionsResponse restockingConditionsRetrieved,
            Stream generatedMaps,
            Polygon gisData, ApproverReviewModel approverReview)
        {
            // setup
            var sut = CreateSut();

            fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

            foreach (var comp in fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!) {
                comp.Id = propertyProfile.Compartments[0].Id;
                comp.CompartmentId = propertyProfile.Compartments[0].Id;
                foreach (var felling in comp.ConfirmedFellingDetails) {
                    if (felling.SubmittedFlaPropertyCompartment == null) {
                        felling.SubmittedFlaPropertyCompartment = new SubmittedFlaPropertyCompartment();
                    }

                    felling.SubmittedFlaPropertyCompartment.CompartmentId = propertyProfile.Compartments[0].Id;
                    felling.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments[0].Id;

                    foreach (var restocking in felling.ConfirmedRestockingDetails) {
                        restocking.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments[0].Id;
                    }
                }
            }

            foreach (var compartment in propertyProfile.Compartments) {
                compartment.GISData = JsonConvert.SerializeObject(gisData);
            }

            var currentDate = DateTime.UtcNow.Date;
            fla.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = currentDate.AddDays(-3),
                    FellingLicenceApplication = fla,
                    Status = FellingLicenceStatus.Submitted
                }
            };

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps, approverReview);

            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<byte[]>("Failed"));

            var result = await sut.GeneratePdfApplicationAsync(_internalUser, fla.Id, false, CancellationToken.None);

            // assert

            result.IsFailure.Should().BeTrue();

            // verify

            _createApplicationSnapshotDocumentServiceMock.Verify(v => v.CreateApplicationSnapshotAsync(
                fla.Id,
                It.IsAny<PDFGeneratorRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _fellingLicenceApplicationRepositoryMock.Verify(v => v.GetAsync(
                fla.Id,
                CancellationToken.None)
                , Times.AtLeastOnce);

            _addDocumentsServiceMock.Verify(x => x.AddDocumentsAsInternalUserAsync(
                It.IsAny<AddDocumentsRequest>(),
                It.IsAny<CancellationToken>()), Times.Never());

            _generatePdfApplicationAuditServiceMock.Verify(v =>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.GeneratingPdfFellingLicence
                        && JsonSerializer.Serialize(e.AuditData, _options) ==
                        JsonSerializer.Serialize(new {
                            ApplicationId = fla.Id,
                            IsFinal = false
                        }, _options)),
                    CancellationToken.None
                    ));

            _generatePdfApplicationAuditServiceMock.Verify(v =>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.GeneratingPdfFellingLicenceFailure
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new {
                                 ApplicationId = fla.Id,
                                 IsFinal = false,
                                 Error = "Failed"
                             }, _options)),
                    CancellationToken.None
                ));
            
            _mockGetFcAreas.Verify();
        }

        private void SetUpReturns(FellingLicenceApplication fla,
            ExternalAccountModel applicant,
            WoodlandOwnerModel woodlandOwner,
            InternalUserAccount approverAccount,
            PropertyProfile propertyProfile,
            WoodlandOfficerReviewStatusModel woodlandOfficerReviewStatus,
            ConditionsStatusModel conditionsStatus,
            ConditionsResponse restockingConditionsRetrieved,
            Stream generatedMaps,
            ApproverReviewModel approverReview)
        {
            _fellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            _externalAccountServiceMock
                .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(applicant);

            _woodlandOwnerServiceMock
                .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(woodlandOwner);

            _internalAccountServiceMock
                .Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(approverAccount);

            _propertyProfileRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(propertyProfile);

            _getWoodlandOfficerReviewServiceMock
                .Setup(r => r.GetWoodlandOfficerReviewStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(woodlandOfficerReviewStatus);

            _getWoodlandOfficerReviewServiceMock
                .Setup(r => r.GetConditionsStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conditionsStatus);

            _calculateConditionsServiceMock
                .Setup(r => r.RetrieveExistingConditionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(restockingConditionsRetrieved);

            _foresterAccessMock
                .Setup(r => r.GenerateImage_MultipleCompartmentsAsync(It.IsAny<List<InternalCompartmentDetails<BaseShape>>>(),  It.IsAny<CancellationToken>(), 
                    It.IsAny<int>(),It.IsAny<MapGeneration>(), It.IsAny<string>()))
                .ReturnsAsync(generatedMaps);

            _approverReviewServiceMock
                .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(approverReview);



        }

        private GeneratePdfApplicationUseCase CreateSut()
        {
            _fellingLicenceApplicationRepositoryMock.Reset();
            _generatePdfApplicationAuditServiceMock.Reset();
            _addDocumentAuditServiceMock.Reset();
            _addDocumentsServiceMock.Reset();
            _createApplicationSnapshotDocumentAuditServiceMock.Reset();
            _createApplicationSnapshotDocumentServiceMock.Reset();
            _getWoodlandOfficerReviewServiceMock.Reset();
            _approverReviewServiceMock.Reset();
            _externalAccountServiceMock.Reset();
            _internalAccountServiceMock.Reset();
            _woodlandOwnerServiceMock.Reset();
            _propertyProfileRepositoryMock.Reset();
            _foresterAccessMock.Reset();
            _calculateConditionsServiceMock.Reset();
            _mockGetFcAreas.Reset();

            _unitOfWOrkMock.Reset();
            _fellingLicenceApplicationRepositoryMock.Reset();

            _fellingLicenceApplicationRepositoryMock.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
            _clock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));
            _mockGetFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Bullers Hill\nKennford\nExeter\nEX6 7XR\nPhone: 0300 067 4960\nEmail: adminhub.bullershill@forestrycommission.gov.uk");

            return new GeneratePdfApplicationUseCase(
                _generatePdfApplicationAuditServiceMock.Object,
                new RequestContext("test", new RequestUserModel(_internalUser.Principal)),
                _getWoodlandOfficerReviewServiceMock.Object,
                _approverReviewServiceMock.Object,
                _fellingLicenceApplicationRepositoryMock.Object,
                _internalAccountServiceMock.Object,
                _externalAccountServiceMock.Object,
                _woodlandOwnerServiceMock.Object,
                _propertyProfileRepositoryMock.Object,
                _addDocumentsServiceMock.Object,
                _createApplicationSnapshotDocumentServiceMock.Object,
                _foresterAccessMock.Object,
                _mockGetFcAreas.Object,
                _documentVisibilityOptions.Object,
                _clock.Object,
                _calculateConditionsServiceMock.Object,
                _licencePdfGeneratorApiOptions.Object,
                new NullLogger<GeneratePdfApplicationUseCase>());
        }

        private static Result<StoreFileSuccessResult, StoreFileFailureResult> SavedFileSuccessResult(string location, int fileSize)
        {
            var s = new StoreFileSuccessResult(location, fileSize);
            return Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(s);
        }

    }
}

using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
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
using Newtonsoft.Json;
using NodaTime;
using System.Text.Json;
using Forestry.Flo.HostApplicationsCommon.Services;
using ExternalAccountModel = Forestry.Flo.Services.Applicants.Models.UserAccountModel;
using InternalUserAccount = Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount;
using JsonSerializer = System.Text.Json.JsonSerializer;
using WoodlandOwnerModel = Forestry.Flo.Services.Applicants.Models.WoodlandOwnerModel;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.External.Web.Tests.Services
{
    public class GeneratePdfApplicationUseCaseTests
    {
        private static readonly Fixture FixtureInstance = new();
        private static readonly DateTime UtcNow = DateTime.UtcNow;
        private readonly Mock<IClock> _clock;
        private readonly Mock<IFileStorageService> _fileStorageService;
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepositoryMock;
        private readonly Mock<IAuditService<GeneratePdfApplicationUseCaseBase>> _GeneratePdfApplicationAuditServiceMock;
        private readonly Mock<IAuditService<AddDocumentService>> _AddDocumentAuditServiceMock;
        private readonly Mock<IAddDocumentService>? _addDocumentsServiceMock;
        private readonly Mock<IAuditService<CreateApplicationSnapshotDocumentService>> _createApplicationSnapshotDocumentAuditServiceMock;
        private readonly Mock<ICreateApplicationSnapshotDocumentService> _createApplicationSnapshotDocumentServiceMock;
        private readonly Mock<IGetWoodlandOfficerReviewService> _getWoodlandOfficerReviewServiceMock;
        private readonly Mock<IRetrieveUserAccountsService> _externalAccountServiceMock;
        private readonly Mock<IUserAccountService> _internalAccountServiceMock;
        private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerServiceMock;
        private readonly Mock<IPropertyProfileRepository> _propertyProfileRepositoryMock;
        private readonly Mock<IForesterServices> _foresterAccessMock;
        private readonly Mock<ICalculateConditions> _calculateConditionsServiceMock;
        private readonly Mock<IOptions<DocumentVisibilityOptions>> _documentVisibilityOptions;
        private readonly Mock<IOptions<PDFGeneratorAPIOptions>> _licencePDFGeneratorAPIOptions;
        private readonly Mock<IGetConfiguredFcAreas> _mockGetFcAreas;
        private readonly Mock<IApproverReviewService> _approverReviewMock = new();

        private readonly ExternalApplicant _externalUser;
        private readonly Mock<IUnitOfWork> _unitOfWOrkMock;

        private static readonly DocumentVisibilityOptions DocumentVisibilityOptions = new()
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
                VisibleToConsultees = false
            }
        };

        private static readonly PDFGeneratorAPIOptions PDFGeneratorAPIOptions = new()
        {
            BaseUrl = "",
            TemplateName = "FellingLicence.html",
            Version = "0.0"
        };

        public GeneratePdfApplicationUseCaseTests()
        {
            _fellingLicenceApplicationRepositoryMock = new Mock<IFellingLicenceApplicationInternalRepository>();
            _GeneratePdfApplicationAuditServiceMock = new Mock<IAuditService<GeneratePdfApplicationUseCaseBase>>();
            _AddDocumentAuditServiceMock = new Mock<IAuditService<AddDocumentService>>();
            _addDocumentsServiceMock = new Mock<IAddDocumentService>();
            _createApplicationSnapshotDocumentAuditServiceMock = new Mock<IAuditService<CreateApplicationSnapshotDocumentService>>();
            _createApplicationSnapshotDocumentServiceMock = new Mock<ICreateApplicationSnapshotDocumentService>();
            _getWoodlandOfficerReviewServiceMock = new Mock<IGetWoodlandOfficerReviewService>();
            _externalAccountServiceMock = new Mock<IRetrieveUserAccountsService>();
            _internalAccountServiceMock = new Mock<IUserAccountService>();
            _woodlandOwnerServiceMock = new Mock<IRetrieveWoodlandOwners>();
            _propertyProfileRepositoryMock = new Mock<IPropertyProfileRepository>();
            _foresterAccessMock = new Mock<IForesterServices>();
            _calculateConditionsServiceMock = new Mock<ICalculateConditions>();
            _documentVisibilityOptions = new Mock<IOptions<DocumentVisibilityOptions>>();
            _licencePDFGeneratorAPIOptions = new Mock<IOptions<PDFGeneratorAPIOptions>>();
            _mockGetFcAreas = new Mock<IGetConfiguredFcAreas>();

            _clock = new Mock<IClock>();
            _fileStorageService = new Mock<IFileStorageService>();
            _unitOfWOrkMock = new Mock<IUnitOfWork>();

            _documentVisibilityOptions.Reset();
            _documentVisibilityOptions.Setup(x => x.Value).Returns(DocumentVisibilityOptions);

            _licencePDFGeneratorAPIOptions.Reset();
            _licencePDFGeneratorAPIOptions.Setup(x => x.Value).Returns(PDFGeneratorAPIOptions);

            _fileStorageService.Setup(f => f.StoreFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<FileUploadReason>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SavedFileSuccessResult("testLocation", 4));

            var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<Guid>(),
                FixtureInstance.Create<string>(),
                AccountTypeInternal.AdminOfficer);
            _externalUser = new ExternalApplicant(user);
        }

        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Theory, AutoMoqData]
        public async Task ShouldAddPDFApplication_WhenNoFellingAndRestockingDetailsNotApproved(
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
            byte[] pdfGenerated)
        {
            // setup
            var sut = CreateSut();

            fla.LinkedPropertyProfile!.ProposedFellingDetails = new List<ProposedFellingDetail>();

            foreach (var comp in fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!)
            {
                comp.SubmittedCompartmentDesignations = null;
                comp.Id = propertyProfile.Compartments[0].Id;
                comp.CompartmentId = propertyProfile.Compartments[0].Id;
                comp.GISData = JsonConvert.SerializeObject(gisData);
                foreach (var felling in comp.ConfirmedFellingDetails)
                {
                    if (felling.SubmittedFlaPropertyCompartment == null)
                    {
                        felling.SubmittedFlaPropertyCompartment = new SubmittedFlaPropertyCompartment();
                    }

                    felling.SubmittedFlaPropertyCompartment.CompartmentId = propertyProfile.Compartments[0].Id;
                    felling.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments[0].Id;

                    foreach (var restocking in felling.ConfirmedRestockingDetails)
                    {
                        restocking.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments[0].Id;
                    }
                }
            }

            foreach (var compartment in propertyProfile.Compartments)
            {
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

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps);

            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pdfGenerated);
            
            _addDocumentsServiceMock.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult([Guid.NewGuid()], new List<string>())));
            

            var result = await sut.GeneratePreviewDocumentAsync(_externalUser.UserAccountId!.Value, fla.Id, CancellationToken.None);

            // assert

            Assert.True(result.IsSuccess);

            // verify

            _createApplicationSnapshotDocumentServiceMock.Verify(v=>v.CreateApplicationSnapshotAsync(
                fla.Id,
                It.IsAny<PDFGeneratorRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
            
            _fellingLicenceApplicationRepositoryMock.Verify(v => v.GetAsync(
                fla.Id,
                CancellationToken.None)
                , Times.AtLeastOnce);

            _addDocumentsServiceMock.Verify(x => x.AddDocumentsAsInternalUserAsync(
                It.Is<AddDocumentsRequest>(r => r.UserAccountId == _externalUser.UserAccountId
                                                && r.ActorType == ActorType.System
                                                && r.FellingApplicationId == fla.Id
                                                && r.DocumentPurpose == DocumentPurpose.ApplicationDocument
                                                && !r.VisibleToApplicant
                                                && !r.VisibleToConsultee),
                It.IsAny<CancellationToken>()), Times.Once());

            _GeneratePdfApplicationAuditServiceMock.Verify(v=>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e=>e.EventName == AuditEvents.GeneratingPdfFellingLicence
                        && JsonSerializer.Serialize(e.AuditData, _options) == 
                        JsonSerializer.Serialize(new
                            {
                                ApplicationId = fla.Id,
                                IsFinal = false }, _options)),
                    CancellationToken.None
                    ));

            _mockGetFcAreas.Verify();
        }

        [Theory, AutoMoqData]
        public async Task ShouldAddPDFApplication_WhenGivenFellingAndRestockingDetailsNotApproved(
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
            Polygon gisData)
        {
            // setup
            
            var sut = CreateSut();

            foreach (var felling in fla.LinkedPropertyProfile!.ProposedFellingDetails!)
            {
                felling.PropertyProfileCompartmentId = propertyProfile.Compartments[0].Id;
            }

            foreach (var restocking in fla.LinkedPropertyProfile!.ProposedFellingDetails!.SelectMany(p => p.ProposedRestockingDetails!))
            {
                restocking.PropertyProfileCompartmentId = propertyProfile.Compartments[0].Id;
            }

            foreach (var comp in fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!)
            {
                comp.Id = propertyProfile.Compartments[0].Id;
                comp.CompartmentId = propertyProfile.Compartments[0].Id;
                comp.GISData = JsonConvert.SerializeObject(gisData);
                foreach (var felling in comp.ConfirmedFellingDetails)
                {
                    if (felling.SubmittedFlaPropertyCompartment == null)
                    {
                        felling.SubmittedFlaPropertyCompartment = new SubmittedFlaPropertyCompartment();
                    }
                    
                    felling.SubmittedFlaPropertyCompartment.CompartmentId = propertyProfile.Compartments[0].Id;
                    felling.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments[0].Id;

                    foreach (var restocking in felling.ConfirmedRestockingDetails)
                    {
                        restocking.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments[0].Id;
                    }
                }
            }

            foreach (var compartment in propertyProfile.Compartments)
            {
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

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps);

            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pdfGenerated);

            _addDocumentsServiceMock.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult([Guid.NewGuid()], new List<string>())));


            var result = await sut.GeneratePreviewDocumentAsync(_externalUser.UserAccountId!.Value, fla.Id, CancellationToken.None);

            // assert

            Assert.True(result.IsSuccess);

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
                It.Is<AddDocumentsRequest>(r => r.UserAccountId == _externalUser.UserAccountId
                                                && r.ActorType == ActorType.System
                                                && r.FellingApplicationId == fla.Id
                                                && r.DocumentPurpose == DocumentPurpose.ApplicationDocument
                                                && !r.VisibleToApplicant
                                                && !r.VisibleToConsultee),
                It.IsAny<CancellationToken>()), Times.Once());

            _GeneratePdfApplicationAuditServiceMock.Verify(v =>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.GeneratingPdfFellingLicence
                        && JsonSerializer.Serialize(e.AuditData, _options) ==
                        JsonSerializer.Serialize(new
                        {
                            ApplicationId = fla.Id,
                            IsFinal = false
                        }, _options)),
                    CancellationToken.None
                    ));

            _mockGetFcAreas.Verify();
        }

        [Theory, AutoMoqData]
        public async Task ShouldDisplayTheCorrectCompartments_ForFellingAndRestocking(
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
           Polygon gisData1,
           Polygon gisData2,
           ApproverReviewModel approverReview)
        {
            // setup
            var sut = CreateSut();

            var compartment1 = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
            var compartment2 = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Skip(1).First();

            foreach (var compartment in fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments)
            {
                compartment.Id = Guid.NewGuid();
                foreach (var felling in compartment.ConfirmedFellingDetails)
                {
                    felling.SubmittedFlaPropertyCompartmentId = compartment.Id;
                    felling.SubmittedFlaPropertyCompartment = compartment;
                }
            }

            compartment1.GISData = JsonConvert.SerializeObject(gisData1);
            compartment2.GISData = JsonConvert.SerializeObject(gisData2);

            foreach (var felling in fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.SelectMany(x => x.ConfirmedFellingDetails))
            {
                felling.SubmittedFlaPropertyCompartmentId = compartment1.Id;

                foreach (var restocking in felling.ConfirmedRestockingDetails)
                {
                    restocking.ConfirmedFellingDetail = felling;
                    restocking.SubmittedFlaPropertyCompartmentId = compartment2.Id;
                }
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

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps);
            
            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pdfGenerated);

            _addDocumentsServiceMock.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult([Guid.NewGuid()], new List<string>())));

            var result = await sut.GeneratePreviewDocumentAsync(_externalUser.UserAccountId!.Value, fla.Id, CancellationToken.None);

            // assert

            Assert.True(result.IsSuccess);

            // verify

            _createApplicationSnapshotDocumentServiceMock.Verify(v => v.CreateApplicationSnapshotAsync(
                fla.Id,
                It.Is<PDFGeneratorRequest>(x =>
                    x.data.operationsMaps.All(y => y == gisData1.ToString()) &&
                    x.data.restockingMaps.All(y => y == gisData2.ToString())),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task ShouldIncludeTenYearLicenceSupplementaryPoints(
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
           Polygon gisData1,
           Polygon gisData2,
           ApproverReviewModel approverReview)
        {
            // setup
            var sut = CreateSut();

            fla.IsForTenYearLicence = true;

            var compartment1 = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
            var compartment2 = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Skip(1).First();

            foreach (var compartment in fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments)
            {
                compartment.Id = Guid.NewGuid();
                foreach (var felling in compartment.ConfirmedFellingDetails)
                {
                    felling.SubmittedFlaPropertyCompartmentId = compartment.Id;
                    felling.SubmittedFlaPropertyCompartment = compartment;
                }
            }

            compartment1.GISData = JsonConvert.SerializeObject(gisData1);
            compartment2.GISData = JsonConvert.SerializeObject(gisData2);

            foreach (var felling in fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.SelectMany(x => x.ConfirmedFellingDetails))
            {
                felling.SubmittedFlaPropertyCompartmentId = compartment1.Id;

                foreach (var restocking in felling.ConfirmedRestockingDetails)
                {
                    restocking.ConfirmedFellingDetail = felling;
                    restocking.SubmittedFlaPropertyCompartmentId = compartment2.Id;
                }
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

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps);

            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pdfGenerated);

            _addDocumentsServiceMock.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult([Guid.NewGuid()], new List<string>())));

            var result = await sut.GeneratePreviewDocumentAsync(_externalUser.UserAccountId!.Value, fla.Id, CancellationToken.None);

            // assert

            Assert.True(result.IsSuccess);

            // verify

            var expectedText = $"This licence is issued in summary relating to the Management Plan referenced {fla.WoodlandManagementPlanReference ?? ""} " +
                               "and associated Plan of Operations and final maps. These must be attached to the licence at all times.";

            _createApplicationSnapshotDocumentServiceMock.Verify(v => v.CreateApplicationSnapshotAsync(
                fla.Id,
                It.Is<PDFGeneratorRequest>(x =>
                    x.data.restockingAdvisoryDetails.Contains(expectedText)),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task ShouldAddPDFApplication_WhenFellingAndRestockingDetailsApproved(
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
            Polygon gisData)
        {
            // setup
            var sut = CreateSut();

            foreach (var compartment in fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments)
            {
                compartment.CompartmentId = propertyProfile.Compartments.FirstOrDefault().Id;
                compartment.GISData = JsonConvert.SerializeObject(gisData);
                compartment.Id = Guid.NewGuid();
                foreach (var felling in compartment.ConfirmedFellingDetails)
                {
                    felling.SubmittedFlaPropertyCompartmentId = compartment.Id;
                    felling.SubmittedFlaPropertyCompartment = compartment;
                }
            }

            foreach (var felling in fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.SelectMany(x => x.ConfirmedFellingDetails))
            {
                foreach (var restocking in felling.ConfirmedRestockingDetails)
                {
                    restocking.ConfirmedFellingDetail = felling;
                    restocking.SubmittedFlaPropertyCompartmentId = felling.SubmittedFlaPropertyCompartmentId;
                }
            }

            foreach (var compartment in propertyProfile.Compartments)
            {
                compartment.GISData = JsonConvert.SerializeObject(gisData);
            }

            var currentDate = DateTime.UtcNow.Date;
            fla.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = currentDate.AddDays(-3),
                    FellingLicenceApplication = fla,
                    Status = FellingLicenceStatus.Approved
                }
            };
            fla.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps);

            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pdfGenerated);

            _addDocumentsServiceMock.Setup(r => r.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsSuccessResult([Guid.NewGuid()], new List<string>())));


            var result = await sut.GeneratePreviewDocumentAsync(_externalUser.UserAccountId!.Value, fla.Id, CancellationToken.None);

            // assert

            Assert.True(result.IsSuccess);

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
                It.Is<AddDocumentsRequest>(r => r.UserAccountId == _externalUser.UserAccountId
                                                && r.ActorType == ActorType.System
                                                && r.FellingApplicationId == fla.Id
                                                && r.DocumentPurpose == DocumentPurpose.ApplicationDocument
                                                && !r.VisibleToApplicant
                                                && !r.VisibleToConsultee),
                It.IsAny<CancellationToken>()), Times.Once());

            _GeneratePdfApplicationAuditServiceMock.Verify(v =>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.GeneratingPdfFellingLicence
                        && JsonSerializer.Serialize(e.AuditData, _options) ==
                        JsonSerializer.Serialize(new
                        {
                            ApplicationId = fla.Id,
                            IsFinal = false
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
            Polygon gisData)
        {
            // setup
            var sut = CreateSut();

            fla.LinkedPropertyProfile!.ProposedFellingDetails = new List<ProposedFellingDetail>();

            foreach (var felling in fla.LinkedPropertyProfile!.ProposedFellingDetails)
            {
                felling.PropertyProfileCompartmentId = propertyProfile.Compartments![0].Id;
            }

            foreach (var restocking in fla.LinkedPropertyProfile!.ProposedFellingDetails!.SelectMany(p => p.ProposedRestockingDetails!))
            {
                restocking.PropertyProfileCompartmentId = propertyProfile.Compartments[0].Id;
            }

            foreach (var comp in fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!)
            {
                comp.Id = propertyProfile.Compartments[0].Id;
                comp.CompartmentId = propertyProfile.Compartments[0].Id;
                comp.SubmittedCompartmentDesignations = null;
                comp.GISData = JsonConvert.SerializeObject(gisData);
                foreach (var felling in comp.ConfirmedFellingDetails)
                {
                    if (felling.SubmittedFlaPropertyCompartment == null)
                    {
                        felling.SubmittedFlaPropertyCompartment = new SubmittedFlaPropertyCompartment();
                    }

                    felling.SubmittedFlaPropertyCompartment.CompartmentId = propertyProfile.Compartments[0].Id;
                    felling.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments[0].Id;

                    foreach (var restocking in felling.ConfirmedRestockingDetails)
                    {
                        restocking.SubmittedFlaPropertyCompartmentId = propertyProfile.Compartments[0].Id;
                    }
                }
            }

            foreach (var compartment in propertyProfile.Compartments)
            {
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

            SetUpReturns(fla, applicant, woodlandOwner, approverAccount, propertyProfile, woodlandOfficerReviewStatus, conditionsStatus, restockingConditionsRetrieved, generatedMaps);

            _createApplicationSnapshotDocumentServiceMock
                .Setup(r => r.CreateApplicationSnapshotAsync(fla.Id, It.IsAny<PDFGeneratorRequest>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<byte[]>("Failed"));

            var result = await sut.GeneratePreviewDocumentAsync(_externalUser.UserAccountId!.Value, fla.Id, CancellationToken.None);

            // assert

            Assert.True(result.IsFailure);

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

            _GeneratePdfApplicationAuditServiceMock.Verify(v =>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.GeneratingPdfFellingLicence
                        && JsonSerializer.Serialize(e.AuditData, _options) ==
                        JsonSerializer.Serialize(new
                        {
                            ApplicationId = fla.Id,
                            IsFinal = false
                        }, _options)),
                    CancellationToken.None
                    ));

            _GeneratePdfApplicationAuditServiceMock.Verify(v =>
                v.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.GeneratingPdfFellingLicenceFailure
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 ApplicationId = fla.Id,
                                 IsFinal = false,
                                 Error = "Failed"
                             }, _options)),
                    CancellationToken.None
                ));
        }

        private void SetUpReturns(FellingLicenceApplication fla, 
            ExternalAccountModel applicant, 
            WoodlandOwnerModel woodlandOwner,
            InternalUserAccount approverAccount, 
            PropertyProfile propertyProfile,
            WoodlandOfficerReviewStatusModel woodlandOfficerReviewStatus, 
            ConditionsStatusModel conditionsStatus,
            ConditionsResponse restockingConditionsRetrieved, 
            Stream generatedMaps)
        {
            _fellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            _externalAccountServiceMock
                .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(applicant);

            _woodlandOwnerServiceMock
                .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
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
                .Setup(r => r.GenerateImage_MultipleCompartmentsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<InternalCompartmentDetails<BaseShape>>>(), It.IsAny<CancellationToken>(),
                    It.IsAny<int>(), It.IsAny<MapGeneration>(), It.IsAny<string>()))
                .ReturnsAsync(generatedMaps);
        }

        private GeneratePdfApplicationUseCase CreateSut()
        {
            _fellingLicenceApplicationRepositoryMock.Reset();
            _GeneratePdfApplicationAuditServiceMock.Reset();
            _AddDocumentAuditServiceMock.Reset();
            _addDocumentsServiceMock.Reset();
            _createApplicationSnapshotDocumentAuditServiceMock.Reset();
            _createApplicationSnapshotDocumentServiceMock.Reset();
            _getWoodlandOfficerReviewServiceMock.Reset();
            _externalAccountServiceMock.Reset();
            _internalAccountServiceMock.Reset();
            _woodlandOwnerServiceMock.Reset();
            _propertyProfileRepositoryMock.Reset();
            _foresterAccessMock.Reset();
            _calculateConditionsServiceMock.Reset();
            _mockGetFcAreas.Reset();
            _approverReviewMock.Reset();

            _unitOfWOrkMock.Reset();
            _fellingLicenceApplicationRepositoryMock.Reset();
            
            _fellingLicenceApplicationRepositoryMock.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
            _clock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));
            _mockGetFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Bullers Hill\nKennford\nExeter\nEX6 7XR\nPhone: 0300 067 4960\nEmail: adminhub.bullershill@forestrycommission.gov.uk");

            return new GeneratePdfApplicationUseCase(
                _GeneratePdfApplicationAuditServiceMock.Object,
                new RequestContext("test", new RequestUserModel(_externalUser.Principal)),
                _getWoodlandOfficerReviewServiceMock.Object,
                _fellingLicenceApplicationRepositoryMock.Object,
                _internalAccountServiceMock.Object,
                _externalAccountServiceMock.Object,
                _woodlandOwnerServiceMock.Object,
                _propertyProfileRepositoryMock.Object,
                _addDocumentsServiceMock.Object,
                _createApplicationSnapshotDocumentServiceMock.Object,
                _foresterAccessMock.Object,
                _mockGetFcAreas.Object,
                _clock.Object,
                _calculateConditionsServiceMock.Object,
                _approverReviewMock.Object,
                _licencePDFGeneratorAPIOptions.Object,
                new NullLogger<GeneratePdfApplicationUseCase>());
        }

        private static Result<StoreFileSuccessResult, StoreFileFailureResult> SavedFileSuccessResult(string location, int fileSize)
        {
            var s = new StoreFileSuccessResult(location, fileSize);
            return Result.Success<StoreFileSuccessResult, StoreFileFailureResult>(s);
        }

    }
}

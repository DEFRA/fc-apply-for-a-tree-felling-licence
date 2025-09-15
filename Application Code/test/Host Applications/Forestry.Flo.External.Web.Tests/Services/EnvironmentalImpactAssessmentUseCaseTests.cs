using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using System.Security.Claims;

namespace Forestry.Flo.External.Web.Tests.Services;

public class EnvironmentalImpactAssessmentUseCaseTests
{
    private readonly Mock<IAddDocumentService> _mockAddDocumentService = new();
    private readonly Mock<IUpdateFellingLicenceApplication> _mockUpdateFellingLicenceApplication = new();
    private readonly Mock<IAuditService<EnvironmentalImpactAssessmentUseCase>> _mockAuditService = new();
    private readonly Mock<IRetrieveUserAccountsService> _mockRetrieveUserAccountsService = new();
    private readonly Mock<IRetrieveWoodlandOwners> _mockRetrieveWoodlandOwners = new();
    private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _mockGetFellingLicenceApplicationServiceForExternalUsers = new();
    private readonly Mock<IGetPropertyProfiles> _mockGetPropertyProfilesService = new();
    private readonly Mock<IGetCompartments> _mockGetCompartmentsService = new();
    private readonly Mock<IOptions<EiaOptions>> _mockEiaOptions = new();
    private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService = new();
    private readonly Mock<IClock> _mockClock = new();
    private readonly Mock<ILogger<EnvironmentalImpactAssessmentUseCase>> _mockLogger = new();
    private readonly RequestContext _requestContext = new("test", new RequestUserModel(new ClaimsPrincipal()));
    private readonly ExternalApplicant _externalApplicant;
    private readonly Guid _applicationId = Guid.NewGuid();
    private const string ExternalUri = "testUri";

    private static readonly IFixture Fixture = new Fixture();
    private static FormFileCollection FileCollection =>
    [
        new FormFile(
            new MemoryStream(new byte[1]),
            0,
            1,
            "file1",
            "file1.pdf"
        )
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        }
    ];

    public EnvironmentalImpactAssessmentUseCaseTests()
    {
        Fixture.CustomiseFixtureForFellingLicenceApplications();
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            Fixture.Create<string>(),
            Fixture.Create<string>(),
            Fixture.Create<Guid>(),
            Fixture.Create<Guid>(),
            agencyId: Fixture.Create<Guid>(),
            woodlandOwnerName: Fixture.Create<string>());

        _externalApplicant = new ExternalApplicant(user);
    }

    [Fact]
    public async Task AddEiaDocumentsToApplicationAsync_ReturnsSuccess_WhenDocumentsAreAdded()
    {
        // Arrange
        var sut = CreateSut();
        var modelState = new ModelStateDictionary();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserAccessModel()));

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        // Act
        var result = await sut.AddEiaDocumentsToApplicationAsync(_externalApplicant, _applicationId, FileCollection, modelState, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        modelState.ErrorCount.Should().Be(0);
        _mockAddDocumentService.Verify(v => v.AddDocumentsAsExternalApplicantAsync(
            It.Is<AddDocumentsExternalRequest>(x =>
                x.ActorType == ActorType.ExternalApplicant &&
                x.UserAccountId == _externalApplicant.UserAccountId &&
                x.DocumentPurpose == DocumentPurpose.EiaAttachment &&
                x.FellingApplicationId == _applicationId),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(_externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetIsEditable(_applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockAuditService.Verify(x =>
                x.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                        e.ActorType == ActorType.ExternalApplicant &&
                        e.EventName == AuditEvents.ApplicantUploadEiaDocumentsSuccess),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddEiaDocumentsToApplicationAsync_ReturnsSuccess_WhenNoDocumentsProvided()
    {
        // Arrange
        var sut = CreateSut();
        var files = new FormFileCollection();
        var modelState = new ModelStateDictionary();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserAccessModel()));

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        // Act
        var result = await sut.AddEiaDocumentsToApplicationAsync(_externalApplicant, _applicationId, files, modelState, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        modelState.ErrorCount.Should().Be(0);
        _mockAddDocumentService.VerifyNoOtherCalls();

        _mockRetrieveUserAccountsService
            .VerifyNoOtherCalls();

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .VerifyNoOtherCalls();

        // Audit should not be called as no action taken
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddEiaDocumentsToApplicationAsync_ReturnsFailure_WhenApplicationNotFound()
    {
        // Arrange
        var sut = CreateSut();
        var modelState = new ModelStateDictionary();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserAccessModel()));

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("Not found"));

        // Act
        var result = await sut.AddEiaDocumentsToApplicationAsync(_externalApplicant, _applicationId, FileCollection, modelState, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        modelState.ErrorCount.Should().Be(0);

        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(_externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetIsEditable(_applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockAddDocumentService.VerifyNoOtherCalls();

        _mockAuditService.Verify(x =>
                x.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                        e.ActorType == ActorType.ExternalApplicant &&
                        e.EventName == AuditEvents.ApplicantUploadEiaDocumentsFailure),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddEiaDocumentsToApplicationAsync_ReturnsFailure_WhenAddDocumentFails()
    {
        // Arrange
        var sut = CreateSut();
        var modelState = new ModelStateDictionary();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserAccessModel()));

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _mockAddDocumentService
            .Setup(x => x.AddDocumentsAsExternalApplicantAsync(It.IsAny<AddDocumentsExternalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AddDocumentsSuccessResult, AddDocumentsFailureResult>(new AddDocumentsFailureResult(["Upload failed"])));

        // Act
        var result = await sut.AddEiaDocumentsToApplicationAsync(_externalApplicant, _applicationId, FileCollection, modelState, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        modelState.ErrorCount.Should().Be(1);
        modelState["eia-file-upload"].Errors[0].ErrorMessage.Should().Be("Upload failed");

        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(_externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetIsEditable(_applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockAddDocumentService.Verify(v => v.AddDocumentsAsExternalApplicantAsync(
            It.Is<AddDocumentsExternalRequest>(x =>
                x.ActorType == ActorType.ExternalApplicant &&
                x.UserAccountId == _externalApplicant.UserAccountId &&
                x.DocumentPurpose == DocumentPurpose.EiaAttachment &&
                x.FellingApplicationId == _applicationId),
            It.IsAny<CancellationToken>()));

        _mockAuditService.Verify(x =>
                x.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                        e.ActorType == ActorType.ExternalApplicant &&
                        e.EventName == AuditEvents.ApplicantUploadEiaDocumentsFailure),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddEiaDocumentsToApplicationAsync_ReturnsFailure_WhenUpdateStepStatusFails()
    {
        // Arrange
        var sut = CreateSut();
        var modelState = new ModelStateDictionary();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserAccessModel()));

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new FellingLicenceApplication
            {
                Id = _applicationId,
                ApplicationReference = "REF123",
                Documents = new List<Document>
                {
                    Fixture.Build<Document>().With(x => x.Purpose, DocumentPurpose.EiaAttachment).Without(x => x.DeletionTimestamp).Create()
                },
                FellingLicenceApplicationStepStatus = new FellingLicenceApplicationStepStatus
                {
                    EnvironmentalImpactAssessmentStatus = null
                },
                WoodlandOwnerId = Guid.NewGuid()
            }));

        _mockUpdateFellingLicenceApplication
            .Setup(x => x.UpdateEnvironmentalImpactAssessmentStatusAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Could not update application step status."));

        // Act
        var result = await sut.AddEiaDocumentsToApplicationAsync(_externalApplicant, _applicationId, FileCollection, modelState, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Could not update application step status.");

        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(_externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetIsEditable(_applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockAddDocumentService.VerifyNoOtherCalls();

        _mockAuditService.Verify(x =>
                x.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                        e.ActorType == ActorType.ExternalApplicant &&
                        e.EventName == AuditEvents.ApplicantUploadEiaDocumentsFailure),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddEiaDocumentsToApplicationAsync_ReturnsFailure_WhenUserLacksAccess()
    {
        // Arrange
        var sut = CreateSut();
        var modelState = new ModelStateDictionary();

        const string error = "retrieval failure";

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new FellingLicenceApplication
            {
                Id = _applicationId,
                ApplicationReference = "REF123",
                Documents = new List<Document>
                {
                    Fixture.Build<Document>().With(x => x.Purpose, DocumentPurpose.EiaAttachment).Without(x => x.DeletionTimestamp).Create()
                },
                FellingLicenceApplicationStepStatus = new FellingLicenceApplicationStepStatus
                {
                    EnvironmentalImpactAssessmentStatus = null
                },
                WoodlandOwnerId = Guid.NewGuid()
            }));

        _mockUpdateFellingLicenceApplication
            .Setup(x => x.UpdateEnvironmentalImpactAssessmentStatusAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Could not update application step status."));

        // Act
        var result = await sut.AddEiaDocumentsToApplicationAsync(_externalApplicant, _applicationId, FileCollection, modelState, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Application not found or user cannot access it.");

        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(_externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .VerifyNoOtherCalls();

        _mockAddDocumentService.VerifyNoOtherCalls();

        _mockAuditService.Verify(x =>
                x.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                        e.ActorType == ActorType.ExternalApplicant &&
                        e.EventName == AuditEvents.ApplicantUploadEiaDocumentsFailure),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkEiaAsCompletedAsync_ReturnsSuccess_WhenEiaIsMarkedCompleted()
    {
        // Arrange
        var sut = CreateSut();
        var eiaRecord = new EnvironmentalImpactAssessmentRecord { HasApplicationBeenCompleted = true };

        SetUpAccessSuccess();

        _mockUpdateFellingLicenceApplication
            .Setup(x => x.UpdateEnvironmentalImpactAssessmentAsync(_applicationId, eiaRecord, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await sut.MarkEiaAsCompletedAsync(_applicationId, eiaRecord, _externalApplicant, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockUpdateFellingLicenceApplication.Verify(x => x.UpdateEnvironmentalImpactAssessmentAsync(_applicationId, eiaRecord, It.IsAny<CancellationToken>()), Times.Once);
        
        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(_externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetIsEditable(_applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockAuditService.Verify(x =>
                x.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                        e.ActorType == ActorType.ExternalApplicant &&
                        e.EventName == AuditEvents.ApplicantCompleteEiaSection),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkEiaAsCompletedAsync_ReturnsFailure_WhenApplicationIsNotEditable()
    {
        // Arrange
        var sut = CreateSut();
        var eiaRecord = new EnvironmentalImpactAssessmentRecord
        {
            HasApplicationBeenCompleted = true,
            HasApplicationBeenSent = true
        };

        SetUpAccessSuccess(false);

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new FellingLicenceApplication
            {
                Id = _applicationId,
                ApplicationReference = "REF123",
                Documents = new List<Document>
                {
                    Fixture.Build<Document>().With(x => x.Purpose, DocumentPurpose.EiaAttachment).Without(x => x.DeletionTimestamp).Create()
                },
                FellingLicenceApplicationStepStatus = new FellingLicenceApplicationStepStatus
                {
                    EnvironmentalImpactAssessmentStatus = false
                },
                WoodlandOwnerId = Guid.NewGuid(),
                StatusHistories = [
                    new StatusHistory
                    {
                        Status = FellingLicenceStatus.Refused,
                    }
                ]
            }));

        // Act
        var result = await sut.MarkEiaAsCompletedAsync(_applicationId, eiaRecord, _externalApplicant, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Application not found or user cannot access it.");

        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(_externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockGetFellingLicenceApplicationServiceForExternalUsers 
            .Verify(x => x.GetIsEditable(_applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockAuditService.Verify(x =>
                x.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                        e.ActorType == ActorType.ExternalApplicant &&
                        e.EventName == AuditEvents.ApplicantCompleteEiaSectionFailure),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkEiaAsCompletedAsync_ReturnsFailure_WhenUpdateEiaRecordFails()
    {
        // Arrange
        var sut = CreateSut();
        var eiaRecord = new EnvironmentalImpactAssessmentRecord { HasApplicationBeenCompleted = true };

        SetUpAccessSuccess();

        _mockUpdateFellingLicenceApplication
            .Setup(x => x.UpdateEnvironmentalImpactAssessmentAsync(_applicationId, eiaRecord, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Could not update EIA record."));

        // Act
        var result = await sut.MarkEiaAsCompletedAsync(_applicationId, eiaRecord, _externalApplicant, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Could not update EIA record.");
        _mockUpdateFellingLicenceApplication.Verify(x => x.UpdateEnvironmentalImpactAssessmentAsync(_applicationId, eiaRecord, It.IsAny<CancellationToken>()), Times.Once);

        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(_externalApplicant.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetIsEditable(_applicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockAuditService.Verify(x =>
            x.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                e.ActorType == ActorType.ExternalApplicant &&
                e.EventName == AuditEvents.ApplicantCompleteEiaSectionFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private void SetUpAccessSuccess(bool? editable = null)
    {
        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserAccessModel()));

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(editable ?? true));
    }

    private EnvironmentalImpactAssessmentUseCase CreateSut()
    {
        _mockAddDocumentService.Reset();
        _mockUpdateFellingLicenceApplication.Reset();
        _mockAuditService.Reset();
        _mockRetrieveUserAccountsService.Reset();
        _mockRetrieveWoodlandOwners.Reset();
        _mockGetFellingLicenceApplicationServiceForExternalUsers.Reset();
        _mockGetPropertyProfilesService.Reset();
        _mockGetCompartmentsService.Reset();
        _mockEiaOptions.Reset();
        _mockAgentAuthorityService.Reset();
        _mockClock.Reset();
        _mockLogger.Reset();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserAccessModel()));

        _mockGetFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new FellingLicenceApplication
            {
                Id = _applicationId,
                ApplicationReference = "REF123",
                Documents = new List<Document>
                {
                    Fixture.Build<Document>().With(x => x.Purpose, DocumentPurpose.EiaAttachment).Without(x => x.DeletionTimestamp).Create()
                },
                FellingLicenceApplicationStepStatus = new FellingLicenceApplicationStepStatus
                {
                    EnvironmentalImpactAssessmentStatus = false
                },
                WoodlandOwnerId = Guid.NewGuid()
            }));

        _mockAddDocumentService
            .Setup(x => x.AddDocumentsAsExternalApplicantAsync(It.IsAny<AddDocumentsExternalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddDocumentsSuccessResult([Guid.NewGuid()], []));

        _mockUpdateFellingLicenceApplication
            .Setup(x => x.UpdateEnvironmentalImpactAssessmentStatusAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockEiaOptions.Setup(x => x.Value).Returns(new EiaOptions
        {
            EiaApplicationExternalUri = ExternalUri
        });


        return new EnvironmentalImpactAssessmentUseCase(
            _mockAddDocumentService.Object,
            _mockUpdateFellingLicenceApplication.Object,
            _mockAuditService.Object,
            _mockRetrieveUserAccountsService.Object,
            _mockRetrieveWoodlandOwners.Object,
            _mockGetFellingLicenceApplicationServiceForExternalUsers.Object,
            _mockGetPropertyProfilesService.Object,
            _mockEiaOptions.Object,
            _mockGetCompartmentsService.Object,
            _mockAgentAuthorityService.Object,
            _requestContext,
            _mockClock.Object,
            _mockLogger.Object
        );
    }
}
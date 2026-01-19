using AutoFixture;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.TreeHealth;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Moq;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Microsoft.AspNetCore.Http;
using System.Text;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.ResultModels;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Forestry.Flo.External.Web.Tests.Services;

public class CollectTreeHealthIssuesUseCaseTests
{
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsMock = new();
    private readonly Mock<IRetrieveWoodlandOwners> _retrieveWoodlandOwnersMock = new();
    private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _retrieveFellingLicenceApplicationMock = new();
    private readonly Mock<IGetPropertyProfiles> _retrievePropertyProfilesMock = new();
    private readonly Mock<IGetCompartments> _retrieveCompartmentsMock = new();
    private readonly Mock<IAgentAuthorityService> _retrieveAgentAuthorityMock = new();
    private readonly Mock<IUpdateFellingLicenceApplicationForExternalUsers> _updateFellingLicenceApplicationMock = new();
    private readonly Mock<IAuditService<CollectTreeHealthIssuesUseCase>> _auditMock = new();
    private readonly Mock<IAddDocumentService> _addDocumentsMock = new();
    private FormFileCollection _formFileCollection = new();

    private readonly TreeHealthOptions _treeHealthOptions = new();

    private RequestContext _requestContext;

    private readonly Fixture _fixture = new();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task GetTreeHealthIssuesViewModelWhenCannotRetrieveUserAccess(
        Guid applicationId,
        Guid userId,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));
        
        var user = GetExternalApplicant(userId);

        var result = await sut.GetTreeHealthIssuesViewModelAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetTreeHealthIssuesViewModelWhenCannotRetrieveApplication(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccessModel,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetTreeHealthIssuesViewModelAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        
        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
        _addDocumentsMock.VerifyNoOtherCalls();
    }

    // assume other failure scenarios for retrieving ApplicationSummary using the usecasebase methods are covered in other test classes

    [Theory, AutoMoqData]
    public async Task GetTreeHealthIssuesViewModelWhenNoTreeHealthDataSavedYet(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency,
        string singleTreeHealthReason)
    {
        _treeHealthOptions.TreeHealthIssues = [singleTreeHealthReason];

        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        application.IsTreeHealthIssue = null;
        application.TreeHealthIssues = [];
        application.TreeHealthIssueOther = null;
        application.TreeHealthIssueOtherDetails = null;

        foreach (var applicationDocument in application.Documents)
        {
            applicationDocument.Purpose = DocumentPurpose.Attachment;
        }

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _retrieveAgentAuthorityMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.From(agency));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetTreeHealthIssuesViewModelAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value.TreeHealthDocuments);
        Assert.False(result.Value.TreeHealthIssues.NoTreeHealthIssues);
        Assert.False(result.Value.TreeHealthIssues.OtherTreeHealthIssue);
        Assert.Null(result.Value.TreeHealthIssues.OtherTreeHealthIssueDetails);

        Assert.Single(result.Value.TreeHealthIssues.TreeHealthIssueSelections);
        Assert.Equal(singleTreeHealthReason, result.Value.TreeHealthIssues.TreeHealthIssueSelections.Keys.Single());
        Assert.False(result.Value.TreeHealthIssues.TreeHealthIssueSelections[singleTreeHealthReason]);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(3));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();

        _retrieveAgentAuthorityMock.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetTreeHealthIssuesViewModelWhenTreeHealthDataExists(
        Guid applicationId,
        Guid userId,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency,
        List<string> selectedTreeHealthIssues,
        string unselectedTreeHealthIssue,
        string selectedTreeHealthIssueNoLongerInOptions)
    {
        _treeHealthOptions.TreeHealthIssues = [ ..selectedTreeHealthIssues, unselectedTreeHealthIssue ];

        var sut = CreateSut();

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        application.IsTreeHealthIssue = true;
        application.TreeHealthIssues = [ ..selectedTreeHealthIssues, selectedTreeHealthIssueNoLongerInOptions ];
        application.TreeHealthIssueOther = true;

        foreach (var applicationDocument in application.Documents)
        {
            applicationDocument.Purpose = DocumentPurpose.TreeHealthAttachment;
            applicationDocument.DeletionTimestamp = null;
            applicationDocument.VisibleToApplicant = true;
        }

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _retrieveAgentAuthorityMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.From(agency));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetTreeHealthIssuesViewModelAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(application.Documents.Count, result.Value.TreeHealthDocuments.Count());
        foreach (var expectedDocument in application.Documents)
        {
            Assert.Contains(result.Value.TreeHealthDocuments, doc =>
                doc.FileName == expectedDocument.FileName
                && doc.Id == expectedDocument.Id
                && doc.DocumentPurpose == expectedDocument.Purpose
                && doc.FileType == expectedDocument.FileType);
        }
        
        Assert.False(result.Value.TreeHealthIssues.NoTreeHealthIssues);
        Assert.True(result.Value.TreeHealthIssues.OtherTreeHealthIssue);
        Assert.Equal(application.TreeHealthIssueOtherDetails, result.Value.TreeHealthIssues.OtherTreeHealthIssueDetails);

        Assert.Equal(selectedTreeHealthIssues.Count + 2, result.Value.TreeHealthIssues.TreeHealthIssueSelections.Count);
        foreach (var expectedIssue in selectedTreeHealthIssues)
        {
            Assert.True(result.Value.TreeHealthIssues.TreeHealthIssueSelections.ContainsKey(expectedIssue));
            Assert.True(result.Value.TreeHealthIssues.TreeHealthIssueSelections[expectedIssue]);
        }
        Assert.True(result.Value.TreeHealthIssues.TreeHealthIssueSelections.ContainsKey(selectedTreeHealthIssueNoLongerInOptions));
        Assert.True(result.Value.TreeHealthIssues.TreeHealthIssueSelections[selectedTreeHealthIssueNoLongerInOptions]);
        Assert.True(result.Value.TreeHealthIssues.TreeHealthIssueSelections.ContainsKey(unselectedTreeHealthIssue));
        Assert.False(result.Value.TreeHealthIssues.TreeHealthIssueSelections[unselectedTreeHealthIssue]);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(3));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();

        _retrieveAgentAuthorityMock.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SubmitTreeHealthIssuesWhenCannotRetrieveUserAccess(
        Guid applicationId,
        Guid userId,
        TreeHealthIssuesViewModel model,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.SubmitTreeHealthIssuesAsync(applicationId, user, model, new FormFileCollection(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SubmitTreeHealthIssuesError.StoreTreeHealthIssues, result.Error);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesSubmittedFailure
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new Dictionary<string, string>
                         {
                             { "Error1", error }
                         }, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();
        
        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SubmitTreeHealthIssuesWhenUserCannotAccessApplication(
        Guid applicationId,
        Guid userId,
        TreeHealthIssuesViewModel model,
        UserAccessModel userAccessModel,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.SubmitTreeHealthIssuesAsync(applicationId, user, model, new FormFileCollection(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SubmitTreeHealthIssuesError.StoreTreeHealthIssues, result.Error);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        
        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetIsEditable(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesSubmittedFailure
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new Dictionary<string, string>
                         {
                             { "Error1", "Application not found or user cannot access it" }
                         }, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SubmitTreeHealthIssuesWhenApplicationIsNotEditable(
        Guid applicationId,
        Guid userId,
        TreeHealthIssuesViewModel model,
        UserAccessModel userAccessModel)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        var user = GetExternalApplicant(userId);

        var result = await sut.SubmitTreeHealthIssuesAsync(applicationId, user, model, new FormFileCollection(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SubmitTreeHealthIssuesError.StoreTreeHealthIssues, result.Error);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetIsEditable(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesSubmittedFailure
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new Dictionary<string, string>
                         {
                             { "Error1", "Application not found or user cannot access it" }
                         }, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SubmitTreeHealthIssuesWhenCannotRetrieveApplicationToAddFiles(
        Guid applicationId,
        Guid userId,
        TreeHealthIssuesViewModel model,
        UserAccessModel userAccessModel,
        string error)
    {
        model.TreeHealthIssues.NoTreeHealthIssues = false;

        var sut = CreateSut();

        AddFileToFormCollection();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.SubmitTreeHealthIssuesAsync(applicationId, user, model, _formFileCollection, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SubmitTreeHealthIssuesError.DocumentUpload, result.Error);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetIsEditable(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesDocumentsUploadedFailure
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new Dictionary<string, string>
                         {
                             { "Error1", "Application not found or user cannot access it" }
                         }, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SubmitTreeHealthIssuesWhenFailsToAddFiles(
        Guid applicationId,
        Guid userId,
        TreeHealthIssuesViewModel model,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        AddDocumentsFailureResult error)
    {
        model.TreeHealthIssues.NoTreeHealthIssues = false;

        var sut = CreateSut();

        AddFileToFormCollection();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _addDocumentsMock
            .Setup(x => x.AddDocumentsAsExternalApplicantAsync(It.IsAny<AddDocumentsExternalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AddDocumentsSuccessResult, AddDocumentsFailureResult>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.SubmitTreeHealthIssuesAsync(applicationId, user, model, _formFileCollection, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SubmitTreeHealthIssuesError.DocumentUpload, result.Error);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetIsEditable(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        var expectedErrorDictionary = error.UserFacingFailureMessages
            .Select((msg, idx) => new { Key = $"Error{idx + 1}", Value = msg })
            .ToDictionary(x => x.Key, x => x.Value);

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesDocumentsUploadedFailure
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(expectedErrorDictionary, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        var expectedDocumentsCount = application.Documents!.Count(x =>
            x.DeletionTimestamp is null
            && x.Purpose is DocumentPurpose.EiaAttachment or DocumentPurpose.Attachment or DocumentPurpose.TreeHealthAttachment);

        _addDocumentsMock
            .Verify(x => x.AddDocumentsAsExternalApplicantAsync(It.Is<AddDocumentsExternalRequest>(d => 
                d.ActorType == ActorType.ExternalApplicant
                && d.ApplicationDocumentCount == expectedDocumentsCount
                && d.DocumentPurpose == DocumentPurpose.TreeHealthAttachment
                && d.FellingApplicationId == applicationId
                && d.FileToStoreModels.Count == 1
                && d.ReceivedByApi == false
                && d.UserAccountId == userId
                && d.VisibleToApplicant
                && d.VisibleToConsultee
                && d.WoodlandOwnerId == application.WoodlandOwnerId), It.IsAny<CancellationToken>()), Times.Once);
        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SubmitTreeHealthIssuesWithDocumentsWhenFailsToUpdateApplication(
        Guid applicationId,
        Guid userId,
        TreeHealthIssuesViewModel model,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        AddDocumentsSuccessResult addDocumentsResult,
        string error)
    {
        model.TreeHealthIssues.NoTreeHealthIssues = false;

        var sut = CreateSut();

        AddFileToFormCollection();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _addDocumentsMock
            .Setup(x => x.AddDocumentsAsExternalApplicantAsync(It.IsAny<AddDocumentsExternalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(addDocumentsResult));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.UpdateApplicationTreeHealthIssuesDataAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<TreeHealthIssuesModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.SubmitTreeHealthIssuesAsync(applicationId, user, model, _formFileCollection, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SubmitTreeHealthIssuesError.StoreTreeHealthIssues, result.Error);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetIsEditable(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock
            .Verify(x => x.UpdateApplicationTreeHealthIssuesDataAsync(applicationId, userAccessModel, model.TreeHealthIssues, It.IsAny<CancellationToken>()), Times.Once);
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesDocumentsUploaded
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new 
                         {
                             documentsCount = 1
                         }, _serializerOptions)),
                CancellationToken.None), Times.Once);

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesSubmittedFailure
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new Dictionary<string, string>
                         {
                             { "Error1", error }
                         }, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        var expectedDocumentsCount = application.Documents!.Count(x =>
            x.DeletionTimestamp is null
            && x.Purpose is DocumentPurpose.EiaAttachment or DocumentPurpose.Attachment or DocumentPurpose.TreeHealthAttachment);

        _addDocumentsMock
            .Verify(x => x.AddDocumentsAsExternalApplicantAsync(It.Is<AddDocumentsExternalRequest>(d =>
                d.ActorType == ActorType.ExternalApplicant
                && d.ApplicationDocumentCount == expectedDocumentsCount
                && d.DocumentPurpose == DocumentPurpose.TreeHealthAttachment
                && d.FellingApplicationId == applicationId
                && d.FileToStoreModels.Count == 1
                && d.ReceivedByApi == false
                && d.UserAccountId == userId
                && d.VisibleToApplicant
                && d.VisibleToConsultee
                && d.WoodlandOwnerId == application.WoodlandOwnerId), It.IsAny<CancellationToken>()), Times.Once);
        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SubmitTreeHealthIssuesWithDocumentsWhenSuccessfullyUpdatesApplication(
        Guid applicationId,
        Guid userId,
        TreeHealthIssuesViewModel model,
        UserAccessModel userAccessModel,
        FellingLicenceApplication application,
        AddDocumentsSuccessResult addDocumentsResult)
    {
        model.TreeHealthIssues.NoTreeHealthIssues = false;

        var sut = CreateSut();

        AddFileToFormCollection();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _addDocumentsMock
            .Setup(x => x.AddDocumentsAsExternalApplicantAsync(It.IsAny<AddDocumentsExternalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AddDocumentsSuccessResult, AddDocumentsFailureResult>(addDocumentsResult));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.UpdateApplicationTreeHealthIssuesDataAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<TreeHealthIssuesModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var user = GetExternalApplicant(userId);

        var result = await sut.SubmitTreeHealthIssuesAsync(applicationId, user, model, _formFileCollection, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetIsEditable(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetApplicationByIdAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock
            .Verify(x => x.UpdateApplicationTreeHealthIssuesDataAsync(applicationId, userAccessModel, model.TreeHealthIssues, It.IsAny<CancellationToken>()), Times.Once);
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesDocumentsUploaded
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new
                         {
                             documentsCount = 1
                         }, _serializerOptions)),
                CancellationToken.None), Times.Once);

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesSubmitted
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new { isTreeHealthIssue = true }, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        var expectedDocumentsCount = application.Documents!.Count(x =>
            x.DeletionTimestamp is null
            && x.Purpose is DocumentPurpose.EiaAttachment or DocumentPurpose.Attachment or DocumentPurpose.TreeHealthAttachment);

        _addDocumentsMock
            .Verify(x => x.AddDocumentsAsExternalApplicantAsync(It.Is<AddDocumentsExternalRequest>(d =>
                d.ActorType == ActorType.ExternalApplicant
                && d.ApplicationDocumentCount == expectedDocumentsCount
                && d.DocumentPurpose == DocumentPurpose.TreeHealthAttachment
                && d.FellingApplicationId == applicationId
                && d.FileToStoreModels.Count == 1
                && d.ReceivedByApi == false
                && d.UserAccountId == userId
                && d.VisibleToApplicant
                && d.VisibleToConsultee
                && d.WoodlandOwnerId == application.WoodlandOwnerId), It.IsAny<CancellationToken>()), Times.Once);
        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SubmitTreeHealthIssuesWithNoDocumentsWhenSuccessfullyUpdatesApplication(
        Guid applicationId,
        Guid userId,
        TreeHealthIssuesViewModel model,
        UserAccessModel userAccessModel)
    {
        model.TreeHealthIssues.NoTreeHealthIssues = false;

        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.UpdateApplicationTreeHealthIssuesDataAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<TreeHealthIssuesModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var user = GetExternalApplicant(userId);

        var result = await sut.SubmitTreeHealthIssuesAsync(applicationId, user, model, [], CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetIsEditable(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock
            .Verify(x => x.UpdateApplicationTreeHealthIssuesDataAsync(applicationId, userAccessModel, model.TreeHealthIssues, It.IsAny<CancellationToken>()), Times.Once);
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesSubmitted
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new { isTreeHealthIssue = true }, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        _addDocumentsMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SubmitTreeHealthIssuesWithDocumentsWhenSuccessfullyUpdatesApplicationAndIsNotTreeHealthIssue(
        Guid applicationId,
        Guid userId,
        TreeHealthIssuesViewModel model,
        UserAccessModel userAccessModel)
    {
        model.TreeHealthIssues.NoTreeHealthIssues = true;

        var sut = CreateSut();

        AddFileToFormCollection();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetIsEditable(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        _updateFellingLicenceApplicationMock
            .Setup(x => x.UpdateApplicationTreeHealthIssuesDataAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<TreeHealthIssuesModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var user = GetExternalApplicant(userId);

        var result = await sut.SubmitTreeHealthIssuesAsync(applicationId, user, model, _formFileCollection, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock
            .Verify(x => x.GetIsEditable(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock
            .Verify(x => x.UpdateApplicationTreeHealthIssuesDataAsync(applicationId, userAccessModel, model.TreeHealthIssues, It.IsAny<CancellationToken>()), Times.Once);
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthIssuesSubmitted
                         && e.UserId == userId
                         && e.ActorType == ActorType.ExternalApplicant
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new { isTreeHealthIssue = false }, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();

        _addDocumentsMock.VerifyNoOtherCalls();
    }

    private ExternalApplicant GetExternalApplicant(Guid? userId = null)
    {
        var userPrinciple = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: userId ?? Guid.NewGuid(), accountTypeExternal: AccountTypeExternal.FcUser, isFcUser: true);
        return new ExternalApplicant(userPrinciple);
    }

    private void AddFileToFormCollection(string fileName = "test.csv", string expectedFileContents = "test", string contentType = "text/csv")
    {
        var fileBytes = Encoding.Default.GetBytes(expectedFileContents);
        var formFileMock = new Mock<IFormFile>();

        formFileMock.Setup(ff => ff.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((s, _) =>
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

    private CollectTreeHealthIssuesUseCase CreateSut()
    {
        _retrieveUserAccountsMock.Reset();
        _retrieveWoodlandOwnersMock.Reset();
        _retrieveFellingLicenceApplicationMock.Reset();
        _retrievePropertyProfilesMock.Reset();
        _retrieveCompartmentsMock.Reset();
        _retrieveAgentAuthorityMock.Reset();
        _updateFellingLicenceApplicationMock.Reset();
        _auditMock.Reset();
        _addDocumentsMock.Reset();

        _formFileCollection = [];

        var userPrinciple = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(), accountTypeExternal: AccountTypeExternal.FcUser);
        _requestContext = new("test", new RequestUserModel(userPrinciple));
        
        return new CollectTreeHealthIssuesUseCase(
            _retrieveUserAccountsMock.Object,
            _retrieveWoodlandOwnersMock.Object,
            _retrieveFellingLicenceApplicationMock.Object,
            _retrievePropertyProfilesMock.Object,
            _retrieveCompartmentsMock.Object,
            _retrieveAgentAuthorityMock.Object,
            _updateFellingLicenceApplicationMock.Object,
            _auditMock.Object,
            new OptionsWrapper<TreeHealthOptions>(_treeHealthOptions),
            _addDocumentsMock.Object,
            _requestContext,
            new NullLogger<CollectTreeHealthIssuesUseCase>());
    }
}
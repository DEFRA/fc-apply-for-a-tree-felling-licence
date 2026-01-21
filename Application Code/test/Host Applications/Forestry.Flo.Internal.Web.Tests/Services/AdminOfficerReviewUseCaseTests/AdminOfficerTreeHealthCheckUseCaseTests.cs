using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.AdminOfficerReviewUseCaseTests;

public class AdminOfficerTreeHealthCheckUseCaseTests
{
    private readonly Mock<IGetFellingLicenceApplicationForInternalUsers> _getFlaServiceMock = new();
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerServiceMock = new();
    private readonly Mock<IUpdateAdminOfficerReviewService> _updateAdminOfficerReviewServiceMock = new();
    private readonly Mock<IAuditService<AdminOfficerReviewUseCaseBase>> _auditServiceMock = new();
    private InternalUser _internalUser;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task GetViewModelWhenUnableToFindApplication(Guid applicationId, string error)
    {
        var sut = CreateSut();

        _getFlaServiceMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>(error));

        var result = await sut.GetTreeHealthCheckAdminOfficerViewModelAsync(applicationId, CancellationToken.None);
        Assert.True(result.IsFailure);

        _getFlaServiceMock
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getFlaServiceMock.VerifyNoOtherCalls();

        _woodlandOwnerServiceMock.VerifyNoOtherCalls();
        _updateAdminOfficerReviewServiceMock.VerifyNoOtherCalls();
        _auditServiceMock.VerifyNoOtherCalls();
    }

    // extractapplicationsummary from base usecase class is tested in other test classes

    [Theory, AutoMoqData]
    public async Task GetViewModelWhenApplicantHasEnteredNoTreeHealthIssues(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner,
        bool? treeHealthCheckComplete)
    {
        var sut = CreateSut();

        InitialiseFellingLicenceApplication(application, false, treeHealthChecksComplete: treeHealthCheckComplete);

        _getFlaServiceMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _woodlandOwnerServiceMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        var result = await sut.GetTreeHealthCheckAdminOfficerViewModelAsync(applicationId, CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.Equal(treeHealthCheckComplete is true, result.Value.Confirmed);
        Assert.NotNull(result.Value.TreeHealthIssuesViewModel);
        Assert.NotNull(result.Value.TreeHealthIssuesViewModel.TreeHealthIssues);
        Assert.True(result.Value.TreeHealthIssuesViewModel.TreeHealthIssues.NoTreeHealthIssues);
        Assert.Empty(result.Value.TreeHealthIssuesViewModel.TreeHealthIssues.TreeHealthIssueSelections);
        Assert.False(result.Value.TreeHealthIssuesViewModel.TreeHealthIssues.OtherTreeHealthIssue);
        Assert.Null(result.Value.TreeHealthIssuesViewModel.TreeHealthIssues.OtherTreeHealthIssueDetails);
        Assert.Empty(result.Value.TreeHealthIssuesViewModel.TreeHealthDocuments);

        _getFlaServiceMock
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getFlaServiceMock.VerifyNoOtherCalls();

        _woodlandOwnerServiceMock
            .Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _woodlandOwnerServiceMock.VerifyNoOtherCalls();

        _updateAdminOfficerReviewServiceMock.VerifyNoOtherCalls();
        _auditServiceMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetViewModelWhenApplicantHasEnteredTreeHealthIssues(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner,
        bool? treeHealthCheckComplete,
        bool treeHealthCheckOther,
        string treeHealthOtherDetails,
        List<string> treeHealthIssues)
    {
        var sut = CreateSut();

        var attachment = new Document
        {
            Purpose = DocumentPurpose.Attachment,
            AttachedById = Guid.NewGuid(),
            AttachedByType = ActorType.ExternalApplicant,
            CreatedTimestamp = DateTime.Today,
            FileName = "filename.pdf",
            FileSize = 543,
            FileType = "pdf",
            MimeType = "application/pdf",
            Location = "here/there",
            VisibleToApplicant = true,
            VisibleToConsultee = true
        };
        var treeHealthDoc = new Document
        {
            Purpose = DocumentPurpose.TreeHealthAttachment,
            AttachedById = Guid.NewGuid(),
            AttachedByType = ActorType.ExternalApplicant,
            CreatedTimestamp = DateTime.Today,
            FileName = "treehealth.pdf",
            FileSize = 543,
            FileType = "pdf",
            MimeType = "application/pdf",
            Location = "here/there",
            VisibleToApplicant = true,
            VisibleToConsultee = true
        };
        var deletedTreeHealthDoc = new Document
        {
            Purpose = DocumentPurpose.TreeHealthAttachment,
            AttachedById = Guid.NewGuid(),
            AttachedByType = ActorType.ExternalApplicant,
            CreatedTimestamp = DateTime.Today,
            DeletionTimestamp = DateTime.UtcNow,
            FileName = "deletedtreehealth.pdf",
            FileSize = 543,
            FileType = "pdf",
            MimeType = "application/pdf",
            Location = "here/there",
            VisibleToApplicant = true,
            VisibleToConsultee = true
        };

        InitialiseFellingLicenceApplication(application, true, treeHealthCheckOther, 
            treeHealthCheckOther ? treeHealthOtherDetails : null, treeHealthIssues, treeHealthCheckComplete, [attachment, treeHealthDoc, deletedTreeHealthDoc]);

        _getFlaServiceMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _woodlandOwnerServiceMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        var result = await sut.GetTreeHealthCheckAdminOfficerViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.Equal(treeHealthCheckComplete is true, result.Value.Confirmed);
        Assert.NotNull(result.Value.TreeHealthIssuesViewModel);
        Assert.NotNull(result.Value.TreeHealthIssuesViewModel.TreeHealthIssues);
        Assert.False(result.Value.TreeHealthIssuesViewModel.TreeHealthIssues.NoTreeHealthIssues);
        Assert.Equivalent(treeHealthIssues.Distinct().ToDictionary(item => item, _ => true), result.Value.TreeHealthIssuesViewModel.TreeHealthIssues.TreeHealthIssueSelections);
        Assert.Equal(treeHealthCheckOther, result.Value.TreeHealthIssuesViewModel.TreeHealthIssues.OtherTreeHealthIssue);
        Assert.Equal(treeHealthCheckOther ? treeHealthOtherDetails : null, result.Value.TreeHealthIssuesViewModel.TreeHealthIssues.OtherTreeHealthIssueDetails);
        Assert.Single(result.Value.TreeHealthIssuesViewModel.TreeHealthDocuments);

        var resultDoc = result.Value.TreeHealthIssuesViewModel.TreeHealthDocuments.Single();
        Assert.Equal(treeHealthDoc.Purpose, resultDoc.DocumentPurpose);
        Assert.Equal(treeHealthDoc.FileName, resultDoc.FileName);
        Assert.Equal(treeHealthDoc.FileSize, resultDoc.FileSize);
        Assert.Equal(treeHealthDoc.FileType, resultDoc.FileType);
        Assert.Equal(treeHealthDoc.MimeType, resultDoc.MimeType);

        _getFlaServiceMock
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getFlaServiceMock.VerifyNoOtherCalls();

        _woodlandOwnerServiceMock
            .Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _woodlandOwnerServiceMock.VerifyNoOtherCalls();

        _updateAdminOfficerReviewServiceMock.VerifyNoOtherCalls();
        _auditServiceMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ConfirmTreeHealthCheckedWhenServiceReturnsFailure(Guid applicationId, Guid userId, string error)
    {
        var sut = CreateSut();

        var user = GetInternalUser(userId);

        _updateAdminOfficerReviewServiceMock
            .Setup(x => x.ConfirmTreeHealthCheckAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.ConfirmTreeHealthCheckedAsync(applicationId, user, CancellationToken.None);
        Assert.True(result.IsFailure);

        _getFlaServiceMock.VerifyNoOtherCalls();
        _woodlandOwnerServiceMock.VerifyNoOtherCalls();

        _updateAdminOfficerReviewServiceMock
            .Verify(x => x.ConfirmTreeHealthCheckAsync(applicationId, userId, It.IsAny<CancellationToken>()), Times.Once);
        _updateAdminOfficerReviewServiceMock.VerifyNoOtherCalls();

        _auditServiceMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthOaoCheckCompletedFailure
                         && e.UserId == userId
                         && e.ActorType == ActorType.InternalUser
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new
                         {
                             error
                         }, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditServiceMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ConfirmTreeHealthCheckedWhenServiceReturnsSuccess(Guid applicationId, Guid userId)
    {
        var sut = CreateSut();

        var user = GetInternalUser(userId);

        _updateAdminOfficerReviewServiceMock
            .Setup(x => x.ConfirmTreeHealthCheckAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.ConfirmTreeHealthCheckedAsync(applicationId, user, CancellationToken.None);
        Assert.True(result.IsSuccess);

        _getFlaServiceMock.VerifyNoOtherCalls();
        _woodlandOwnerServiceMock.VerifyNoOtherCalls();

        _updateAdminOfficerReviewServiceMock
            .Verify(x => x.ConfirmTreeHealthCheckAsync(applicationId, userId, It.IsAny<CancellationToken>()), Times.Once);
        _updateAdminOfficerReviewServiceMock.VerifyNoOtherCalls();

        _auditServiceMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.TreeHealthOaoCheckCompleted
                         && e.UserId == userId
                         && e.ActorType == ActorType.InternalUser
                         && e.SourceEntityId == applicationId
                         && e.SourceEntityType == SourceEntityType.FellingLicenceApplication
                         && JsonSerializer.Serialize(e.AuditData, _serializerOptions) ==
                         JsonSerializer.Serialize(new { }, _serializerOptions)),
                CancellationToken.None), Times.Once);
        _auditServiceMock.VerifyNoOtherCalls();
    }

    private AdminOfficerTreeHealthCheckUseCase CreateSut()
    {
        _getFlaServiceMock.Reset();
        _woodlandOwnerServiceMock.Reset();
        _updateAdminOfficerReviewServiceMock.Reset();
        _auditServiceMock.Reset();

        var agentAuthorityServiceMock = new Mock<IAgentAuthorityService>();
        agentAuthorityServiceMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: Flo.Services.Common.User.AccountTypeInternal.AdminOfficer);
        _internalUser = new InternalUser(userPrincipal);

        return new AdminOfficerTreeHealthCheckUseCase(
            Mock.Of<IUserAccountService>(),
            Mock.Of<IRetrieveUserAccountsService>(),
            new NullLogger<AdminOfficerTreeHealthCheckUseCase>(),
            Mock.Of<IFellingLicenceApplicationInternalRepository>(),
            _woodlandOwnerServiceMock.Object,
            _updateAdminOfficerReviewServiceMock.Object,
            _getFlaServiceMock.Object,
            _auditServiceMock.Object,
            agentAuthorityServiceMock.Object,
            Mock.Of<IGetConfiguredFcAreas>(),
            Mock.Of<IWoodlandOfficerReviewSubStatusService>(),
            new RequestContext("test", new RequestUserModel(_internalUser.Principal)));

    }

    private void InitialiseFellingLicenceApplication(
        FellingLicenceApplication application,
        bool isTreeHealthIssue,
        bool? treeHealthIssueOther = null,
        string? treeHealthIssueOtherDetails = null,
        List<string>? treeHealthIssues = null,
        bool? treeHealthChecksComplete = null,
        List<Document>? documents = null)
    {
        application.AssigneeHistories = [];
        application.StatusHistories =
        [
            new StatusHistory
            {
                Created = DateTime.Now,
                CreatedById = Guid.NewGuid(),
                Status = FellingLicenceStatus.Draft
            }
        ];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        application.Documents = documents ?? [];

        application.IsTreeHealthIssue = isTreeHealthIssue;
        application.TreeHealthIssueOtherDetails = treeHealthIssueOtherDetails;
        application.TreeHealthIssues = treeHealthIssues ?? [];
        application.TreeHealthIssueOther = treeHealthIssueOther;
        application.AdminOfficerReview.IsTreeHealthAnswersChecked = treeHealthChecksComplete;
    }

    private InternalUser GetInternalUser(Guid? userId = null)
    {
        var userPrinciple = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: userId ?? Guid.NewGuid(), accountTypeInternal: AccountTypeInternal.AdminOfficer);
        return new InternalUser(userPrinciple);
    }
}
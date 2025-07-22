using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.FileStorage.ResultModels;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class RetrieveLegacyDocumentsServiceRetrieveContentTests
{
    private Mock<ILegacyDocumentsRepository> _mockLegacyDocumentsRepo = new();
    private Mock<IUserAccountRepository> _mockUserAccountRepo = new();
    private Mock<IAgencyRepository> _mockAgencyRepo = new();
    private readonly Mock<IFileStorageService> _mockFileStorage = new();
    private readonly Mock<IAuditService<RetrieveLegacyDocumentsService>> _mockAuditService = new();

    private readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly Guid RequestContextUserId = Guid.NewGuid();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenGetDocumentFromRepositoryFails(Guid userId, Guid legacyDocumentId)
    {
        var sut = CreateSut();
        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LegacyDocument, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.VerifyNoOtherCalls();
        _mockFileStorage.VerifyNoOtherCalls();

        AssertFailureAudit(null);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenGetUserAccountFails(Guid userId, Guid legacyDocumentId, LegacyDocument entity)
    {
        var sut = CreateSut();

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LegacyDocument, UserDbErrorReason>(entity));

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.VerifyNoOtherCalls();
        _mockFileStorage.VerifyNoOtherCalls();

        AssertFailureAudit(entity);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenUserAccountWoodlandOwnerIdDoesNotMatch(Guid userId, Guid legacyDocumentId, LegacyDocument entity, UserAccount userAccount)
    {
        var sut = CreateSut();
        
        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LegacyDocument, UserDbErrorReason>(entity));

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.VerifyNoOtherCalls();
        _mockFileStorage.VerifyNoOtherCalls();

        AssertFailureAudit(entity);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenFailsToGetContentForWoodlandOwnerUser(Guid userId, Guid legacyDocumentId, LegacyDocument entity, UserAccount userAccount)
    {
        var sut = CreateSut();

        userAccount.WoodlandOwnerId = entity.WoodlandOwnerId;

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LegacyDocument, UserDbErrorReason>(entity));

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        _mockFileStorage.Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<GetFileSuccessResult, FileAccessFailureReason>(FileAccessFailureReason.NotFound));

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.VerifyNoOtherCalls();

        _mockFileStorage.Verify(x => x.GetFileAsync(entity.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorage.VerifyNoOtherCalls();

        AssertFailureAudit(entity);
        _mockAuditService.VerifyNoOtherCalls();
    }


    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenGetsContentForWoodlandOwnerUser(Guid userId, Guid legacyDocumentId, LegacyDocument entity, UserAccount userAccount, GetFileSuccessResult fileResult)
    {
        var sut = CreateSut();

        userAccount.WoodlandOwnerId = entity.WoodlandOwnerId;

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LegacyDocument, UserDbErrorReason>(entity));

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        _mockFileStorage.Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<GetFileSuccessResult, FileAccessFailureReason>(fileResult));

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.MimeType, result.Value.ContentType);
        Assert.Equal(entity.FileName, result.Value.FileName);
        Assert.Equal(fileResult.FileBytes, result.Value.FileBytes);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.VerifyNoOtherCalls();

        _mockFileStorage.Verify(x => x.GetFileAsync(entity.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorage.VerifyNoOtherCalls();

        AssertSuccessAudit(entity);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenUserAccountHasNoWoodlandOwnerIdOrAgencyId(Guid userId, Guid legacyDocumentId, LegacyDocument entity)
    {
        var sut = CreateSut();

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LegacyDocument, UserDbErrorReason>(entity));

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(new UserAccount()));

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.VerifyNoOtherCalls();
        _mockFileStorage.VerifyNoOtherCalls();

        AssertFailureAudit(entity);
        _mockAuditService.VerifyNoOtherCalls();
    }


    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenFailsToGetContentForFcAgentUser(
        Guid userId, 
        Guid legacyDocumentId,
        LegacyDocument entity, 
        UserAccount userAccount,
        Agency fcAgency)
    {
        var sut = CreateSut();

        userAccount.WoodlandOwnerId = null;
        userAccount.AgencyId = fcAgency.Id;

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LegacyDocument, UserDbErrorReason>(entity));

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        _mockAgencyRepo
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(fcAgency));

        _mockFileStorage.Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<GetFileSuccessResult, FileAccessFailureReason>(FileAccessFailureReason.NotFound));

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.VerifyNoOtherCalls();

        _mockFileStorage.Verify(x => x.GetFileAsync(entity.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorage.VerifyNoOtherCalls();

        AssertFailureAudit(entity);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenGetsContentForFcAgentUser(
        Guid userId, 
        Guid legacyDocumentId,
        LegacyDocument entity, 
        UserAccount userAccount, 
        Agency fcAgency,
        GetFileSuccessResult fileResult)
    {
        var sut = CreateSut();

        userAccount.WoodlandOwnerId = null;
        userAccount.AgencyId = fcAgency.Id;

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LegacyDocument, UserDbErrorReason>(entity));

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        _mockAgencyRepo
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(fcAgency));

        _mockFileStorage.Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<GetFileSuccessResult, FileAccessFailureReason>(fileResult));

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.MimeType, result.Value.ContentType);
        Assert.Equal(entity.FileName, result.Value.FileName);
        Assert.Equal(fileResult.FileBytes, result.Value.FileBytes);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.VerifyNoOtherCalls();

        _mockFileStorage.Verify(x => x.GetFileAsync(entity.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorage.VerifyNoOtherCalls();

        AssertSuccessAudit(entity);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenAuthorityNotAvailableForAgentUser(
        Guid userId,
        Guid legacyDocumentId,
        LegacyDocument entity,
        UserAccount userAccount,
        Agency fcAgency)
    {
        var sut = CreateSut();

        userAccount.WoodlandOwnerId = null;

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LegacyDocument, UserDbErrorReason>(entity));

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        _mockAgencyRepo
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(fcAgency));

        _mockAgencyRepo
            .Setup(x => x.FindAgentAuthorityStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgentAuthorityStatus>.None);

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.Verify(x => x.FindAgentAuthorityStatusAsync(userAccount.AgencyId!.Value, entity.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.VerifyNoOtherCalls();

        _mockFileStorage.VerifyNoOtherCalls();

        AssertFailureAudit(entity);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenFailsToGetContentForAgentUser(
        Guid userId,
        Guid legacyDocumentId,
        LegacyDocument entity,
        UserAccount userAccount,
        Agency fcAgency,
        AgentAuthorityStatus authority)
    {
        var sut = CreateSut();

        userAccount.WoodlandOwnerId = null;

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LegacyDocument, UserDbErrorReason>(entity));

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        _mockAgencyRepo
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(fcAgency));

        _mockAgencyRepo
            .Setup(x => x.FindAgentAuthorityStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgentAuthorityStatus>.From(authority));

        _mockFileStorage.Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<GetFileSuccessResult, FileAccessFailureReason>(FileAccessFailureReason.NotFound));

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.Verify(x => x.FindAgentAuthorityStatusAsync(userAccount.AgencyId!.Value, entity.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.VerifyNoOtherCalls();

        _mockFileStorage.Verify(x => x.GetFileAsync(entity.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorage.VerifyNoOtherCalls();

        AssertFailureAudit(entity);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenGetsContentForAgentUser(
        Guid userId,
        Guid legacyDocumentId,
        LegacyDocument entity,
        UserAccount userAccount,
        Agency fcAgency,
        AgentAuthorityStatus authority,
        GetFileSuccessResult fileResult)
    {
        var sut = CreateSut();

        userAccount.WoodlandOwnerId = null;

        _mockLegacyDocumentsRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LegacyDocument, UserDbErrorReason>(entity));

        _mockUserAccountRepo
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        _mockAgencyRepo
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(fcAgency));

        _mockAgencyRepo
            .Setup(x => x.FindAgentAuthorityStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgentAuthorityStatus>.From(authority));

        _mockFileStorage.Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<GetFileSuccessResult, FileAccessFailureReason>(fileResult));

        var result = await sut.RetrieveLegacyDocumentContentAsync(userId, legacyDocumentId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.MimeType, result.Value.ContentType);
        Assert.Equal(entity.FileName, result.Value.FileName);
        Assert.Equal(fileResult.FileBytes, result.Value.FileBytes);

        _mockLegacyDocumentsRepo.Verify(x => x.GetAsync(legacyDocumentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockLegacyDocumentsRepo.VerifyNoOtherCalls();

        _mockUserAccountRepo.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepo.VerifyNoOtherCalls();

        _mockAgencyRepo.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.Verify(x => x.FindAgentAuthorityStatusAsync(userAccount.AgencyId!.Value, entity.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepo.VerifyNoOtherCalls();

        _mockFileStorage.Verify(x => x.GetFileAsync(entity.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorage.VerifyNoOtherCalls();

        AssertSuccessAudit(entity);
        _mockAuditService.VerifyNoOtherCalls();
    }

    private RetrieveLegacyDocumentsService CreateSut()
    {
        _mockLegacyDocumentsRepo.Reset();
        _mockFileStorage.Reset();
        _mockAuditService.Reset();
        _mockUserAccountRepo.Reset();
        _mockAgencyRepo.Reset();

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId);
        var requestContext = new RequestContext(
            RequestContextCorrelationId,
            new RequestUserModel(user));

        return new RetrieveLegacyDocumentsService(
            _mockLegacyDocumentsRepo.Object,
            new Mock<IWoodlandOwnerRepository>().Object,
            _mockUserAccountRepo.Object,
            _mockAgencyRepo.Object,
            _mockFileStorage.Object,
            _mockAuditService.Object,
            requestContext,
            new NullLogger<RetrieveLegacyDocumentsService>());
    }

    private void AssertFailureAudit(LegacyDocument? entity)
    {
        if (entity != null)
        {
            _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AccessLegacyDocumentFailureEvent
                && y.SourceEntityId == entity.WoodlandOwnerId
                && y.SourceEntityType == SourceEntityType.WoodlandOwner
                && JsonSerializer.Serialize(y.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    entity.FileName,
                    entity.FileType,
                    entity.DocumentType
                }, _serializerOptions)
            ), It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AccessLegacyDocumentFailureEvent
                && y.SourceEntityId == null
                && y.SourceEntityType == SourceEntityType.WoodlandOwner
            ), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    private void AssertSuccessAudit(LegacyDocument entity)
    {
        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
            y.EventName == AuditEvents.AccessLegacyDocumentEvent
            && y.SourceEntityId == entity.WoodlandOwnerId
            && y.SourceEntityType == SourceEntityType.WoodlandOwner
            && JsonSerializer.Serialize(y.AuditData, _serializerOptions) ==
            JsonSerializer.Serialize(new
            {
                entity.FileName,
                entity.FileType,
                entity.DocumentType
            }, _serializerOptions)
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
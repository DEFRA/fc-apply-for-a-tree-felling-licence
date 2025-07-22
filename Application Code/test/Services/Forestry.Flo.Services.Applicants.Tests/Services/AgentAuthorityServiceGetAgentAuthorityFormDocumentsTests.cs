using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgentAuthorityServiceGetAgentAuthorityFormDocumentsTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = new();
    private Mock<IAgencyRepository> _mockRepository = new();
    private Mock<IUserAccountRepository> _mockUserAccountRepository = new();
    private Mock<IFileStorageService> _mockFileStorageService = new();
    private Mock<IClock> _mockClock = new();
    private Instant _now = new Instant();


    [Theory, AutoData]
    public async Task WhenCannotLocateUserAccount(
        GetAgentAuthorityFormDocumentsRequest request)
    {
        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);

        _mockUserAccountRepository.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenCannotLocateAgentAuthority(
        UserAccount user,
        GetAgentAuthorityFormDocumentsRequest request)
    {
        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthority, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgentAuthorityIsDeactivated(
        UserAccount user,
        AgentAuthority agentAuthority,
        GetAgentAuthorityFormDocumentsRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Deactivated;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgentAuthorityIsNotLinkedToUsersAgency(
        UserAccount user,
        AgentAuthority agentAuthority,
        GetAgentAuthorityFormDocumentsRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);
        user.Agency.IsFcAgency = false;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenUserIsNotAnAgent(
        UserAccount user,
        AgentAuthority agentAuthority,
        GetAgentAuthorityFormDocumentsRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);
        user.AgencyId = null;
        user.Agency = null;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgentAuthorityHasNoAafWithGivenId(
        UserAccount user,
        AgentAuthority agentAuthority,
        GetAgentAuthorityFormDocumentsRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);
        user.AgencyId = agentAuthority.Agency.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenFileStorageFailsWithSingleFile(
        UserAccount user,
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        AafDocument document,
        GetAgentAuthorityFormDocumentsRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        agentAuthorityForm.AafDocuments = new List<AafDocument>(1) { document };
        user.AgencyId = agentAuthority.Agency.Id;
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockFileStorageService
            .Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<GetFileSuccessResult, FileAccessFailureReason>(FileAccessFailureReason.NotFound));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.Verify(x => x.GetFileAsync(document.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenFileStorageFailsWithMultipleFiles(
        UserAccount user,
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        AafDocument[] documents,
        GetAgentAuthorityFormDocumentsRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        agentAuthorityForm.AafDocuments = documents.ToList();
        user.AgencyId = agentAuthority.Agency.Id;
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockFileStorageService
            .Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<GetFileSuccessResult, FileAccessFailureReason>(FileAccessFailureReason.NotFound));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.Verify(x => x.GetFileAsync(documents.First().Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAccessRetrievesSingleFile(
        UserAccount user,
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        AafDocument document,
        byte[] documentBytes,
        GetAgentAuthorityFormDocumentsRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        agentAuthorityForm.AafDocuments = new List<AafDocument>(1) { document };
        user.AgencyId = agentAuthority.Agency.Id;
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockFileStorageService
            .Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<GetFileSuccessResult, FileAccessFailureReason>(new GetFileSuccessResult(documentBytes)));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.Verify(x => x.GetFileAsync(document.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        Assert.Equal(document.FileName, result.Value.FileName);
        Assert.Equal(document.MimeType, result.Value.ContentType);
        Assert.Equal(documentBytes, result.Value.FileBytes);
    }

    [Theory, AutoData]
    public async Task WhenAccessRetrievesMultipleFiles(
        UserAccount user,
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        AafDocument document1,
        AafDocument document2,
        byte[] documentBytes1,
        byte[] documentBytes2,
        GetAgentAuthorityFormDocumentsRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        agentAuthorityForm.AafDocuments = new List<AafDocument>(2) { document1, document2 };
        user.AgencyId = agentAuthority.Agency.Id;
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockFileStorageService
            .Setup(x => x.GetFileAsync(document1.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<GetFileSuccessResult, FileAccessFailureReason>(new GetFileSuccessResult(documentBytes1)));
        _mockFileStorageService
            .Setup(x => x.GetFileAsync(document2.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<GetFileSuccessResult, FileAccessFailureReason>(new GetFileSuccessResult(documentBytes2)));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.Verify(x => x.GetFileAsync(document1.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.Verify(x => x.GetFileAsync(document2.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        Assert.Equal("AAF Document.zip", result.Value.FileName);
        Assert.Equal("application/zip", result.Value.ContentType);
    }

    [Theory, AutoData]
    public async Task WhenAccessSucceedsForFcUser(
        UserAccount user,
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        AafDocument document,
        byte[] documentBytes,
        GetAgentAuthorityFormDocumentsRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        agentAuthorityForm.AafDocuments = new List<AafDocument>(1) { document };
        user.Agency.IsFcAgency = true;
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockFileStorageService
            .Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<GetFileSuccessResult, FileAccessFailureReason>(new GetFileSuccessResult(documentBytes)));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.AccessedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.Verify(x => x.GetFileAsync(document.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        Assert.Equal(document.FileName, result.Value.FileName);
        Assert.Equal(document.MimeType, result.Value.ContentType);
        Assert.Equal(documentBytes, result.Value.FileBytes);
    }

    private IAgentAuthorityService CreateSut()
    {
        _mockUnitOfWork.Reset();
        _mockRepository.Reset();
        _mockRepository.SetupGet(x => x.UnitOfWork).Returns(_mockUnitOfWork.Object);

        _mockUserAccountRepository.Reset();

        _mockClock.Reset();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(_now);

        _mockFileStorageService.Reset();

        return new AgentAuthorityService(
            _mockRepository.Object,
            _mockUserAccountRepository.Object,
            _mockFileStorageService.Object,
            new FileTypesProvider(),
            _mockClock.Object,
            new NullLogger<AgentAuthorityService>());
    }
}
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgentAuthorityServiceInternalGetAgentAuthorityFormDocumentsTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = new();
    private Mock<IAgencyRepository> _mockRepository = new();
    private Mock<IFileStorageService> _mockFileStorageService = new();
    private Mock<IClock> _mockClock = new();
    private Instant _now = new Instant();

    [Theory, AutoData]
    public async Task WhenCannotLocateAgentAuthority(
        GetAgentAuthorityFormDocumentsInternalRequest request)
    {
        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthority, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgentAuthorityHasNoAafWithGivenId(
        AgentAuthority agentAuthority,
        GetAgentAuthorityFormDocumentsInternalRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(0);

        var sut = CreateSut();


        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenFileStorageFailsWithSingleFile(
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        AafDocument document,
        GetAgentAuthorityFormDocumentsInternalRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        agentAuthorityForm.AafDocuments = new List<AafDocument>(1) { document };
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockFileStorageService
            .Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<GetFileSuccessResult, FileAccessFailureReason>(FileAccessFailureReason.NotFound));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.Verify(x => x.GetFileAsync(document.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenFileStorageFailsWithMultipleFiles(
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        AafDocument[] documents,
        GetAgentAuthorityFormDocumentsInternalRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        agentAuthorityForm.AafDocuments = documents.ToList();
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockFileStorageService
            .Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<GetFileSuccessResult, FileAccessFailureReason>(FileAccessFailureReason.NotFound));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.Verify(x => x.GetFileAsync(documents.First().Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAccessRetrievesSingleFile(
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        AafDocument document,
        byte[] documentBytes,
        GetAgentAuthorityFormDocumentsInternalRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        agentAuthorityForm.AafDocuments = new List<AafDocument>(1) { document };
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(agentAuthority));

        _mockFileStorageService
            .Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<GetFileSuccessResult, FileAccessFailureReason>(new GetFileSuccessResult(documentBytes)));

        var result = await sut.GetAgentAuthorityFormDocumentsAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

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
        AgentAuthority agentAuthority,
        AgentAuthorityForm agentAuthorityForm,
        AafDocument document1,
        AafDocument document2,
        byte[] documentBytes1,
        byte[] documentBytes2,
        GetAgentAuthorityFormDocumentsInternalRequest request)
    {
        agentAuthority.Status = AgentAuthorityStatus.Created;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>(1) { agentAuthorityForm };
        agentAuthorityForm.AafDocuments = new List<AafDocument>(2) { document1, document2 };
        request.AgentAuthorityFormId = agentAuthorityForm.Id;

        var sut = CreateSut();

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

        _mockRepository.Verify(x => x.GetAgentAuthorityAsync(request.AgentAuthorityId, It.IsAny<CancellationToken>()), Times.Once);

        _mockFileStorageService.Verify(x => x.GetFileAsync(document1.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.Verify(x => x.GetFileAsync(document2.Location, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.VerifyNoOtherCalls();

        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        Assert.Equal("AAF Document.zip", result.Value.FileName);
        Assert.Equal("application/zip", result.Value.ContentType);
    }

    private IAgentAuthorityInternalService CreateSut()
    {
        _mockUnitOfWork.Reset();
        _mockRepository.Reset();
        _mockRepository.SetupGet(x => x.UnitOfWork).Returns(_mockUnitOfWork.Object);

        _mockClock.Reset();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(_now);

        _mockFileStorageService.Reset();

        return new AgentAuthorityService(
            _mockRepository.Object,
            new Mock<IUserAccountRepository>().Object,
            _mockFileStorageService.Object,
            new FileTypesProvider(),
            _mockClock.Object,
            new NullLogger<AgentAuthorityService>());
    }
}
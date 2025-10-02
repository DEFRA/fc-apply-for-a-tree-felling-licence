using AutoFixture;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services.AgentAuthority;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FileStorage.Model;

namespace Forestry.Flo.External.Web.Tests.Services;

public class DownloadAgentAuthorityFormDocumentUseCaseTests
{
    private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService = new();
    private readonly Mock<IAuditService<DownloadAgentAuthorityFormDocumentUseCase>> _mockAudit = new();
    private readonly Fixture _fixture = new();
    private ExternalApplicant? _externalApplicant;

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WhenAgentAuthorityFormDocumentDownloadRequestProcessedCorrectly(
        Guid agentAuthorityId,
        Guid agentAuthorityFormId)
    {
        // arrange
        var sut = CreateSut();

        var response = new FileToStoreModel
        {
            ContentType = "application/pdf",
            FileBytes = new byte[1],
            FileName = "test"
        };

        _mockAgentAuthorityService.Setup(r =>
                r.GetAgentAuthorityFormDocumentsAsync(
                    It.IsAny<GetAgentAuthorityFormDocumentsRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        // act
        var result = await sut.DownloadAgentAuthorityFormDocumentAsync(
            _externalApplicant!,
            agentAuthorityId,
            agentAuthorityFormId,
            CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == _externalApplicant.AgencyId
                && a.EventName == AuditEvents.DownloadAgentAuthorityFormFiles),
            CancellationToken.None), Times.Once);

        _mockAgentAuthorityService.Verify(
            x => x.GetAgentAuthorityFormDocumentsAsync(It.Is<GetAgentAuthorityFormDocumentsRequest>(
                    v => v.AgentAuthorityId == agentAuthorityId &&
                         v.AccessedByUser == _externalApplicant!.UserAccountId),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenAgentAuthorityFormDocumentDownloadRequestNotProcessedCorrectly(
        Guid agentAuthorityId,
        Guid agentAuthorityFormId)
    {
        // arrange
        var sut = CreateSut();

        _mockAgentAuthorityService.Setup(r =>
                r.GetAgentAuthorityFormDocumentsAsync(
                    It.IsAny<GetAgentAuthorityFormDocumentsRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FileToStoreModel>("error"));

        // act
        var result = await sut.DownloadAgentAuthorityFormDocumentAsync(
            _externalApplicant!,
            agentAuthorityId,
            agentAuthorityFormId,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == _externalApplicant.AgencyId
                && a.EventName == AuditEvents.DownloadAgentAuthorityFormFilesFailure),
            CancellationToken.None), Times.Once);

        _mockAgentAuthorityService.Verify(
            x => x.GetAgentAuthorityFormDocumentsAsync(It.Is<GetAgentAuthorityFormDocumentsRequest>(
                    v => v.AgentAuthorityId == agentAuthorityId &&
                         v.AccessedByUser == _externalApplicant!.UserAccountId),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    private DownloadAgentAuthorityFormDocumentUseCase CreateSut()
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            agencyId: _fixture.Create<Guid>(),
            woodlandOwnerName: _fixture.Create<string>());

        _externalApplicant = new ExternalApplicant(user);

        _mockAgentAuthorityService.Reset();
        _mockAudit.Reset();

        return new DownloadAgentAuthorityFormDocumentUseCase(
            _mockAgentAuthorityService.Object,
            new NullLogger<DownloadAgentAuthorityFormDocumentUseCase>(),
            _mockAudit.Object,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal))
        );
    }
}
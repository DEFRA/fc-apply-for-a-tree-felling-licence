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

namespace Forestry.Flo.External.Web.Tests.Services;

public class RemoveAgentAuthorityFormDocumentUseCaseTests
{
    private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService = new();
    private readonly Mock<IAuditService<RemoveAgentAuthorityFormDocumentUseCase>> _mockAudit = new();
    private readonly Fixture _fixture = new();
    private ExternalApplicant? _externalApplicant;

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WhenAgentAuthorityFormDocumentRemoveRequestProcessedCorrectly(
        Guid agentAuthorityId,
        Guid agentAuthorityFormId)
    {
        // arrange
        var sut = CreateSut();
          
        _mockAgentAuthorityService.Setup(r =>
                r.RemoveAgentAuthorityFormAsync(
                    It.IsAny<RemoveAgentAuthorityFormRequest>()
                    , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // act
        var result = await sut.RemoveAgentAuthorityDocumentAsync(
            _externalApplicant!,
            agentAuthorityId,
            agentAuthorityFormId, 
            CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == _externalApplicant.AgencyId
                && a.EventName == AuditEvents.RemoveAgentAuthorityForm),
            CancellationToken.None), Times.Once);

        _mockAgentAuthorityService.Verify(
            x => x.RemoveAgentAuthorityFormAsync(It.Is<RemoveAgentAuthorityFormRequest>(
                    v => v.AgentAuthorityId == agentAuthorityId &&
                         v.AgentAuthorityFormId == agentAuthorityFormId &&
                         v.RemovedByUser == _externalApplicant!.UserAccountId),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenAgentAuthorityFormDocumentRemoveRequestNotProcessedCorrectly(
        Guid agentAuthorityId,
        Guid agentAuthorityFormId)
    {
        // arrange
        var sut = CreateSut();

        _mockAgentAuthorityService.Setup(r =>
                r.RemoveAgentAuthorityFormAsync(
                    It.IsAny<RemoveAgentAuthorityFormRequest>()
                    , It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("failed"));

        // act
        var result = await sut.RemoveAgentAuthorityDocumentAsync(
            _externalApplicant!,
            agentAuthorityId,
            agentAuthorityFormId,
            CancellationToken.None);

        // assert
        result.IsFailure.Should().BeTrue();

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == _externalApplicant.AgencyId
                && a.EventName == AuditEvents.RemoveAgentAuthorityFormFailure),
            CancellationToken.None), Times.Once);

        _mockAgentAuthorityService.Verify(
            x => x.RemoveAgentAuthorityFormAsync(It.Is<RemoveAgentAuthorityFormRequest>(
                    v => v.AgentAuthorityId == agentAuthorityId &&
                         v.AgentAuthorityFormId == agentAuthorityFormId &&
                         v.RemovedByUser == _externalApplicant!.UserAccountId),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    private RemoveAgentAuthorityFormDocumentUseCase CreateSut()
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

        return new RemoveAgentAuthorityFormDocumentUseCase(
            _mockAgentAuthorityService.Object,
            new NullLogger<RemoveAgentAuthorityFormDocumentUseCase>(),
            _mockAudit.Object,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal))
        );
    }
}
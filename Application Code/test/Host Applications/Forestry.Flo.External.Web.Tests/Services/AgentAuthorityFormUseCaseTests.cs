using System.Text.Json;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.AgentAuthorityForm;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Tests.Services;

public class AgentAuthorityFormUseCaseTests
{
    private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService = new();
    private readonly Mock<IAuditService<AgentAuthorityFormUseCase>> _mockAudit = new();
    private readonly Fixture _fixture = new();
    private ExternalApplicant? _externalApplicant;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private AgentAuthorityFormUseCase CreateSut()
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

        return new AgentAuthorityFormUseCase(
            _mockAgentAuthorityService.Object,
            new NullLogger<AgentAuthorityFormUseCase>(),
            _mockAudit.Object,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal))
        );
    }

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WhenAgentAuthorityRequestProcessedCorrectly(
        AgentAuthorityFormModel model)
    {
        //arrange
        var sut = CreateSut();

        var response = new AddAgentAuthorityResponse
        {
            AgencyId = new Guid(_externalApplicant!.AgencyId!),
            AgencyName = _fixture.Create<string>(),
        };

        //setup
        _mockAgentAuthorityService.Setup(r =>
                r.AddAgentAuthorityAsync(It.IsAny<AddAgentAuthorityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        //act

        var result = await sut.HandleNewAgentAuthorityRequestAsync(
            model,
            _externalApplicant!,
            CancellationToken.None);

        //assert

        result.IsSuccess.Should().BeTrue();

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == response.AgencyId.ToString()
                && a.EventName == AuditEvents.CreateAgentAuthorityForm
                && JsonSerializer.Serialize(a.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    AgencyName = response.AgencyName,
                    AgentAuthorityId = response.AgentAuthorityId
                }, _options)
                )
            , CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenAgentAuthorityCannotBeAdded(AgentAuthorityFormModel model)
    {
        //arrange
        var sut = CreateSut();

        const string error = "Cannot add authority";

        //setup

        _mockAgentAuthorityService.Setup(r =>
                r.AddAgentAuthorityAsync(It.IsAny<AddAgentAuthorityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AddAgentAuthorityResponse>(error));

        //act

        var result = await sut.HandleNewAgentAuthorityRequestAsync(
            model,
            _externalApplicant!,
            CancellationToken.None);

        //assert

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Failed to add new agent authority");

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId.ToString() == model.AgencyId.ToString()
                && a.EventName == AuditEvents.CreateAgentAuthorityFormFailure
                && JsonSerializer.Serialize(a.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                   OrganisationName = model.OrganisationName,
                   ContactName = model.ContactName,
                    Error = error
                }, _options)), CancellationToken.None), Times.Once);
    }
}
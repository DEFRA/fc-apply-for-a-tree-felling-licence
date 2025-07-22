using AutoFixture;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services.AgentAuthority;
using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.External.Web.Tests.Services;

public class GetAgentAuthorityFormDocumentUseCaseTests
{
    private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService = new();
    private readonly Fixture _fixture = new();
    private ExternalApplicant? _externalApplicant;

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WhenGetAgentAuthorityFormDocumentRequestProcessedCorrectly(
        Guid agentAuthorityId,
        AgentAuthorityFormsWithWoodlandOwnerResponseModel response)
    {
        // arrange
        var sut = CreateSut();

        _mockAgentAuthorityService.Setup(r =>
                r.GetAgentAuthorityFormDocumentsByAuthorityIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(), 
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        // act
        var result = await sut.GetAgentAuthorityFormDocumentsAsync(
            _externalApplicant!,
            agentAuthorityId,
            CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();
        Assert.Equal(response.AgentAuthorityFormResponseModels.Count, result.Value.HistoricAuthorityForms.Count + (result.Value.HasCurrentAuthorityForm ? 1 :0));
        Assert.Equal(result.Value.WoodlandOwnerOrOrganisationName, response.WoodlandOwnerModel.IsOrganisation ? response.WoodlandOwnerModel.OrganisationName : response.WoodlandOwnerModel.ContactName );
        Assert.Equal(agentAuthorityId, result.Value.AgentAuthorityId);

        _mockAgentAuthorityService.Verify(
            x => x.GetAgentAuthorityFormDocumentsByAuthorityIdAsync(
                It.Is<Guid>(u=>u == _externalApplicant!.UserAccountId), 
                It.Is<Guid>(a=>a == agentAuthorityId),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenGetAgentAuthorityFormDocumentRequestNotProcessedCorrectly(
        Guid agentAuthorityId)
    {
        // arrange
        var sut = CreateSut();

        _mockAgentAuthorityService.Setup(r =>
                r.GetAgentAuthorityFormDocumentsByAuthorityIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(), 
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthorityFormsWithWoodlandOwnerResponseModel>("error"));

        // act
        var result = await sut.GetAgentAuthorityFormDocumentsAsync(
            _externalApplicant!,
            agentAuthorityId,
            CancellationToken.None);

        // assert
        result.IsFailure.Should().BeTrue();

        _mockAgentAuthorityService.Verify(
            x => x.GetAgentAuthorityFormDocumentsByAuthorityIdAsync(
                It.Is<Guid>(u => u == _externalApplicant!.UserAccountId),
                It.Is<Guid>(a => a == agentAuthorityId),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    private GetAgentAuthorityFormDocumentsUseCase CreateSut()
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

        return new GetAgentAuthorityFormDocumentsUseCase(
            _mockAgentAuthorityService.Object,
            new NullLogger<GetAgentAuthorityFormDocumentsUseCase>());
    }
}
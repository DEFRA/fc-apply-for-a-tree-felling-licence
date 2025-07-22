using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.External.Web.Tests.Services;

public class SetWoodlandOwnerForAgentUseCaseTests
{
    private readonly AgentUserHomePageUseCase _sut;
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService;
    private readonly WoodlandOwner _woodlandOwner;
    private readonly Fixture _fixture;
    private readonly Mock<ILogger<AgentUserHomePageUseCase>> _logger;

    public SetWoodlandOwnerForAgentUseCaseTests()
    {
        _fixture = new Fixture();
        _agentAuthorityService = new Mock<IAgentAuthorityService>();
        _logger = new Mock<ILogger<AgentUserHomePageUseCase>>();

        _woodlandOwner = _fixture.Build<WoodlandOwner>()
            .With(wo => wo.IsOrganisation, true)
            .Create();

        _sut = new AgentUserHomePageUseCase( 
            _agentAuthorityService.Object, 
            _logger.Object);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnWoodlandOwners_GivenAgencyHasAuthorityClaims_WhenRequestingAuthorityList(
        GetAgentAuthoritiesResponse agentAuthorities,
        Guid agencyId)
    {
        var filter = new[] { AgentAuthorityStatus.FormUploaded, AgentAuthorityStatus.Created };

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
           _fixture.Create<string>(),
           _fixture.Create<string>(),
           _fixture.Create<Guid>(),
           _woodlandOwner.Id,
           AccountTypeExternal.Agent,
           Guid.NewGuid());
        var externalApplicant = new ExternalApplicant(user);
        //arrange
        
        _agentAuthorityService
            .Setup(r => r.GetAgentAuthoritiesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AgentAuthorityStatus[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(agentAuthorities));

        //act
        var result = await _sut.GetWoodlandOwnersForAgencyAsync(externalApplicant, agencyId, CancellationToken.None);

        //assert

        result.Value.Should().NotBeNull();
        result.Value.Count.Should().Be(agentAuthorities.AgentAuthorities.Count);

        _agentAuthorityService.Verify(x => x.GetAgentAuthoritiesAsync(externalApplicant.UserAccountId!.Value, agencyId, filter, It.IsAny<CancellationToken>()), Times.Once);

        Assert.True(result.Value.All(r => agentAuthorities.AgentAuthorities.Any(e =>
            r.Id == e.WoodlandOwner.Id
            && r.ContactEmail == e.WoodlandOwner.ContactEmail
            && r.ContactName == e.WoodlandOwner.ContactName
            && r.OrgName == e.WoodlandOwner.OrganisationName
        )));
    }
    
}
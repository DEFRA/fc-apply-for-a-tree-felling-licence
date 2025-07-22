using AutoFixture.Xunit2;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.Services.Applicants.Tests.Repositories;

public class AgencyRepositoryTests
{
    private ApplicantsContext _applicantsContext;
    private AgencyRepository _sut;
    
    public void CreateSut()
    {
        _applicantsContext = TestApplicantsDatabaseFactory.CreateDefaultTestContext();
        _sut = new AgencyRepository(_applicantsContext);
    }
    
    [Theory, AutoData]
    public async Task ShouldGetAgency_GivenAgencyId(Entities.Agent.Agency agency)
    {
        //arrange
        CreateSut();
        _applicantsContext.Agencies.Add(agency);
        agency.IsFcAgency = false;
        await _applicantsContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetAsync(agency.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ContactName.Should().Be(agency.ContactName);
    }
    
    [Theory, AutoData]
    public async Task ShouldThrowNotFoundException_GivenNotExistingAgencyId(Agency agency)
    {
        //arrange
        CreateSut();
        
        //act
        var result =  await _sut.GetAsync(agency.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoData]
    public async Task ShouldNotThrow_WhenIsFcAgencyFlagSetToFalse(Agency agency)
    {
        //arrange
        CreateSut();
        agency.IsFcAgency = false;
        _applicantsContext.Agencies.Add(agency);
        await _applicantsContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetAsync(agency.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ContactName.Should().Be(agency.ContactName);
    }

    [Theory, AutoData]
    public async Task ShouldThrow_WhenIsFcAgencyFlagSetToTrue(Agency agency)
    {
        //arrange
        CreateSut();
        agency.IsFcAgency = true;
        _applicantsContext.Agencies.Add(agency);

        //Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _applicantsContext.SaveChangesAsync());
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldSaveAgentAuthorityDetails(
        AgentAuthority agentAuthority)
    {
        agentAuthority.CreatedByUser.Agency = null;
        agentAuthority.CreatedByUser.WoodlandOwner = null;
        agentAuthority.ChangedByUser = null;
        agentAuthority.AgentAuthorityForms = new List<AgentAuthorityForm>();

        CreateSut();

        var result = await _sut.AddAgentAuthorityAsync(agentAuthority, CancellationToken.None);
        
        Assert.True(result.IsSuccess);

        var agencyInDb = _applicantsContext.AgentAuthorities.Single();
        Assert.Equal(result.Value, agencyInDb);

        var woodlandOwnerInDb = _applicantsContext.WoodlandOwners.Single();
        Assert.Equal(result.Value.WoodlandOwner, woodlandOwnerInDb);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldReturnNotFoundWhenDeleteUnknownAgentAuthorityId(
        Guid agentAuthorityId,
        AgentAuthority anotherAuthority)
    {
        CreateSut();
        _applicantsContext.AgentAuthorities.Add(anotherAuthority);
        await _applicantsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.DeleteAgentAuthorityAsync(agentAuthorityId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);

        Assert.Equal(anotherAuthority, _applicantsContext.AgentAuthorities.Single());
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldDeleteKnownAgentAuthorityId(
        AgentAuthority agentAuthority)
    {
        CreateSut();
        _applicantsContext.AgentAuthorities.Add(agentAuthority);
        await _applicantsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.DeleteAgentAuthorityAsync(agentAuthority.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(0, _applicantsContext.AgentAuthorities.Count());
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindAgentAuthorityStatusWhenNoneExistShouldReturnNone(
        Guid agencyId,
        Guid woodlandOwnerId)
    {
        CreateSut();
        
        var result = await _sut.FindAgentAuthorityStatusAsync(agencyId, woodlandOwnerId, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindAgentAuthorityStatusWhenNoneExistForAgencyIdShouldReturnNone(
        AgentAuthority entity,
        Guid agencyId)
    {
        CreateSut();
        await _sut.AddAgentAuthorityAsync(entity, CancellationToken.None);

        var result = await _sut.FindAgentAuthorityStatusAsync(agencyId, entity.WoodlandOwner.Id, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindAgentAuthorityStatusWhenNoneExistForWoodlandOwnerIdShouldReturnNone(
        AgentAuthority entity,
        Guid woodlandOwnerId)
    {
        CreateSut();
        await _sut.AddAgentAuthorityAsync(entity, CancellationToken.None);

        var result = await _sut.FindAgentAuthorityStatusAsync(entity.Agency.Id, woodlandOwnerId, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindAgentAuthorityStatusShouldReturnStatusOfMatchingEntity(AgentAuthority entity)
    {
        CreateSut();
        await _sut.AddAgentAuthorityAsync(entity, CancellationToken.None);

        var result = await _sut.FindAgentAuthorityStatusAsync(entity.Agency.Id, entity.WoodlandOwner.Id, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(entity.Status, result.Value);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindAgentAuthorityWhenNoneExistShouldReturnNone(
        Guid agencyId,
        Guid woodlandOwnerId)
    {
        CreateSut();

        var result = await _sut.FindAgentAuthorityAsync(agencyId, woodlandOwnerId, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindAgentAuthorityWhenNoneExistForAgencyIdShouldReturnNone(
        AgentAuthority entity,
        Guid agencyId)
    {
        CreateSut();
        await _sut.AddAgentAuthorityAsync(entity, CancellationToken.None);

        var result = await _sut.FindAgentAuthorityAsync(agencyId, entity.WoodlandOwner.Id, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindAgentAuthorityWhenNoneExistForWoodlandOwnerIdShouldReturnNone(
        AgentAuthority entity,
        Guid woodlandOwnerId)
    {
        CreateSut();
        await _sut.AddAgentAuthorityAsync(entity, CancellationToken.None);

        var result = await _sut.FindAgentAuthorityAsync(entity.Agency.Id, woodlandOwnerId, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindAgentAuthorityShouldReturnMatchingEntity(AgentAuthority entity)
    {
        CreateSut();
        await _sut.AddAgentAuthorityAsync(entity, CancellationToken.None);

        var result = await _sut.FindAgentAuthorityAsync(entity.Agency.Id, entity.WoodlandOwner.Id, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(entity, result.Value);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindApprovedAgencyShouldReturnNoneWhenNoEntitiesToFind(Guid woodlandOwnerId)
    {
        CreateSut();

        var result = await _sut.FindAgencyForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindApprovedAgencyShouldReturnNoneWhenWoodlandOwnerIdNotManaged(
        Guid woodlandOwnerId,
        AgentAuthority agentAuthority)
    {
        CreateSut();
        await _sut.AddAgentAuthorityAsync(agentAuthority, CancellationToken.None);

        var result = await _sut.FindAgencyForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindApprovedAgencyShouldReturnNoneWhenAuthorityIsDeactivated(
        AgentAuthority agentAuthority)
    {
        CreateSut();

        agentAuthority.Status = AgentAuthorityStatus.Deactivated;
        await _sut.AddAgentAuthorityAsync(agentAuthority, CancellationToken.None);

        var result = await _sut.FindAgencyForWoodlandOwnerAsync(agentAuthority.WoodlandOwner.Id, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task FindApprovedAgencyShouldReturnAgencyWhenAuthorityApproved(
        AgentAuthority agentAuthority)
    {
        CreateSut();

        agentAuthority.Status = AgentAuthorityStatus.FormUploaded;
        await _sut.AddAgentAuthorityAsync(agentAuthority, CancellationToken.None);

        var result = await _sut.FindAgencyForWoodlandOwnerAsync(agentAuthority.WoodlandOwner.Id, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(agentAuthority.Agency, result.Value);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task GetApprovedAuthorityByWoodlandOwnerShouldReturnNoneWhenNoEntitiesToFind(Guid woodlandOwnerId)
    {
        CreateSut();

        var result = await _sut.GetActiveAuthorityByWoodlandOwnerIdAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task GetActiveAuthorityByWoodlandOwnerShouldReturnNoneWhenWoodlandOwnerIdNotManaged(
        Guid woodlandOwnerId,
        AgentAuthority agentAuthority)
    {
        CreateSut();
        await _sut.AddAgentAuthorityAsync(agentAuthority, CancellationToken.None);

        var result = await _sut.GetActiveAuthorityByWoodlandOwnerIdAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task GetActiveAuthorityByWoodlandOwnerShouldReturnNoneWhenAuthorityNotActive(
        AgentAuthority agentAuthority)
    {
        CreateSut();

        agentAuthority.Status = AgentAuthorityStatus.Deactivated;
        await _sut.AddAgentAuthorityAsync(agentAuthority, CancellationToken.None);

        var result = await _sut.GetActiveAuthorityByWoodlandOwnerIdAsync(agentAuthority.WoodlandOwner.Id, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task GetActiveAuthorityByWoodlandOwnerShouldReturnAuthorityWhenAuthorityApproved(
        AgentAuthority agentAuthority)
    {
        CreateSut();

        agentAuthority.Status = AgentAuthorityStatus.FormUploaded;
        await _sut.AddAgentAuthorityAsync(agentAuthority, CancellationToken.None);

        var result = await _sut.GetActiveAuthorityByWoodlandOwnerIdAsync(agentAuthority.WoodlandOwner.Id, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(agentAuthority, result.Value);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task GetActiveAuthorityByWoodlandOwnerShouldReturnAuthorityWhenAuthorityCreated(
        AgentAuthority agentAuthority)
    {
        CreateSut();

        agentAuthority.Status = AgentAuthorityStatus.Created;
        await _sut.AddAgentAuthorityAsync(agentAuthority, CancellationToken.None);

        var result = await _sut.GetActiveAuthorityByWoodlandOwnerIdAsync(agentAuthority.WoodlandOwner.Id, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(agentAuthority, result.Value);
    }


    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldDeleteAgency_WithNoAuthorities(Agency agency)
    {
        CreateSut();

        _applicantsContext.Agencies.Add(agency);
        await _applicantsContext.SaveChangesAsync();

        var result = await _sut.DeleteAgencyAsync(agency.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedAgency = _applicantsContext.Agencies.FirstOrDefault(x => x.Id == agency.Id);

        Assert.Null(storedAgency);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldNotDeleteAgency_WithAuthorities(AgentAuthority agentAuthority, Agency agency)
    {
        CreateSut();

        agentAuthority.Agency = agency;

        _applicantsContext.Agencies.Add(agency);

        await _sut.AddAgentAuthorityAsync(agentAuthority, CancellationToken.None);

        await _applicantsContext.SaveChangesAsync();

        var result = await _sut.DeleteAgencyAsync(agency.Id, CancellationToken.None);

        Assert.True(result.IsFailure);

        var storedAgency = _applicantsContext.Agencies.FirstOrDefault(x => x.Id == agency.Id);

        Assert.NotNull(storedAgency);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task GetActiveAgentAuthorities_WithSomeDeactivated(
        List<AgentAuthority> agentAuthorities)
    {
        CreateSut();
        
        if (agentAuthorities.All(x => x.Status != AgentAuthorityStatus.Deactivated))
        {
            agentAuthorities.First().Status = AgentAuthorityStatus.Deactivated;
        }

        var expected = agentAuthorities
            .Where(x => x.Status != AgentAuthorityStatus.Deactivated)
            .ToList();

        _applicantsContext.AgentAuthorities.AddRange(agentAuthorities);
        await _applicantsContext.SaveChangesAsync();

        var result = await _sut.GetActiveAgentAuthoritiesAsync(CancellationToken.None);

        Assert.Equal(expected.Count, result.Count);
        foreach (var agentAuthority in expected)
        {
            Assert.Contains(result, a => a.Id == agentAuthority.Id);
        }
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task GetAgenciesForActiveAccounts_WithSomeDeactivatedAndSomeWithNoAccounts(
        List<Agency> expected,
        List<Agency> withDeactivatedAccounts,
        List<Agency> withNoAccounts)
    {
        CreateSut();

        _applicantsContext.Agencies.AddRange(expected);
        _applicantsContext.Agencies.AddRange(withDeactivatedAccounts);
        _applicantsContext.Agencies.AddRange(withNoAccounts);
        await _applicantsContext.SaveChangesAsync(CancellationToken.None);

        foreach (var agency in expected)
        {
            var user = new UserAccount
            {
                Agency = agency,
                AccountType = AccountTypeExternal.AgentAdministrator,
                Email = Guid.NewGuid().ToString(),
                Status = UserAccountStatus.Active
            };
            _applicantsContext.UserAccounts.Add(user);
            await _applicantsContext.SaveChangesAsync(CancellationToken.None);
        }
        foreach (var agency in withDeactivatedAccounts)
        {
            var user = new UserAccount
            {
                Agency = agency,
                AccountType = AccountTypeExternal.AgentAdministrator,
                Email = Guid.NewGuid().ToString(),
                Status = UserAccountStatus.Deactivated
            };
            _applicantsContext.UserAccounts.Add(user);
            await _applicantsContext.SaveChangesAsync(CancellationToken.None);
        }

        var result = await _sut.GetAgenciesForActiveAccountsAsync(CancellationToken.None);

        Assert.Equal(expected.Count, result.Count);
        foreach (var agency in expected)
        {
            Assert.Contains(result, a => a.Id == agency.Id);
        }
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task GetAgenciesWithIdNotIn(
        List<Agency> expected,
        List<Agency> notExpected)
    {
        CreateSut();

        _applicantsContext.Agencies.AddRange(expected);
        _applicantsContext.Agencies.AddRange(notExpected);
        await _applicantsContext.SaveChangesAsync(CancellationToken.None);

        var idsNotIn = notExpected.Select(a => a.Id).ToList();

        var result = await _sut.GetAgenciesWithIdNotIn(idsNotIn, CancellationToken.None);

        Assert.Equal(expected.Count, result.Count);
        foreach (var agency in expected)
        {
            Assert.Contains(result, a => a.Id == agency.Id);
        }
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task CanAddAgency(Agency newAgency)
    {
        CreateSut();
        
        var result = await _sut.AddAgencyAsync(newAgency, default);

        Assert.True(result.IsSuccess);
        Assert.IsType<Agency>(result.Value);
    }
}
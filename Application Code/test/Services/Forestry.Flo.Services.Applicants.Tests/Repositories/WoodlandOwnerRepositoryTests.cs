using AutoFixture.Xunit2;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.Services.Applicants.Tests.Repositories;

public class WoodlandOwnerRepositoryTests
{
    private readonly ApplicantsContext _applicantsContext;
    private readonly WoodlandOwnerRepository _sut;
    
    public WoodlandOwnerRepositoryTests()
    {
        _applicantsContext = TestApplicantsDatabaseFactory.CreateDefaultTestContext();
        _sut = new WoodlandOwnerRepository(_applicantsContext);
    }
    
    [Theory, AutoData]
    public async Task ShouldGetWoodlandOwner_GivenWoodlandOwnerId(WoodlandOwner woodlandOwner)
    {
        //arrange
        _applicantsContext.WoodlandOwners.Add(woodlandOwner);
        await _applicantsContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetAsync(woodlandOwner.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ContactName.Should().Be(woodlandOwner.ContactName);
    }
    
    [Theory, AutoData]
    public async Task ShouldThrowNotFoundException_GivenNotExistingWoodlandOwnerId(WoodlandOwner woodlandOwner)
    {
        //arrange
        
        //act
        var result =  await _sut.GetAsync(woodlandOwner.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoData]
    public async Task ShouldAddNewWoodlandOwner(WoodlandOwner woodlandOwner)
    {
        //arrange
        
        //act
        var result =  await _sut.AddWoodlandOwnerAsync(woodlandOwner, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ContactName.Should().Be(woodlandOwner.ContactName);
        result.Value.ContactEmail.Should().Be(woodlandOwner.ContactEmail);
        result.Value.ContactTelephone.Should().Be(woodlandOwner.ContactTelephone);
        result.Value.ContactAddress.Should().Be(woodlandOwner.ContactAddress);
        result.Value.IsOrganisation.Should().Be(woodlandOwner.IsOrganisation);
        result.Value.OrganisationName.Should().Be(woodlandOwner.OrganisationName);
        result.Value.OrganisationAddress.Should().Be(woodlandOwner.OrganisationAddress);
        result.Value.Id.Should().NotBeEmpty();
    }

    [Theory, AutoData]
    public async Task ShouldRetrieveAllWoodlandOwners(List<WoodlandOwner> expectedWoodlandOwners)
    {
        // arrange
        _applicantsContext.WoodlandOwners.AddRange(expectedWoodlandOwners);
        await _applicantsContext.SaveChangesAsync();

        // act
        var result =  await _sut.GetAllAsync(CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count().Should().Be(expectedWoodlandOwners.Count);

        foreach (var actualWoodlandOwner in result.Value)
        {
            var expectedWoodlandOwner = expectedWoodlandOwners.Single(x => x.Id == actualWoodlandOwner.Id);

            Assert.Equal(expectedWoodlandOwner.Id, actualWoodlandOwner.Id);
            Assert.Equal(expectedWoodlandOwner.ContactName, actualWoodlandOwner.ContactName);
            Assert.Equal(expectedWoodlandOwner.ContactEmail, actualWoodlandOwner.ContactEmail);
            Assert.Equal(expectedWoodlandOwner.ContactTelephone, actualWoodlandOwner.ContactTelephone);
            Assert.Equal(expectedWoodlandOwner.ContactAddress!.Line1, actualWoodlandOwner.ContactAddress!.Line1);
            Assert.Equal(expectedWoodlandOwner.ContactAddress.Line2, actualWoodlandOwner.ContactAddress.Line2);
            Assert.Equal(expectedWoodlandOwner.ContactAddress.Line3, actualWoodlandOwner.ContactAddress.Line3);
            Assert.Equal(expectedWoodlandOwner.ContactAddress.Line4, actualWoodlandOwner.ContactAddress.Line4);
            Assert.Equal(expectedWoodlandOwner.ContactAddress.PostalCode, actualWoodlandOwner.ContactAddress.PostalCode);
            Assert.Equal(expectedWoodlandOwner.IsOrganisation, actualWoodlandOwner.IsOrganisation);
            Assert.Equal(expectedWoodlandOwner.OrganisationName, actualWoodlandOwner.OrganisationName);
            Assert.Equal(expectedWoodlandOwner.OrganisationAddress!.Line1, actualWoodlandOwner.OrganisationAddress!.Line1);
            Assert.Equal(expectedWoodlandOwner.OrganisationAddress.Line2, actualWoodlandOwner.OrganisationAddress.Line2);
            Assert.Equal(expectedWoodlandOwner.OrganisationAddress.Line3, actualWoodlandOwner.OrganisationAddress.Line3);
            Assert.Equal(expectedWoodlandOwner.OrganisationAddress.Line4, actualWoodlandOwner.OrganisationAddress.Line4);
            Assert.Equal(expectedWoodlandOwner.OrganisationAddress.PostalCode, actualWoodlandOwner.OrganisationAddress.PostalCode);
        }
    }


    [Theory, AutoDataWithNonFcAgency]
    public async Task GetWoodlandOwnersForActiveAccounts_WithSomeDeactivatedAndSomeWithNoAccounts(
        List<WoodlandOwner> expected,
        List<WoodlandOwner> withDeactivatedAccounts,
        List<WoodlandOwner> withNoAccounts)
    {
        _applicantsContext.WoodlandOwners.AddRange(expected);
        _applicantsContext.WoodlandOwners.AddRange(withDeactivatedAccounts);
        _applicantsContext.WoodlandOwners.AddRange(withNoAccounts);
        await _applicantsContext.SaveChangesAsync(CancellationToken.None);

        foreach (var woodlandOwner in expected)
        {
            var user = new UserAccount
            {
                WoodlandOwner = woodlandOwner,
                AccountType = AccountTypeExternal.AgentAdministrator,
                Email = Guid.NewGuid().ToString(),
                Status = UserAccountStatus.Active
            };
            _applicantsContext.UserAccounts.Add(user);
            await _applicantsContext.SaveChangesAsync(CancellationToken.None);
        }
        foreach (var woodlandOwner in withDeactivatedAccounts)
        {
            var user = new UserAccount
            {
                WoodlandOwner = woodlandOwner,
                AccountType = AccountTypeExternal.AgentAdministrator,
                Email = Guid.NewGuid().ToString(),
                Status = UserAccountStatus.Deactivated
            };
            _applicantsContext.UserAccounts.Add(user);
            await _applicantsContext.SaveChangesAsync(CancellationToken.None);
        }

        var result = await _sut.GetWoodlandOwnersForActiveAccountsAsync(CancellationToken.None);

        Assert.Equal(expected.Count, result.Count);
        foreach (var woodlandOwner in expected)
        {
            Assert.Contains(result, a => a.Id == woodlandOwner.Id);
        }
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task GetWoodlandOwnersWithIdNotIn(
        List<WoodlandOwner> expected,
        List<WoodlandOwner> notExpected)
    {
        _applicantsContext.WoodlandOwners.AddRange(expected);
        _applicantsContext.WoodlandOwners.AddRange(notExpected);
        await _applicantsContext.SaveChangesAsync(CancellationToken.None);

        var idsNotIn = notExpected.Select(a => a.Id).ToList();

        var result = await _sut.GetWoodlandOwnersWithIdNotIn(idsNotIn, CancellationToken.None);

        Assert.Equal(expected.Count, result.Count);
        foreach (var woodlandOwner in expected)
        {
            Assert.Contains(result, a => a.Id == woodlandOwner.Id);
        }
    }
}
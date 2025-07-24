using AutoFixture;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.Services.Applicants.Tests.Repositories;

public class UserAccountRepositoryTests
{
    private readonly ApplicantsContext _applicantsContext;
    private readonly UserAccountRepository _sut;
    
    public UserAccountRepositoryTests()
    {
        _applicantsContext = TestApplicantsDatabaseFactory.CreateDefaultTestContext();
        _sut = new UserAccountRepository(_applicantsContext);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldGetUser_GivenUserId(UserAccount userAccount)
    {
        //arrange
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetAsync(userAccount.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(userAccount.Email);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldGetUserWithWoodlandOwner_GivenUserId(UserAccount userAccount)
    {
        //arrange
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetAsync(userAccount.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Agency.Should().NotBeNull();
        result.Value.Agency!.Id.Should().Be(userAccount.Agency!.Id);
        result.Value.WoodlandOwner.Should().NotBeNull();
        result.Value.WoodlandOwner!.Id.Should().Be(userAccount.WoodlandOwner!.Id);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldThrowNoFoundException_GivenNotExistingUserId(UserAccount userAccount)
    {
        //arrange
        //act
        var result =  await _sut.GetAsync(userAccount.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldGetUser_GivenUserEmail(UserAccount userAccount)
    {
        //arrange
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetByEmailAsync(userAccount.Email, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(userAccount.Email);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldGetUser_GivenUserEmail_IsCaseInsensitive(UserAccount userAccount)
    {
        //arrange
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();

        //act
        var result = await _sut.GetByEmailAsync(userAccount.Email.ToLower(), CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(userAccount.Email);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldGetUserWithWoodlandOwner_GivenUserEmail(UserAccount userAccount)
    {
        //arrange
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetByEmailAsync(userAccount.Email, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Agency.Should().NotBeNull();
        result.Value.Agency!.Id.Should().Be(userAccount.Agency!.Id);
        result.Value.WoodlandOwner.Should().NotBeNull();
        result.Value.WoodlandOwner!.Id.Should().Be(userAccount.WoodlandOwner!.Id);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldThrowNoFoundException_GivenNotExistingUseEmail(UserAccount userAccount)
    {
        //arrange
        //act
        var result =  await _sut.GetByEmailAsync(userAccount.Email, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldGetUser_GivenUserIdentityId(UserAccount userAccount)
    {
        //arrange
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetByUserIdentifierAsync(userAccount.IdentityProviderId!, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(userAccount.Email);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldGetUserWithWoodlandOwner_GivenIdentityId(UserAccount userAccount)
    {
        //arrange
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetByUserIdentifierAsync(userAccount.IdentityProviderId!, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Agency.Should().NotBeNull();
        result.Value.Agency!.Id.Should().Be(userAccount.Agency!.Id);
        result.Value.WoodlandOwner.Should().NotBeNull();
        result.Value.WoodlandOwner!.Id.Should().Be(userAccount.WoodlandOwner!.Id);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldThrowNoFoundException_GivenNotExistingIdentityId(UserAccount userAccount)
    {
        //arrange
        //act
        var result = await _sut.GetByUserIdentifierAsync(userAccount.IdentityProviderId!, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldUpdateUser_GivenExistingUser(UserAccount userAccount)
    {
        //arrange
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();
        userAccount.Title = "Test";
        
        //act
        _sut.Update(userAccount);
         await _sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
         
        //assert
        var result = await _applicantsContext.UserAccounts.FindAsync(userAccount.Id);
        result.Should().NotBeNull();
        result!.Title.Should().Be(userAccount.Title);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldAddUser_GivenValidUser(UserAccount userAccount)
    {
        //arrange
       
        //act
        _sut.Add(userAccount);
         await _sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
         
        //assert
        var result = await _applicantsContext.UserAccounts.FindAsync(userAccount.Id);
        result.Should().NotBeNull();
        result!.Email.Should().Be(userAccount.Email);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldGetUsersList_GivenWoodlandOwnerId(UserAccount userAccount)
    {
        //arrange
        userAccount.Status = UserAccountStatus.Active;
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();

        //act
        var result = await _sut.GetUsersWithWoodlandOwnerIdAsync(userAccount.WoodlandOwnerId!.Value, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.First().WoodlandOwner.Should().NotBeNull();
        result.Value.First().WoodlandOwner!.Id.Should().Be(userAccount.WoodlandOwner!.Id);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldNotGetInvitedUser_GivenWoodlandOwnerId(UserAccount userAccount)
    {
        //arrange
        userAccount.Status = UserAccountStatus.Invited;
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();

        //act
        var result = await _sut.GetUsersWithWoodlandOwnerIdAsync(userAccount.WoodlandOwnerId!.Value, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldNotGetDeactivatedUser_GivenWoodlandOwnerId(UserAccount userAccount)
    {
        //arrange
        userAccount.Status = UserAccountStatus.Deactivated;
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();

        //act
        var result = await _sut.GetUsersWithWoodlandOwnerIdAsync(userAccount.WoodlandOwnerId!.Value, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldNotGetUser_GivenMismatchedWoodlandOwnerId(
        UserAccount userAccount, 
        Guid woodlandOwnerId)
    {
        //arrange
        userAccount.Status = UserAccountStatus.Active;
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();

        //act
        var result = await _sut.GetUsersWithWoodlandOwnerIdAsync(woodlandOwnerId, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenNoUsersInRepo(Guid agencyId)
    {
        var result = await _sut.GetUsersWithAgencyIdAsync(agencyId, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldReturnFailureWhenNoUsersInRepoMatchOnAgencyId(
        Guid agencyId,
        List<UserAccount> users)
    {
        users.ForEach(x => x.Status = UserAccountStatus.Active);
        await _applicantsContext.UserAccounts.AddRangeAsync(users);
        await _applicantsContext.SaveEntitiesAsync();

        var result = await _sut.GetUsersWithAgencyIdAsync(agencyId, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldReturnExpectedUserForAgencyId(List<UserAccount> users)
    {
        users.ForEach(x => x.Status = UserAccountStatus.Active);
        await _applicantsContext.UserAccounts.AddRangeAsync(users);
        await _applicantsContext.SaveEntitiesAsync();

        var result = await _sut.GetUsersWithAgencyIdAsync(users[0].AgencyId!.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(users[0]);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldNotGetInvitedUser_GivenAgencyId(UserAccount userAccount)
    {
        //arrange
        userAccount.Status = UserAccountStatus.Invited;
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();

        //act
        var result = await _sut.GetUsersWithWoodlandOwnerIdAsync(userAccount.AgencyId!.Value, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldNotGetDeactivatedUser_GivenAgencyId(UserAccount userAccount)
    {
        //arrange
        userAccount.Status = UserAccountStatus.Deactivated;
        _applicantsContext.UserAccounts.Add(userAccount);
        await _applicantsContext.SaveChangesAsync();

        //act
        var result = await _sut.GetUsersWithWoodlandOwnerIdAsync(userAccount.AgencyId!.Value, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }
}

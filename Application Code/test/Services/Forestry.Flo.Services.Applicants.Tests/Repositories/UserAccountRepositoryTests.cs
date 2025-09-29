using AutoFixture.Xunit2;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
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
        Assert.True(result.IsSuccess);
        Assert.Equal(userAccount.Email, result.Value.Email);
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
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Agency);
        Assert.Equal(userAccount.Agency!.Id, result.Value.Agency!.Id);
        Assert.NotNull(result.Value.WoodlandOwner);
        Assert.Equal(userAccount.WoodlandOwner!.Id, result.Value.WoodlandOwner!.Id);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldThrowNoFoundException_GivenNotExistingUserId(UserAccount userAccount)
    {
        //arrange
        //act
        var result =  await _sut.GetAsync(userAccount.Id, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
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
        Assert.True(result.IsSuccess);
        Assert.Equal(userAccount.Email, result.Value.Email);
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
        Assert.True(result.IsSuccess);
        Assert.Equal(userAccount.Email, result.Value.Email);
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
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Agency);
        Assert.Equal(userAccount.Agency!.Id, result.Value.Agency!.Id);
        Assert.NotNull(result.Value.WoodlandOwner);
        Assert.Equal(userAccount.WoodlandOwner!.Id, result.Value.WoodlandOwner!.Id);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldThrowNoFoundException_GivenNotExistingUseEmail(UserAccount userAccount)
    {
        //arrange
        //act
        var result =  await _sut.GetByEmailAsync(userAccount.Email, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
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
        Assert.True(result.IsSuccess);
        Assert.Equal(userAccount.Email, result.Value.Email);
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
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Agency);
        Assert.Equal(userAccount.Agency!.Id, result.Value.Agency!.Id);
        Assert.NotNull(result.Value.WoodlandOwner);
        Assert.Equal(userAccount.WoodlandOwner!.Id, result.Value.WoodlandOwner!.Id);
    }
    
    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldThrowNoFoundException_GivenNotExistingIdentityId(UserAccount userAccount)
    {
        //arrange
        //act
        var result = await _sut.GetByUserIdentifierAsync(userAccount.IdentityProviderId!, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }


    [Theory, AutoMoqData]
    public async Task ShouldReturnUserAccount_WhenMatchedByEmail(List<UserAccount> internalUsers)
    {
        // Arrange
        var identityProviderId = Guid.NewGuid().ToString();
        var userToMatch = internalUsers[0];
        userToMatch.IdentityProviderId = null;
        userToMatch.Email = "testuser@domain.com";
        _applicantsContext.UserAccounts.AddRange(internalUsers);
        await _applicantsContext.SaveEntitiesAsync(CancellationToken.None);

        // Act
        var result = await _sut.GetByUserIdentifierAsync(identityProviderId, CancellationToken.None, userToMatch.Email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userToMatch.Email, result.Value.Email);
        Assert.Equal(identityProviderId, result.Value.IdentityProviderId);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenNoUserMatchesByEmail(List<UserAccount> internalUsers)
    {
        // Arrange
        var identityProviderId = Guid.NewGuid().ToString();
        var email = "notfound@domain.com";
        internalUsers.ForEach(u => u.IdentityProviderId = null);
        _applicantsContext.UserAccounts.AddRange(internalUsers);
        await _applicantsContext.SaveEntitiesAsync(CancellationToken.None);

        // Act
        var result = await _sut.GetByUserIdentifierAsync(identityProviderId, CancellationToken.None, email);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotMatchByEmail_IfIdentityProviderIdIsNotNull(List<UserAccount> internalUsers)
    {
        // Arrange
        var identityProviderId = Guid.NewGuid().ToString();
        var userToMatch = internalUsers[0];
        userToMatch.IdentityProviderId = "existing-id";
        userToMatch.Email = "testuser@domain.com";
        _applicantsContext.UserAccounts.AddRange(internalUsers);
        await _applicantsContext.SaveEntitiesAsync(CancellationToken.None);

        // Act
        var result = await _sut.GetByUserIdentifierAsync(identityProviderId, CancellationToken.None, userToMatch.Email);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
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
        Assert.NotNull(result);
        Assert.Equal(userAccount.Title, result!.Title);
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
        Assert.NotNull(result);
        Assert.Equal(userAccount.Email, result!.Email);
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
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.First().WoodlandOwner);
        Assert.Equal(userAccount.WoodlandOwner!.Id, result.Value.First().WoodlandOwner!.Id);
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
        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
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
        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
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
        Assert.False(result.IsSuccess);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenNoUsersInRepo(Guid agencyId)
    {
        var result = await _sut.GetUsersWithAgencyIdAsync(agencyId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
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

        Assert.False(result.IsSuccess);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldReturnExpectedUserForAgencyId(List<UserAccount> users)
    {
        users.ForEach(x => x.Status = UserAccountStatus.Active);
        await _applicantsContext.UserAccounts.AddRangeAsync(users);
        await _applicantsContext.SaveEntitiesAsync();

        var result = await _sut.GetUsersWithAgencyIdAsync(users[0].AgencyId!.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(users[0], result.Value.Single());
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
        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
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
        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }
}

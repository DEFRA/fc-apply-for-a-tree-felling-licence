using FluentAssertions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.Services.InternalUsers.Tests.Repositories;

public class InternalUserAccountRepositoryTests
{
    private readonly InternalUsersContext _internalUsersContext;
    private readonly UserAccountRepository _sut;
    
    public InternalUserAccountRepositoryTests()
    {

        _internalUsersContext = TestInternalUserDatabaseFactory.CreateDefaultTestContext();
        _sut = new UserAccountRepository(_internalUsersContext);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnUserAccount_WhenFullNameWithNoTitlePresent(List<UserAccount> internalUsers)
    {
        //arrange
        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var userToCheck = internalUsers[0];

        //act
        var result = await _sut.GetByFullnameAsync(userToCheck.FirstName!, userToCheck.LastName!, CancellationToken.None);

        //assert
        result.Count.Should().BeGreaterThan(0);
        result.First().Should().Be(internalUsers[0]);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnMaybeNone_WhenFullNameNotFound(UserAccount userToCheck)
    {
        //act
        var result = await _sut.GetByFullnameAsync(userToCheck.FirstName!, userToCheck.LastName!, CancellationToken.None);

        //assert
        result.Count.Should().Be(0);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnMaybeNone_WhenFullNameWithTitleNotFound(UserAccount userToCheck)
    {
        //act
        var result = await _sut.GetByFullnameAsync(userToCheck.Title, userToCheck.FirstName!, userToCheck.LastName!, CancellationToken.None);

        //assert
        result.Count.Should().Be(0);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnUserAccount_WhenFullNameWithTitlePresent(List<UserAccount> internalUsers)
    {
        //arrange
        var userToCheck = internalUsers[0];

        userToCheck.FirstName = "first";
        userToCheck.LastName = "last";
        userToCheck.Title = "title";

        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        
        //act
        var result = await _sut.GetByFullnameAsync(userToCheck.Title, userToCheck.FirstName!, userToCheck.LastName!, CancellationToken.None);

        //assert
        result.Count.Should().BeGreaterThan(0);
        result.First().Should().Be(internalUsers[0]);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnAllUserAccounts_WhenConfirmed(List<UserAccount> internalUsers)
    {
        foreach (var user in internalUsers)
        {
            user.Status = Status.Confirmed;
        }

        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetConfirmedUserAccountsAsync(CancellationToken.None);

        result.Count().Should().Be(internalUsers.Count);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnNoUserAccounts_WhenUnConfirmed(List<UserAccount> internalUsers)
    {
        foreach (var user in internalUsers)
        {
            user.Status = Status.Requested;
        }

        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetConfirmedUserAccountsAsync(CancellationToken.None);

        result.Count().Should().Be(0);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSomeUserAccounts_WithMixture(List<UserAccount> internalUsers)
    {
        foreach (var user in internalUsers)
        {
            user.Status = Status.Confirmed;
        }

        internalUsers[0].Status = Status.Requested;

        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetConfirmedUserAccountsAsync(CancellationToken.None);

        result.Count().Should().Be(2);
    }

    [Theory, AutoMoqData]
    public async Task ShouldAddUserAccount(UserAccount user)
    {
        var result = await _sut.AddAsync(user, CancellationToken.None);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        result.Should().Be(user);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnUserAccount_WhenPresent(List<UserAccount> internalUsers)
    {
        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetAsync(internalUsers[0].Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(internalUsers[0]);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenNotPresent(List<UserAccount> internalUsers)
    {
        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnUserAccountByIdentityProviderId_WhenPresent(List<UserAccount> internalUsers)
    {
        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetByIdentityProviderIdAsync(internalUsers[0].IdentityProviderId!, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(internalUsers[0]);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureByIdentityProviderId_WhenNotPresent(List<UserAccount> internalUsers)
    {
        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetByIdentityProviderIdAsync(string.Empty, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnAllUserAccounts_WhenNotConfirmed(List<UserAccount> internalUsers, UserAccount incompleteUser)
    {
        foreach (var user in internalUsers)
        {
            user.Status = Status.Requested;
            if (user.AccountType == AccountTypeInternal.FcStaffMember)
            {
                user.AccountType = AccountTypeInternal.AdminOfficer;
            }
        }

        incompleteUser.Status = Status.Requested;
        incompleteUser.AccountType = AccountTypeInternal.FcStaffMember;

        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        _internalUsersContext.UserAccounts.Add(incompleteUser);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetUnconfirmedUserAccountsAsync(CancellationToken.None);

        result.Count().Should().Be(internalUsers.Count);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnNoUserAccounts_WhenAllConfirmed(List<UserAccount> internalUsers)
    {
        foreach (var user in internalUsers)
        {
            user.Status = Status.Confirmed;
        }

        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetUnconfirmedUserAccountsAsync(CancellationToken.None);

        result.Count().Should().Be(0);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSomeUnconfirmedUserAccounts_WithMixture(List<UserAccount> internalUsers)
    {
        foreach (var user in internalUsers)
        {
            user.Status = Status.Requested;
        }

        internalUsers[0].Status = Status.Confirmed;

        _internalUsersContext.UserAccounts.AddRange(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetUnconfirmedUserAccountsAsync(CancellationToken.None);

        result.Count().Should().Be(2);
    }

    [Theory, AutoMoqData]
    public async Task CanRetrieveUserAccountsByAccountType(List<UserAccount> internalUsers)
    {
        await _internalUsersContext.AddRangeAsync(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var filterAccountType = internalUsers.First().AccountType;
        var matchingAccounts = internalUsers.Where(x => x.AccountType == filterAccountType);

        var result =
            await _sut.GetConfirmedUserAccountsByAccountTypeAsync(filterAccountType, null, CancellationToken.None);

        result.Should().BeEquivalentTo(matchingAccounts);
    }

    [Theory, AutoMoqData]
    public async Task CanRetrieveUserAccountsByAccountTypeAndAccountTypeOther(List<UserAccount> internalUsers)
    {
        await _internalUsersContext.AddRangeAsync(internalUsers);
        await _internalUsersContext.SaveEntitiesAsync(CancellationToken.None);

        var filterAccountType = internalUsers.First().AccountType;
        var filterAccountTypeOther = internalUsers.First().AccountTypeOther;
        var matchingAccounts = internalUsers.Where(x => x.AccountType == filterAccountType && x.AccountTypeOther == filterAccountTypeOther);

        var result =
            await _sut.GetConfirmedUserAccountsByAccountTypeAsync(filterAccountType, filterAccountTypeOther, CancellationToken.None);

        result.Should().BeEquivalentTo(matchingAccounts);
    }
}

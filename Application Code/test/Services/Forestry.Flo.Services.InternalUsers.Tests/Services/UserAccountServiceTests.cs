using System.Security.Claims;
using System.Text.Json;
using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Moq;
using NodaTime;

namespace Forestry.Flo.Services.InternalUsers.Tests.Services;

public class UserAccountServiceTests
{
    private readonly Mock<IUserAccountRepository> _userAccountRepositoryMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IAuditService<UserAccountService>> _auditMock;

    private static readonly Fixture FixtureInstance = new();
    private readonly ClaimsPrincipal _claimsPrincipal = new();

    public UserAccountServiceTests()
    {
        _userAccountRepositoryMock = new Mock<IUserAccountRepository>();
        _clockMock = new Mock<IClock>();
        _auditMock = new Mock<IAuditService<UserAccountService>>();
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoData]
    public async Task SuccessfullyCreateFcUserAccount(UserAccount userAccount)
    {
        var sut = CreateSut();

        _userAccountRepositoryMock.Setup(s => s.AddAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        _userAccountRepositoryMock.Setup(s => s.AddAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        _userAccountRepositoryMock.Setup(s => s.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<int>());

        await sut.CreateFcUserAccountAsync(userAccount.IdentityProviderId, userAccount.Email);

        _userAccountRepositoryMock.Verify(v => v.AddAsync(It.Is<UserAccount>(x =>
            x.Status == Status.Requested
            && x.Email == userAccount.Email
            && x.IdentityProviderId == userAccount.IdentityProviderId
            && x.AccountType == AccountTypeInternal.FcStaffMember),
                CancellationToken.None),
            Times.Once);

        _userAccountRepositoryMock.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnUserAccount_WhenSuccessfullyRetrieved(UserAccount user)
    {
        var sut = CreateSut();

        _userAccountRepositoryMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await sut.GetUserAccountAsync(user.Id, CancellationToken.None);

        Assert.True(result.HasValue);

        _userAccountRepositoryMock.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnEmpty_WhenNotRetrieved(UserAccount user)
    {
        var sut = CreateSut();

        _userAccountRepositoryMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.GetUserAccountAsync(user.Id, CancellationToken.None);

        Assert.True(result.HasNoValue);

        _userAccountRepositoryMock.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnUserAccount_WhenSuccessfullyRetrievedByIdentityProviderId(UserAccount user)
    {
        var sut = CreateSut();

        _userAccountRepositoryMock.Setup(s => s.GetByIdentityProviderIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(user);

        var result = await sut.GetUserAccountByIdentityProviderIdAsync(user.IdentityProviderId!, CancellationToken.None);

        Assert.True(result.HasValue);

        _userAccountRepositoryMock.Verify(v => v.GetByIdentityProviderIdAsync(user.IdentityProviderId!, CancellationToken.None, null), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnEmpty_WhenNotRetrievedByIdentityProviderId(UserAccount user)
    {
        var sut = CreateSut();

        _userAccountRepositoryMock.Setup(s => s.GetByIdentityProviderIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.GetUserAccountByIdentityProviderIdAsync(user.IdentityProviderId!, CancellationToken.None);

        Assert.True(result.HasNoValue);

        _userAccountRepositoryMock.Verify(v => v.GetByIdentityProviderIdAsync(user.IdentityProviderId!, CancellationToken.None, null), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnUserAccount_WhenUpdated(UserAccount user, UpdateRegistrationDetailsModel model)
    {
        var sut = CreateSut();

        model.UserAccountId = user.Id;

        _userAccountRepositoryMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userAccountRepositoryMock.Setup(s => s.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<int>);

        var result = await sut.UpdateUserAccountDetailsAsync(model, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(user, result.Value);

        _userAccountRepositoryMock.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _userAccountRepositoryMock.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenUserNotFound(UserAccount user, UpdateRegistrationDetailsModel model)
    {
        var sut = CreateSut();

        model.UserAccountId = user.Id;

        _userAccountRepositoryMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.General));


        var result = await sut.UpdateUserAccountDetailsAsync(model, CancellationToken.None);

        Assert.True(result.IsFailure);

        _userAccountRepositoryMock.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _userAccountRepositoryMock.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DatabaseUpdatedForConfirmationFlag_WhenUserRetrieved(bool confirmed)
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();

        _userAccountRepositoryMock.Setup(s => s.GetAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userAccountRepositoryMock.Setup(s => s.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<int>);

        if (confirmed)
        {
            await sut.UpdateUserAccountConfirmedAsync(user.Id, true, CancellationToken.None);
        }
        else
        {
            await sut.UpdateUserAccountDeniedAsync(user.Id, CancellationToken.None);
        }

        _userAccountRepositoryMock.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _userAccountRepositoryMock.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Once);

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.SourceEntityId == user.Id
                && x.UserId == user.Id
                && x.EventName == AuditEvents.UpdateAccountEvent
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    AccountType = user.AccountType,
                    IdentityProviderId = user.IdentityProviderId,

                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DatabaseNotUpdatedForConfirmationFlag_WhenUserNotRetrieved(bool confirmed)
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();

        _userAccountRepositoryMock.Setup(s => s.GetAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.General));

        if (confirmed)
        {
            await sut.UpdateUserAccountConfirmedAsync(user.Id, true, CancellationToken.None);
        }
        else
        {
            await sut.UpdateUserAccountDeniedAsync(user.Id, CancellationToken.None);
        }

        _userAccountRepositoryMock.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _userAccountRepositoryMock.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Never);

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.SourceEntityId == user.Id
                && x.UserId == user.Id
                && x.EventName == AuditEvents.UpdateAccountEvent
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    AccountType = user.AccountType,
                    IdentityProviderId = user.IdentityProviderId,

                }, _options)),
            CancellationToken.None), Times.Never);
    }

    [Theory, AutoData]
    public async Task ShouldReturnAccountList_WhenUsersRetrieved(List<UserAccount> users)
    {
        var sut = CreateSut();

        _userAccountRepositoryMock.Setup(s => s.GetUsersWithIdsInAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var userIds = users.Select(x => x.Id).ToList();

        var result = await sut.RetrieveUserAccountsByIdsAsync(userIds, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(users.Count, result.Value.Count);

        _userAccountRepositoryMock.Verify(v => v.GetUsersWithIdsInAsync(userIds, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenUsersNotRetrieved(List<UserAccount> users)
    {
        var sut = CreateSut();

        _userAccountRepositoryMock.Setup(s => s.GetUsersWithIdsInAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.General));

        var userIds = users.Select(x => x.Id).ToList();

        var result = await sut.RetrieveUserAccountsByIdsAsync(userIds, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _userAccountRepositoryMock.Verify(v => v.GetUsersWithIdsInAsync(userIds, CancellationToken.None), Times.Once);
    }

    [Theory]
    [InlineData(Status.Requested)]
    [InlineData(Status.Closed)]
    [InlineData(Status.Confirmed)]
    [InlineData(Status.Denied)]
    public async Task ShouldReturnUserAccountModel_WhenAccountStatusChanged(Status status)
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();

        _userAccountRepositoryMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userAccountRepositoryMock.Setup(s => s.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<int>());

        var result = await sut.SetUserAccountStatusAsync(user.Id, status, CancellationToken.None);

        Assert.True(result.IsSuccess);
        
        // assert model mapped correctly

        Assert.Equal(status, result.Value.Status);
        Assert.Equal(user.AccountType, result.Value.AccountType);
        Assert.Equal(user.FullName(), result.Value.FullName);
        Assert.Equal(user.Email, result.Value.Email);
        Assert.Equal(user.Id, result.Value.UserAccountId);

        _userAccountRepositoryMock.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _userAccountRepositoryMock.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ShouldFailure_WhenUserNotFound()
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();

        _userAccountRepositoryMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.SetUserAccountStatusAsync(user.Id, Status.Closed, CancellationToken.None);

        Assert.True(result.IsFailure);

        _userAccountRepositoryMock.Verify(v => v.GetAsync(user.Id, CancellationToken.None), Times.Once);
        _userAccountRepositoryMock.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnUsersForAccountType(AccountTypeInternal accountType, List<UserAccount> accounts)
    {
        var sut = CreateSut();

        _userAccountRepositoryMock
            .Setup(x => x.GetConfirmedUserAccountsByAccountTypeAsync(It.IsAny<AccountTypeInternal>(),
                It.IsAny<AccountTypeInternalOther?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var result = await sut.GetConfirmedUsersByAccountTypeAsync(accountType, null, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(accounts.Count(), result.Value.Count());
        Assert.True(accounts.All(x => result.Value.Any(a =>
            a.AccountType == x.AccountType
            && a.Status == x.Status
            && a.Email == x.Email
            && a.FirstName == x.FirstName
            && a.LastName == x.LastName
            && a.UserAccountId == x.Id)));

        _userAccountRepositoryMock
            .Verify(x => x.GetConfirmedUserAccountsByAccountTypeAsync(accountType, null, It.IsAny<CancellationToken>()), Times.Once);
        _userAccountRepositoryMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnUsersForAccountTypeOther(
        AccountTypeInternal accountType, 
        AccountTypeInternalOther accountTypeOther, 
        List<UserAccount> accounts)
    {
        var sut = CreateSut();

        _userAccountRepositoryMock
            .Setup(x => x.GetConfirmedUserAccountsByAccountTypeAsync(It.IsAny<AccountTypeInternal>(),
                It.IsAny<AccountTypeInternalOther?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var result = await sut.GetConfirmedUsersByAccountTypeAsync(accountType, accountTypeOther, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(accounts.Count(), result.Value.Count());
        Assert.True(accounts.All(x => result.Value.Any(a =>
            a.AccountType == x.AccountType
            && a.Status == x.Status
            && a.Email == x.Email
            && a.FirstName == x.FirstName
            && a.LastName == x.LastName
            && a.UserAccountId == x.Id)));

        _userAccountRepositoryMock
            .Verify(x => x.GetConfirmedUserAccountsByAccountTypeAsync(accountType, accountTypeOther, It.IsAny<CancellationToken>()), Times.Once);
        _userAccountRepositoryMock.VerifyNoOtherCalls();
    }


    private UserAccountService CreateSut()
    {
        _userAccountRepositoryMock.Reset();
        _clockMock.Reset();
        _auditMock.Reset();

        return new UserAccountService(
            _auditMock.Object,
            _userAccountRepositoryMock.Object,
            new RequestContext("test", new RequestUserModel(_claimsPrincipal)));
    }
}
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class RetrieveUserAccountsServiceRetrieveUserAccessTests
{
    private Mock<IUserAccountRepository> _mockUserAccountRepository = new();
    private Mock<IAgencyRepository> _mockAgencyRepository = new();

    [Theory, AutoData]
    public async Task WhenCannotRetrieveUserAccountFromRepository(Guid userId)
    {
        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.RetrieveUserAccessAsync(userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);

        _mockAgencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenUserIsWoodlandOwner(Guid userId)
    {
        var userAccount = new UserAccount
        {
            AccountType = AccountTypeExternal.WoodlandOwnerAdministrator,
            WoodlandOwnerId = Guid.NewGuid()
        };

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        var result = await sut.RetrieveUserAccessAsync(userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(userId, result.Value.UserAccountId);
        Assert.Equal(userAccount.WoodlandOwnerId, result.Value.WoodlandOwnerIds.Single());
        Assert.False(result.Value.IsFcUser);
        Assert.Null(result.Value.AgencyId);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);

        _mockAgencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenUserIsFcUser(Guid userId)
    {
        var userAccount = new UserAccount
        {
            AccountType = AccountTypeExternal.FcUser
        };

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        var result = await sut.RetrieveUserAccessAsync(userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(userId, result.Value.UserAccountId);
        Assert.True(result.Value.IsFcUser);
        Assert.Null(result.Value.AgencyId);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);

        _mockAgencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenCannotRetrieveAgentAuthorities(Guid userId)
    {
        var userAccount = new UserAccount
        {
            AccountType = AccountTypeExternal.AgentAdministrator,
            AgencyId = Guid.NewGuid()
        };

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _mockAgencyRepository
            .Setup(x => x.ListAuthoritiesByAgencyAsync(It.IsAny<Guid>(), It.IsAny<AgentAuthorityStatus[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IEnumerable<AgentAuthority>, UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.RetrieveUserAccessAsync(userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepository.Verify(x => x.ListAuthoritiesByAgencyAsync(
            userAccount.AgencyId!.Value,
            It.Is<AgentAuthorityStatus[]?>(f => f != null && f.Contains(AgentAuthorityStatus.Created)
                                                          && f.Contains(AgentAuthorityStatus.FormUploaded)
                                                          && f.Contains(AgentAuthorityStatus.Deactivated) == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task WhenUserIsAgentUser(Guid userId, List<AgentAuthority> agentAuthorities)
    {
        var userAccount = new UserAccount
        {
            AccountType = AccountTypeExternal.AgentAdministrator,
            AgencyId = Guid.NewGuid()
        };

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _mockAgencyRepository
            .Setup(x => x.ListAuthoritiesByAgencyAsync(It.IsAny<Guid>(), It.IsAny<AgentAuthorityStatus[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<AgentAuthority>, UserDbErrorReason>(agentAuthorities));

        var result = await sut.RetrieveUserAccessAsync(userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(userId, result.Value.UserAccountId);
        Assert.Equal(agentAuthorities.Count, result.Value.WoodlandOwnerIds.Count);
        agentAuthorities.ForEach(a => Assert.Contains(result.Value.WoodlandOwnerIds, r => r == a.WoodlandOwner.Id));
        Assert.False(result.Value.IsFcUser);
        Assert.Equal(userAccount.AgencyId, result.Value.AgencyId);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepository.Verify(x => x.ListAuthoritiesByAgencyAsync(
            userAccount.AgencyId!.Value,
            It.Is<AgentAuthorityStatus[]?>(f => f != null && f.Contains(AgentAuthorityStatus.Created) 
                                                          && f.Contains(AgentAuthorityStatus.FormUploaded) 
                                                          && f.Contains(AgentAuthorityStatus.Deactivated) == false), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private RetrieveUserAccountsService CreateSut()
    {
        _mockUserAccountRepository.Reset();
        _mockAgencyRepository.Reset();

        return new RetrieveUserAccountsService(
            _mockUserAccountRepository.Object,
            _mockAgencyRepository.Object,
            new NullLogger<RetrieveUserAccountsService>());
    }
}
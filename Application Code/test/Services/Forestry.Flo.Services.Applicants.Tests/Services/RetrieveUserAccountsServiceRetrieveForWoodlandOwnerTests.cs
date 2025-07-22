using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class RetrieveUserAccountsServiceRetrieveForWoodlandOwnerTests
{
    private Mock<IUserAccountRepository> _mockUserAccountRepository = new();
    private Mock<IAgencyRepository> _mockAgencyRepository = new();

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldReturnFailureWhenNoAgencyAndUserRetrievalFails(
        Guid woodlandOwnerId)
    {
        var sut = CreateSut();

        _mockAgencyRepository
            .Setup(x => x.FindAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.None);
        _mockUserAccountRepository
            .Setup(x => x.GetUsersWithWoodlandOwnerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockAgencyRepository.Verify(x => x.FindAgencyForWoodlandOwnerAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepository.VerifyNoOtherCalls();

        _mockUserAccountRepository.Verify(x => x.GetUsersWithWoodlandOwnerIdAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldReturnNoUsersWhenNoAgencyOrWoodlandOwnerAccountsMatchWoodlandOwnerId(
        Guid woodlandOwnerId)
    {
        var sut = CreateSut();

        _mockAgencyRepository
            .Setup(x => x.FindAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.None);
        _mockUserAccountRepository
            .Setup(x => x.GetUsersWithWoodlandOwnerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        _mockAgencyRepository.Verify(x => x.FindAgencyForWoodlandOwnerAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepository.VerifyNoOtherCalls();

        _mockUserAccountRepository.Verify(x => x.GetUsersWithWoodlandOwnerIdAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldReturnFailureWhenNoUsersForAgencyMatchOnWoodlandOwnerId(
        Guid woodlandOwnerId,
        Agency agency)
    {
        var sut = CreateSut();

        _mockAgencyRepository
            .Setup(x => x.FindAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.From(agency));
        _mockUserAccountRepository
            .Setup(x => x.GetUsersWithAgencyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockAgencyRepository.Verify(x => x.FindAgencyForWoodlandOwnerAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepository.VerifyNoOtherCalls();

        _mockUserAccountRepository.Verify(x => x.GetUsersWithAgencyIdAsync(agency.Id, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldReturnWoodlandOwnerUsersWhenNoAgencyMatchesWoodlandOwnerId(
        Guid woodlandOwnerId,
        List<UserAccount> woodlandOwnerUsers)
    {
        woodlandOwnerUsers.ForEach(x=>x.Status = UserAccountStatus.Active);

        var sut = CreateSut();

        _mockAgencyRepository
            .Setup(x => x.FindAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.None);
        _mockUserAccountRepository
            .Setup(x => x.GetUsersWithWoodlandOwnerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<UserAccount>, UserDbErrorReason>(woodlandOwnerUsers));

        var result = await sut.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(result.Value.Count, woodlandOwnerUsers.Count);

        foreach (var userAccount in woodlandOwnerUsers)
        {
            Assert.Contains(result.Value, x =>
                x.Email == userAccount.Email
                && x.UserAccountId == userAccount.Id
                && x.FirstName == userAccount.FirstName
                && x.LastName == userAccount.LastName
                && x.Status is UserAccountStatus.Active);
        }

        _mockAgencyRepository.Verify(x => x.FindAgencyForWoodlandOwnerAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepository.VerifyNoOtherCalls();

        _mockUserAccountRepository.Verify(x => x.GetUsersWithWoodlandOwnerIdAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task ShouldReturnAgentUsersWhenAgencyMatchesWoodlandOwnerId(
        Guid woodlandOwnerId,
        Agency agency,
        List<UserAccount> agentUsers)
    {
        var sut = CreateSut();
        agentUsers.ForEach(x => x.Status = UserAccountStatus.Active);

        _mockAgencyRepository
            .Setup(x => x.FindAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.From(agency));
        _mockUserAccountRepository
            .Setup(x => x.GetUsersWithAgencyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<UserAccount>, UserDbErrorReason>(agentUsers));

        var result = await sut.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(result.Value.Count, agentUsers.Count);

        foreach (var userAccount in agentUsers)
        {
            Assert.Contains(result.Value, x =>
                x.Email == userAccount.Email
                && x.UserAccountId == userAccount.Id
                && x.FirstName == userAccount.FirstName
                && x.LastName == userAccount.LastName
                && x.Status is UserAccountStatus.Active);
        }

        _mockAgencyRepository.Verify(x => x.FindAgencyForWoodlandOwnerAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepository.VerifyNoOtherCalls();

        _mockUserAccountRepository.Verify(x => x.GetUsersWithAgencyIdAsync(agency.Id, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();
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
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class RetrieveUserAccountsServiceIsUserAccountLinkedToFcAgencyTests
{
    private Mock<IUserAccountRepository> _mockUserAccountRepository = new();
    private Mock<IAgencyRepository> _mockAgencyRepository = new();

    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenAccountCannotBeFound(Guid userAccountId)
    {
        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.IsUserAccountLinkedToFcAgencyAsync(userAccountId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userAccountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockAgencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFalseWhenAccountNotLinkedToAnAgency(
        Guid userAccountId,
        UserAccount userAccount)
    {
        userAccount.AgencyId = null;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));

        var result = await sut.IsUserAccountLinkedToFcAgencyAsync(userAccountId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userAccountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockAgencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFalseWhenNoFcAgencyCanBeFound(
        Guid userAccountId,
        UserAccount userAccount)
    {
        userAccount.AgencyId = Guid.NewGuid();

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _mockAgencyRepository
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.None);

        var result = await sut.IsUserAccountLinkedToFcAgencyAsync(userAccountId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userAccountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockAgencyRepository.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFalseWhenUserAgencyIdDoesNotMatchFcAgencyId(
        Guid userAccountId,
        UserAccount userAccount,
        Agency fcAgency)
    {
        userAccount.AgencyId = Guid.NewGuid();

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _mockAgencyRepository
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.From(fcAgency));

        var result = await sut.IsUserAccountLinkedToFcAgencyAsync(userAccountId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userAccountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockAgencyRepository.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldReturnTrueWhenUserAgencyIdDoesMatchFcAgencyId(
        Guid userAccountId,
        UserAccount userAccount,
        Agency fcAgency)
    {
        userAccount.AgencyId = fcAgency.Id;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _mockAgencyRepository
            .Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Agency>.From(fcAgency));

        var result = await sut.IsUserAccountLinkedToFcAgencyAsync(userAccountId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userAccountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockAgencyRepository.Verify(x => x.FindFcAgency(It.IsAny<CancellationToken>()), Times.Once);
        _mockAgencyRepository.VerifyNoOtherCalls();
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
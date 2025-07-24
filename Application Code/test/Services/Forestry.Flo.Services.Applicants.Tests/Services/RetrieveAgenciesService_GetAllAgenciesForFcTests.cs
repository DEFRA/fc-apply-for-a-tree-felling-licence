using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class RetrieveAgenciesService_GetAllAgenciesForFcTests
{
    private readonly Mock<IUserAccountRepository> _userAccountRepository = new();
    private readonly Mock<IAgencyRepository> _agencyRepository = new();

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenPerformingUserIsNotFound(
        Guid performingUserId)
    {
        var sut = CreateSut();

        _userAccountRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.GetAllAgenciesForFcAsync(performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _userAccountRepository
            .Verify(x => x.GetAsync(performingUserId, It.IsAny<CancellationToken>()),
                Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _agencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenUserIsNotFc(
        Guid performingUserId,
        UserAccount user)
    {
        var sut = CreateSut();

        user.AccountType = AccountTypeExternal.Agent;

        _userAccountRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        var result = await sut.GetAllAgenciesForFcAsync(performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _userAccountRepository
            .Verify(x => x.GetAsync(performingUserId, It.IsAny<CancellationToken>()),
                Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _agencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task RetrievalOfExternalApplicantAgencies(
        Guid performingUserId,
        UserAccount user,
        List<Agency> agencies)
    {
        foreach (var agency in agencies)
        {
            TestUtils.SetProtectedProperty(agency, nameof(agency.Id), Guid.NewGuid());
        }

        var expectedIds = agencies.Select(x => x.Id).ToList();

        var sut = CreateSut();

        user.AccountType = AccountTypeExternal.FcUser;

        _userAccountRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _agencyRepository.Setup(x => x.GetAgenciesForActiveAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(agencies);

        _agencyRepository.Setup(x => x.GetAgenciesWithIdNotIn(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Agency>(0));

        var result = await sut.GetAllAgenciesForFcAsync(performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(agencies.Count, result.Value.Count);
        foreach (var agency in agencies)
        {
            Assert.Contains(result.Value, a =>
                a.Id == agency.Id
                && a.ContactName == agency.ContactName
                && a.ContactEmail == agency.ContactEmail
                && a.OrganisationName == agency.OrganisationName
                && a.HasActiveUserAccounts);
        }

        _userAccountRepository
            .Verify(x => x.GetAsync(performingUserId, It.IsAny<CancellationToken>()),
                Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _agencyRepository.Verify(x => x.GetAgenciesForActiveAccountsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _agencyRepository.Verify(x => x.GetAgenciesWithIdNotIn(expectedIds, It.IsAny<CancellationToken>()), Times.Once);
        _agencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task RetrievalOfFcManagedAgencies(
        Guid performingUserId,
        UserAccount user,
        List<Agency> agencies)
    {
        foreach (var agency in agencies)
        {
            TestUtils.SetProtectedProperty(agency, nameof(agency.Id), Guid.NewGuid());
        }
        
        var sut = CreateSut();

        user.AccountType = AccountTypeExternal.FcUser;

        _userAccountRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _agencyRepository.Setup(x => x.GetAgenciesForActiveAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Agency>(0));

        _agencyRepository.Setup(x => x.GetAgenciesWithIdNotIn(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agencies);

        var result = await sut.GetAllAgenciesForFcAsync(performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(agencies.Count, result.Value.Count);
        foreach (var agency in agencies)
        {
            Assert.Contains(result.Value, a =>
                a.Id == agency.Id
                && a.ContactName == agency.ContactName
                && a.ContactEmail == agency.ContactEmail
                && a.OrganisationName == agency.OrganisationName
                && a.HasActiveUserAccounts == false);
        }

        _userAccountRepository
            .Verify(x => x.GetAsync(performingUserId, It.IsAny<CancellationToken>()),
                Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _agencyRepository.Verify(x => x.GetAgenciesForActiveAccountsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _agencyRepository.Verify(x => x.GetAgenciesWithIdNotIn(new List<Guid>(0), It.IsAny<CancellationToken>()), Times.Once);
        _agencyRepository.VerifyNoOtherCalls();
    }

    private RetrieveAgenciesService CreateSut()
    {
        _userAccountRepository.Reset();
        _agencyRepository.Reset();

        return new RetrieveAgenciesService(
            _userAccountRepository.Object,
            _agencyRepository.Object,
            new NullLogger<RetrieveAgenciesService>());
    }
}
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class RetrieveWoodlandOwnersService_RetrieveAllWoodlandOwnersForFcTests
{
    private Mock<IWoodlandOwnerRepository> _woodlandOwnerRepository = new();
    private Mock<IUserAccountRepository> _userAccountRepository = new();
    private Mock<IAgencyRepository> _agencyRepository = new();

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenPerformingUserIsNotFound(
        Guid performingUserId)
    {
        var sut = CreateSut();

        _userAccountRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.GetAllWoodlandOwnersForFcAsync(performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _userAccountRepository
            .Verify(x => x.GetAsync(performingUserId, It.IsAny<CancellationToken>()),
                Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _woodlandOwnerRepository.VerifyNoOtherCalls();
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

        var result = await sut.GetAllWoodlandOwnersForFcAsync(performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _userAccountRepository
            .Verify(x => x.GetAsync(performingUserId, It.IsAny<CancellationToken>()),
                Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _woodlandOwnerRepository.VerifyNoOtherCalls();
        _agencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task RetrievalOfExternalApplicantWoodlandOwners(
        Guid performingUserId,
        UserAccount user,
        List<WoodlandOwner> woodlandOwners)
    {
        foreach (var woodlandOwner in woodlandOwners)
        {
            TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), Guid.NewGuid());
        }

        var expectedIds = woodlandOwners.Select(x => x.Id).ToList();

        var sut = CreateSut();

        user.AccountType = AccountTypeExternal.FcUser;

        _userAccountRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _woodlandOwnerRepository
            .Setup(x => x.GetWoodlandOwnersForActiveAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwners);
        _woodlandOwnerRepository
            .Setup(x => x.GetWoodlandOwnersWithIdNotIn(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WoodlandOwner>(0));

        _agencyRepository
            .Setup(x => x.GetActiveAgentAuthoritiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentAuthority>(0));

        _agencyRepository
            .Setup(x => x.GetAgenciesForActiveAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Agency>(0));

        var result = await sut.GetAllWoodlandOwnersForFcAsync(performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(woodlandOwners.Count, result.Value.Count);
        foreach (var woodlandOwner in woodlandOwners)
        {
            Assert.Contains(result.Value, x =>
                x.Id == woodlandOwner.Id
                && x.IsOrganisation == woodlandOwner.IsOrganisation
                && x.ContactEmail == woodlandOwner.ContactEmail
                && x.ContactName == woodlandOwner.ContactName
                && x.OrganisationName == woodlandOwner.OrganisationName
                && x.HasActiveUserAccounts == true
                && x.AgencyId.HasNoValue()
                && x.AgencyContactName == null
                && x.AgencyName == null);
        }

        _userAccountRepository
            .Verify(x => x.GetAsync(performingUserId, It.IsAny<CancellationToken>()),
                Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _woodlandOwnerRepository.Verify(x => x.GetWoodlandOwnersForActiveAccountsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _woodlandOwnerRepository.Verify(x => x.GetWoodlandOwnersWithIdNotIn(expectedIds, It.IsAny<CancellationToken>()), Times.Once);
        _woodlandOwnerRepository.VerifyNoOtherCalls();

        _agencyRepository.Verify(x => x.GetActiveAgentAuthoritiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _agencyRepository.Verify(x => x.GetAgenciesForActiveAccountsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _agencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task RetrievalOfAgencyLinkedWoodlandOwners(
        Guid performingUserId,
        UserAccount user,
        List<AgentAuthority> agentAuthorities)
    {
        foreach (var agentAuthority in agentAuthorities)
        {
            var woodlandOwner = agentAuthority.WoodlandOwner;
            var agency = agentAuthority.Agency;
            TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), Guid.NewGuid());
            TestUtils.SetProtectedProperty(agency, nameof(agency.Id), Guid.NewGuid());
        }

        var expectedIds = agentAuthorities.Select(x => x.WoodlandOwner.Id).ToList();
        var withUsers = agentAuthorities.Select(x => x.Agency).Take(2).ToList();

        var sut = CreateSut();

        user.AccountType = AccountTypeExternal.FcUser;

        _userAccountRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _woodlandOwnerRepository
            .Setup(x => x.GetWoodlandOwnersForActiveAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WoodlandOwner>(0));
        _woodlandOwnerRepository
            .Setup(x => x.GetWoodlandOwnersWithIdNotIn(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WoodlandOwner>(0));

        _agencyRepository
            .Setup(x => x.GetActiveAgentAuthoritiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(agentAuthorities);

        _agencyRepository
            .Setup(x => x.GetAgenciesForActiveAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(withUsers);

        var result = await sut.GetAllWoodlandOwnersForFcAsync(performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(agentAuthorities.Count, result.Value.Count);
        foreach (var agentAuthority in agentAuthorities)
        {
            Assert.Contains(result.Value, x =>
                x.Id == agentAuthority.WoodlandOwner.Id
                && x.IsOrganisation == agentAuthority.WoodlandOwner.IsOrganisation
                && x.ContactEmail == agentAuthority.WoodlandOwner.ContactEmail
                && x.ContactName == agentAuthority.WoodlandOwner.ContactName
                && x.OrganisationName == agentAuthority.WoodlandOwner.OrganisationName
                && x.HasActiveUserAccounts == withUsers.Contains(agentAuthority.Agency)
                && x.AgencyId == agentAuthority.Agency.Id
                && x.AgencyContactName == agentAuthority.Agency.ContactName
                && x.AgencyName == agentAuthority.Agency.OrganisationName);
        }

        _userAccountRepository
            .Verify(x => x.GetAsync(performingUserId, It.IsAny<CancellationToken>()),
                Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _woodlandOwnerRepository.Verify(x => x.GetWoodlandOwnersForActiveAccountsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _woodlandOwnerRepository.Verify(x => x.GetWoodlandOwnersWithIdNotIn(expectedIds, It.IsAny<CancellationToken>()), Times.Once);
        _woodlandOwnerRepository.VerifyNoOtherCalls();

        _agencyRepository.Verify(x => x.GetActiveAgentAuthoritiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _agencyRepository.Verify(x => x.GetAgenciesForActiveAccountsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _agencyRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task RetrievalOfFcManagedWoodlandOwners(
        Guid performingUserId,
        UserAccount user,
        List<WoodlandOwner> woodlandOwners)
    {
        foreach (var woodlandOwner in woodlandOwners)
        {
            TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), Guid.NewGuid());
        }

        var sut = CreateSut();

        user.AccountType = AccountTypeExternal.FcUser;

        _userAccountRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _woodlandOwnerRepository
            .Setup(x => x.GetWoodlandOwnersForActiveAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WoodlandOwner>(0));
        _woodlandOwnerRepository
            .Setup(x => x.GetWoodlandOwnersWithIdNotIn(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwners);

        _agencyRepository
            .Setup(x => x.GetActiveAgentAuthoritiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentAuthority>(0));

        _agencyRepository
            .Setup(x => x.GetAgenciesForActiveAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Agency>(0));

        var result = await sut.GetAllWoodlandOwnersForFcAsync(performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(woodlandOwners.Count, result.Value.Count);
        foreach (var woodlandOwner in woodlandOwners)
        {
            Assert.Contains(result.Value, x =>
                x.Id == woodlandOwner.Id
                && x.IsOrganisation == woodlandOwner.IsOrganisation
                && x.ContactEmail == woodlandOwner.ContactEmail
                && x.ContactName == woodlandOwner.ContactName
                && x.OrganisationName == woodlandOwner.OrganisationName
                && x.HasActiveUserAccounts == false
                && x.AgencyId == null
                && x.AgencyContactName == null
                && x.AgencyName == null);
        }

        _userAccountRepository
            .Verify(x => x.GetAsync(performingUserId, It.IsAny<CancellationToken>()),
                Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _woodlandOwnerRepository.Verify(x => x.GetWoodlandOwnersForActiveAccountsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _woodlandOwnerRepository.Verify(x => x.GetWoodlandOwnersWithIdNotIn(new List<Guid>(0), It.IsAny<CancellationToken>()), Times.Once);
        _woodlandOwnerRepository.VerifyNoOtherCalls();

        _agencyRepository.Verify(x => x.GetActiveAgentAuthoritiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _agencyRepository.Verify(x => x.GetAgenciesForActiveAccountsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _agencyRepository.VerifyNoOtherCalls();
    }

    private RetrieveWoodlandOwnersService CreateSut()
    {
        _woodlandOwnerRepository.Reset();
        _userAccountRepository.Reset();
        _agencyRepository.Reset();

        return new RetrieveWoodlandOwnersService(
            _woodlandOwnerRepository.Object,
            _userAccountRepository.Object,
            _agencyRepository.Object,
            new NullLogger<RetrieveWoodlandOwnersService>());
    }
}
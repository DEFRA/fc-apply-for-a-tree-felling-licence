using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgentAuthorityServiceGetAgentAuthoritiesTests
{
    private Mock<IAgencyRepository> _mockRepository = new();
    private Mock<IUserAccountRepository> _mockUserAccountRepository = new();

    [Theory, AutoData]
    public async Task WhenCannotLocateUserAccount(
        Guid userId,
        Guid agencyId,
        AgentAuthorityStatus[]? filter)
    {
        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.GetAgentAuthoritiesAsync(userId, agencyId, filter, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenUserAccountNotLinkedToAnAgency(
        UserAccount user,
        Guid userId,
        Guid agencyId,
        AgentAuthorityStatus[]? filter)
    {
        user.Agency = null;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        var result = await sut.GetAgentAuthoritiesAsync(userId, agencyId, filter, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenRetrieveFromDatabaseFails(
        UserAccount user,
        Guid userId,
        AgentAuthorityStatus[]? filter)
    {
        var sut = CreateSut();

        var agency = user.Agency;
        TestUtils.SetProtectedProperty(agency, nameof(agency.Id), Guid.NewGuid());

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));
        _mockRepository
            .Setup(x => x.ListAuthoritiesByAgencyAsync(It.IsAny<Guid>(), It.IsAny<AgentAuthorityStatus[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IEnumerable<AgentAuthority>, UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.GetAgentAuthoritiesAsync(userId, user.Agency.Id, filter, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.ListAuthoritiesByAgencyAsync(user.Agency.Id, filter, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenRetrieveFromDatabaseSucceeds(
        UserAccount user,
        Guid userId,
        AgentAuthorityStatus[]? filter,
        List<AgentAuthority> agentAuthorities)
    {
        var sut = CreateSut();

        var agency = user.Agency;
        TestUtils.SetProtectedProperty(agency, nameof(agency.Id), Guid.NewGuid());

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));
        _mockRepository
            .Setup(x => x.ListAuthoritiesByAgencyAsync(It.IsAny<Guid>(), It.IsAny<AgentAuthorityStatus[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<AgentAuthority>, UserDbErrorReason>(agentAuthorities));

        var result = await sut.GetAgentAuthoritiesAsync(userId, user.Agency.Id, filter, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUserAccountRepository.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.ListAuthoritiesByAgencyAsync(user.Agency.Id, filter, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();

        Assert.Equal(agentAuthorities.Count, result.Value.AgentAuthorities.Count);
        Assert.True(agentAuthorities.All(x => result.Value.AgentAuthorities.Any(aa =>
            x.Id == aa.Id
            && x.CreatedByUser.FullName() == aa.CreatedByName
            && x.CreatedTimestamp == aa.CreatedTimestamp
            && x.Status == aa.Status
            && x.WoodlandOwner.Id == aa.WoodlandOwner.Id
            && x.WoodlandOwner.ContactName == aa.WoodlandOwner.ContactName
            && x.WoodlandOwner.ContactEmail == aa.WoodlandOwner.ContactEmail
            && x.WoodlandOwner.ContactTelephone == aa.WoodlandOwner.ContactTelephone
            && x.WoodlandOwner.ContactAddress == aa.WoodlandOwner.ContactAddress
            && x.WoodlandOwner.IsOrganisation == aa.WoodlandOwner.IsOrganisation
            && x.WoodlandOwner.OrganisationName == aa.WoodlandOwner.OrganisationName
            && x.WoodlandOwner.OrganisationAddress == aa.WoodlandOwner.OrganisationAddress
            )));
    }

    private IAgentAuthorityService CreateSut()
    {
        _mockRepository.Reset();
        _mockUserAccountRepository.Reset();

        return new AgentAuthorityService(
            _mockRepository.Object,
            _mockUserAccountRepository.Object,
            new Mock<IFileStorageService>().Object,
            new FileTypesProvider(),
            new Mock<IClock>().Object,
            new NullLogger<AgentAuthorityService>());
    }
}
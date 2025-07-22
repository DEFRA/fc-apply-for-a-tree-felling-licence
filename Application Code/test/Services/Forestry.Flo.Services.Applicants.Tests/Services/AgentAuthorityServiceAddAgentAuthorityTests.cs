using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgentAuthorityServiceAddAgentAuthorityTests
{
    private Mock<IAgencyRepository> _mockRepository = new();
    private Mock<IUserAccountRepository> _mockUserAccountRepository = new();
    private Mock<IClock> _mockClock = new();
    private Instant _now = new Instant();

    [Theory, AutoData]
    public async Task WhenCannotLocateUserAccount(
        AddAgentAuthorityRequest request)
    {
        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.AddAgentAuthorityAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.CreatedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockClock.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenUserAccountNotFound(
        UserAccount user,
        AddAgentAuthorityRequest request)
    {
        user.Agency = null;

        var sut = CreateSut();

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.AddAgentAuthorityAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.CreatedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockClock.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgencyIsNotFound(
    UserAccount user,
    Agency agency,
    AddAgentAuthorityRequest request)
    {
        var sut = CreateSut();

        request.AgencyId = agency.Id;

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Agency, UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.AddAgentAuthorityAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.CreatedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.GetAsync(agency.Id, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenSaveToDatabaseFails(
        UserAccount user,
        Agency agency,
        AddAgentAuthorityRequest request)
    {
        var sut = CreateSut();

        request.AgencyId = agency.Id;

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Agency, UserDbErrorReason>(agency));

        _mockRepository
            .Setup(x => x.AddAgentAuthorityAsync(It.IsAny<AgentAuthority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgentAuthority, UserDbErrorReason>(UserDbErrorReason.NotUnique));

        var result = await sut.AddAgentAuthorityAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.CreatedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.AddAgentAuthorityAsync(It.Is<AgentAuthority>(a =>
            a.Agency == agency
            && a.CreatedByUser == user
            && a.CreatedTimestamp == _now.ToDateTimeUtc()
            && a.Status == AgentAuthorityStatus.Created
            && a.WoodlandOwner.ContactAddress == request.WoodlandOwner.ContactAddress
            && a.WoodlandOwner.ContactName == request.WoodlandOwner.ContactName
            && a.WoodlandOwner.ContactEmail == request.WoodlandOwner.ContactEmail
            && a.WoodlandOwner.ContactTelephone == request.WoodlandOwner.ContactTelephone
            && a.WoodlandOwner.IsOrganisation == request.WoodlandOwner.IsOrganisation
            && a.WoodlandOwner.OrganisationAddress == request.WoodlandOwner.OrganisationAddress
            && a.WoodlandOwner.OrganisationName == request.WoodlandOwner.OrganisationName),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockRepository.Verify(x => x.GetAsync(agency.Id, It.IsAny<CancellationToken>()), Times.Once);

        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenSuccessful(
        UserAccount user,
        AddAgentAuthorityRequest request,
        Agency agency,
        AgentAuthority savedEntity)
    {
        var sut = CreateSut();

        request.AgencyId = agency.Id;

        _mockUserAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(user));

        _mockRepository
            .Setup(x => x.AddAgentAuthorityAsync(It.IsAny<AgentAuthority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AgentAuthority, UserDbErrorReason>(savedEntity));

        _mockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Agency, UserDbErrorReason>(agency));

        var result = await sut.AddAgentAuthorityAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUserAccountRepository.Verify(x => x.GetAsync(request.CreatedByUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserAccountRepository.VerifyNoOtherCalls();

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.AddAgentAuthorityAsync(It.Is<AgentAuthority>(a =>
            a.Agency == agency
            && a.CreatedByUser == user
            && a.CreatedTimestamp == _now.ToDateTimeUtc()
            && a.Status == AgentAuthorityStatus.Created
            && a.WoodlandOwner.ContactAddress == request.WoodlandOwner.ContactAddress
            && a.WoodlandOwner.ContactName == request.WoodlandOwner.ContactName
            && a.WoodlandOwner.ContactEmail == request.WoodlandOwner.ContactEmail
            && a.WoodlandOwner.ContactTelephone == request.WoodlandOwner.ContactTelephone
            && a.WoodlandOwner.IsOrganisation == request.WoodlandOwner.IsOrganisation
            && a.WoodlandOwner.OrganisationAddress == request.WoodlandOwner.OrganisationAddress
            && a.WoodlandOwner.OrganisationName == request.WoodlandOwner.OrganisationName),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockRepository.Verify(x=>x.GetAsync(agency.Id,It.IsAny<CancellationToken>()), Times.Once);

        _mockRepository.VerifyNoOtherCalls();


        Assert.Equal(savedEntity.Id, result.Value.AgentAuthorityId);
        Assert.Equal(agency.OrganisationName, result.Value.AgencyName);
        Assert.Equal(agency.Id, result.Value.AgencyId);
    }

    private IAgentAuthorityService CreateSut()
    {
        _mockRepository.Reset();
        _mockUserAccountRepository.Reset();
        _mockClock.Reset();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(_now);

        return new AgentAuthorityService(
            _mockRepository.Object,
            _mockUserAccountRepository.Object,
            new Mock<IFileStorageService>().Object,
            new FileTypesProvider(),
            _mockClock.Object,
            new NullLogger<AgentAuthorityService>());
    }
}
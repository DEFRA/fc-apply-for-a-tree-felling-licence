using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgentAuthorityServiceGetAgencyForWoodlandOwnerTests
{
    private readonly Mock<IAgencyRepository> _mockRepository = new();

    [Theory, AutoData]
    public async Task WhenNoneFoundInRepository(Guid woodlandOwnerId)
    {
        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetActiveAuthorityByWoodlandOwnerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgentAuthority>.None);

        var result = await sut.GetAgencyForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.HasNoValue);

        _mockRepository.Verify(x => x.GetActiveAuthorityByWoodlandOwnerIdAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgencyAuthorityFoundInRepository(
        Guid woodlandOwnerId,
        AgentAuthority authority)
    {
        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetActiveAuthorityByWoodlandOwnerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgentAuthority>.From(authority));

        var result = await sut.GetAgencyForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(authority.Agency.ContactName, result.Value.ContactName);
        Assert.Equal(authority.Agency.ContactEmail, result.Value.ContactEmail);
        Assert.Equal(authority.Agency.OrganisationName, result.Value.OrganisationName);
        Assert.Equal(authority.Agency.Address, result.Value.Address);

        _mockRepository.Verify(x => x.GetActiveAuthorityByWoodlandOwnerIdAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }
    
    private IAgentAuthorityService CreateSut()
    {
        _mockRepository.Reset();

        return new AgentAuthorityService(
            _mockRepository.Object,
            new Mock<IUserAccountRepository>().Object,
            new Mock<IFileStorageService>().Object,
            new FileTypesProvider(),
            new Mock<IClock>().Object,
            new NullLogger<AgentAuthorityService>());
    }
}
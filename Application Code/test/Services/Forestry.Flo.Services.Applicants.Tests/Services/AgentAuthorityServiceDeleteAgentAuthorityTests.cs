using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgentAuthorityServiceDeleteAgentAuthorityTests
{
    private Mock<IAgencyRepository> _mockRepository = new();
    private Mock<IUserAccountRepository> _mockUserAccountRepository = new();
    private Mock<IClock> _mockClock = new();
    private Instant _now = new Instant();

    [Theory, AutoData]
    public async Task WhenAgentAuthorityNotFoundInRepository(
        Guid agentAuthorityId)
    {
        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.DeleteAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotFound));

        var result = await sut.DeleteAgentAuthorityAsync(agentAuthorityId, CancellationToken.None);

        Assert.True(result.IsFailure);
        
        _mockRepository.Verify(x => x.DeleteAgentAuthorityAsync(agentAuthorityId, It.IsAny<CancellationToken>()));
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenAgentAuthorityDeletedSuccessfully(
        Guid agentAuthorityId)
    {
        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.DeleteAgentAuthorityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.DeleteAgentAuthorityAsync(agentAuthorityId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockRepository.Verify(x => x.DeleteAgentAuthorityAsync(agentAuthorityId, It.IsAny<CancellationToken>()));
        _mockRepository.VerifyNoOtherCalls();
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
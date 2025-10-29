using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services
{
    public class AgentAuthorityServiceGetAgentAuthorityForWoodlandOwnerTests
    {
        private readonly Mock<IAgencyRepository> _agencyRepositoryMock;
        private readonly Mock<IUserAccountRepository> _userAccountRepositoryMock;
        private readonly Mock<IFileStorageService> _fileStorageServiceMock;
        private readonly Mock<FileTypesProvider> _fileTypesProviderMock;
        private readonly Mock<IClock> _clockMock;
        private readonly Mock<ILogger<AgentAuthorityService>> _loggerMock;
        private readonly AgentAuthorityService _service;

        public AgentAuthorityServiceGetAgentAuthorityForWoodlandOwnerTests()
        {
            _agencyRepositoryMock = new Mock<IAgencyRepository>();
            _userAccountRepositoryMock = new Mock<IUserAccountRepository>();
            _fileStorageServiceMock = new Mock<IFileStorageService>();
            _fileTypesProviderMock = new Mock<FileTypesProvider>();
            _clockMock = new Mock<IClock>();
            _loggerMock = new Mock<ILogger<AgentAuthorityService>>();

            _service = new AgentAuthorityService(
                _agencyRepositoryMock.Object,
                _userAccountRepositoryMock.Object,
                _fileStorageServiceMock.Object,
                _fileTypesProviderMock.Object,
                _clockMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Returns_MaybeNone_When_No_AgentAuthority_Found()
        {
            // Arrange
            var woodlandOwnerId = Guid.NewGuid();
            _agencyRepositoryMock.Setup(r => r.GetActiveAuthorityByWoodlandOwnerIdAsync(woodlandOwnerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<AgentAuthority>.None);

            // Act
            var result = await _service.GetAgentAuthorityForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

            // Assert
            Assert.True(result.HasNoValue);
        }

        [Fact]
        public async Task Returns_AgentAuthorityModel_When_AgentAuthority_Found()
        {
            // Arrange
            var woodlandOwnerId = Guid.NewGuid();
            var agentAuthority = (AgentAuthority)Activator.CreateInstance(typeof(AgentAuthority), true)!;
            typeof(AgentAuthority).GetProperty("Id")!.SetValue(agentAuthority, Guid.NewGuid());
            _agencyRepositoryMock.Setup(r => r.GetActiveAuthorityByWoodlandOwnerIdAsync(woodlandOwnerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<AgentAuthority>.From(agentAuthority));

            // Act
            var result = await _service.GetAgentAuthorityForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(agentAuthority.Id, result.Value.Id);
        }

        [Fact]
        public async Task Returns_MaybeNone_On_Exception()
        {
            // Arrange
            var woodlandOwnerId = Guid.NewGuid();
            _agencyRepositoryMock.Setup(r => r.GetActiveAuthorityByWoodlandOwnerIdAsync(woodlandOwnerId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _service.GetAgentAuthorityForWoodlandOwnerAsync(woodlandOwnerId, CancellationToken.None);

            // Assert
            Assert.True(result.HasNoValue);
        }
    }
}

using CSharpFunctionalExtensions;
using Forestry.Flo.Services.AdminHubs.Entities;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.AdminHubs.Repositories;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Forestry.Flo.Services.AdminHubs.Tests.Services
{
    public class AdminHubServiceTests
    {
        private AdminHubService _sut;
        private readonly Mock<IAdminHubRepository> _adminHubRepository = new();

        [Theory, AutoMoqData]
        public async Task RetrieveAdminHubDataAsync_WhenAdminHubsExist(
            IReadOnlyCollection<AdminHub> adminHubs)
        {
            _sut = CreateSut();

            //Arrange
            _adminHubRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            var request = new GetAdminHubsDataRequestModel(
                Guid.NewGuid(),
                AccountTypeInternal.AdminHubManager);

            //Act
            var result = await _sut.RetrieveAdminHubDataAsync(request, CancellationToken.None);

            //Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(adminHubs.Count, result.Value.Count);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);

            foreach (var actualHub in result.Value)
            {
                AssertAdminHub(adminHubs.Single(x => x.Id == actualHub.Id), actualHub);
            }
        }

        [Fact]
        public async Task RetrieveAdminHubDataAsync_WhenNoAdminHubsFound()
        {
            _sut = CreateSut();

            IReadOnlyCollection<AdminHub> noHubs = new List<AdminHub>(0).AsReadOnly();

            //arrange
            _adminHubRepository
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(noHubs));

            var request = new GetAdminHubsDataRequestModel(
                Guid.NewGuid(),
                AccountTypeInternal.AdminHubManager);

            //Act
            var result = await _sut.RetrieveAdminHubDataAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task RetrieveAdminHubDataAsync_WhenCurrentUserNotAdminHubManager()
        {
            _sut = CreateSut();

            var request = new GetAdminHubsDataRequestModel(
                Guid.NewGuid(),
                AccountTypeInternal.AdminOfficer);

            //Act
            var result = await _sut.RetrieveAdminHubDataAsync(request, CancellationToken.None);

            Assert.True(result.IsFailure);
            _adminHubRepository.VerifyNoOtherCalls();
            Assert.Equal(ManageAdminHubOutcome.Unauthorized, result.Error);
        }


        [Theory, AutoMoqData]
        public async Task AddAdminOfficerAsync_WhenSuccessful(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminOfficerId,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            adminHubs.First().AdminManagerId = adminManagerId;
            
            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            _adminHubRepository.Setup(x => x.AddAdminOfficerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var request = new AddAdminOfficerToAdminHubRequestModel(
                adminHubs.First().Id, adminOfficerId, AccountTypeInternal.AdminHubManager, adminManagerId);

            //Act
            var result = await _sut.AddAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsSuccess);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.Verify(x => x.AddAdminOfficerAsync(request.AdminHubId, request.UserId, It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AddAdminOfficerAsync_WhenUserIsNotAdminHubManager()
        {
            _sut = CreateSut();

            //arrange
            var request = new AddAdminOfficerToAdminHubRequestModel(
                Guid.NewGuid(), Guid.NewGuid(), AccountTypeInternal.AdminOfficer, Guid.NewGuid());

            //Act
            var result = await _sut.AddAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ManageAdminHubOutcome.Unauthorized, result.Error);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task AddAdminOfficerAsync_WhenAdminHubNotFound(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid unknownAdminHubId,
            Guid adminOfficerId,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            var request = new AddAdminOfficerToAdminHubRequestModel(
                unknownAdminHubId, adminOfficerId, AccountTypeInternal.AdminHubManager, adminManagerId);

            //Act
            var result = await _sut.AddAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ManageAdminHubOutcome.AdminHubNotFound, result.Error);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task AddAdminOfficerAsync_WhenUserNotManagerOfHub(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminOfficerId,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            var request = new AddAdminOfficerToAdminHubRequestModel(
                adminHubs.First().Id, adminOfficerId, AccountTypeInternal.AdminHubManager, adminManagerId);

            //Act
            var result = await _sut.AddAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ManageAdminHubOutcome.Unauthorized, result.Error);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task AddAdminOfficerAsync_WhenAdminOfficerAlreadyAtAdminHub(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminOfficerId,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            adminHubs.First().AdminManagerId = adminManagerId;
            adminHubs.Last().AdminOfficers.Add(new AdminHubOfficer(adminHubs.Last(), adminOfficerId));

            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            var request = new AddAdminOfficerToAdminHubRequestModel(
                adminHubs.First().Id, adminOfficerId, AccountTypeInternal.AdminHubManager, adminManagerId);

            //Act
            var result = await _sut.AddAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ManageAdminHubOutcome.InvalidAssignment, result.Error);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task AddAdminOfficerAsync_WhenAddToRepositoryFails(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminOfficerId,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            adminHubs.First().AdminManagerId = adminManagerId;

            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            _adminHubRepository.Setup(x => x.AddAdminOfficerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotUnique));

            var request = new AddAdminOfficerToAdminHubRequestModel(
                adminHubs.First().Id, adminOfficerId, AccountTypeInternal.AdminHubManager, adminManagerId);

            //Act
            var result = await _sut.AddAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ManageAdminHubOutcome.UpdateFailure, result.Error);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.Verify(x => x.AddAdminOfficerAsync(request.AdminHubId, request.UserId, It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task RemoveAdminOfficerAsync_WhenSuccessful(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminOfficerId,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            adminHubs.First().AdminManagerId = adminManagerId;
            adminHubs.First().AdminOfficers.Add(new AdminHubOfficer(adminHubs.First(), adminOfficerId));

            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            _adminHubRepository.Setup(x => x.RemoveAdminOfficerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var request = new RemoveAdminOfficerFromAdminHubRequestModel(
                adminHubs.First().Id, adminOfficerId, AccountTypeInternal.AdminHubManager, adminManagerId);

            //Act
            var result = await _sut.RemoveAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsSuccess);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.Verify(x => x.RemoveAdminOfficerAsync(request.AdminHubId, request.UserId, It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task RemoveAdminOfficerAsync_WhenUserIsNotAdminHubManager()
        {
            _sut = CreateSut();

            //arrange

            var request = new RemoveAdminOfficerFromAdminHubRequestModel(
                Guid.NewGuid(), Guid.NewGuid(), AccountTypeInternal.AdminOfficer, Guid.NewGuid());

            //Act
            var result = await _sut.RemoveAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsFailure);
            _adminHubRepository.VerifyNoOtherCalls();
            Assert.Equal(ManageAdminHubOutcome.Unauthorized, result.Error);
        }

        [Theory, AutoMoqData]
        public async Task RemoveAdminOfficerAsync_WhenAdminHubNotFound(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminOfficerId,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            var request = new RemoveAdminOfficerFromAdminHubRequestModel(
                Guid.NewGuid(), adminOfficerId, AccountTypeInternal.AdminHubManager, adminManagerId);

            //Act
            var result = await _sut.RemoveAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsFailure);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
            Assert.Equal(ManageAdminHubOutcome.AdminHubNotFound, result.Error);
        }

        [Theory, AutoMoqData]
        public async Task RemoveAdminOfficerAsync_WhenUserNotManagerOfHub(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminOfficerId,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            adminHubs.First().AdminOfficers.Add(new AdminHubOfficer(adminHubs.First(), adminOfficerId));

            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            var request = new RemoveAdminOfficerFromAdminHubRequestModel(
                adminHubs.First().Id, adminOfficerId, AccountTypeInternal.AdminHubManager, adminManagerId);

            //Act
            var result = await _sut.RemoveAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ManageAdminHubOutcome.Unauthorized, result.Error);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task RemoveAdminOfficerAsync_WhenGivenOfficerIsNotAtGivenHub(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminOfficerId,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            adminHubs.First().AdminManagerId = adminManagerId;
            adminHubs.Last().AdminOfficers.Add(new AdminHubOfficer(adminHubs.Last(), adminOfficerId));

            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            var request = new RemoveAdminOfficerFromAdminHubRequestModel(
                adminHubs.First().Id, adminOfficerId, AccountTypeInternal.AdminHubManager, adminManagerId);

            //Act
            var result = await _sut.RemoveAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ManageAdminHubOutcome.InvalidAssignment, result.Error);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task RemoveAdminOfficerAsync_WhenRemoveFromRepositoryFails(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminOfficerId,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            adminHubs.First().AdminManagerId = adminManagerId;
            adminHubs.First().AdminOfficers.Add(new AdminHubOfficer(adminHubs.First(), adminOfficerId));

            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            _adminHubRepository.Setup(x => x.RemoveAdminOfficerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotFound));

            var request = new RemoveAdminOfficerFromAdminHubRequestModel(
                adminHubs.First().Id, adminOfficerId, AccountTypeInternal.AdminHubManager, adminManagerId);

            //Act
            var result = await _sut.RemoveAdminOfficerAsync(request, It.IsAny<CancellationToken>());

            //Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ManageAdminHubOutcome.UpdateFailure, result.Error);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.Verify(x => x.RemoveAdminOfficerAsync(request.AdminHubId, request.UserId, It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task UpdateAdminHubDetailsAsync_WhenSuccessful(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminManagerId,
            Guid newAdminManagerId,
            string newName,
            string newAddress)
        {
            _sut = CreateSut();

            //arrange
            adminHubs.First().AdminManagerId = adminManagerId;

            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            _adminHubRepository.Setup(x => x.UpdateAdminHubDetailsAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var request = new UpdateAdminHubDetailsRequestModel(
                adminHubs.First().Id, newAdminManagerId, newName, newAddress, adminManagerId, AccountTypeInternal.AdminHubManager);

            //Act
            var result = await _sut.UpdateAdminHubDetailsAsync(request, CancellationToken.None);

            //assert
            Assert.True(result.IsSuccess);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.Verify(x => x.UpdateAdminHubDetailsAsync(
                request.AdminHubId, newAdminManagerId, newName, newAddress, It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateAdminHubDetailsAsync_WhenUserIsNotAdminHubManager()
        {
            _sut = CreateSut();

            //arrange

            var request = new UpdateAdminHubDetailsRequestModel(
                Guid.NewGuid(), Guid.NewGuid(), string.Empty, string.Empty, Guid.NewGuid(), AccountTypeInternal.AdminOfficer);

            //Act
            var result = await _sut.UpdateAdminHubDetailsAsync(request, CancellationToken.None);

            //assert
            Assert.True(result.IsFailure);
            _adminHubRepository.VerifyNoOtherCalls();
            Assert.Equal(ManageAdminHubOutcome.Unauthorized, result.Error);
        }

        [Theory, AutoMoqData]
        public async Task UpdateAdminHubDetailsAsync_WhenAdminHubNotFound(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminManagerId,
            Guid newAdminManagerId,
            string newName,
            string newAddress)
        {
            _sut = CreateSut();

            //arrange
            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            var request = new UpdateAdminHubDetailsRequestModel(
                Guid.NewGuid(), newAdminManagerId, newName, newAddress, adminManagerId, AccountTypeInternal.AdminHubManager);

            //Act
            var result = await _sut.UpdateAdminHubDetailsAsync(request, CancellationToken.None);

            //assert
            Assert.True(result.IsFailure);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
            Assert.Equal(ManageAdminHubOutcome.AdminHubNotFound, result.Error);
        }

        [Theory, AutoMoqData]
        public async Task UpdateAdminHubDetailsAsync_WhenUserNotManagerOfHub(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminManagerId,
            Guid newAdminManagerId,
            string newName, 
            string newAddress)
        {
            _sut = CreateSut();

            //arrange
            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            var request = new UpdateAdminHubDetailsRequestModel(
                adminHubs.First().Id, newAdminManagerId, newName, newAddress, adminManagerId, AccountTypeInternal.AdminHubManager);

            //Act
            var result = await _sut.UpdateAdminHubDetailsAsync(request, CancellationToken.None);

            //assert
            Assert.True(result.IsFailure);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
            Assert.Equal(ManageAdminHubOutcome.Unauthorized, result.Error);
        }

        [Theory, AutoMoqData]
        public async Task UpdateAdminHubDetailsAsync_WhenNewValuesAreUnchanged(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminManagerId)
        {
            _sut = CreateSut();

            //arrange
            adminHubs.First().AdminManagerId = adminManagerId;

            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            _adminHubRepository.Setup(x => x.UpdateAdminHubDetailsAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var request = new UpdateAdminHubDetailsRequestModel(
                adminHubs.First().Id, adminManagerId, adminHubs.First().Name, adminHubs.First().Address, adminManagerId, AccountTypeInternal.AdminHubManager);

            //Act
            var result = await _sut.UpdateAdminHubDetailsAsync(request, CancellationToken.None);

            //assert
            Assert.True(result.IsFailure);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
            Assert.Equal(ManageAdminHubOutcome.NoChangeSubmitted, result.Error);
        }

        [Theory, AutoMoqData]
        public async Task UpdateAdminHubDetailsAsync_WhenUpdateInRepositoryFails(
            IReadOnlyCollection<AdminHub> adminHubs,
            Guid adminManagerId,
            Guid newAdminManagerId,
            string newName, 
            string newAddress)
        {
            _sut = CreateSut();

            //arrange
            adminHubs.First().AdminManagerId = adminManagerId;

            _adminHubRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(adminHubs));

            _adminHubRepository.Setup(x => x.UpdateAdminHubDetailsAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotFound));

            var request = new UpdateAdminHubDetailsRequestModel(
                adminHubs.First().Id, newAdminManagerId, newName, newAddress, adminManagerId, AccountTypeInternal.AdminHubManager);

            //Act
            var result = await _sut.UpdateAdminHubDetailsAsync(request, CancellationToken.None);

            //assert
            Assert.True(result.IsFailure);
            _adminHubRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.Verify(x => x.UpdateAdminHubDetailsAsync(
                request.AdminHubId, newAdminManagerId, newName, newAddress, It.IsAny<CancellationToken>()), Times.Once);
            _adminHubRepository.VerifyNoOtherCalls();
            Assert.Equal(ManageAdminHubOutcome.UpdateFailure, result.Error);
        }

        private AdminHubService CreateSut()
        {
            _adminHubRepository.Reset();

            return new AdminHubService(
                _adminHubRepository.Object,
                new NullLogger<AdminHubService>());
        }

        private static void AssertAdminHub(AdminHub expectedAdminHub, AdminHubModel actualAdminHub)
        {
            Assert.Equal(expectedAdminHub.Id, actualAdminHub.Id);
            Assert.Equal(expectedAdminHub.AdminManagerId, actualAdminHub.AdminManagerUserAccountId);
            Assert.Equal(expectedAdminHub.Name, actualAdminHub.Name);

            foreach (var actualHubArea in actualAdminHub.Areas)
            {
                var expectedArea = expectedAdminHub.Areas.Single(x => x.Id == actualHubArea.Id);
                Assert.Equal(expectedArea.Id, actualHubArea.Id);
                Assert.Equal(expectedArea.Name, actualHubArea.Name);
            }

            foreach (var actualHubOfficer in actualAdminHub.AdminOfficers)
            {
                var expectedOfficer = expectedAdminHub.AdminOfficers.Single(x => x.Id == actualHubOfficer.Id);
                Assert.Equal(expectedOfficer.Id, actualHubOfficer.Id);
                Assert.Equal(expectedOfficer.UserAccountId, actualHubOfficer.UserAccountId);
                Assert.Equal(expectedOfficer.Id, actualHubOfficer.Id);
            }
        }
    }
}
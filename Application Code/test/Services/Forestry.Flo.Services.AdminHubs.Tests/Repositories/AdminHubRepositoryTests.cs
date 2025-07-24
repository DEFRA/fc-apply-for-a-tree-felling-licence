using AutoFixture;
using Forestry.Flo.Services.AdminHubs.Entities;
using Forestry.Flo.Services.AdminHubs.Repositories;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.AdminHubs.Tests.Repositories
{
    public class AdminHubRepositoryTests
    {
        private readonly AdminHubContext _adminHubContext;
        private readonly AdminHubRepository _sut;
        private readonly Fixture _fixtureInstance = new();

        public AdminHubRepositoryTests()
        {
            _adminHubContext = TestAdminHubDatabaseFactory.CreateDefaultTestContext();
            _sut = new AdminHubRepository(_adminHubContext);
        }

        [Theory, AutoMoqData]
        public async Task GetAll_WhenAdminHubsFound(List<AdminHub> adminHubs)
        {
            //arrange
            await _adminHubContext.AddRangeAsync(adminHubs);
            await _adminHubContext.SaveChangesAsync();

            //act
            var result = await _sut.GetAllAsync(new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal(adminHubs.Count, result.Value.Count);

            foreach (var adminHub in result.Value)
            {
                Assert.True(adminHub.AdminOfficers.Any());
                Assert.True(adminHub.Areas.Any());
            }
        }

        [Fact]
        public async Task GetAll_WhenNoAdminHubsFound()
        {
            //arrange

            //act
            var result = await _sut.GetAllAsync(new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.Count);
        }

        [Theory, AutoMoqData]
        public async Task AddAdminOfficerToAdminHub(AdminHub adminHub, Guid adminOfficerUserId)
        {
            //arrange
            await _adminHubContext.AddAsync(adminHub);
            await _adminHubContext.SaveChangesAsync();
            var originalAdminOfficerCount = adminHub.AdminOfficers.Count;

            //act
            var result = await _sut.AddAdminOfficerAsync(adminHub.Id, adminOfficerUserId, new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);

            var addedAdminOfficer =
                _adminHubContext.AdminHubOfficers.FirstOrDefault(x => x.UserAccountId == adminOfficerUserId);
            Assert.NotNull(addedAdminOfficer);
            Assert.True(addedAdminOfficer!.AdminHubId == adminHub.Id);
            Assert.True(addedAdminOfficer.AdminHub == adminHub);
            Assert.Equal(originalAdminOfficerCount+1, adminHub.AdminOfficers.Count);
        }

        [Theory, AutoMoqData]
        public async Task AddAdminOfficerToUnknownAdminHub(AdminHub adminHub, Guid adminOfficerUserId)
        {
            //arrange
            await _adminHubContext.AddAsync(adminHub);
            await _adminHubContext.SaveChangesAsync();
            var originalAdminOfficerCount = adminHub.AdminOfficers.Count;

            var nonExistentAdminHub = await TestAdminHubDatabaseFactory.CreateTestAdminHub(_adminHubContext, _fixtureInstance, save: false);

            //act
            var result = await _sut.AddAdminOfficerAsync(nonExistentAdminHub.Id, adminOfficerUserId, new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.True(result.Error == UserDbErrorReason.NotFound);
            Assert.Equal(originalAdminOfficerCount, adminHub.AdminOfficers.Count);
        }
        
        [Theory, AutoMoqData]
        public async Task AddAdminOfficerWhoAlreadyExistsToAdminHub(AdminHub adminHub)
        {
            //arrange
            await _adminHubContext.AddAsync(adminHub);
            await _adminHubContext.SaveChangesAsync();
            var originalAdminOfficerCount = adminHub.AdminOfficers.Count;
            var existingAdminOfficerUserId = adminHub.AdminOfficers.Last().UserAccountId;

            //act
            var result = await _sut.AddAdminOfficerAsync(adminHub.Id, existingAdminOfficerUserId, new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.True(result.Error == UserDbErrorReason.NotUnique);
            Assert.Equal(originalAdminOfficerCount, adminHub.AdminOfficers.Count);
        }

        [Theory, AutoMoqData]
        public async Task RemoveAdminOfficerFromAdminHub(AdminHub adminHub)
        {
            //arrange
            await _adminHubContext.AddAsync(adminHub);
            await _adminHubContext.SaveChangesAsync();
            var originalAdminOfficerCount = adminHub.AdminOfficers.Count;
            var adminOfficerUserIdToRemove = adminHub.AdminOfficers.Last().UserAccountId;

            //act
            var result = await _sut.RemoveAdminOfficerAsync(adminHub.Id, adminOfficerUserIdToRemove, new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);

            var addedAdminOfficer =
                _adminHubContext.AdminHubOfficers.FirstOrDefault(x => x.UserAccountId == adminOfficerUserIdToRemove);
            Assert.Null(addedAdminOfficer);
            Assert.Equal(originalAdminOfficerCount-1, adminHub.AdminOfficers.Count);
        }

        [Theory, AutoMoqData]
        public async Task RemoveAdminOfficerFromUnknownAdminHub(AdminHub adminHub, Guid adminOfficerUserId)
        {
            //arrange
            await _adminHubContext.AddAsync(adminHub);
            await _adminHubContext.SaveChangesAsync();
            var originalAdminOfficerCount = adminHub.AdminOfficers.Count;

            var nonExistentAdminHub = await TestAdminHubDatabaseFactory.CreateTestAdminHub(_adminHubContext, _fixtureInstance, save: false);

            //act
            var result = await _sut.RemoveAdminOfficerAsync(nonExistentAdminHub.Id, adminOfficerUserId, new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.True(result.Error == UserDbErrorReason.NotFound);
            Assert.Equal(originalAdminOfficerCount, adminHub.AdminOfficers.Count);
        }

        [Theory, AutoMoqData]
        public async Task RemoveAdminOfficerWhenDoesNotExistAtAdminHub(AdminHub adminHub, Guid adminOfficerUserId)
        {
            //arrange
            await _adminHubContext.AddAsync(adminHub);
            await _adminHubContext.SaveChangesAsync();
            var originalAdminOfficerCount = adminHub.AdminOfficers.Count;

            //act
            var result = await _sut.RemoveAdminOfficerAsync(adminHub.Id, adminOfficerUserId, new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.True(result.Error == UserDbErrorReason.NotFound);
            Assert.Equal(originalAdminOfficerCount, adminHub.AdminOfficers.Count);
        }
        
        [Theory, AutoMoqData]
        public async Task RemoveAdminOfficerWithUnknownUserIdFromUnknownAdminHub(AdminHub adminHub, Guid adminOfficerUserId)
        {
            //arrange
            await _adminHubContext.AddAsync(adminHub);
            await _adminHubContext.SaveChangesAsync();
            var originalAdminOfficerCount = adminHub.AdminOfficers.Count;

            var nonExistentAdminHub = await TestAdminHubDatabaseFactory.CreateTestAdminHub(_adminHubContext, _fixtureInstance, save: false);

            //act
            var result = await _sut.RemoveAdminOfficerAsync(nonExistentAdminHub.Id, adminOfficerUserId, new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            Assert.True(result.Error == UserDbErrorReason.NotFound);
            Assert.Equal(originalAdminOfficerCount, adminHub.AdminOfficers.Count);
        }
    }
}

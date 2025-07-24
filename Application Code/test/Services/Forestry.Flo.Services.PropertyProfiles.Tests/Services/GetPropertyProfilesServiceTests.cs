using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.PropertyProfiles.Tests.Services
{
    public class GetPropertyProfilesServiceTests
    {
        private readonly Mock<IPropertyProfileRepository> _mockGetPropertyProfilesRepository = new();

        [Theory, AutoMoqData]
        public async Task GetPropertyByIdWhenUserHasAccess(PropertyProfile profile)
        {
            //arrange
            var sut = CreateSut();

            _mockGetPropertyProfilesRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<PropertyProfile, UserDbErrorReason>(profile));

            //act
            var result = await sut.GetPropertyByIdAsync(profile.Id, new UserAccessModel(), new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal(profile.Id, result.Value.Id);
            _mockGetPropertyProfilesRepository.Verify(x=>x.GetByIdAsync(profile.Id,It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task CannotGetPropertyByIdWhenUserDoesNotHaveAccess(PropertyProfile profile)
        {
            //arrange
            var sut = CreateSut(canAccess:false);

            //act
            var result = await sut.GetPropertyByIdAsync(profile.Id, new UserAccessModel(), new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            _mockGetPropertyProfilesRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CannotGetPropertyWhenIdIsNotFound()
        {
            //arrange
            var sut = CreateSut();

            _mockGetPropertyProfilesRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<PropertyProfile, UserDbErrorReason>(UserDbErrorReason.NotFound));

            //act
            var result = await sut.GetPropertyByIdAsync(Guid.NewGuid(), new UserAccessModel(), new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            _mockGetPropertyProfilesRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
                , Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task GetPropertyListWhenUserHasAccess(List<PropertyProfile> profiles)
        {
            //arrange
            var sut = CreateSut();

            _mockGetPropertyProfilesRepository
                .Setup(x => x.ListAsync(It.IsAny<ListPropertyProfilesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(profiles);

            var query = new ListPropertyProfilesQuery(WoodlandOwnerId: profiles.First().WoodlandOwnerId);

            //act
            var result = await sut.ListAsync(query, new UserAccessModel(), new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal(profiles.Count, result.Value.Count());
            _mockGetPropertyProfilesRepository.Verify(x => x.ListAsync(query, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task GetPropertyListWhenUserDoesNotHaveAccess(IEnumerable<PropertyProfile> profiles)
        {
            //arrange
            var sut = CreateSut(canAccess: false);

            var query = new ListPropertyProfilesQuery(WoodlandOwnerId: profiles.First().WoodlandOwnerId);

            //act
            var result = await sut.ListAsync(query, new UserAccessModel(), new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            _mockGetPropertyProfilesRepository.Verify(x => x.ListAsync(query, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetPropertyListWhenNoPropertiesFound()
        {
            //arrange
            var sut = CreateSut();

            var query = new ListPropertyProfilesQuery(WoodlandOwnerId: Guid.NewGuid());

            _mockGetPropertyProfilesRepository
                .Setup(x => x.ListAsync(It.IsAny<ListPropertyProfilesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PropertyProfile>());

            //act
            var result = await sut.ListAsync(query, new UserAccessModel(), new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
            _mockGetPropertyProfilesRepository.Verify(x => x.ListAsync(query, It.IsAny<CancellationToken>()), Times.Once);
        }

        private GetPropertyProfilesService CreateSut(bool canAccess = true)
        {
            _mockGetPropertyProfilesRepository.Reset();

            _mockGetPropertyProfilesRepository
                .Setup(x => x.CheckUserCanAccessPropertyProfileAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(canAccess));

            _mockGetPropertyProfilesRepository
                .Setup(x => x.CheckUserCanAccessPropertyProfilesAsync(It.IsAny<ListPropertyProfilesQuery>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(canAccess));

            return new GetPropertyProfilesService(_mockGetPropertyProfilesRepository.Object);
        }
    }
}

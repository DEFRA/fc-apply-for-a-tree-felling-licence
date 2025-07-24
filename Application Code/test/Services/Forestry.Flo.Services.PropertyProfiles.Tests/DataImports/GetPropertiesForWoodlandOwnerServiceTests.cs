using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.DataImports;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.PropertyProfiles.Tests.DataImports;

public class GetPropertiesForWoodlandOwnerServiceTests
{
    private readonly Mock<IPropertyProfileRepository> _mockGetPropertyProfilesRepository = new();

    [Theory, AutoMoqData]
    public async Task GetPropertyListWhenUserHasAccess(
        List<PropertyProfile> profiles,
        UserAccessModel userAccessModel,
        Guid woodlandOwnerId)
    {
        var expected = GetExpected(profiles);

        //arrange
        var sut = CreateSut();

        _mockGetPropertyProfilesRepository
            .Setup(x => x.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<PropertyProfile>, UserDbErrorReason>(profiles));

        //act
        var result = await sut.GetPropertiesForDataImport(userAccessModel, woodlandOwnerId, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Equal(profiles.Count, result.Value.Count());
        
        Assert.Equivalent(expected, result.Value);

        _mockGetPropertyProfilesRepository.Verify(x => x.ListAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockGetPropertyProfilesRepository.Verify(x => x.CheckUserCanAccessPropertyProfilesAsync(
                It.Is<ListPropertyProfilesQuery>(l => l.WoodlandOwnerId == woodlandOwnerId && l.Ids.Count == profiles.Count && l.Ids.All(i => profiles.Select(p => p.Id).Contains(i))),
                userAccessModel,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetPropertyListWhenUserDoesNotHaveAccess(
        List<PropertyProfile> profiles,
        UserAccessModel userAccessModel,
        Guid woodlandOwnerId)
    {
        //arrange
        var sut = CreateSut(false);

        _mockGetPropertyProfilesRepository
            .Setup(x => x.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<PropertyProfile>, UserDbErrorReason>(profiles));

        //act
        var result = await sut.GetPropertiesForDataImport(userAccessModel, woodlandOwnerId, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);

        _mockGetPropertyProfilesRepository.Verify(x => x.ListAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockGetPropertyProfilesRepository.Verify(x => x.CheckUserCanAccessPropertyProfilesAsync(
                It.Is<ListPropertyProfilesQuery>(l => l.WoodlandOwnerId == woodlandOwnerId && l.Ids.Count == profiles.Count && l.Ids.All(i => profiles.Select(p => p.Id).Contains(i))),
                userAccessModel,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetPropertyListWhenNoneExist(
        UserAccessModel userAccessModel,
        Guid woodlandOwnerId)
    {
        //arrange
        var sut = CreateSut();

        _mockGetPropertyProfilesRepository
            .Setup(x => x.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<PropertyProfile>, UserDbErrorReason>([]));

        //act
        var result = await sut.GetPropertiesForDataImport(userAccessModel, woodlandOwnerId, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        _mockGetPropertyProfilesRepository.Verify(x => x.ListAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockGetPropertyProfilesRepository.Verify(x => x.CheckUserCanAccessPropertyProfilesAsync(
                It.Is<ListPropertyProfilesQuery>(l => l.WoodlandOwnerId == woodlandOwnerId && l.Ids.Count == 0),
                userAccessModel,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private IEnumerable<PropertyIds> GetExpected(List<PropertyProfile> profiles) =>
            profiles.Select(p => new PropertyIds
            {
                Id = p.Id,
                Name = p.Name,
                CompartmentIds = p.Compartments.Select(c => new CompartmentIds
                {
                    Id = c.Id,
                    CompartmentName = c.CompartmentNumber,
                    Area = c.TotalHectares
                }).ToList()
            });

    private GetPropertiesForWoodlandOwnerService CreateSut(bool canAccess = true)
    {
        _mockGetPropertyProfilesRepository.Reset();

        _mockGetPropertyProfilesRepository
            .Setup(x => x.CheckUserCanAccessPropertyProfileAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(canAccess));

        _mockGetPropertyProfilesRepository
            .Setup(x => x.CheckUserCanAccessPropertyProfilesAsync(It.IsAny<ListPropertyProfilesQuery>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(canAccess));

        return new GetPropertiesForWoodlandOwnerService(
            _mockGetPropertyProfilesRepository.Object, 
            new NullLogger<GetPropertiesForWoodlandOwnerService>());
    }
}
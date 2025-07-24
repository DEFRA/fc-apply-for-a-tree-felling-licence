using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class GetFellingLicenceApplicationForExternalUsersServiceTests
{
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationRepository = new();

    [Theory, AutoMoqData]
    public async Task GetApplicationsForWoodlandOwnerAsync_ReturnsFailureWhenUserHasNoAccess(
        Guid woodlandOwnerId)
    {
        var sut = CreateSut();

        var uam = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { Guid.NewGuid() }
        };

        var result = await sut.GetApplicationsForWoodlandOwnerAsync(woodlandOwnerId, uam, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationsForWoodlandOwnerAsync_ReturnsListWhenUserHasAccess(
        Guid woodlandOwnerId,
        List<FellingLicenceApplication> applications)
    {
        var sut = CreateSut();

        var uam = new UserAccessModel
        {
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { woodlandOwnerId }
        };

        _fellingLicenceApplicationRepository.Setup(x => x.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applications);

        var result = await sut.GetApplicationsForWoodlandOwnerAsync(woodlandOwnerId, uam, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(applications, result.Value);

        _fellingLicenceApplicationRepository.Verify(x => x.ListAsync(woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    private GetFellingLicenceApplicationForExternalUsersService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();

        return new GetFellingLicenceApplicationForExternalUsersService(
            _fellingLicenceApplicationRepository.Object);
    }
}
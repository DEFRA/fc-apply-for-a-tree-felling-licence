using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class GetConfiguredFcAreasServiceTests
{
    private readonly Mock<IAdminHubService> _adminHubServiceMock = new();

    [Fact]
    public async Task GetAllWhenRetrieveAdminHubsFails()
    {
        var sut = CreateSut();

        _adminHubServiceMock
            .Setup(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(ManageAdminHubOutcome.Unauthorized));

        var result = await sut.GetAllAsync(CancellationToken.None);

        Assert.True(result.IsFailure);

        _adminHubServiceMock.Verify(x => x.RetrieveAdminHubDataAsync(
            It.Is<GetAdminHubsDataRequestModel>(r => r.PerformingUserAccountType == AccountTypeInternal.AdminHubManager), 
            It.IsAny<CancellationToken>()), Times.Once);
        _adminHubServiceMock.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task GetAllWhenRetrieveAdminHubsSucceeds(IReadOnlyCollection<AdminHubModel> adminHubs)
    {
        var sut = CreateSut();
        _adminHubServiceMock
            .Setup(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(adminHubs));
        
        var result = await sut.GetAllAsync(CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(adminHubs.Sum(ah => ah.Areas.Count), result.Value.Count);
        foreach (var areaModel in adminHubs.SelectMany(x => x.Areas))
        {
            var hub = adminHubs.First(x => x.Areas.Contains(areaModel));
            Assert.Contains(result.Value,
                x => x.AdminHubName == hub.Name && x.Area == areaModel && x.AreaCostCode == areaModel.Code);
        }
        
        _adminHubServiceMock.Verify(x => x.RetrieveAdminHubDataAsync(
            It.Is<GetAdminHubsDataRequestModel>(r => r.PerformingUserAccountType == AccountTypeInternal.AdminHubManager),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _adminHubServiceMock.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task TryGetAdminHubAddressWhenRetrieveAdminHubDataFailsReturnsName(string adminHubName)
    {
        var sut = CreateSut();

        _adminHubServiceMock
            .Setup(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(ManageAdminHubOutcome.Unauthorized));

        var result = await sut.TryGetAdminHubAddress(adminHubName, CancellationToken.None);

        Assert.Equal(adminHubName, result);
    }

    [Theory, AutoData]
    public async Task TryGetAdminHubAddressWhenRetrieveAdminHubDataSuccessWithNoMatchingHub(
        string adminHubName,
        IReadOnlyCollection<AdminHubModel> adminHubs)
    {
        var sut = CreateSut();

        _adminHubServiceMock
            .Setup(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(adminHubs));

        var result = await sut.TryGetAdminHubAddress(adminHubName, CancellationToken.None);

        Assert.Equal(adminHubName, result);
    }

    [Theory, AutoData]
    public async Task TryGetAdminHubAddressWhenRetrieveAdminHubDataSuccessWithAddress(IReadOnlyCollection<AdminHubModel> adminHubs)
    {
        var sut = CreateSut();

        _adminHubServiceMock
            .Setup(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(adminHubs));

        var requestedAdminHub = adminHubs.Last();

        var result = await sut.TryGetAdminHubAddress(requestedAdminHub.Name, CancellationToken.None);

        Assert.Equal(requestedAdminHub.Address, result);
    }

    [Theory, AutoData]
    public async Task TryGetAdminHubAddressWhenRetrieveAdminHubDataSuccessWithNullAddress(IReadOnlyCollection<AdminHubModel> adminHubs)
    {
        var sut = CreateSut();

        _adminHubServiceMock
            .Setup(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(adminHubs));

        var requestedAdminHub = adminHubs.Last();
        requestedAdminHub.Address = null;

        var result = await sut.TryGetAdminHubAddress(requestedAdminHub.Name, CancellationToken.None);

        Assert.Equal(requestedAdminHub.Name, result);
    }

    private GetConfiguredFcAreasService CreateSut()
    {
        _adminHubServiceMock.Reset();

        return new GetConfiguredFcAreasService(_adminHubServiceMock.Object, new NullLogger<GetConfiguredFcAreasService>());
    }
}
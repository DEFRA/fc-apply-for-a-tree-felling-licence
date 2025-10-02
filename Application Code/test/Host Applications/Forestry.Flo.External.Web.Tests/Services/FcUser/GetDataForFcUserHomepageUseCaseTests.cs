using AutoFixture;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Tests.Common;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services.FcUser;
using Forestry.Flo.Services.Applicants.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Tests.Services.FcUser;

public class GetDataForFcUserHomepageUseCaseTests
{
    private readonly Mock<IRetrieveWoodlandOwners> _mockRetrieveWoodlandOwnersService = new();
    private readonly Mock<IRetrieveAgencies> _mockRetrieveAgenciesService = new();

    private readonly Fixture _fixture = new();
    private ExternalApplicant? _externalApplicant;

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WithExpectedModel(
        List<WoodlandOwnerFcModel> woodlandOwners,
        List<AgencyFcModel> agencies)
    {
        // arrange
        var sut = CreateSut();

        _mockRetrieveWoodlandOwnersService.Setup(r =>
                r.GetAllWoodlandOwnersForFcAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwners));

        _mockRetrieveAgenciesService.Setup(r =>
                r.GetAllAgenciesForFcAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(agencies);


        // act
        var result = await sut.ExecuteAsync(
            _externalApplicant!,
            CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(woodlandOwners.Count(x => !x.HasActiveUserAccounts), result.Value.AllWoodlandOwnersManagedByFc.Count);
        Assert.Equal(woodlandOwners.Count(x => x.HasActiveUserAccounts), result.Value.AllExternalWoodlandOwners.Count);

        Assert.Equal(agencies.Count(x => !x.HasActiveUserAccounts), result.Value.AllAgenciesManagedByFc.Count);
        Assert.Equal(agencies.Count(x => x.HasActiveUserAccounts), result.Value.AllExternalAgencies.Count);

        _mockRetrieveWoodlandOwnersService.Verify(
            x => x.GetAllWoodlandOwnersForFcAsync(_externalApplicant!.UserAccountId!.Value,
                It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveAgenciesService.Verify(
            x => x.GetAllAgenciesForFcAsync(_externalApplicant!.UserAccountId!.Value,
                It.IsAny<CancellationToken>()), Times.Once);
    }

    //when user null, 


    [Fact]
    public async Task ReturnsFailure_WhenWoodlandOwnersServiceReturnsFailure()
    {
        // arrange
        var sut = CreateSut();

        _mockRetrieveWoodlandOwnersService.Setup(r =>
                r.GetAllWoodlandOwnersForFcAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<WoodlandOwnerFcModel>>("error"));

        // act
        var result = await sut.ExecuteAsync(
            _externalApplicant!,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockRetrieveWoodlandOwnersService.Verify(
            x => x.GetAllWoodlandOwnersForFcAsync(_externalApplicant!.UserAccountId!.Value,
                It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveAgenciesService.Verify(
            x => x.GetAllAgenciesForFcAsync(_externalApplicant!.UserAccountId!.Value,
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenAgenciesServiceReturnsFailure(
        List<WoodlandOwnerFcModel> woodlandOwners)
    {
        // arrange
        var sut = CreateSut();

        _mockRetrieveWoodlandOwnersService.Setup(r =>
                r.GetAllWoodlandOwnersForFcAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwners));

        _mockRetrieveAgenciesService.Setup(r =>
                r.GetAllAgenciesForFcAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<AgencyFcModel>>("error"));

        // act
        var result = await sut.ExecuteAsync(
            _externalApplicant!,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockRetrieveWoodlandOwnersService.Verify(
            x => x.GetAllWoodlandOwnersForFcAsync(_externalApplicant!.UserAccountId!.Value,
                It.IsAny<CancellationToken>()), Times.Once);

        _mockRetrieveAgenciesService.Verify(
            x => x.GetAllAgenciesForFcAsync(_externalApplicant!.UserAccountId!.Value,
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Throws_WhenUserNotSupplied()
    {
        // arrange
        var sut = CreateSut();

        // act/assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.ExecuteAsync(null, CancellationToken.None));
    }

    private GetDataForFcUserHomepageUseCase CreateSut()
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            agencyId: _fixture.Create<Guid>(),
            woodlandOwnerName: _fixture.Create<string>());

        _externalApplicant = new ExternalApplicant(user);

        _mockRetrieveAgenciesService.Reset();
        _mockRetrieveWoodlandOwnersService.Reset();

        return new GetDataForFcUserHomepageUseCase(
            _mockRetrieveWoodlandOwnersService.Object,
            _mockRetrieveAgenciesService.Object,
            new NullLogger<GetDataForFcUserHomepageUseCase>()
        );
    }
}

using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserAccountModel = Forestry.Flo.Services.Applicants.Models.UserAccountModel;

namespace Forestry.Flo.External.Web.Tests.Services;

public class GetListOfUsersForWoodlandUseCaseTests
{
    private readonly Mock<IRetrieveUserAccountsService> _mockRetrieveUserAccountsService = new ();
    private static readonly Fixture FixtureInstance = new();

    [Theory, AutoMoqData]
    public async Task GetWoodlandOwnerUsersSuccess(
        Guid userId,
        Guid woodlandOwnerId,
        List<UserAccountModel> returnedUsers)
    {
        var user = new ExternalApplicant(UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: userId,
            woodlandOwnerId: woodlandOwnerId,
            accountTypeExternal: AccountTypeExternal.WoodlandOwnerAdministrator));

        //arrange
        var sut = CreateSut();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(returnedUsers));

        //act
        var result = await sut.RetrieveListOfWoodlandOwnerUsersAsync(
            user,
            woodlandOwnerId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(returnedUsers, result.Value.WoodlandOwnerUsers);
        
        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(
                woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.VerifyNoOtherCalls();

    }

    [Theory, AutoMoqData]
    public async Task GetWoodlandOwnerUsersFailure(
        Guid userId,
        Guid woodlandOwnerId,
        string error)
    {
        var user = new ExternalApplicant(UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: userId,
            woodlandOwnerId: woodlandOwnerId,
            accountTypeExternal: AccountTypeExternal.WoodlandOwnerAdministrator));

        //arrange
        var sut = CreateSut();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<UserAccountModel>>(error));

        //act
        var result = await sut.RetrieveListOfWoodlandOwnerUsersAsync(
            user,
            woodlandOwnerId,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(
                woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.VerifyNoOtherCalls();

    }

    [Theory]
    [InlineData(AccountTypeExternal.Agent)]
    [InlineData(AccountTypeExternal.AgentAdministrator)]
    [InlineData(AccountTypeExternal.WoodlandOwner)]
    [InlineData(AccountTypeExternal.WoodlandOwnerAdministrator)]
    public async Task WhenNoWoodlandOwnerUsersReturnedAsStandardUser(
        AccountTypeExternal accountType)
    {
        var userId = Guid.NewGuid();
        var woodlandOwnerId = Guid.NewGuid();

        var user = new ExternalApplicant(UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: userId,
            woodlandOwnerId: woodlandOwnerId,
            accountTypeExternal: accountType));

        //arrange
        var sut = CreateSut();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<UserAccountModel>(0)));

        //act
        var result = await sut.RetrieveListOfWoodlandOwnerUsersAsync(
            user,
            woodlandOwnerId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.WoodlandOwnerUsers);

        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(
                woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.VerifyNoOtherCalls();

    }

    [Theory, AutoMoqData]
    public async Task WhenNoWoodlandOwnerUsersReturnedAsFcUser(
        Guid userId,
        Guid woodlandOwnerId,
        List<UserAccountModel> returnedUsers)
    {
        var user = new ExternalApplicant(UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            localAccountId: userId,
            woodlandOwnerId: woodlandOwnerId,
            accountTypeExternal: AccountTypeExternal.FcUser,
            isFcUser: true));

        //arrange
        var sut = CreateSut();

        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<UserAccountModel>(0)));
        _mockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountsForFcAgencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(returnedUsers));

        //act
        var result = await sut.RetrieveListOfWoodlandOwnerUsersAsync(
            user,
            woodlandOwnerId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(returnedUsers, result.Value.WoodlandOwnerUsers);

        _mockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(
                woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountsForFcAgencyAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockRetrieveUserAccountsService.VerifyNoOtherCalls();

    }

    private ListWoodlandOwnerUsersUseCase CreateSut()
    {
        _mockRetrieveUserAccountsService.Reset();

        return new ListWoodlandOwnerUsersUseCase(
            _mockRetrieveUserAccountsService.Object,
                new NullLogger<ListWoodlandOwnerUsersUseCase>());
    }
}


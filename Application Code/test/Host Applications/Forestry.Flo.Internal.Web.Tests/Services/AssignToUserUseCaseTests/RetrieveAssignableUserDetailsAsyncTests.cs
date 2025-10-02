using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Tests.Common;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.AssignToUserUseCaseTests;

public class RetrieveAssignableUserDetailsAsyncTests : AssignToUserUseCaseTestsBase
{
    [Theory, AutoMoqData]
    public async Task ShouldIncludeAllUsersThatCanApproveApplications(
    FellingLicenceApplication fla,
    AssignedUserRole role,
    WoodlandOwnerModel woodlandOwner,
    Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount createdByUser,
    List<UserAccount> users,
    string returnUrl)
    {
        // Arrange
        var sut = CreateSut();

        foreach (var user in users)
        {
            user.CanApproveApplications = true;
            user.AccountType = AccountTypeInternal.FieldManager;
        }

        fla.StatusHistories = new List<StatusHistory>()
        {
            new StatusHistory { Created = DateTime.UtcNow.AddHours(-1), Status = FellingLicenceStatus.Submitted, CreatedById = createdByUser.Id}
        };

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);
        MockFlaRepository.Setup(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(fla);
        MockExternalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdByUser);
        MockWoodlandOwnerRepository.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);
        MockInternalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(users.First()));
        MockFlaRepository.Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fla.StatusHistories);

        // Act
        var result = await sut.RetrieveDetailsToAssignFlaToUserAsync(fla.Id, role, returnUrl, _testUser, CancellationToken.None);

        // Assert
        Assert.Equal(fla.Id, result.Value.FellingLicenceApplicationSummary.Id);
        Assert.Equal(users.Count(), result.Value.UserAccounts.Count());
        Assert.False(result.Value.HiddenAccounts);

        MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null));
    }

    [Theory, AutoMoqData]
    public async Task ShouldExcludeAllUsersThatCannotApproveApplications(
    FellingLicenceApplication fla,
    AssignedUserRole role,
    WoodlandOwnerModel woodlandOwner,
    Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount createdByUser,
    List<UserAccount> users,
    string returnUrl)
    {
        // Arrange
        var sut = CreateSut();

        foreach (var user in users)
        {
            user.CanApproveApplications = false;
        }

        fla.StatusHistories = new List<StatusHistory>()
        {
            new StatusHistory { Created = DateTime.UtcNow.AddHours(-1), Status = FellingLicenceStatus.Submitted, CreatedById = createdByUser.Id}
        };

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);
        MockFlaRepository.Setup(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(fla);
        MockExternalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdByUser);
        MockWoodlandOwnerRepository.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);
        MockInternalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(users.First()));
        MockFlaRepository.Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fla.StatusHistories);

        // Act
        var result = await sut.RetrieveDetailsToAssignFlaToUserAsync(fla.Id, role, returnUrl, _testUser, CancellationToken.None);

        // Assert
        Assert.Equal(fla.Id, result.Value.FellingLicenceApplicationSummary!.Id);
        Assert.Empty(result.Value.UserAccounts);
        Assert.True(result.Value.HiddenAccounts);

        MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null));
    }

    [Theory, AutoMoqData]
    public async Task ShouldExcludeUserThatCannotApproveApplications(
    FellingLicenceApplication fla,
    AssignedUserRole role,
    WoodlandOwnerModel woodlandOwner,
    Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount createdByUser,
    List<UserAccount> users,
    string returnUrl)
    {
        // Arrange
        var sut = CreateSut();

        foreach (var user in users)
        {
            TestUtils.SetProtectedProperty(user, "Id", Guid.NewGuid());
            user.CanApproveApplications = true;
        }
        users.First().CanApproveApplications = false;

        fla.StatusHistories = new List<StatusHistory>()
        {
            new StatusHistory { Created = DateTime.UtcNow.AddHours(-1), Status = FellingLicenceStatus.Submitted, CreatedById = createdByUser.Id}
        };

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);
        MockFlaRepository.Setup(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(fla);
        MockExternalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdByUser);
        MockWoodlandOwnerRepository.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);
        MockInternalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(users.First()));
        MockFlaRepository.Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fla.StatusHistories);

        // Act
        var result = await sut.RetrieveDetailsToAssignFlaToUserAsync(fla.Id, role, returnUrl, _testUser, CancellationToken.None);

        // Assert
        Assert.Equal(fla.Id, result.Value.FellingLicenceApplicationSummary.Id);
        Assert.DoesNotContain(false, result.Value.UserAccounts.Select(u => u.CanApproveApplications));
        Assert.True(result.Value.HiddenAccounts);
        Assert.Equal(users.Count-1, result.Value.UserAccounts.Count());

        MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null));
    }

    [Theory, AutoMoqData]
    public async Task ShouldExcludeUserWithSameEmailAsFlaSubmitter(
    FellingLicenceApplication fla,
    WoodlandOwnerModel woodlandOwner,
    AssignedUserRole role,
    Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount createdByUser,
    List<UserAccount> users,
    string returnUrl)
    {
        // Arrange
        var sut = CreateSut();

        foreach (var user in users)
        {
            TestUtils.SetProtectedProperty(user, "Id", Guid.NewGuid());
            user.CanApproveApplications = true;
            user.AccountType = AccountTypeInternal.FieldManager;
        }
        createdByUser.Email = users.First().Email;

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        fla.StatusHistories = new List<StatusHistory>()
        {
            new StatusHistory { Created = DateTime.UtcNow.AddHours(-1), Status = FellingLicenceStatus.Submitted, CreatedById = createdByUser.Id}
        };

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);
        MockFlaRepository.Setup(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(fla);
        MockExternalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdByUser);
        MockWoodlandOwnerRepository.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);
        MockInternalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(users.First()));
        MockFlaRepository.Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fla.StatusHistories);

        // Act
        var result = await sut.RetrieveDetailsToAssignFlaToUserAsync(fla.Id, role, returnUrl, _testUser, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(fla.Id, result.Value.FellingLicenceApplicationSummary!.Id);
        Assert.DoesNotContain(createdByUser.Email, result.Value.UserAccounts.Select(x => x.Email));
        Assert.True(result.Value.HiddenAccounts);
        Assert.Equal(users.Count-1, result.Value.UserAccounts.Count());

        MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null));
    }
}
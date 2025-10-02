using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;
using System.Security.Claims;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public partial class FellingLicenceApplicationUseCaseTests
{
    [Theory, AutoMoqData]
    public async Task ShouldRetrieveReopenWithdrawnApplicationModel_WhenApplicationIsWithdrawn(
        FellingLicenceApplication application,
        List<ActivityFeedItemModel> activityFeedItems,
        WoodlandOwnerModel woodlandOwner, 
        UserAccount userAccount,
        Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Status = FellingLicenceStatus.Withdrawn, Created = DateTime.UtcNow }
        };

        application.WoodlandOwnerId = woodlandOwner.Id!.Value;
        var claims = new List<Claim>
        {
            new Claim(FloClaimTypes.UserCanApproveApplications, "false"),
            new Claim(FloClaimTypes.LocalAccountId, account.Id.ToString())
        };
        var internalUser = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")));
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.FieldManager,
                AssignedUserId = (Guid)internalUser.UserAccountId!
            }
        };

        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));
        ArrangeDefaultMocks(application, woodlandOwner, userAccount);

        _fellingLicenceApplicationRepository
            .Setup(s => s.GetAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _activityFeedItemProvider
            .Setup(p => p.RetrieveAllRelevantActivityFeedItemsAsync(
                It.IsAny<ActivityFeedItemProviderModel>(), 
                It.IsAny<ActorType>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(activityFeedItems);

        // Act
        var result = await _sut.RetrieveReopenWithdrawnApplicationModelAsync(application.Id, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(application.Id, result.Value.ApplicationId);
        Assert.Equivalent(activityFeedItems, result.Value.ActivityFeed.ActivityFeedItemModels);
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailToRetrieveReopenWithdrawnApplicationModel_WhenApplicationIsNotWithdrawn(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner,
        UserAccount userAccount,
        Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Status = FellingLicenceStatus.Submitted, Created = DateTime.UtcNow }
        };

        application.WoodlandOwnerId = woodlandOwner.Id!.Value;
        var claims = new List<Claim>
        {
            new Claim(FloClaimTypes.UserCanApproveApplications, "false"),
            new Claim(FloClaimTypes.LocalAccountId, account.Id.ToString())
        };
        var internalUser = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")));
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.FieldManager,
                AssignedUserId = (Guid)internalUser.UserAccountId!
            }
        };

        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));
        ArrangeDefaultMocks(application, woodlandOwner, userAccount);

        // Act
        var result = await _sut.RetrieveReopenWithdrawnApplicationModelAsync(application.Id, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Application is not in a withdrawn state", result.Error);
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailToRetrieveReopenWithdrawnApplicationModel_WhenApplicationDetailsCannotBeRetrieved(
        Guid applicationId)
    {
        // Arrange
        _fellingLicenceApplicationRepository
            .Setup(s => s.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        // Act
        var result = await _sut.RetrieveReopenWithdrawnApplicationModelAsync(applicationId, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Could not locate Felling Licence Application with the given id", result.Error);
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailToRetrieveReopenWithdrawnApplicationModel_WhenActivityFeedItemsCannotBeRetrieved(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner,
        UserAccount userAccount,
        Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Status = FellingLicenceStatus.Withdrawn, Created = DateTime.UtcNow }
        };

        application.WoodlandOwnerId = woodlandOwner.Id!.Value;

        var claims = new List<Claim>
        {
            new Claim(FloClaimTypes.UserCanApproveApplications, "false"),
            new Claim(FloClaimTypes.LocalAccountId, account.Id.ToString())
        };
        var internalUser = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")));
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.FieldManager,
                AssignedUserId = (Guid)internalUser.UserAccountId!
            }
        };

        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));
        ArrangeDefaultMocks(application, woodlandOwner, userAccount);

        _activityFeedItemProvider
            .Setup(p => p.RetrieveAllRelevantActivityFeedItemsAsync(
                It.IsAny<ActivityFeedItemProviderModel>(),
                It.IsAny<ActorType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<ActivityFeedItemModel>>("Error retrieving activity feed items"));

        // Act
        var result = await _sut.RetrieveReopenWithdrawnApplicationModelAsync(application.Id, "TestPage", CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Error retrieving activity feed items", result.Error);
    }
}

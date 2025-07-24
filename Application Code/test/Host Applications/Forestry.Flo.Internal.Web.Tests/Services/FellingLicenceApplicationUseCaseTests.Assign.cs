using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;
using System.Security.Claims;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public partial class FellingLicenceApplicationUseCaseTests
{

    [Theory, AutoMoqData]
    public async Task ShouldAllowUserToApprove(
    FellingLicenceApplication application, WoodlandOwnerModel woodlandOwner, UserAccount userAccount,
    Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(FloClaimTypes.UserCanApproveApplications, "true"),
            new Claim(FloClaimTypes.LocalAccountId, account.Id.ToString())
        };
        var internalUser = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")));

        application.WoodlandOwnerId = woodlandOwner.Id!.Value;
        application.CreatedById = Guid.NewGuid();

        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.SentForApproval }
        };

        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.FieldManager,
                AssignedUserId = (Guid)internalUser.UserAccountId!
            }
        };

        ArrangeDefaultMocks(application, woodlandOwner, userAccount);
        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));

        // Act
        var result = await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, internalUser, CancellationToken.None);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Id.Should().Be(application.Id);
        result.Value.UserCanApproveRefuseReferApplication.Should().BeTrue();
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotAllowUserToApprove_WhenLackingClaims(
    FellingLicenceApplication application, WoodlandOwnerModel woodlandOwner, UserAccount userAccount,
    Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(FloClaimTypes.UserCanApproveApplications, "false"),
            new Claim(FloClaimTypes.LocalAccountId, account.Id.ToString())
        };
        var internalUser = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")));

        application.WoodlandOwnerId = woodlandOwner.Id!.Value;

        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Created = DateTime.UtcNow.AddHours(-1), Status = FellingLicenceStatus.Submitted},
            new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.SentForApproval }
        };

        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.FieldManager,
                AssignedUserId = (Guid)internalUser.UserAccountId!
            }
        };

        ArrangeDefaultMocks(application, woodlandOwner, userAccount);
        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));

        // Act
        var result = await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, internalUser, CancellationToken.None);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Id.Should().Be(application.Id);
        result.Value.UserCanApproveRefuseReferApplication.Should().BeFalse();
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotAllowUserToApprove_WhenAlsoSubmittedApplication(
    FellingLicenceApplication application, WoodlandOwnerModel woodlandOwner, UserAccount userAccount,
    Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount externalAccount,
    Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(FloClaimTypes.UserCanApproveApplications, "true"),
            new Claim(FloClaimTypes.LocalAccountId, account.Id.ToString()),
            new Claim(FloClaimTypes.Email, externalAccount.Email.ToString())
        };
        var internalUser = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")));

        application.WoodlandOwnerId = woodlandOwner.Id!.Value;

        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Created = DateTime.UtcNow.AddHours(-1), Status = FellingLicenceStatus.Submitted, CreatedById = externalAccount.Id},
            new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.SentForApproval }
        };

        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.FieldManager,
                AssignedUserId = (Guid)internalUser.UserAccountId!
            }
        };

        ArrangeDefaultMocks(application, woodlandOwner, userAccount);
        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));
        _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalAccount);

        // Act
        var result = await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, internalUser, CancellationToken.None);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Id.Should().Be(application.Id);
        result.Value.ViewingUser.Should().Be(internalUser);
        result.Value.ViewingUser?.EmailAddress.Should().Be(externalAccount.Email);
        result.Value.UserCanApproveRefuseReferApplication.Should().BeFalse();
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotAllowUserToApprove_WhenUserIsNotFieldManager(
    FellingLicenceApplication application, WoodlandOwnerModel woodlandOwner, UserAccount userAccount,
    Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(FloClaimTypes.UserCanApproveApplications, "true"),
            new Claim(FloClaimTypes.LocalAccountId, account.Id.ToString())
        };
        var internalUser = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")));

        application.WoodlandOwnerId = woodlandOwner.Id!.Value;
        application.CreatedById = Guid.NewGuid();

        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.SentForApproval }
        };

        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.WoodlandOfficer,
                AssignedUserId = (Guid)internalUser.UserAccountId!
            }
        };

        ArrangeDefaultMocks(application, woodlandOwner, userAccount);
        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));

        // Act
        var result = await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, internalUser, CancellationToken.None);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Id.Should().Be(application.Id);
        result.Value.ViewingUser.Should().Be(internalUser);
        result.Value.UserCanApproveRefuseReferApplication.Should().BeFalse();
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotAllowUserToApprove_WhenApplicationIsNotEditable(
    FellingLicenceApplication application, WoodlandOwnerModel woodlandOwner, UserAccount userAccount,
    Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(FloClaimTypes.UserCanApproveApplications, "true"),
            new Claim(FloClaimTypes.LocalAccountId, account.Id.ToString())
        };
        var internalUser = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")));

        application.WoodlandOwnerId = woodlandOwner.Id!.Value;
        application.CreatedById = Guid.NewGuid();

        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.SentForApproval }
        };

        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                Role = AssignedUserRole.WoodlandOfficer,
                AssignedUserId = (Guid)internalUser.UserAccountId!
            }
        };

        ArrangeDefaultMocks(application, woodlandOwner, userAccount);
        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));

        // Act
        var result = await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, internalUser, CancellationToken.None);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Id.Should().Be(application.Id);
        result.Value.ViewingUser.Should().Be(internalUser);
        result.Value.UserCanApproveRefuseReferApplication.Should().BeFalse();
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotBeEditable_WhenApplicationIsApprovedOrRefused(
    FellingLicenceApplication application, WoodlandOwnerModel woodlandOwner, UserAccount userAccount,
    Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount account)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(FloClaimTypes.UserCanApproveApplications, "true"),
            new Claim(FloClaimTypes.LocalAccountId, account.Id.ToString())
        };
        var internalUser = new InternalUser(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")));

        application.WoodlandOwnerId = woodlandOwner.Id!.Value;
        application.CreatedById = Guid.NewGuid();

        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Created = DateTime.UtcNow.AddDays(-5), Status = FellingLicenceStatus.SentForApproval }, // some previous status
            new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.Approved }  // Latest status is 'Approved'
        };

        ArrangeDefaultMocks(application, woodlandOwner, userAccount);
        _userAccountService.Setup(r => r.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(account));

        // Act
        var result = await _sut.RetrieveFellingLicenceApplicationReviewSummaryAsync(application.Id, internalUser, CancellationToken.None);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Id.Should().Be(application.Id);
        result.Value.ViewingUser.Should().Be(internalUser);
        result.Value.UserCanApproveRefuseReferApplication.Should().BeFalse();
        result.Value.IsEditable.Should().BeFalse();
    }
}
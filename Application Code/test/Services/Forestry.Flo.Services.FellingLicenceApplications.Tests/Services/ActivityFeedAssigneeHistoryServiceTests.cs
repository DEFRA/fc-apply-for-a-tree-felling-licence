using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.User;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ActivityFeedAssigneeHistoryServiceTests
{
    private readonly Mock<InternalUsers.Repositories.IUserAccountRepository> _internalAccountsRepository = new();
    private readonly Mock<Applicants.Repositories.IUserAccountRepository> _externalAccountsRepository = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();

    [Theory, AutoMoqData]
    public async Task ShouldReturnExpectedItems(
        Guid applicationId,
        List<AssigneeHistory> assigneeHistories,
        List<InternalUsers.Entities.UserAccount.UserAccount> internalUsers,
        List<Applicants.Entities.UserAccount.UserAccount> externalApplicants)
    {
        var sut = CreateSut();

        //setup repositories
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assigneeHistories);
        _internalAccountsRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<InternalUsers.Entities.UserAccount.UserAccount>, UserDbErrorReason>(internalUsers));
        _externalAccountsRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Applicants.Entities.UserAccount.UserAccount>, UserDbErrorReason>(externalApplicants));

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = new[] { ActivityFeedItemType.AssigneeHistoryNotification }
        };
        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(assigneeHistories.Count, result.Value.Count);

        foreach (var assigneeHistory in assigneeHistories)
        {
            //can't override id in applicant/user entities
            string expectedText = "Application assigned to " + assigneeHistory.Role.GetDisplayName() + " (";
            if (assigneeHistory.Role == AssignedUserRole.Author || assigneeHistory.Role == AssignedUserRole.Applicant)
            {
                expectedText += "unknown applicant)";
            }
            else
            {
                expectedText += "unknown user)";
            }
            Assert.Contains(result.Value, x => 
                x.Text == expectedText
                && x.ActivityFeedItemType == ActivityFeedItemType.AssigneeHistoryNotification
                && x.VisibleToConsultee == false
                && x.VisibleToApplicant == true
                && x.FellingLicenceApplicationId == applicationId
                && x.CreatedTimestamp == assigneeHistory.TimestampAssigned);
        }

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        var expectedInternalIds = assigneeHistories
            .Where(x => x.Role != AssignedUserRole.Author && x.Role != AssignedUserRole.Applicant)
            .Select(x => x.AssignedUserId)
            .ToList();
        _internalAccountsRepository
            .Verify(x => x.GetUsersWithIdsInAsync(expectedInternalIds, It.IsAny<CancellationToken>()), Times.Once);
        _internalAccountsRepository.VerifyNoOtherCalls();
        var expectedExternalIds = assigneeHistories
            .Where(x => x.Role == AssignedUserRole.Author || x.Role == AssignedUserRole.Applicant)
            .Select(x => x.AssignedUserId)
            .ToList();
        _externalAccountsRepository
            .Verify(x => x.GetUsersWithIdsInAsync(expectedExternalIds, It.IsAny<CancellationToken>()));
        _externalAccountsRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenCannotRetrieveInternalAccounts(
        Guid applicationId,
        List<AssigneeHistory> assigneeHistories)
    {
        var sut = CreateSut();

        //setup repositories
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assigneeHistories);
        _internalAccountsRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<InternalUsers.Entities.UserAccount.UserAccount>, UserDbErrorReason>(UserDbErrorReason.General));

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = new[] { ActivityFeedItemType.AssigneeHistoryNotification }
        };
        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        var expectedInternalIds = assigneeHistories
            .Where(x => x.Role != AssignedUserRole.Author && x.Role != AssignedUserRole.Applicant)
            .Select(x => x.AssignedUserId)
            .ToList();
        _internalAccountsRepository
            .Verify(x => x.GetUsersWithIdsInAsync(expectedInternalIds, It.IsAny<CancellationToken>()), Times.Once);
        _internalAccountsRepository.VerifyNoOtherCalls();
        _externalAccountsRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenCannotRetrieveExternalAccounts(
    Guid applicationId,
    List<AssigneeHistory> assigneeHistories,
    List<InternalUsers.Entities.UserAccount.UserAccount> internalUsers)
    {
        var sut = CreateSut();

        //setup repositories
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assigneeHistories);
        _internalAccountsRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<InternalUsers.Entities.UserAccount.UserAccount>, UserDbErrorReason>(internalUsers));
        _externalAccountsRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<Applicants.Entities.UserAccount.UserAccount>, UserDbErrorReason>(UserDbErrorReason.General));

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = new[] { ActivityFeedItemType.AssigneeHistoryNotification }
        };
        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        var expectedInternalIds = assigneeHistories
            .Where(x => x.Role != AssignedUserRole.Author && x.Role != AssignedUserRole.Applicant)
            .Select(x => x.AssignedUserId)
            .ToList();
        _internalAccountsRepository
            .Verify(x => x.GetUsersWithIdsInAsync(expectedInternalIds, It.IsAny<CancellationToken>()), Times.Once);
        _internalAccountsRepository.VerifyNoOtherCalls();
        var expectedExternalIds = assigneeHistories
            .Where(x => x.Role == AssignedUserRole.Author || x.Role == AssignedUserRole.Applicant)
            .Select(x => x.AssignedUserId)
            .ToList();
        _externalAccountsRepository
            .Verify(x => x.GetUsersWithIdsInAsync(expectedExternalIds, It.IsAny<CancellationToken>()));
        _externalAccountsRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public void ShouldSupportCorrectItemTypes()
    {
        var sut = CreateSut();
        var result = sut.SupportedItemTypes();
        Assert.Equal(new[] {ActivityFeedItemType.AssigneeHistoryNotification}, result);
    }

    private ActivityFeedAssigneeHistoryService CreateSut()
    {
        _internalAccountsRepository.Reset();
        _externalAccountsRepository.Reset();
        _fellingLicenceApplicationRepository.Reset();

        return new ActivityFeedAssigneeHistoryService(
            _fellingLicenceApplicationRepository.Object,
            _internalAccountsRepository.Object,
            _externalAccountsRepository.Object,
            new NullLogger<ActivityFeedAssigneeHistoryService>());
    }
}
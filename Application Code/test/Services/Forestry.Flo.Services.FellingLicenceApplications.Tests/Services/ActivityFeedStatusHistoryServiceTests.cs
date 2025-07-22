using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ActivityFeedStatusHistoryServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();

    [Theory, AutoMoqData]
    public async Task ShouldReturnExpectedItems(
        Guid applicationId,
        List<StatusHistory> statusHistories)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statusHistories);

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = new[] { ActivityFeedItemType.StatusHistoryNotification }
        };

        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(statusHistories.Count, result.Value.Count);

        foreach (var statusHistory in statusHistories)
        {
            var expectedText = "Application status set to " + statusHistory.Status.GetDisplayNameByActorType(ActorType.InternalUser);
            Assert.Contains(result.Value, x =>
                x.ActivityFeedItemType == ActivityFeedItemType.StatusHistoryNotification
                && x.VisibleToApplicant == true
                && x.VisibleToConsultee == false
                && x.CreatedTimestamp == statusHistory.Created
                && x.Text == expectedText);
        }
    }

    [Fact]
    public void ShouldSupportCorrectItemTypes()
    {
        var sut = CreateSut();
        var result = sut.SupportedItemTypes();
        Assert.Equal(new[] {ActivityFeedItemType.StatusHistoryNotification}, result);
    }

    private ActivityFeedStatusHistoryService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();

        return new ActivityFeedStatusHistoryService(
            _fellingLicenceApplicationRepository.Object,
            new NullLogger<ActivityFeedStatusHistoryService>());
    }
}
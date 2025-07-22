using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Repositories;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.Notifications.Tests;

public class NotificationHistoryRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task GetForApplicationReturnsEmptyWhenNoneExist(string applicationReference)
    {
        var sut = CreateSut();

        var result = await sut.GetNotificationHistoryForApplicationAsync(
            applicationReference,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task GetForApplicationReturnsEmptyWhenNoneExistForGivenId(
        NotificationHistory existingItem,
        string applicationReferenceSought)
    {
        var sut = CreateSut();

        sut.Add(existingItem);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        var result = await sut.GetNotificationHistoryForApplicationAsync(
            applicationReferenceSought,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task GetForApplicationReturnsItemWhenNoFilteringApplied(
        NotificationHistory existingItem)
    {
        var sut = CreateSut();

        sut.Add(existingItem);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        var result = await sut.GetNotificationHistoryForApplicationAsync(
            existingItem.ApplicationReference!,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingItem, result.Value.Single());
    }

    [Theory, AutoMoqData]
    public async Task GetForApplicationReturnsNoItemWhenFilteredOut(
        NotificationHistory existingItem)
    {
        var sut = CreateSut();

        existingItem.NotificationType = NotificationType.ApplicationResubmitted;

        sut.Add(existingItem);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        var result = await sut.GetNotificationHistoryForApplicationAsync(
            existingItem.ApplicationReference!,
            new [] { NotificationType.ExternalConsulteeInvite },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task GetForApplicationReturnsSpecificItemWhenFilteringApplied(
        NotificationHistory existingItem1,
        NotificationHistory existingItem2)
    {
        var sut = CreateSut();

        existingItem1.NotificationType = NotificationType.ApplicationResubmitted;
        existingItem2.ApplicationReference = existingItem1.ApplicationReference;
        existingItem2.NotificationType = NotificationType.ApplicationSubmissionConfirmation;

        sut.Add(existingItem1);
        sut.Add(existingItem2);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        var result = await sut.GetNotificationHistoryForApplicationAsync(
            existingItem1.ApplicationReference!,
            new [] { NotificationType.ApplicationResubmitted },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingItem1, result.Value.Single());
    }

    private NotificationHistoryRepository CreateSut()
    {
        var context = TestNotificationHistoryDatabaseFactory.CreateDefaultTestContext();
        return new NotificationHistoryRepository(context);
    }
}
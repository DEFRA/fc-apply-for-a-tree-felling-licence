using System;
using System.Linq;
using System.Reflection;
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
    public async Task GetForApplicationReturnsEmptyWhenNoneExist(Guid applicationId)
    {
        var sut = CreateSut();

        var result = await sut.GetNotificationHistoryForApplicationAsync(
            applicationId,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetForApplicationThrowsWithEmptyGuid()
    {
        var applicationId = Guid.Empty;
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(async () => await sut.GetNotificationHistoryForApplicationAsync(
            applicationId,
            null,
            CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task GetForApplicationReturnsEmptyWhenNoneExistForGivenId(
        NotificationHistory existingItem,
        Guid applicationId)
    {
        var sut = CreateSut();

        sut.Add(existingItem);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        var result = await sut.GetNotificationHistoryForApplicationAsync(
            applicationId,
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
            existingItem.ApplicationId!.Value,
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
            existingItem.ApplicationId!.Value,
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
            existingItem1.ApplicationId!.Value,
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
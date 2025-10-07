using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Repositories;
using Forestry.Flo.Tests.Common;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common;
using Xunit;

namespace Forestry.Flo.Services.Notifications.Tests;

public class NotificationHistoryRepositoryTests
{
    [Theory, AutoMoqData]
    public async Task GetByIdWhenIdIsUnknown(
        NotificationHistory existingEntity,
        Guid unknownId)
    {
        var sut = CreateSut();
        sut.Add(existingEntity);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        var result = await sut.GetByIdAsync(unknownId, CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }

    [Theory, AutoMoqData]
    public async Task GetByIdWhenIdIsKnown(
        NotificationHistory existingEntity)
    {
        var sut = CreateSut();
        sut.Add(existingEntity);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
     
        var result = await sut.GetByIdAsync(existingEntity.Id, CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(existingEntity, result.Value);
    }

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

    [Theory, AutoMoqData]
    public async Task GetExistingExternalIdsWhenNoIdsInRequest(
        NotificationHistory existingItem)
    {
        var sut = CreateSut();
        sut.Add(existingItem);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        var result = await sut.GetExistingExternalIdsAsync([], CancellationToken.None);

        Assert.Empty(result);
    }

    [Theory, AutoMoqData]
    public async Task GetExistingExternalIdsWhenNoMatchingIds(
        NotificationHistory existingItem,
        Guid unknownExternalId)
    {
        var sut = CreateSut();
        sut.Add(existingItem);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
        
        var result = await sut.GetExistingExternalIdsAsync([unknownExternalId], CancellationToken.None);
        
        Assert.Empty(result);
    }

    [Theory, AutoMoqData]
    public async Task GetExistingExternalIdsWhenSomeMatchingIds(
        NotificationHistory existingItem,
        Guid unknownExternalId)
    {
        var sut = CreateSut();
        sut.Add(existingItem);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
        
        var result = await sut.GetExistingExternalIdsAsync([existingItem.ExternalId!.Value, unknownExternalId], CancellationToken.None);
        
        Assert.Single(result);
        Assert.Contains(existingItem.ExternalId!.Value, result);
    }

    [Theory, AutoMoqData]
    public async Task UpdateByIdWhenNoEntityFoundForId(
        NotificationHistory existingItem,
        string existingText,
        string updatedText,
        Guid unknownId)
    {
        var sut = CreateSut();
        existingItem.Text = existingText;
        sut.Add(existingItem);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        var result = await sut.UpdateByIdAsync(unknownId, nh =>
        {
            nh.Text = updatedText;
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);

        Assert.Equal(existingText, existingItem.Text);
    }

    [Theory, AutoMoqData]
    public async Task UpdateByIdWhenEntityFoundForId(
        NotificationHistory existingItem,
        string existingText,
        string updatedText)
    {
        var sut = CreateSut();
        existingItem.Text = existingText;
        sut.Add(existingItem);
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        var result = await sut.UpdateByIdAsync(existingItem.Id, nh =>
        {
            nh.Text = updatedText;
        }, CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(existingItem, result.Value);
        Assert.Equal(updatedText, existingItem.Text);
    }

    private NotificationHistoryRepository CreateSut()
    {
        var context = TestNotificationHistoryDatabaseFactory.CreateDefaultTestContext();
        return new NotificationHistoryRepository(context);
    }
}
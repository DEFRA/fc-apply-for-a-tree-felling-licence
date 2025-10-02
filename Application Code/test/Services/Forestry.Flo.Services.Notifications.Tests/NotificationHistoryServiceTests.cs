using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Repositories;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Forestry.Flo.Services.Notifications.Tests;

public class NotificationHistoryServiceTests
{
    private Mock<INotificationHistoryRepository> _repository = new Mock<INotificationHistoryRepository>();

    [Theory, AutoData] 
    public async Task ShouldReturnFailureWhenRepositoryReturnsFailure(
        Guid applicationId,
        NotificationType[]? typeFilter)
    {
        var sut = CreateSut();
        _repository.Setup(x => x.GetNotificationHistoryForApplicationAsync(
                It.IsAny<Guid>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<NotificationHistory>>("error"));

        var result =
            await sut.RetrieveNotificationHistoryAsync(applicationId, typeFilter, CancellationToken.None);

        Assert.True(result.IsFailure);

        _repository.Verify(x => x.GetNotificationHistoryForApplicationAsync(applicationId, typeFilter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnEmptyListWhenRepositoryReturnsEmptyList(
        Guid applicationId,
        NotificationType[]? typeFilter)
    {
        var sut = CreateSut();
        _repository.Setup(x => x.GetNotificationHistoryForApplicationAsync(
                It.IsAny<Guid>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<NotificationHistory>(0)));

        var result =
            await sut.RetrieveNotificationHistoryAsync(applicationId, typeFilter, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        _repository.Verify(x => x.GetNotificationHistoryForApplicationAsync(applicationId, typeFilter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnExpectedItemsWhenRepositoryReturnsItems(
        Guid applicationId,
        NotificationType[]? typeFilter,
        NotificationHistory entry1,
        List<NotificationRecipient> entry1recipients,
        NotificationHistory entry2)
    {
        var sut = CreateSut();

        entry1.Recipients = JsonConvert.SerializeObject(entry1recipients);
        entry2.Recipients = JsonConvert.SerializeObject(new List<NotificationRecipient>(0));

        _repository.Setup(x => x.GetNotificationHistoryForApplicationAsync(
                It.IsAny<Guid>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<NotificationHistory> { entry1, entry2 }));

        var result =
            await sut.RetrieveNotificationHistoryAsync(applicationId, typeFilter, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        _repository.Verify(x => x.GetNotificationHistoryForApplicationAsync(applicationId, typeFilter, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Contains(result.Value, x =>
            x.Type == entry1.NotificationType
            && x.Recipients.All(y => entry1recipients.Contains(y))
            && x.CreatedTimestamp == entry1.CreatedTimestamp
            && x.Source == entry1.Source
            && x.Text == entry1.Text);

        Assert.Contains(result.Value, x =>
            x.Type == entry2.NotificationType
            && (x.Recipients?.Count() ?? 0) == 0
            && x.CreatedTimestamp == entry2.CreatedTimestamp
            && x.Source == entry2.Source
            && x.Text == entry2.Text);
    }

    [Theory, AutoData]
    public async Task GetNotificationHistoryByIdWhenNoItemExistsForId(Guid unknownId)
    {
        var sut = CreateSut();

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<NotificationHistory, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.GetNotificationHistoryByIdAsync(unknownId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound.GetDescription(), result.Error);

        _repository.Verify(x => x.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task GetNotificationHistoryByIdWhenItemExists(
        NotificationHistory entry,
        Guid knownId,
        List<NotificationRecipient> entryRecipients)
    {
        var sut = CreateSut();
        entry.Recipients = JsonConvert.SerializeObject(entryRecipients);

        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<NotificationHistory, UserDbErrorReason>(entry));
        
        var result = await sut.GetNotificationHistoryByIdAsync(knownId, CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(entry.Id, result.Value.Id);
        Assert.Equal(entry.NotificationType, result.Value.Type);
        Assert.Equal(entry.Text, result.Value.Text);
        Assert.Equal(entry.Source, result.Value.Source);
        Assert.Equal(entry.CreatedTimestamp, result.Value.CreatedTimestamp);
        Assert.Equal(entryRecipients.Count, result.Value.Recipients?.Count() ?? 0);
        Assert.Equivalent(entryRecipients, result.Value.Recipients);
        Assert.Equal(entry.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(entry.ApplicationId, result.Value.ApplicationId);
        Assert.Equal(entry.ExternalId, result.Value.ExternalId);
        Assert.Equal(entry.Status, result.Value.Status);
        Assert.Equal(entry.Response, result.Value.Response);
        Assert.Equal(entry.LastUpdatedById, result.Value.LastUpdatedById);
        Assert.Equal(entry.LastUpdatedDate, result.Value.LastUpdatedDate);

        _repository.Verify(x => x.GetByIdAsync(knownId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task UpdateResponseStatusByIdWhenRepositoryReturnsFailure(
        Guid id,
        NotificationStatus newStatus,
        string response,
        Guid lastUpdatedById,
        DateTime lastUpdatedDate)
    {
        var sut = CreateSut();

        _repository
            .Setup(x => x.UpdateByIdAsync(It.IsAny<Guid>(), It.IsAny<Action<NotificationHistory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<NotificationHistory, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.UpdateResponseStatusByIdAsync(id, newStatus, response, lastUpdatedById, lastUpdatedDate, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound.GetDescription(), result.Error);

        _repository.Verify(x => x.UpdateByIdAsync(id, It.IsAny<Action<NotificationHistory>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task UpdateResponseStatusByIdWhenRepositoryReturnsSuccess(
        Guid id,
        NotificationStatus newStatus,
        string response,
        Guid lastUpdatedById,
        DateTime lastUpdatedDate,
        NotificationHistory existingItem)
    {
        var sut = CreateSut();

        Action<NotificationHistory>? capturedUpdate = null;

        _repository
            .Setup(x => x.UpdateByIdAsync(It.IsAny<Guid>(), It.IsAny<Action<NotificationHistory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<NotificationHistory, UserDbErrorReason>(existingItem))
            .Callback((Guid _, Action<NotificationHistory> y, CancellationToken _) => capturedUpdate = y);

        var result = await sut.UpdateResponseStatusByIdAsync(id, newStatus, response, lastUpdatedById, lastUpdatedDate, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _repository.Verify(x => x.UpdateByIdAsync(id, It.IsAny<Action<NotificationHistory>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();

        capturedUpdate.Invoke(existingItem);
        Assert.Equal(newStatus, existingItem.Status);
        Assert.Equal(response, existingItem.Response);
        Assert.Equal(lastUpdatedById, existingItem.LastUpdatedById);
        Assert.Equal(lastUpdatedDate, existingItem.LastUpdatedDate);
    }

    private NotificationHistoryService CreateSut()
    {
        _repository.Reset();

        return new NotificationHistoryService(
            _repository.Object,
            new NullLogger<NotificationHistoryService>());
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
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
        string applicationReference,
        NotificationType[]? typeFilter)
    {
        var sut = CreateSut();
        _repository.Setup(x => x.GetNotificationHistoryForApplicationAsync(
                It.IsAny<string>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<NotificationHistory>>("error"));

        var result =
            await sut.RetrieveNotificationHistoryAsync(applicationReference, typeFilter, CancellationToken.None);

        Assert.True(result.IsFailure);

        _repository.Verify(x => x.GetNotificationHistoryForApplicationAsync(applicationReference, typeFilter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnEmptyListWhenRepositoryReturnsEmptyList(
        string applicationReference,
        NotificationType[]? typeFilter)
    {
        var sut = CreateSut();
        _repository.Setup(x => x.GetNotificationHistoryForApplicationAsync(
                It.IsAny<string>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<NotificationHistory>(0)));

        var result =
            await sut.RetrieveNotificationHistoryAsync(applicationReference, typeFilter, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        _repository.Verify(x => x.GetNotificationHistoryForApplicationAsync(applicationReference, typeFilter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnExpectedItemsWhenRepositoryReturnsItems(
        string applicationReference,
        NotificationType[]? typeFilter,
        NotificationHistory entry1,
        List<NotificationRecipient> entry1recipients,
        NotificationHistory entry2)
    {
        var sut = CreateSut();

        entry1.Recipients = JsonConvert.SerializeObject(entry1recipients);
        entry2.Recipients = JsonConvert.SerializeObject(new List<NotificationRecipient>(0));

        _repository.Setup(x => x.GetNotificationHistoryForApplicationAsync(
                It.IsAny<string>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<NotificationHistory> { entry1, entry2 }));

        var result =
            await sut.RetrieveNotificationHistoryAsync(applicationReference, typeFilter, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        _repository.Verify(x => x.GetNotificationHistoryForApplicationAsync(applicationReference, typeFilter, It.IsAny<CancellationToken>()), Times.Once);

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

    private NotificationHistoryService CreateSut()
    {
        _repository.Reset();

        return new NotificationHistoryService(
            _repository.Object,
            new NullLogger<NotificationHistoryService>());
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Repositories;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.Notifications.Tests;

public class NotificationHistoryServiceAddListTests
{
    private readonly Mock<INotificationHistoryRepository> _repository = new();
    private NotificationHistoryService CreateSut() => new NotificationHistoryService(_repository.Object, new NullLogger<NotificationHistoryService>());

    [Fact]
    public async Task AddsAll_WhenNoDuplicates()
    {
        var models = new List<NotificationHistoryModel>
        {
            new NotificationHistoryModel { ExternalId = Guid.NewGuid(), CreatedTimestamp = DateTime.UtcNow, Type = NotificationType.PublicRegisterComment, Text = "A" },
            new NotificationHistoryModel { ExternalId = Guid.NewGuid(), CreatedTimestamp = DateTime.UtcNow, Type = NotificationType.PublicRegisterComment, Text = "B" }
        };
        _repository.Setup(r => r.GetExistingExternalIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid>());
        _repository.Setup(r => r.Add(It.IsAny<NotificationHistory>())).Returns((NotificationHistory nh) => nh);
        _repository.Setup(r => r.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var sut = CreateSut();
        var result = await sut.AddNotificationHistoryListAsync(models, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.Add(It.IsAny<NotificationHistory>()), Times.Exactly(2));
        _repository.Verify(r => r.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SkipsDuplicates_ByExternalId()
    {
        var duplicateId = Guid.NewGuid();
        var models = new List<NotificationHistoryModel>
        {
            new NotificationHistoryModel { ExternalId = duplicateId, CreatedTimestamp = DateTime.UtcNow, Type = NotificationType.PublicRegisterComment, Text = "A" },
            new NotificationHistoryModel { ExternalId = Guid.NewGuid(), CreatedTimestamp = DateTime.UtcNow, Type = NotificationType.PublicRegisterComment, Text = "B" }
        };
        _repository.Setup(r => r.GetExistingExternalIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid> { duplicateId });
        _repository.Setup(r => r.Add(It.IsAny<NotificationHistory>())).Returns((NotificationHistory nh) => nh);
        _repository.Setup(r => r.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();
        var result = await sut.AddNotificationHistoryListAsync(models, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.Add(It.IsAny<NotificationHistory>()), Times.Exactly(1));
        _repository.Verify(r => r.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddsNone_WhenAllAreDuplicates()
    {
        var duplicateId1 = Guid.NewGuid();
        var duplicateId2 = Guid.NewGuid();
        var models = new List<NotificationHistoryModel>
        {
            new NotificationHistoryModel { ExternalId = duplicateId1, CreatedTimestamp = DateTime.UtcNow, Type = NotificationType.PublicRegisterComment, Text = "A" },
            new NotificationHistoryModel { ExternalId = duplicateId2, CreatedTimestamp = DateTime.UtcNow, Type = NotificationType.PublicRegisterComment, Text = "B" }
        };
        _repository.Setup(r => r.GetExistingExternalIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Guid> { duplicateId1, duplicateId2 });
        _repository.Setup(r => r.Add(It.IsAny<NotificationHistory>())).Returns((NotificationHistory nh) => nh);
        _repository.Setup(r => r.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var sut = CreateSut();
        var result = await sut.AddNotificationHistoryListAsync(models, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.Add(It.IsAny<NotificationHistory>()), Times.Never);
        _repository.Verify(r => r.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddsAll_WhenNoExternalIds()
    {
        var models = new List<NotificationHistoryModel>
        {
            new NotificationHistoryModel { ExternalId = null, CreatedTimestamp = DateTime.UtcNow, Type = NotificationType.PublicRegisterComment, Text = "A" },
            new NotificationHistoryModel { ExternalId = null, CreatedTimestamp = DateTime.UtcNow, Type = NotificationType.PublicRegisterComment, Text = "B" }
        };
        _repository.Setup(r => r.Add(It.IsAny<NotificationHistory>())).Returns((NotificationHistory nh) => nh);
        _repository.Setup(r => r.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var sut = CreateSut();
        var result = await sut.AddNotificationHistoryListAsync(models, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.Add(It.IsAny<NotificationHistory>()), Times.Exactly(2));
        _repository.Verify(r => r.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnsFailureWhenRepositoryThrows()
    {
        var models = new List<NotificationHistoryModel>
        {
            new NotificationHistoryModel { ExternalId = Guid.NewGuid(), CreatedTimestamp = DateTime.UtcNow, Type = NotificationType.PublicRegisterComment, Text = "A" },
            new NotificationHistoryModel { ExternalId = Guid.NewGuid(), CreatedTimestamp = DateTime.UtcNow, Type = NotificationType.PublicRegisterComment, Text = "B" }
        };
        _repository
            .Setup(r => r.GetExistingExternalIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var sut = CreateSut();
        var result = await sut.AddNotificationHistoryListAsync(models, CancellationToken.None);

        Assert.False(result.IsSuccess);
        _repository.Verify(r => r.GetExistingExternalIdsAsync(It.Is<IEnumerable<Guid>>(e => e.Count() == 2 && e.Contains(models.First().ExternalId.Value) && e.Contains(models.Last().ExternalId.Value) ), It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
    }
}

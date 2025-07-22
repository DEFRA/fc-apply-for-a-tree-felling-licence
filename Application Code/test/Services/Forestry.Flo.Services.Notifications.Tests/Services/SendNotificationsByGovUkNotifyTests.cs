using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Notifications.Configuration;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Repositories;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using Notify.Interfaces;
using Notify.Models.Responses;
using Xunit;

namespace Forestry.Flo.Services.Notifications.Tests.Services;

public class SendNotificationsByGovUkNotifyTests
{
    private readonly Mock<IAsyncNotificationClient> _mockClient = new();
    private readonly Mock<INotificationHistoryRepository> _mockNotificationHistoryRepository = new();
    private readonly Mock<IUnitOfWork> _mockNotificationsUow = new();


    [Theory, AutoData]
    public async Task SendsUsingCorrectTemplateId(
        GovUkNotifyOptions options, 
        ConditionsToApplicantDataModel model,
        NotificationRecipient recipient)
    {
        var sut = CreateSut(options);

        var expectedTemplateId = options.TemplateIds.Last();

        _mockClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new EmailNotificationResponse());

        _mockNotificationHistoryRepository.Setup(x => x.Add(It.IsAny<NotificationHistory>()))
            .Returns(new NotificationHistory());

        _mockNotificationsUow.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        await sut.SendNotificationAsync(
            model,
            expectedTemplateId.Key,
            [recipient]);

        _mockClient.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(),
            expectedTemplateId.Value,
            It.IsAny<Dictionary<string, dynamic>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SendsToExpectedRecipientAddress(
        GovUkNotifyOptions options,
        ConditionsToApplicantDataModel model,
        NotificationRecipient recipient)
    {
        var sut = CreateSut(options);

        var expectedTemplateId = options.TemplateIds.Last();

        _mockClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new EmailNotificationResponse());

        _mockNotificationHistoryRepository.Setup(x => x.Add(It.IsAny<NotificationHistory>()))
            .Returns(new NotificationHistory());

        _mockNotificationsUow.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        await sut.SendNotificationAsync(
            model,
            expectedTemplateId.Key,
            [recipient]);

        _mockClient.Verify(x => x.SendEmailAsync(
            recipient.Address,
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, dynamic>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SendsToExpectedMultipleRecipients(
        GovUkNotifyOptions options,
        ConditionsToApplicantDataModel model,
        NotificationRecipient recipient,
        NotificationRecipient[] copyTos)
    {
        var sut = CreateSut(options);

        var expectedTemplateId = options.TemplateIds.Last();

        _mockClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new EmailNotificationResponse());

        _mockNotificationHistoryRepository.Setup(x => x.Add(It.IsAny<NotificationHistory>()))
            .Returns(new NotificationHistory());

        _mockNotificationsUow.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        await sut.SendNotificationAsync(
            model,
            expectedTemplateId.Key,
            [recipient],
            copyTos);

        _mockClient.Verify(x => x.SendEmailAsync(
            recipient.Address,
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, dynamic>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);

        foreach (var copyTo in copyTos)
        {
            _mockClient.Verify(x => x.SendEmailAsync(
                copyTo.Address,
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()), Times.Once);
        }
    }

    [Theory, AutoData]
    public async Task SendsExpectedPersonalisation(
        GovUkNotifyOptions options,
        ConditionsToApplicantDataModel model,
        NotificationRecipient recipient)
    {
        var sut = CreateSut(options);

        var expectedTemplateId = options.TemplateIds.Last();

        var expectedContent = new Dictionary<string, dynamic>()
        {
            { "ApplicationReference", model.ApplicationReference },
            { "ConditionsText", model.ConditionsText },
            { "Name", model.Name },
            { "PropertyName", model.PropertyName },
            { "SenderEmail", model.SenderEmail },
            { "SenderName", model.SenderName },
            { "ViewApplicationURL", model.ViewApplicationURL },
            { "WoodlandOwnerName", model.WoodlandOwnerName },
            { "AdminHubFooter", model.AdminHubFooter },
            { "HasAttachments", false },
            { "Attachment1", string.Empty },
            { "Attachment2", string.Empty },
            { "Attachment3", string.Empty },
            { "Attachment4", string.Empty },
            { "Attachment5", string.Empty },
        };

        _mockClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new EmailNotificationResponse());

        _mockNotificationHistoryRepository.Setup(x => x.Add(It.IsAny<NotificationHistory>()))
            .Returns(new NotificationHistory());

        _mockNotificationsUow.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        await sut.SendNotificationAsync(
            model,
            expectedTemplateId.Key,
            [recipient]);

        _mockClient.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<Dictionary<string, dynamic>>(m => AllMatch(expectedContent, m)),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);

    }

    private bool AllMatch(Dictionary<string, dynamic> expected, Dictionary<string, dynamic> actual)
    {
        if (expected.Count != actual.Count) return false;

        foreach (var kvp in expected)
        {
            if (!actual.TryGetValue(kvp.Key, out var value) || !value.Equals(kvp.Value))
            {
                return false;
            }
        }

        return true;
    }

    private SendNotificationsByGovUkNotify CreateSut(GovUkNotifyOptions options)
    {
        _mockClient.Reset();
        _mockNotificationHistoryRepository.Reset();
        _mockNotificationsUow.Reset();
        _mockNotificationHistoryRepository.SetupGet(x => x.UnitOfWork).Returns(_mockNotificationsUow.Object);

        return new SendNotificationsByGovUkNotify(
            new OptionsWrapper<GovUkNotifyOptions>(options),
            _mockNotificationHistoryRepository.Object,
            _mockClient.Object,
            SystemClock.Instance,
            new NullLogger<SendNotificationsByGovUkNotify>());

    }
}
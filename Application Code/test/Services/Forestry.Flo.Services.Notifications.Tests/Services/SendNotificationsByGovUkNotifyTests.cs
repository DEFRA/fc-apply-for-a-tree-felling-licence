using System;
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
using Newtonsoft.Json;
using NodaTime;
using Notify.Exceptions;
using Notify.Interfaces;
using Notify.Models.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.Notifications.Tests.Services;

public class SendNotificationsByGovUkNotifyTests
{
    private readonly Mock<IAsyncNotificationClient> _mockClient = new();
    private readonly Mock<INotificationHistoryRepository> _mockNotificationHistoryRepository = new();
    private readonly Mock<IUnitOfWork> _mockNotificationsUow = new();



    [Theory, AutoData]
    public async Task SendWhenClientThrowsException(
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
            .ThrowsAsync(new NotifyClientException());

        var result = await sut.SendNotificationAsync(
            model,
            expectedTemplateId.Key,
            [recipient]);

        Assert.True(result.IsFailure);

        _mockClient.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(),
            expectedTemplateId.Value,
            It.IsAny<Dictionary<string, dynamic>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);

        _mockNotificationHistoryRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task SendWithNoRecipients(
        GovUkNotifyOptions options,
        ConditionsToApplicantDataModel model)
    {
        var sut = CreateSut(options);

        var expectedTemplateId = options.TemplateIds.Last();

        await Assert.ThrowsAsync<ArgumentException>(async () => await sut.SendNotificationAsync(
            model,
            expectedTemplateId.Key,
            []));

        _mockClient.VerifyNoOtherCalls();
        _mockNotificationHistoryRepository.VerifyNoOtherCalls();
    }

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

        _mockNotificationHistoryRepository.Verify(x => x.Add(It.Is<NotificationHistory>(n =>
            n.ApplicationReference == model.ApplicationReference
            && n.NotificationType == expectedTemplateId.Key
            && n.Recipients == JsonConvert.SerializeObject(new List<NotificationRecipient> { recipient })
            && n.Text == JsonConvert.SerializeObject(model)
        )), Times.Once);
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

        List<NotificationRecipient> expectedRecipients = [recipient, ..copyTos];
        _mockNotificationHistoryRepository.Verify(x => x.Add(It.Is<NotificationHistory>(n =>
            n.ApplicationReference == model.ApplicationReference
            && n.NotificationType == expectedTemplateId.Key
            && n.Recipients == JsonConvert.SerializeObject(expectedRecipients)
            && n.Text == JsonConvert.SerializeObject(model)
        )), Times.Once);
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
            { "ApplicationId", model.ApplicationId },
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

        _mockNotificationHistoryRepository.Verify(x => x.Add(It.Is<NotificationHistory>(n =>
            n.ApplicationReference == model.ApplicationReference
            && n.NotificationType == expectedTemplateId.Key
            && n.Recipients == JsonConvert.SerializeObject(new List<NotificationRecipient> {recipient})
            && n.Text == JsonConvert.SerializeObject(model)
            )), Times.Once);
    }

    [Theory, AutoData]
    public async Task CreatesExpectedContent(
        GovUkNotifyOptions options,
        ConditionsToApplicantDataModel model,
        NotificationAttachment attachment,
        TemplatePreviewResponse response)
    {
        var sut = CreateSut(options);

        var expectedTemplateId = options.TemplateIds.Last();

        _mockClient
            .Setup(x => x.GenerateTemplatePreviewAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()))
            .ReturnsAsync(response);

        var result = await sut.CreateNotificationContentAsync(
            model,
            expectedTemplateId.Key,
            [ attachment ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(response.body, result.Value);

        _mockClient.Verify(x => x.GenerateTemplatePreviewAsync(
            expectedTemplateId.Value,
            It.IsAny<Dictionary<string, dynamic>>()), Times.Once);
        _mockClient.VerifyNoOtherCalls();
        _mockNotificationHistoryRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task GenerateTemplatePreviewWhenClientThrowsException(
        GovUkNotifyOptions options,
        ConditionsToApplicantDataModel model,
        NotificationAttachment attachment)
    {
        var sut = CreateSut(options);

        var expectedTemplateId = options.TemplateIds.Last();

        _mockClient
            .Setup(x => x.GenerateTemplatePreviewAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()))
            .ThrowsAsync(new NotifyClientException());

        var result = await sut.CreateNotificationContentAsync(
            model,
            expectedTemplateId.Key,
            [attachment]);

        Assert.True(result.IsFailure);

        _mockClient.Verify(x => x.GenerateTemplatePreviewAsync(
            expectedTemplateId.Value,
            It.IsAny<Dictionary<string, dynamic>>()), Times.Once);
        _mockClient.VerifyNoOtherCalls();
        _mockNotificationHistoryRepository.VerifyNoOtherCalls();
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Notifications.Configuration;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Repositories;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace Forestry.Flo.Services.Notifications.Tests;

public class EmailServiceTests
{
    private readonly Fixture _fixture = new();
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly Mock<INotificationHistoryRepository> _notificationHistoryRepository;
    private readonly IClock _clock;
    private readonly Mock<ITemplateRenderer> _templateRenderer;
    private readonly Mock<ISender> _sender;
    private readonly ILogger<EmailService> _logger;
    private readonly Mock<IOptions<NotificationsOptions>> _options;
    private readonly EmailService _sut;
    private readonly Mock<IUnitOfWork> _unitOfWOrkMock;
    private readonly NotificationsOptions _notificationsOptions;

    public EmailServiceTests()
    {
        _notificationsOptions = _fixture.Create<NotificationsOptions>();
        _notificationHistoryRepository = new Mock<INotificationHistoryRepository>();
        _clock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
        _templateRenderer = new Mock<ITemplateRenderer>();
        _sender = new Mock<ISender>();
        _logger = new NullLogger<EmailService>();
        _options = new Mock<IOptions<NotificationsOptions>>();
        _options.SetupGet(c => c.Value).Returns(_notificationsOptions);
        _sut = new EmailService(_templateRenderer.Object, _sender.Object, _options.Object, _notificationHistoryRepository.Object, _clock, _logger);

        _unitOfWOrkMock = new Mock<IUnitOfWork>();
        _notificationHistoryRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
    }

    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmail_WhenInviteWoodlandOwner_GivenValidNotificationDetails(
        InviteWoodlandOwnerToOrganisationDataModel model,
        NotificationRecipient notificationRecipient, 
        NotificationRecipient[] notificationRecipients,
        string senderName)
    {
        //arrange
        const NotificationType notificationType = NotificationType.InviteWoodlandOwnerUserToOrganisation;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        
        VerifyEmailSent(expectedSubject, new [] { notificationRecipient }, notificationRecipients);
    }

    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmail_WhenInternalAccountApproved(
        InformInternalUserOfAccountApprovalDataModel model,
        NotificationRecipient notificationRecipient,
        NotificationRecipient[] notificationRecipients,
        string senderName)
    {
        //arrange
        const NotificationType notificationType = NotificationType.InformInternalUserOfAccountApproval;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken: CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();

        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients);
    }

    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmail_WhenApplicationResubmitted_GivenValidNotificationDetails(ApplicationResubmittedDataModel model,
        NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients,string senderName, string messageId)
    {
        //arrange
        const NotificationType notificationType = NotificationType.ApplicationResubmitted;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmail_WhenApplicationSubmissionConfirmed_GivenValidNotificationDetails(ApplicationSubmissionConfirmationDataModel model,
        NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients,string senderName, string messageId)
    {
        //arrange
        const NotificationType notificationType = NotificationType.ApplicationSubmissionConfirmation;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmail_WhenExternalConsulteeInvited_GivenValidNotificationDetails(ExternalConsulteeInviteDataModel model,
        NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients,string senderName, string messageId)
    {
        //arrange
        const NotificationType notificationType = NotificationType.ExternalConsulteeInvite;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmail_WhenApplicationReturned_GivenValidNotificationDetails(InformApplicantOfReturnedApplicationDataModel model,
        NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients,string senderName, string messageId)
    {
        //arrange
        const NotificationType notificationType = NotificationType.InformApplicantOfReturnedApplication;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmail_WhenInformFCStaffOfReturnedApplication_GivenValidNotificationDetails(InformFCStaffOfReturnedApplicationDataModel model,
        NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients,string senderName, string messageId)
    {
        //arrange
        const NotificationType notificationType = NotificationType.InformFCStaffOfReturnedApplication;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmail_WhenInformWoodlandOfficerOfAdminOfficerReviewCompletion_GivenValidNotificationDetails(InformAssignedUserOfApplicationStatusTransitionDataModel model,
        NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients,string senderName, string messageId)
    {
        //arrange
        const NotificationType notificationType = NotificationType.InformWoodlandOfficerOfAdminOfficerReviewCompletion;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmail_WhenInviteAgentToOrganisation_GivenValidNotificationDetails(InviteAgentToOrganisationDataModel model,
        NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients,string senderName, string messageId)
    {
        //arrange
        const NotificationType notificationType = NotificationType.InviteAgentUserToOrganisation;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmail_WhenUserAssignedToApplication_GivenValidNotificationDetails(UserAssignedToApplicationDataModel model,
        NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients,string senderName, string messageId)
    {
        //arrange
        const NotificationType notificationType = NotificationType.UserAssignedToApplication;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients);
    }

    [Theory, AutoMoqData]
    public async Task ShouldSendNotificationEmailWithAttachments_GivenValidNotificationDetails(InviteWoodlandOwnerToOrganisationDataModel model,
        NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients, NotificationAttachment[] attachments, string senderName, string messageId)
    {
        //arrange
        const NotificationType notificationType = NotificationType.InviteWoodlandOwnerUserToOrganisation;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        _unitOfWOrkMock.Setup(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>);
        
        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, attachments, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients, attachments);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnFailResult_WhenSendNotificationEmail_GivenNotSupportedNotificationType(InviteWoodlandOwnerToOrganisationDataModel model, NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients, string senderName)
    {
        //arrange
        //act
        var result = await _sut.SendNotificationAsync(model, (NotificationType)1000,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWOrkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnFailResult_WhenSendNotificationEmail_AndEmailCouldNotBeSent(InviteWoodlandOwnerToOrganisationDataModel model, NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients, string senderName)
    {
        //arrange
        const NotificationType notificationType = NotificationType.InviteWoodlandOwnerUserToOrganisation;

        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse{ErrorMessages = new List<string>{"error"}});

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName,  cancellationToken: CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWOrkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task ShouldSaveNotificationHistory_WhenSendNotificationEmail_GivenValidNotificationDetails(InviteWoodlandOwnerToOrganisationDataModel model, NotificationRecipient notificationRecipient, NotificationRecipient[] notificationRecipients, string senderName, string messageId)
    {
        //arrange
        const NotificationType notificationType = NotificationType.InviteWoodlandOwnerUserToOrganisation;
        var expectedSubject = NotificationTemplates.NotificationSubjectTemplate(notificationType, model);

        _unitOfWOrkMock.Setup(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>);
        _sender.Setup(x => x.SendAsync(It.IsAny<IFluentEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendResponse());

        //act
        var result = await _sut.SendNotificationAsync(model, notificationType,
            notificationRecipient, notificationRecipients, null, senderName, cancellationToken:CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        VerifyEmailSent(expectedSubject, new[] { notificationRecipient }, notificationRecipients);
        _unitOfWOrkMock.VerifyAll();
        _notificationHistoryRepository.Verify(r => r.Add(It.Is<NotificationHistory>(h => 
            h.Recipients.Contains(notificationRecipient.Name!)
            && (JsonConvert.DeserializeObject<List<NotificationRecipient>>(h.Recipients) ?? new List<NotificationRecipient>())
                .Any(s => 
                    notificationRecipients.Select(v => v.Address)
                        .Any(a => a == s.Address) || s.Name == notificationRecipient.Name)
            && h.Source == senderName
            && h.NotificationType == notificationType)));
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldCreateNotificationContent_GivenValidNotificationDetails(ExternalConsulteeInviteDataModel model)
    {
        //arrange
        const NotificationType notificationType = NotificationType.ExternalConsulteeInvite;
        
        //act
        var result = await _sut.CreateNotificationContentAsync(model, notificationType);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnFailResult_WhenCreateNotificationContent_GivenNotSupportedNotificationType(ExternalConsulteeInviteDataModel model)
    {
        //arrange
        const NotificationType notificationType = (NotificationType)1000;
        
        //act
        var result = await _sut.CreateNotificationContentAsync(model, notificationType);

        //assert
        result.IsSuccess.Should().BeFalse();
    }

    private void VerifyEmailSent(
        string expectedSubject,
        NotificationRecipient[] notificationRecipients,
        NotificationRecipient[] ccRecipients,
        NotificationAttachment[]? attachments = null)
    {
        _sender.Verify(x => x.SendAsync(It.Is<IFluentEmail>(e =>
                e.Data.IsHtml == true
                && e.Data.ToAddresses.Count == notificationRecipients.Length
                && notificationRecipients.All(r => e.Data.ToAddresses.Any(t => t.EmailAddress == r.Address && t.Name == r.Name))
                && e.Data.CcAddresses.Count == ccRecipients.Length + 1
                && ccRecipients.All(a => e.Data.CcAddresses.Any(r => r.EmailAddress == a.Address && r.Name == a.Name))
                && e.Data.CcAddresses.Any(c => c.EmailAddress == _notificationsOptions.CopyToAddress)
                && e.Data.Subject == expectedSubject
                && ((attachments == null && e.Data.Attachments.Count == 0) 
                    || (attachments != null && e.Data.Attachments.Count == attachments.Length
                    && attachments.All(a => e.Data.Attachments.Any(r => r.Filename == a.FileName))))),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
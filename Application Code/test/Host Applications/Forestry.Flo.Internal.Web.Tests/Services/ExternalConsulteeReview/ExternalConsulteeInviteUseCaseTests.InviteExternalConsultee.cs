using System.Text.Json;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Tests.Common;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.ExternalConsulteeReview;

public partial class ExternalConsulteeInviteUseCaseTests
{
    [Theory, AutoData]
    public async Task WhenApplicationNotFoundForInviteNewConsultee(
        Guid applicationId,
        Guid userId,
        ExternalConsulteeInviteModel model)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);

        var sut = CreateSut();

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var (isSuccess, _, _) = await sut.InviteExternalConsulteeAsync(model, applicationId, user, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _getConfiguredFcAreas.VerifyNoOtherCalls();

        _emailService.VerifyNoOtherCalls();

        _mockUpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenAddingAccessLinkFails(
        Guid applicationId,
        Guid userId,
        FellingLicenceApplication application,
        string adminHubAddress,
        ExternalConsulteeInviteModel model)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        _internalUserContextFlaRepository
            .Setup(x => x.AddExternalAccessLinkAsync(It.IsAny<ExternalAccessLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));
        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);

        var (isSuccess, _, _) = await sut.InviteExternalConsulteeAsync(model, applicationId, user, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository
            .Verify(x => x.AddExternalAccessLinkAsync(It.Is<ExternalAccessLink>(e =>
                e.Name == model.ConsulteeName
                && e.Purpose == model.Purpose!
                && e.AccessCode == model.ExternalAccessCode
                && e.ContactEmail == model.Email
                && e.FellingLicenceApplicationId == applicationId
                && e.CreatedTimeStamp == _fakeClock.GetCurrentInstant().ToDateTimeUtc()
                && e.ExpiresTimeStamp == _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays)
                && e.IsMultipleUseAllowed == true
                && e.LinkType == ExternalAccessLinkType.ConsulteeInvite
                && e.SharedSupportingDocuments == model.SelectedDocumentIds), It.IsAny<CancellationToken>()),
                Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _getConfiguredFcAreas.Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()), Times.Once);

        _emailService.VerifyNoOtherCalls();

        _mockUpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeInvitationFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    InvitedByUserId = userId,
                    ConsulteeName = model.ConsulteeName,
                    ConsulteeEmailAddress = model.Email,
                    ApplicationId = applicationId,
                    InviteExpiryDateTime = _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays),
                    Error = $"External access link creation error, a database error, applicationId: {applicationId}"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUpdatingConsultationStatusFails(
        Guid applicationId,
        Guid userId,
        FellingLicenceApplication application,
        string adminHubAddress,
        ExternalConsulteeInviteModel model,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        _internalUserContextFlaRepository
            .Setup(x => x.AddExternalAccessLinkAsync(It.IsAny<ExternalAccessLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _mockUpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConsultationsStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));
        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);

        var (isSuccess, _, _) = await sut.InviteExternalConsulteeAsync(model, applicationId, user, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository
            .Verify(x => x.AddExternalAccessLinkAsync(It.Is<ExternalAccessLink>(e =>
                e.Name == model.ConsulteeName
                && e.Purpose == model.Purpose!
                && e.AccessCode == model.ExternalAccessCode
                && e.ContactEmail == model.Email
                && e.FellingLicenceApplicationId == applicationId
                && e.CreatedTimeStamp == _fakeClock.GetCurrentInstant().ToDateTimeUtc()
                && e.ExpiresTimeStamp == _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays)
                && e.IsMultipleUseAllowed == true
                && e.LinkType == ExternalAccessLinkType.ConsulteeInvite
                && e.SharedSupportingDocuments == model.SelectedDocumentIds), It.IsAny<CancellationToken>()),
                Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _mockUpdateWoodlandOfficerReviewService
            .Verify(x => x.UpdateConsultationsStatusAsync(applicationId, userId, true, false, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _getConfiguredFcAreas.Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()), Times.Once);

        _emailService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeInvitationFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    InvitedByUserId = userId,
                    ConsulteeName = model.ConsulteeName,
                    ConsulteeEmailAddress = model.Email,
                    ApplicationId = applicationId,
                    InviteExpiryDateTime = _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays),
                    Error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSendingNotificationFails(
        Guid applicationId,
        Guid userId,
        string username,
        FellingLicenceApplication application,
        string adminHubAddress,
        ExternalConsulteeInviteModel model,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId, username: username);
        var user = new InternalUser(userPrincipal);

        var sut = CreateSut();

        var endDate = _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays);
        var notificationType = model.ExemptFromConsultationPublicRegister
            ? NotificationType.ExternalConsulteeInvite
            : NotificationType.ExternalConsulteeInviteWithPublicRegisterInfo;

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        _internalUserContextFlaRepository
            .Setup(x => x.AddExternalAccessLinkAsync(It.IsAny<ExternalAccessLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _mockUpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConsultationsStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);
        _emailService
            .Setup(x => x.SendNotificationAsync(It.IsAny<ExternalConsulteeInviteDataModel>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var (isSuccess, _, _) = await sut.InviteExternalConsulteeAsync(model, applicationId, user, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository
            .Verify(x => x.AddExternalAccessLinkAsync(It.Is<ExternalAccessLink>(e =>
                e.Name == model.ConsulteeName
                && e.Purpose == model.Purpose!
                && e.AccessCode == model.ExternalAccessCode
                && e.ContactEmail == model.Email
                && e.FellingLicenceApplicationId == applicationId
                && e.CreatedTimeStamp == _fakeClock.GetCurrentInstant().ToDateTimeUtc()
                && e.ExpiresTimeStamp == endDate
                && e.IsMultipleUseAllowed == true
                && e.LinkType == ExternalAccessLinkType.ConsulteeInvite
                && e.SharedSupportingDocuments == model.SelectedDocumentIds), It.IsAny<CancellationToken>()),
                Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _mockUpdateWoodlandOfficerReviewService
            .Verify(x => x.UpdateConsultationsStatusAsync(applicationId, userId, true, false, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _getConfiguredFcAreas.Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()), Times.Once);

        _emailService.Verify(x => x.SendNotificationAsync(It.Is<ExternalConsulteeInviteDataModel>(m =>
            m.ApplicationReference == application.ApplicationReference
            && m.ConsulteeName == model.ConsulteeName
            && m.EmailText == model.ConsulteeEmailText
            && m.SenderName == username
            && m.CommentsEndDate == DateTimeDisplay.GetDateDisplayString(endDate)
            && m.ViewApplicationURL == model.ExternalAccessLink
            && m.AdminHubFooter == adminHubAddress),
            notificationType,
            It.Is<NotificationRecipient>(r => r.Address == model.Email && r.Name == model.ConsulteeName),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _emailService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeInvitationFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    InvitedByUserId = userId,
                    ConsulteeName = model.ConsulteeName,
                    ConsulteeEmailAddress = model.Email,
                    ApplicationId = applicationId,
                    InviteExpiryDateTime = _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays),
                    Error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSuccessful(
        Guid applicationId,
        Guid userId,
        string username,
        FellingLicenceApplication application,
        string adminHubAddress,
        ExternalConsulteeInviteModel model)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId, username: username);
        var user = new InternalUser(userPrincipal);

        var sut = CreateSut();

        var endDate = _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays);
        var notificationType = model.ExemptFromConsultationPublicRegister
            ? NotificationType.ExternalConsulteeInvite
            : NotificationType.ExternalConsulteeInviteWithPublicRegisterInfo;

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        _internalUserContextFlaRepository
            .Setup(x => x.AddExternalAccessLinkAsync(It.IsAny<ExternalAccessLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _mockUpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConsultationsStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);
        _emailService
            .Setup(x => x.SendNotificationAsync(It.IsAny<ExternalConsulteeInviteDataModel>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var (isSuccess, _, _) = await sut.InviteExternalConsulteeAsync(model, applicationId, user, CancellationToken.None);

        Assert.True(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository
            .Verify(x => x.AddExternalAccessLinkAsync(It.Is<ExternalAccessLink>(e =>
                e.Name == model.ConsulteeName
                && e.Purpose == model.Purpose!
                && e.AccessCode == model.ExternalAccessCode
                && e.ContactEmail == model.Email
                && e.FellingLicenceApplicationId == applicationId
                && e.CreatedTimeStamp == _fakeClock.GetCurrentInstant().ToDateTimeUtc()
                && e.ExpiresTimeStamp == endDate
                && e.IsMultipleUseAllowed == true
                && e.LinkType == ExternalAccessLinkType.ConsulteeInvite
                && e.SharedSupportingDocuments == model.SelectedDocumentIds), It.IsAny<CancellationToken>()),
                Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _mockUpdateWoodlandOfficerReviewService
            .Verify(x => x.UpdateConsultationsStatusAsync(applicationId, userId, true, false, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _getConfiguredFcAreas.Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()), Times.Once);

        _emailService.Verify(x => x.SendNotificationAsync(It.Is<ExternalConsulteeInviteDataModel>(m =>
            m.ApplicationReference == application.ApplicationReference
            && m.ConsulteeName == model.ConsulteeName
            && m.EmailText == model.ConsulteeEmailText
            && m.SenderName == username
            && m.CommentsEndDate == DateTimeDisplay.GetDateDisplayString(endDate)
            && m.ViewApplicationURL == model.ExternalAccessLink
            && m.AdminHubFooter == adminHubAddress),
            notificationType,
            It.Is<NotificationRecipient>(r => r.Address == model.Email && r.Name == model.ConsulteeName),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _emailService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeInvitationSent
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    InvitedByUserId = userId,
                    ConsulteeName = model.ConsulteeName,
                    ConsulteeEmailAddress = model.Email,
                    ApplicationId = applicationId,
                    InviteExpiryDateTime = _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays),
                    Error = (string?)null
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }
}
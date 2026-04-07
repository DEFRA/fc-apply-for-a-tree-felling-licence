using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Tests.Common;
using LinqKit;
using Moq;
using System.Text.Json;

namespace Forestry.Flo.Internal.Web.Tests.Services.ExternalConsulteeReview;

public partial class ExternalConsulteeInviteUseCaseTests
{
    [Theory, AutoData]
    public async Task WhenApplicationNotFoundForSendReminderToConsultee(
        Guid applicationId,
        Guid userId,
        Guid accessCode,
        string viewApplicationUrl)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);

        var sut = CreateSut();

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var (isSuccess, _, _) = await sut.SendReminderToConsulteeAsync(applicationId, accessCode, viewApplicationUrl, user, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _getConfiguredFcAreas.VerifyNoOtherCalls();

        _internalUserAccountService.VerifyNoOtherCalls();

        _emailService.VerifyNoOtherCalls();

        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenAccessLinkNotFoundByCode(
        Guid applicationId,
        Guid userId,
        Guid accessCode,
        string viewApplicationUrl,
        string adminHubAddress,
        FellingLicenceApplication application)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);

        var sut = CreateSut();

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);

        var (isSuccess, _, _) = await sut.SendReminderToConsulteeAsync(applicationId, accessCode, viewApplicationUrl, user, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();
        
        _getConfiguredFcAreas.Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()), Times.Once);
        _getConfiguredFcAreas.VerifyNoOtherCalls();

        _internalUserAccountService.VerifyNoOtherCalls();

        _emailService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeReminderFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Could not find access link"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenRetrievingInternalUsersForNotificationFails(
        Guid applicationId,
        Guid userId,
        ExternalAccessLink accessLink,
        string viewApplicationUrl,
        string adminHubAddress,
        FellingLicenceApplication application,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);

        // ensure all existing assignees are unassigned so that only the current assignees are returned in the result
        application.AssigneeHistories.ForEach(x => x.TimestampUnassigned = DateTime.UtcNow);
        var ao = new AssigneeHistory
        {
            Role = AssignedUserRole.AdminOfficer,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        var wo = new AssigneeHistory
        {
            Role = AssignedUserRole.WoodlandOfficer,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        var approver = new AssigneeHistory
        {
            Role = AssignedUserRole.FieldManager,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        application.AssigneeHistories.Add(ao);
        application.AssigneeHistories.Add(wo);
        application.AssigneeHistories.Add(approver);
        application.ExternalAccessLinks = [accessLink];

        var sut = CreateSut();

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);

        _internalUserAccountService
            .Setup(x => x.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<UserAccountModel>>(error));

        var (isSuccess, _, _) = await sut.SendReminderToConsulteeAsync(applicationId, accessLink.AccessCode, viewApplicationUrl, user, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _internalUserAccountService
            .Verify(x => x.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(lg => lg.Count == 3 && lg.Contains(ao.AssignedUserId) && lg.Contains(wo.AssignedUserId) && lg.Contains(approver.AssignedUserId)), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserAccountService.VerifyNoOtherCalls();

        _getConfiguredFcAreas.Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()), Times.Once);
        _getConfiguredFcAreas.VerifyNoOtherCalls();

        _emailService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeReminderFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == accessLink.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    InvitedByUserId = userId,
                    ConsulteeName = accessLink.Name,
                    ConsulteeEmailAddress = accessLink.ContactEmail,
                    ApplicationId = accessLink.FellingLicenceApplicationId,
                    InviteExpiryDateTime = accessLink.ExpiresTimeStamp,
                    Error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSendingReminderNotificationFails(
        Guid applicationId,
        Guid userId,
        ExternalAccessLink accessLink,
        string viewApplicationUrl,
        string adminHubAddress,
        FellingLicenceApplication application,
        List<UserAccountModel> assignedStaff,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);

        // ensure all existing assignees are unassigned so that only the current assignees are returned in the result
        application.AssigneeHistories.ForEach(x => x.TimestampUnassigned = DateTime.UtcNow);
        var ao = new AssigneeHistory
        {
            Role = AssignedUserRole.AdminOfficer,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        var wo = new AssigneeHistory
        {
            Role = AssignedUserRole.WoodlandOfficer,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        var approver = new AssigneeHistory
        {
            Role = AssignedUserRole.FieldManager,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        application.AssigneeHistories.Add(ao);
        application.AssigneeHistories.Add(wo);
        application.AssigneeHistories.Add(approver);
        application.ExternalAccessLinks = [accessLink];

        var sut = CreateSut();

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);

        _internalUserAccountService
            .Setup(x => x.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignedStaff));

        _emailService
            .Setup(x => x.SendNotificationAsync(It.IsAny<ExternalConsulteeInviteReminderDataModel>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(error));

        var (isSuccess, _, _) = await sut.SendReminderToConsulteeAsync(applicationId, accessLink.AccessCode, viewApplicationUrl, user, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _internalUserAccountService
            .Verify(x => x.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(lg => lg.Count == 3 && lg.Contains(ao.AssignedUserId) && lg.Contains(wo.AssignedUserId) && lg.Contains(approver.AssignedUserId)), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserAccountService.VerifyNoOtherCalls();

        _getConfiguredFcAreas.Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()), Times.Once);
        _getConfiguredFcAreas.VerifyNoOtherCalls();

        _emailService.Verify(x => x.SendNotificationAsync(It.Is<ExternalConsulteeInviteReminderDataModel>(m =>
                m.ApplicationReference == application.ApplicationReference
                && m.PropertyName == application.SubmittedFlaPropertyDetail.Name
                && m.ConsultationEndDate == DateTimeDisplay.GetDateDisplayString(accessLink.ExpiresTimeStamp)
                && m.ViewApplicationURL == viewApplicationUrl
                && m.AdminHubFooter == adminHubAddress),
            NotificationType.ExternalConsulteeInviteReminder,
            It.Is<NotificationRecipient>(r => r.Address == accessLink.ContactEmail && r.Name == accessLink.Name),
            It.Is<NotificationRecipient[]>(ar => ar.Length == assignedStaff.Count && assignedStaff.All(astaff => ar.Any(ars => ars.Name == astaff.FullName && ars.Address == astaff.Email))),
            null, null, It.IsAny<CancellationToken>()), Times.Once);
        _emailService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeReminderFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == accessLink.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    InvitedByUserId = userId,
                    ConsulteeName = accessLink.Name,
                    ConsulteeEmailAddress = accessLink.ContactEmail,
                    ApplicationId = accessLink.FellingLicenceApplicationId,
                    InviteExpiryDateTime = accessLink.ExpiresTimeStamp,
                    Error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSendingReminderNotificationSucceeds(
        Guid applicationId,
        Guid userId,
        ExternalAccessLink accessLink,
        string viewApplicationUrl,
        string adminHubAddress,
        FellingLicenceApplication application,
        List<UserAccountModel> assignedStaff)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);

        // ensure all existing assignees are unassigned so that only the current assignees are returned in the result
        application.AssigneeHistories.ForEach(x => x.TimestampUnassigned = DateTime.UtcNow);
        var ao = new AssigneeHistory
        {
            Role = AssignedUserRole.AdminOfficer,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        var wo = new AssigneeHistory
        {
            Role = AssignedUserRole.WoodlandOfficer,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        var approver = new AssigneeHistory
        {
            Role = AssignedUserRole.FieldManager,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        application.AssigneeHistories.Add(ao);
        application.AssigneeHistories.Add(wo);
        application.AssigneeHistories.Add(approver);
        application.ExternalAccessLinks = [accessLink];

        var sut = CreateSut();

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);

        _internalUserAccountService
            .Setup(x => x.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignedStaff));

        _emailService
            .Setup(x => x.SendNotificationAsync(It.IsAny<ExternalConsulteeInviteReminderDataModel>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        var (isSuccess, _, _) = await sut.SendReminderToConsulteeAsync(applicationId, accessLink.AccessCode, viewApplicationUrl, user, CancellationToken.None);

        Assert.True(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _internalUserAccountService
            .Verify(x => x.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(lg => lg.Count == 3 && lg.Contains(ao.AssignedUserId) && lg.Contains(wo.AssignedUserId) && lg.Contains(approver.AssignedUserId)), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserAccountService.VerifyNoOtherCalls();

        _getConfiguredFcAreas.Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()), Times.Once);
        _getConfiguredFcAreas.VerifyNoOtherCalls();

        _emailService.Verify(x => x.SendNotificationAsync(It.Is<ExternalConsulteeInviteReminderDataModel>(m =>
                m.ApplicationReference == application.ApplicationReference
                && m.PropertyName == application.SubmittedFlaPropertyDetail.Name
                && m.ConsultationEndDate == DateTimeDisplay.GetDateDisplayString(accessLink.ExpiresTimeStamp)
                && m.ViewApplicationURL == viewApplicationUrl
                && m.AdminHubFooter == adminHubAddress),
            NotificationType.ExternalConsulteeInviteReminder,
            It.Is<NotificationRecipient>(r => r.Address == accessLink.ContactEmail && r.Name == accessLink.Name),
            It.Is<NotificationRecipient[]>(ar => ar.Length == assignedStaff.Count && assignedStaff.All(astaff => ar.Any(ars => ars.Name == astaff.FullName && ars.Address == astaff.Email))),
            null, null, It.IsAny<CancellationToken>()), Times.Once);
        _emailService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeReminderSent
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == accessLink.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    InvitedByUserId = userId,
                    ConsulteeName = accessLink.Name,
                    ConsulteeEmailAddress = accessLink.ContactEmail,
                    ApplicationId = accessLink.FellingLicenceApplicationId,
                    InviteExpiryDateTime = accessLink.ExpiresTimeStamp,
                    Error = (string?)null
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }
}
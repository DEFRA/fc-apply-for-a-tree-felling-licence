using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Tests.Common;
using LinqKit;
using Moq;
using System.Text.Json;
using UserAccountModel = Forestry.Flo.Services.InternalUsers.Models.UserAccountModel;

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

        _internalUserAccountService.VerifyNoOtherCalls();

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
                    Error = UserDbErrorReason.General.ToString()
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
        _internalUserContextFlaRepository
            .Verify(x => x.DeleteExternalAccessLinkAsync(It.IsAny<ExternalAccessLink>(), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _internalUserAccountService.VerifyNoOtherCalls();

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
    public async Task WhenSendingNotificationToConsulteeFails(
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
            .Setup(x => x.SendNotificationAsync(It.IsAny<ExternalConsulteeInviteDataModel>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(error));

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
        _internalUserContextFlaRepository
            .Verify(x => x.DeleteExternalAccessLinkAsync(It.IsAny<ExternalAccessLink>(), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _internalUserAccountService.VerifyNoOtherCalls();

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
            && m.AdminHubFooter == adminHubAddress
            && m.PropertyName == application.SubmittedFlaPropertyDetail.Name),
            notificationType,
            It.Is<NotificationRecipient>(r => r.Address == model.Email && r.Name == model.ConsulteeName),
            null,
            null, null, It.IsAny<CancellationToken>()), Times.Once);
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
        List<UserAccountModel> assignedStaff,
        ExternalConsulteeInviteModel model,
        Guid notificationHistoryId)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId, username: username);
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
        _internalUserAccountService
            .Setup(x => x.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignedStaff));
        _mockUpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConsultationsStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);
        _emailService
            .Setup(x => x.SendNotificationAsync(It.IsAny<ExternalConsulteeInviteDataModel>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notificationHistoryId));
        _internalUserContextUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

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

        _internalUserContextFlaRepository.VerifyGet(x => x.UnitOfWork, Times.Once);
        _internalUserContextUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _internalUserAccountService
            .Verify(x => x.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(lg => lg.Count == 3 && lg.Contains(ao.AssignedUserId) && lg.Contains(wo.AssignedUserId) && lg.Contains(approver.AssignedUserId)), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserAccountService.VerifyNoOtherCalls();

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
            && m.AdminHubFooter == adminHubAddress
            && m.PropertyName == application.SubmittedFlaPropertyDetail.Name),
            notificationType,
            It.Is<NotificationRecipient>(r => r.Address == model.Email && r.Name == model.ConsulteeName),
            null,
            null, null, It.IsAny<CancellationToken>()), Times.Once);
        foreach (var staff in assignedStaff)
        {
            _emailService.Verify(x => x.SendNotificationAsync(It.Is<ExternalConsulteeInviteDataModel>(m =>
                    m.ApplicationReference == application.ApplicationReference
                    && m.ConsulteeName == model.ConsulteeName
                    && m.EmailText == model.ConsulteeEmailText
                    && m.SenderName == username
                    && m.CommentsEndDate == DateTimeDisplay.GetDateDisplayString(endDate)
                    && m.ViewApplicationURL == model.ExternalAccessLink
                    && m.AdminHubFooter == adminHubAddress),
                notificationType,
                It.Is<NotificationRecipient>(r => r.Address == staff.Email && r.Name == staff.FullName),
                null,
                null, null, It.IsAny<CancellationToken>()), Times.Once);
        }
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
        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeInvitationCopyToStaff
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
                    Staff = assignedStaff.Select(s => s.Email).ToList()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);

        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenRetrievingUsersForNotificationFails(
        Guid applicationId,
        Guid userId,
        string username,
        FellingLicenceApplication application,
        string adminHubAddress,
        ExternalConsulteeInviteModel model,
        Guid notificationHistoryId,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId, username: username);
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

        var sut = CreateSut();

        var endDate = _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays);
        var notificationType = model.ExemptFromConsultationPublicRegister
            ? NotificationType.ExternalConsulteeInvite
            : NotificationType.ExternalConsulteeInviteWithPublicRegisterInfo;

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        _internalUserAccountService
            .Setup(x => x.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<UserAccountModel>>(error));
        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);
        _internalUserContextFlaRepository
            .Setup(x => x.AddExternalAccessLinkAsync(It.IsAny<ExternalAccessLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _mockUpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConsultationsStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _emailService
            .Setup(x => x.SendNotificationAsync(It.IsAny<ExternalConsulteeInviteDataModel>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notificationHistoryId));
        _internalUserContextUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        var (isSuccess, _, _) = await sut.InviteExternalConsulteeAsync(model, applicationId, user, CancellationToken.None);

        Assert.True(isSuccess);  // consultee invite was successful, audit the copy to staff failure

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

        _internalUserContextFlaRepository.VerifyGet(x => x.UnitOfWork, Times.Once);
        _internalUserContextUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _internalUserAccountService
            .Verify(x => x.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(lg => lg.Count == 3 && lg.Contains(ao.AssignedUserId) && lg.Contains(wo.AssignedUserId) && lg.Contains(approver.AssignedUserId)), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserAccountService.VerifyNoOtherCalls();

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
                && m.AdminHubFooter == adminHubAddress
                && m.PropertyName == application.SubmittedFlaPropertyDetail.Name),
            notificationType,
            It.Is<NotificationRecipient>(r => r.Address == model.Email && r.Name == model.ConsulteeName),
            null,
            null, null, It.IsAny<CancellationToken>()), Times.Once);

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
        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeInvitationCopyToStaffFailure
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
                    Error = "Failed to retrieve internal staff to copy consultee invite email to, error: " + error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCopyingNotificationToStaffFails(
        Guid applicationId,
        Guid userId,
        string username,
        FellingLicenceApplication application,
        string adminHubAddress,
        ExternalConsulteeInviteModel model,
        Guid notificationHistoryId,
        List<UserAccountModel> assignedStaff,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId, username: username);
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

        var sut = CreateSut();

        var endDate = _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays);
        var notificationType = model.ExemptFromConsultationPublicRegister
            ? NotificationType.ExternalConsulteeInvite
            : NotificationType.ExternalConsulteeInviteWithPublicRegisterInfo;

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        _internalUserAccountService
            .Setup(x => x.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignedStaff));
        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubAddress);
        _internalUserContextFlaRepository
            .Setup(x => x.AddExternalAccessLinkAsync(It.IsAny<ExternalAccessLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _mockUpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConsultationsStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _emailService
            .Setup(x => x.SendNotificationAsync(It.IsAny<ExternalConsulteeInviteDataModel>(), It.IsAny<NotificationType>(), It.Is<NotificationRecipient>(rp => rp.Address == model.Email), It.IsAny<NotificationRecipient[]>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notificationHistoryId));
        _emailService
            .Setup(x => x.SendNotificationAsync(It.IsAny<ExternalConsulteeInviteDataModel>(), It.IsAny<NotificationType>(), It.Is<NotificationRecipient>(rp => rp.Address != model.Email), It.IsAny<NotificationRecipient[]>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(error));
        _internalUserContextUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        var (isSuccess, _, _) = await sut.InviteExternalConsulteeAsync(model, applicationId, user, CancellationToken.None);

        Assert.True(isSuccess);  // consultee invite was successful, audit the copy to staff failure

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

        _internalUserContextFlaRepository.VerifyGet(x => x.UnitOfWork, Times.Once);
        _internalUserContextUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _internalUserAccountService
            .Verify(x => x.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(lg => lg.Count == 3 && lg.Contains(ao.AssignedUserId) && lg.Contains(wo.AssignedUserId) && lg.Contains(approver.AssignedUserId)), It.IsAny<CancellationToken>()), Times.Once);
        _internalUserAccountService.VerifyNoOtherCalls();

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
                && m.AdminHubFooter == adminHubAddress
                && m.PropertyName == application.SubmittedFlaPropertyDetail.Name),
            notificationType,
            It.Is<NotificationRecipient>(r => r.Address == model.Email && r.Name == model.ConsulteeName),
            null,
            null, null, It.IsAny<CancellationToken>()), Times.Once);

        foreach (var staff in assignedStaff)
        {
            _emailService.Verify(x => x.SendNotificationAsync(It.Is<ExternalConsulteeInviteDataModel>(m =>
                    m.ApplicationReference == application.ApplicationReference
                    && m.ConsulteeName == model.ConsulteeName
                    && m.EmailText == model.ConsulteeEmailText
                    && m.SenderName == username
                    && m.CommentsEndDate == DateTimeDisplay.GetDateDisplayString(endDate)
                    && m.ViewApplicationURL == model.ExternalAccessLink
                    && m.AdminHubFooter == adminHubAddress),
                notificationType,
                It.Is<NotificationRecipient>(r => r.Address == staff.Email && r.Name == staff.FullName),
                null,
                null, null, It.IsAny<CancellationToken>()), Times.Once);
        }
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
        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ExternalConsulteeInvitationCopyToStaffFailure
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
                    Error = "Failed to copy invite email to one or more internal staff members"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }
}
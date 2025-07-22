using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Tests.Common;
using Moq;
using System.Text.Json;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using NodaTime;
using InternalUserAccount = Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount;

namespace Forestry.Flo.Internal.Web.Tests.Services.AssignToApplicantTests;

public class AssignToApplicantUseCaseTests : AssignToApplicantUseCaseTestsBase
{

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Theory, AutoMoqData]
    public async Task WhenUnableToRetrieveUserAccess(
        Guid applicationId,
        Guid applicantId,
        Guid performingUserId,
        string error)
    {
        var userPrincipal =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: performingUserId);
        var user = new InternalUser(userPrincipal);

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        var result = await sut.AssignApplicationToApplicantAsync(
            applicationId,
            user,
            applicantId,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<FellingLicenceApplicationSection, bool>>(),
            It.IsAny<Dictionary<Guid, bool>>(),
            CancellationToken.None);

        Assert.True(result.IsFailure);

        MockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error = error,
                    secondaryId = applicantId,
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();

        UpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();
        MockInternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToUpdateApplication(
        Guid applicationId,
        Guid applicantId,
        Guid performingUserId,
        UserAccessModel userAccessModel,
        string caseNote,
        string error)
    {
        var userPrincipal =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: performingUserId);
        var user = new InternalUser(userPrincipal);

        var amendmentSections = new Dictionary<FellingLicenceApplicationSection, bool> 
        {
            { FellingLicenceApplicationSection.OperationDetails, true },
            { FellingLicenceApplicationSection.FellingAndRestockingDetails, true }
        };
        var amendmentCompartments = new Dictionary<Guid, bool>
        {
            { Guid.NewGuid(), true }
        };

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));
        UpdateFellingLicenceApplication
            .Setup(x => x.ReturnToApplicantAsync(It.IsAny<ReturnToApplicantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<Guid>>(error));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrievePublicRegisterForRemoval(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegisterPeriodEndModel>.None);

        var result = await sut.AssignApplicationToApplicantAsync(
            applicationId,
            user,
            applicantId,
            caseNote,
            It.IsAny<string>(),
            amendmentSections,
            amendmentCompartments,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        MockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateFellingLicenceApplication
            .Verify(x => x.ReturnToApplicantAsync(It.Is<ReturnToApplicantRequest>(r =>
                r.ApplicationId == applicationId
                && r.CaseNoteContent == caseNote
                && r.PerformingUserId == performingUserId
                && r.ApplicantToReturnTo == userAccessModel
                && r.SectionsRequiringAttention.OperationDetailsComplete == false
                && r.SectionsRequiringAttention.FellingAndRestockingDetailsComplete.Single().CompartmentId == amendmentCompartments.Single().Key),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error = error,
                    secondaryId = applicantId,
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.Verify(x => x.RetrievePublicRegisterForRemoval(applicationId, It.IsAny<CancellationToken>()),
            Times.Once);

        UpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();
        MockInternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToGetApplicationNotificationDetails(
        Guid applicationId,
        Guid applicantId,
        Guid performingUserId,
        UserAccessModel userAccessModel,
        string caseNote,
        List<Guid> staffMemberIds,
        string error)
    {
        var userPrincipal =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: performingUserId);
        var user = new InternalUser(userPrincipal);

        var amendmentSections = new Dictionary<FellingLicenceApplicationSection, bool>
        {
            { FellingLicenceApplicationSection.OperationDetails, true },
            { FellingLicenceApplicationSection.FellingAndRestockingDetails, true }
        };
        var amendmentCompartments = new Dictionary<Guid, bool>
        {
            { Guid.NewGuid(), true }
        };

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));
        UpdateFellingLicenceApplication
            .Setup(x => x.ReturnToApplicantAsync(It.IsAny<ReturnToApplicantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(staffMemberIds));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrieveApplicationNotificationDetailsAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ApplicationNotificationDetails>(error));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrievePublicRegisterForRemoval(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegisterPeriodEndModel>.None);

        var result = await sut.AssignApplicationToApplicantAsync(
            applicationId,
            user,
            applicantId,
            caseNote,
            It.IsAny<string>(),
            amendmentSections,
            amendmentCompartments,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        MockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateFellingLicenceApplication
            .Verify(x => x.ReturnToApplicantAsync(It.Is<ReturnToApplicantRequest>(r =>
                r.ApplicationId == applicationId
                && r.CaseNoteContent == caseNote
                && r.PerformingUserId == performingUserId
                && r.ApplicantToReturnTo == userAccessModel
                && r.SectionsRequiringAttention.OperationDetailsComplete == false
                && r.SectionsRequiringAttention.FellingAndRestockingDetailsComplete.Single().CompartmentId == amendmentCompartments.Single().Key),
            It.IsAny<CancellationToken>()), Times.Once);
        MockGetFellingLicenceApplication.Verify(x => 
                x.RetrieveApplicationNotificationDetailsAsync(applicationId, It.Is<UserAccessModel>(u => u.UserAccountId == performingUserId && u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error = error,
                    secondaryId = applicantId,
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.Verify(x => x.RetrievePublicRegisterForRemoval(applicationId, It.IsAny<CancellationToken>()),
            Times.Once);

        UpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();
        MockInternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToGetApplicantUserAccountForNotifications(
        Guid applicationId,
        Guid applicantId,
        Guid performingUserId,
        UserAccessModel userAccessModel,
        string caseNote,
        string reference,
        string? propertyName,
        List<Guid> staffMemberIds,
        string error)
    {
        var userPrincipal =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: performingUserId);
        var user = new InternalUser(userPrincipal);

        var amendmentSections = new Dictionary<FellingLicenceApplicationSection, bool>
        {
            { FellingLicenceApplicationSection.OperationDetails, true },
            { FellingLicenceApplicationSection.FellingAndRestockingDetails, true }
        };
        var amendmentCompartments = new Dictionary<Guid, bool>
        {
            { Guid.NewGuid(), true }
        };

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));
        UpdateFellingLicenceApplication
            .Setup(x => x.ReturnToApplicantAsync(It.IsAny<ReturnToApplicantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(staffMemberIds));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrieveApplicationNotificationDetailsAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ApplicationNotificationDetails { AdminHubName = "admin hub", ApplicationReference = reference, PropertyName = propertyName }));
        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccountModel>(error));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrievePublicRegisterForRemoval(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegisterPeriodEndModel>.None);

        var result = await sut.AssignApplicationToApplicantAsync(
            applicationId,
            user,
            applicantId,
            caseNote,
            It.IsAny<string>(),
            amendmentSections,
            amendmentCompartments,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        MockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateFellingLicenceApplication
            .Verify(x => x.ReturnToApplicantAsync(It.Is<ReturnToApplicantRequest>(r =>
                r.ApplicationId == applicationId
                && r.CaseNoteContent == caseNote
                && r.PerformingUserId == performingUserId
                && r.ApplicantToReturnTo == userAccessModel
                && r.SectionsRequiringAttention.OperationDetailsComplete == false
                && r.SectionsRequiringAttention.FellingAndRestockingDetailsComplete.Single().CompartmentId == amendmentCompartments.Single().Key),
            It.IsAny<CancellationToken>()), Times.Once);
        MockGetFellingLicenceApplication.Verify(x =>
                x.RetrieveApplicationNotificationDetailsAsync(applicationId, It.Is<UserAccessModel>(u => u.UserAccountId == performingUserId && u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountByIdAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error = "Could not send notification to applicant",
                    secondaryId = applicantId,
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.Verify(x => x.RetrievePublicRegisterForRemoval(applicationId, It.IsAny<CancellationToken>()),
            Times.Once);

        UpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();
        MockInternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToSendNotificationToApplicant(
        Guid applicationId,
        Guid applicantId,
        Guid performingUserId,
        UserAccessModel userAccessModel,
        string caseNote,
        string reference,
        string? propertyName,
        UserAccountModel applicantAccount,
        List<Guid> staffMemberIds,
        string error)
    {
        var userPrincipal =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: performingUserId);
        var user = new InternalUser(userPrincipal);

        var amendmentSections = new Dictionary<FellingLicenceApplicationSection, bool>
        {
            { FellingLicenceApplicationSection.OperationDetails, true },
            { FellingLicenceApplicationSection.FellingAndRestockingDetails, true }
        };
        var amendmentCompartments = new Dictionary<Guid, bool>
        {
            { Guid.NewGuid(), true }
        };

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));
        UpdateFellingLicenceApplication
            .Setup(x => x.ReturnToApplicantAsync(It.IsAny<ReturnToApplicantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(staffMemberIds));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrieveApplicationNotificationDetailsAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ApplicationNotificationDetails { AdminHubName = "admin hub", ApplicationReference = reference, PropertyName = propertyName }));
        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformApplicantOfReturnedApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrievePublicRegisterForRemoval(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegisterPeriodEndModel>.None);

        var result = await sut.AssignApplicationToApplicantAsync(
            applicationId,
            user,
            applicantId,
            caseNote,
            It.IsAny<string>(),
            amendmentSections,
            amendmentCompartments,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        MockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateFellingLicenceApplication
            .Verify(x => x.ReturnToApplicantAsync(It.Is<ReturnToApplicantRequest>(r =>
                r.ApplicationId == applicationId
                && r.CaseNoteContent == caseNote
                && r.PerformingUserId == performingUserId
                && r.ApplicantToReturnTo == userAccessModel
                && r.SectionsRequiringAttention.OperationDetailsComplete == false
                && r.SectionsRequiringAttention.FellingAndRestockingDetailsComplete.Single().CompartmentId == amendmentCompartments.Single().Key),
            It.IsAny<CancellationToken>()), Times.Once);
        MockGetFellingLicenceApplication.Verify(x =>
                x.RetrieveApplicationNotificationDetailsAsync(applicationId, It.Is<UserAccessModel>(u => u.UserAccountId == performingUserId && u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountByIdAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformApplicantOfReturnedApplicationDataModel>(t =>
                t.ApplicationReference == reference
                && t.PropertyName == propertyName
                && t.CaseNoteContent == caseNote
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(applicationId.ToString())
                && t.Name == applicantAccount.FullName),
            NotificationType.InformApplicantOfReturnedApplication,
            It.Is<NotificationRecipient>(r => r.Name == applicantAccount.FullName && r.Address == applicantAccount.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error = "Could not send notification to applicant",
                    secondaryId = applicantId,
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.Verify(x => x.RetrievePublicRegisterForRemoval(applicationId, It.IsAny<CancellationToken>()),
            Times.Once);

        UpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();
        MockInternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToRetrieveUserAccountToSendNotificationToFCUser(
        Guid applicationId,
        Guid applicantId,
        Guid performingUserId,
        UserAccessModel userAccessModel,
        string caseNote,
        string reference,
        string? propertyName,
        UserAccountModel applicantAccount,
        Guid staffMemberId)
    {
        var userPrincipal =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: performingUserId);
        var user = new InternalUser(userPrincipal);

        var amendmentSections = new Dictionary<FellingLicenceApplicationSection, bool>
        {
            { FellingLicenceApplicationSection.OperationDetails, true },
            { FellingLicenceApplicationSection.FellingAndRestockingDetails, true }
        };
        var amendmentCompartments = new Dictionary<Guid, bool>
        {
            { Guid.NewGuid(), true }
        };

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));
        UpdateFellingLicenceApplication
            .Setup(x => x.ReturnToApplicantAsync(It.IsAny<ReturnToApplicantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Guid> {staffMemberId}));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrieveApplicationNotificationDetailsAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ApplicationNotificationDetails { AdminHubName = "admin hub", ApplicationReference = reference, PropertyName = propertyName }));
        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformApplicantOfReturnedApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<InternalUserAccount>.None);
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrievePublicRegisterForRemoval(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegisterPeriodEndModel>.None);

        var result = await sut.AssignApplicationToApplicantAsync(
            applicationId,
            user,
            applicantId,
            caseNote,
            It.IsAny<string>(),
            amendmentSections,
            amendmentCompartments,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        MockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateFellingLicenceApplication
            .Verify(x => x.ReturnToApplicantAsync(It.Is<ReturnToApplicantRequest>(r =>
                r.ApplicationId == applicationId
                && r.CaseNoteContent == caseNote
                && r.PerformingUserId == performingUserId
                && r.ApplicantToReturnTo == userAccessModel
                && r.SectionsRequiringAttention.OperationDetailsComplete == false
                && r.SectionsRequiringAttention.FellingAndRestockingDetailsComplete.Single().CompartmentId == amendmentCompartments.Single().Key),
            It.IsAny<CancellationToken>()), Times.Once);
        MockGetFellingLicenceApplication.Verify(x =>
                x.RetrieveApplicationNotificationDetailsAsync(applicationId, It.Is<UserAccessModel>(u => u.UserAccountId == performingUserId && u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountByIdAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformApplicantOfReturnedApplicationDataModel>(t =>
                t.ApplicationReference == reference
                && t.PropertyName == propertyName
                && t.CaseNoteContent == caseNote
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(applicationId.ToString())
                && t.Name == applicantAccount.FullName),
            NotificationType.InformApplicantOfReturnedApplication,
            It.Is<NotificationRecipient>(r => r.Name == applicantAccount.FullName && r.Address == applicantAccount.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(staffMemberId, It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error = "Could not send notifications to one or more assigned FC users",
                    secondaryId = applicantId,
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.Verify(x => x.RetrievePublicRegisterForRemoval(applicationId, It.IsAny<CancellationToken>()),
            Times.Once);

        UpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();
        MockInternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToSendNotificationToFCUser(
        Guid applicationId,
        Guid applicantId,
        Guid performingUserId,
        UserAccessModel userAccessModel,
        string caseNote,
        string reference,
        string? propertyName,
        UserAccountModel applicantAccount,
        InternalUserAccount internalUser,
        Guid staffMemberId,
        string error)
    {
        var userPrincipal =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: performingUserId);
        var user = new InternalUser(userPrincipal);

        var amendmentSections = new Dictionary<FellingLicenceApplicationSection, bool>
        {
            { FellingLicenceApplicationSection.OperationDetails, true },
            { FellingLicenceApplicationSection.FellingAndRestockingDetails, true }
        };
        var amendmentCompartments = new Dictionary<Guid, bool>
        {
            { Guid.NewGuid(), true }
        };

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));
        UpdateFellingLicenceApplication
            .Setup(x => x.ReturnToApplicantAsync(It.IsAny<ReturnToApplicantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Guid> {staffMemberId}));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrieveApplicationNotificationDetailsAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ApplicationNotificationDetails { AdminHubName = "admin hub", ApplicationReference = reference, PropertyName = propertyName }));
        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformApplicantOfReturnedApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(internalUser.AsMaybe);
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformFCStaffOfReturnedApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrievePublicRegisterForRemoval(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegisterPeriodEndModel>.None);

        var result = await sut.AssignApplicationToApplicantAsync(
            applicationId,
            user,
            applicantId,
            caseNote,
            It.IsAny<string>(),
            amendmentSections,
            amendmentCompartments,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        MockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateFellingLicenceApplication
            .Verify(x => x.ReturnToApplicantAsync(It.Is<ReturnToApplicantRequest>(r =>
                r.ApplicationId == applicationId
                && r.CaseNoteContent == caseNote
                && r.PerformingUserId == performingUserId
                && r.ApplicantToReturnTo == userAccessModel
                && r.SectionsRequiringAttention.OperationDetailsComplete == false
                && r.SectionsRequiringAttention.FellingAndRestockingDetailsComplete.Single().CompartmentId == amendmentCompartments.Single().Key),
            It.IsAny<CancellationToken>()), Times.Once);
        MockGetFellingLicenceApplication.Verify(x =>
                x.RetrieveApplicationNotificationDetailsAsync(applicationId, It.Is<UserAccessModel>(u => u.UserAccountId == performingUserId && u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountByIdAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformApplicantOfReturnedApplicationDataModel>(t =>
                t.ApplicationReference == reference
                && t.PropertyName == propertyName
                && t.CaseNoteContent == caseNote
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(applicationId.ToString())
                && t.Name == applicantAccount.FullName),
            NotificationType.InformApplicantOfReturnedApplication,
            It.Is<NotificationRecipient>(r => r.Name == applicantAccount.FullName && r.Address == applicantAccount.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformFCStaffOfReturnedApplicationDataModel>(t =>
                t.ApplicationReference == reference
                && t.CaseNoteContent == caseNote
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(applicationId.ToString())
                && t.Name == internalUser.FullName(true)),
            NotificationType.InformFCStaffOfReturnedApplication,
            It.Is<NotificationRecipient>(r => r.Name == internalUser.FullName(true) && r.Address == internalUser.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(staffMemberId, It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error = "Could not send notifications to one or more assigned FC users",
                    secondaryId = applicantId,
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.Verify(x => x.RetrievePublicRegisterForRemoval(applicationId, It.IsAny<CancellationToken>()),
            Times.Once);

        UpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();
        MockInternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationUpdatedSuccessfully_NotOnPr(
        Guid applicationId,
        Guid applicantId,
        Guid performingUserId,
        UserAccessModel userAccessModel,
        string caseNote,
        string reference,
        string? propertyName,
        UserAccountModel applicantAccount,
        Guid staffMemberId,
        InternalUserAccount internalUser)
    {
        var userPrincipal =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: performingUserId);
        var user = new InternalUser(userPrincipal);

        var amendmentSections = new Dictionary<FellingLicenceApplicationSection, bool>
        {
            { FellingLicenceApplicationSection.OperationDetails, true },
            { FellingLicenceApplicationSection.FellingAndRestockingDetails, true }
        };
        var amendmentCompartments = new Dictionary<Guid, bool>
        {
            { Guid.NewGuid(), true }
        };

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));
        UpdateFellingLicenceApplication
            .Setup(x => x.ReturnToApplicantAsync(It.IsAny<ReturnToApplicantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Guid>{staffMemberId}));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrieveApplicationNotificationDetailsAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ApplicationNotificationDetails{ AdminHubName = "admin hub", ApplicationReference = reference, PropertyName = propertyName}));
        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformApplicantOfReturnedApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(internalUser.AsMaybe);
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformFCStaffOfReturnedApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        MockGetFellingLicenceApplication
            .Setup(x => x.RetrievePublicRegisterForRemoval(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegisterPeriodEndModel>.None);

        var result = await sut.AssignApplicationToApplicantAsync(
            applicationId,
            user,
            applicantId,
            caseNote,
            It.IsAny<string>(),
            amendmentSections,
            amendmentCompartments,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateFellingLicenceApplication
            .Verify(x => x.ReturnToApplicantAsync(It.Is<ReturnToApplicantRequest>(r =>
                r.ApplicationId == applicationId
                && r.CaseNoteContent == caseNote
                && r.PerformingUserId == performingUserId
                && r.ApplicantToReturnTo == userAccessModel
                && r.SectionsRequiringAttention.OperationDetailsComplete == false
                && r.SectionsRequiringAttention.FellingAndRestockingDetailsComplete.Single().CompartmentId == amendmentCompartments.Single().Key),
            It.IsAny<CancellationToken>()), Times.Once);
        MockGetFellingLicenceApplication.Verify(x =>
                x.RetrieveApplicationNotificationDetailsAsync(applicationId, It.Is<UserAccessModel>(u => u.UserAccountId == performingUserId && u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockGetFellingLicenceApplication.Verify(x => x.RetrievePublicRegisterForRemoval(applicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountByIdAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformApplicantOfReturnedApplicationDataModel>(t =>
                t.ApplicationReference == reference
                && t.PropertyName == propertyName
                && t.CaseNoteContent == caseNote
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(applicationId.ToString())
                && t.Name == applicantAccount.FullName),
            NotificationType.InformApplicantOfReturnedApplication,
            It.Is<NotificationRecipient>(r => r.Name == applicantAccount.FullName && r.Address == applicantAccount.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformFCStaffOfReturnedApplicationDataModel>(t =>
                t.ApplicationReference == reference
                && t.CaseNoteContent == caseNote
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(applicationId.ToString())
                && t.Name == internalUser.FullName(true)),
            NotificationType.InformFCStaffOfReturnedApplication,
            It.Is<NotificationRecipient>(r => r.Name == internalUser.FullName(true) && r.Address == internalUser.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(staffMemberId, It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicant
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    applicantId = applicantId,
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();

        UpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();
        MockInternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationUpdatedSuccessfully_OnPr(
        Guid applicationId,
        Guid applicantId,
        Guid performingUserId,
        UserAccessModel userAccessModel,
        string caseNote,
        string reference,
        string? propertyName,
        UserAccountModel applicantAccount,
        Guid staffMemberId,
        InternalUserAccount internalUser)
    {
        var userPrincipal =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: performingUserId);
        var user = new InternalUser(userPrincipal);

        var amendmentSections = new Dictionary<FellingLicenceApplicationSection, bool>
        {
            { FellingLicenceApplicationSection.OperationDetails, true },
            { FellingLicenceApplicationSection.FellingAndRestockingDetails, true }
        };
        var amendmentCompartments = new Dictionary<Guid, bool>
        {
            { Guid.NewGuid(), true }
        };

        var publicRegister = Fixture.Build<PublicRegister>()
            .Without(x => x.ConsultationPublicRegisterRemovedTimestamp)
            .Without(x => x.DecisionPublicRegisterPublicationTimestamp)
            .Without(x => x.DecisionPublicRegisterExpiryTimestamp)
            .Without(x => x.DecisionPublicRegisterRemovedTimestamp)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        var prEndModel = Fixture.Build<PublicRegisterPeriodEndModel>()
            .With(x => x.PublicRegister, publicRegister)
            .Create();

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));
        UpdateFellingLicenceApplication
            .Setup(x => x.ReturnToApplicantAsync(It.IsAny<ReturnToApplicantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Guid> { staffMemberId }));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrieveApplicationNotificationDetailsAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ApplicationNotificationDetails { AdminHubName = "admin hub", ApplicationReference = reference, PropertyName = propertyName }));
        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformApplicantOfReturnedApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(internalUser.AsMaybe);
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformFCStaffOfReturnedApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        MockGetFellingLicenceApplication
            .Setup(x => x.RetrievePublicRegisterForRemoval(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegisterPeriodEndModel>.From(prEndModel));

        MockClock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));
        
        MockPublicRegister
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        UpdateFellingLicenceApplication
            .Setup(x => x.SetRemovalDateOnConsultationPublicRegisterEntryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var result = await sut.AssignApplicationToApplicantAsync(
            applicationId,
            user,
            applicantId,
            caseNote,
            It.IsAny<string>(),
            amendmentSections,
            amendmentCompartments,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateFellingLicenceApplication
            .Verify(x => x.ReturnToApplicantAsync(It.Is<ReturnToApplicantRequest>(r =>
                r.ApplicationId == applicationId
                && r.CaseNoteContent == caseNote
                && r.PerformingUserId == performingUserId
                && r.ApplicantToReturnTo == userAccessModel
                && r.SectionsRequiringAttention.OperationDetailsComplete == false
                && r.SectionsRequiringAttention.FellingAndRestockingDetailsComplete.Single().CompartmentId == amendmentCompartments.Single().Key),
            It.IsAny<CancellationToken>()), Times.Once);
        UpdateFellingLicenceApplication.Verify(x => x.SetRemovalDateOnConsultationPublicRegisterEntryAsync(
            publicRegister.FellingLicenceApplicationId,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        MockGetFellingLicenceApplication.Verify(x =>
                x.RetrieveApplicationNotificationDetailsAsync(applicationId, It.Is<UserAccessModel>(u => u.UserAccountId == performingUserId && u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockGetFellingLicenceApplication.Verify(x => x.RetrievePublicRegisterForRemoval(applicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountByIdAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformApplicantOfReturnedApplicationDataModel>(t =>
                t.ApplicationReference == reference
                && t.PropertyName == propertyName
                && t.CaseNoteContent == caseNote
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(applicationId.ToString())
                && t.Name == applicantAccount.FullName),
            NotificationType.InformApplicantOfReturnedApplication,
            It.Is<NotificationRecipient>(r => r.Name == applicantAccount.FullName && r.Address == applicantAccount.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformFCStaffOfReturnedApplicationDataModel>(t =>
                t.ApplicationReference == reference
                && t.CaseNoteContent == caseNote
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(applicationId.ToString())
                && t.Name == internalUser.FullName(true)),
            NotificationType.InformFCStaffOfReturnedApplication,
            It.Is<NotificationRecipient>(r => r.Name == internalUser.FullName(true) && r.Address == internalUser.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(staffMemberId, It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicant
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    applicantId = applicantId,
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();

        MockClock.Verify(x => x.GetCurrentInstant(), Times.Once);

        MockPublicRegister.Verify(x => x.RemoveCaseFromConsultationRegisterAsync(
            publicRegister.EsriId!.Value,
            prEndModel.ApplicationReference, 
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        MockPublicRegister.VerifyNoOtherCalls();

        UpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();
        MockInternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationUpdatedSuccessfully_ButFailsToRemoveFromPr(
        Guid applicationId,
        Guid applicantId,
        Guid performingUserId,
        UserAccessModel userAccessModel,
        string caseNote,
        string reference,
        string? propertyName,
        UserAccountModel applicantAccount,
        Guid staffMemberId,
        InternalUserAccount internalUser)
    {
        var userPrincipal =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: performingUserId);
        var user = new InternalUser(userPrincipal);

        var amendmentSections = new Dictionary<FellingLicenceApplicationSection, bool>
        {
            { FellingLicenceApplicationSection.OperationDetails, true },
            { FellingLicenceApplicationSection.FellingAndRestockingDetails, true }
        };
        var amendmentCompartments = new Dictionary<Guid, bool>
        {
            { Guid.NewGuid(), true }
        };

        var publicRegister = Fixture.Build<PublicRegister>()
            .Without(x => x.ConsultationPublicRegisterRemovedTimestamp)
            .Without(x => x.DecisionPublicRegisterPublicationTimestamp)
            .Without(x => x.DecisionPublicRegisterExpiryTimestamp)
            .Without(x => x.DecisionPublicRegisterRemovedTimestamp)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        var prEndModel = Fixture.Build<PublicRegisterPeriodEndModel>()
            .With(x => x.PublicRegister, publicRegister)
            .With(x => x.AssignedUserIds, [staffMemberId])
            .Create();

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccessModel));
        UpdateFellingLicenceApplication
            .Setup(x => x.ReturnToApplicantAsync(It.IsAny<ReturnToApplicantRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Guid> { staffMemberId }));
        MockGetFellingLicenceApplication
            .Setup(x => x.RetrieveApplicationNotificationDetailsAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ApplicationNotificationDetails { AdminHubName = "admin hub", ApplicationReference = reference, PropertyName = propertyName }));
        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformApplicantOfReturnedApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformFCStaffOfDecisionPublicRegisterAutomaticRemovalOnExpiryDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(internalUser.AsMaybe);
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformFCStaffOfReturnedApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        MockGetFellingLicenceApplication
            .Setup(x => x.RetrievePublicRegisterForRemoval(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegisterPeriodEndModel>.From(prEndModel));

        MockClock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));

        MockPublicRegister
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));

        var result = await sut.AssignApplicationToApplicantAsync(
            applicationId,
            user,
            applicantId,
            caseNote,
            It.IsAny<string>(),
            amendmentSections,
            amendmentCompartments,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockRetrieveUserAccountsService
            .Verify(x => x.RetrieveUserAccessAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateFellingLicenceApplication
            .Verify(x => x.ReturnToApplicantAsync(It.Is<ReturnToApplicantRequest>(r =>
                r.ApplicationId == applicationId
                && r.CaseNoteContent == caseNote
                && r.PerformingUserId == performingUserId
                && r.ApplicantToReturnTo == userAccessModel
                && r.SectionsRequiringAttention.OperationDetailsComplete == false
                && r.SectionsRequiringAttention.FellingAndRestockingDetailsComplete.Single().CompartmentId == amendmentCompartments.Single().Key),
            It.IsAny<CancellationToken>()), Times.Once);
        MockGetFellingLicenceApplication.Verify(x =>
                x.RetrieveApplicationNotificationDetailsAsync(applicationId, It.Is<UserAccessModel>(u => u.UserAccountId == performingUserId && u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockGetFellingLicenceApplication.Verify(x => x.RetrievePublicRegisterForRemoval(applicationId, It.IsAny<CancellationToken>()),
                Times.Once);
        MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountByIdAsync(applicantId, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformApplicantOfReturnedApplicationDataModel>(t =>
                t.ApplicationReference == reference
                && t.PropertyName == propertyName
                && t.CaseNoteContent == caseNote
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(applicationId.ToString())
                && t.Name == applicantAccount.FullName),
            NotificationType.InformApplicantOfReturnedApplication,
            It.Is<NotificationRecipient>(r => r.Name == applicantAccount.FullName && r.Address == applicantAccount.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformFCStaffOfReturnedApplicationDataModel>(t =>
                t.ApplicationReference == reference
                && t.CaseNoteContent == caseNote
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(applicationId.ToString())
                && t.Name == internalUser.FullName(true)),
            NotificationType.InformFCStaffOfReturnedApplication,
            It.Is<NotificationRecipient>(r => r.Name == internalUser.FullName(true) && r.Address == internalUser.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformFCStaffOfDecisionPublicRegisterAutomaticRemovalOnExpiryDataModel>(t =>
                t.ApplicationReference == prEndModel.ApplicationReference
                && t.AdminHubFooter == AdminHubFooter
                && t.ViewApplicationURL.EndsWith(prEndModel.PublicRegister!.FellingLicenceApplicationId.ToString())
                && t.Name == internalUser.FullName(true)),
            NotificationType.InformFcStaffOfApplicationRemovedFromDecisionPublicRegisterFailure,
            It.Is<NotificationRecipient>(r => r.Name == internalUser.FullName(true) && r.Address == internalUser.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once); 
        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(staffMemberId, It.IsAny<CancellationToken>()), Times.Exactly(2));
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicant
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    applicantId = applicantId,
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.ConsultationPublicRegisterApplicationRemovalNotification
                && y.SourceEntityId == publicRegister.FellingLicenceApplicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == performingUserId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    PublicRegisterPeriodEndDate = publicRegister.ConsultationPublicRegisterExpiryTimestamp,
                    NumberOfFcStaffNotificationRecipients = 1,
                    ApplicationRemovalSuccess = false
                }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();

        MockClock.Verify(x => x.GetCurrentInstant(), Times.Once);

        MockPublicRegister.Verify(x => x.RemoveCaseFromConsultationRegisterAsync(
            publicRegister.EsriId!.Value,
            prEndModel.ApplicationReference,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        MockPublicRegister.VerifyNoOtherCalls();

        UpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();
        MockInternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task NotifyApplicantOfLarchAsync_ShouldCallSendNotificationAsync_ForMixLarchZone1(
        Guid applicationId,
        Guid applicantId,
        ApplicationNotificationDetails applicationDetails,
        LarchCheckDetailsModel larchDetails,
        FellingLicenceApplicationSummaryModel applicationSummary,
        UserAccountModel applicantAccount)
    {
        // Arrange
        larchDetails.RecommendSplitApplicationDue = (int)RecommendSplitApplicationEnum.MixLarchZone1;
        var expectedNotificationType = NotificationType.InformApplicantOfReturnedApplicationMixLarchZone1;

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));

        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<InformApplicantOfReturnedLarchApplicationDataModel>(),
                It.IsAny<NotificationType>(),
                It.IsAny<NotificationRecipient>(),
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act  
        var result = await sut.NotifyApplicantOfLarchSplitAsync(
            applicationId,
            applicantId,
            applicationDetails,
            larchDetails,
            applicationSummary,
            CancellationToken.None);

        // Assert  
        Assert.True(result.IsSuccess);

        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformApplicantOfReturnedLarchApplicationDataModel>(model =>
                model.ApplicationReference == applicationDetails.ApplicationReference &&
                model.PropertyName == applicationDetails.PropertyName &&
                model.AdminHubFooter == AdminHubFooter &&
                model.Name == applicantAccount.FullName),
            expectedNotificationType,
            It.Is<NotificationRecipient>(recipient =>
                recipient.Name == applicantAccount.FullName &&
                recipient.Address == applicantAccount.Email),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task NotifyApplicantOfLarchAsync_ShouldCallSendNotificationAsync_ForLarchOnlyMixZone(
        Guid applicationId,
        Guid applicantId,
        ApplicationNotificationDetails applicationDetails,
        LarchCheckDetailsModel larchDetails,
        FellingLicenceApplicationSummaryModel applicationSummary,
        UserAccountModel applicantAccount)
    {
        // Arrange
        larchDetails.RecommendSplitApplicationDue = (int)RecommendSplitApplicationEnum.LarchOnlyMixZone;
        var expectedNotificationType = NotificationType.InformApplicantOfReturnedApplicationLarchOnlyMixZone;

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));

        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<InformApplicantOfReturnedLarchApplicationDataModel>(),
                It.IsAny<NotificationType>(),
                It.IsAny<NotificationRecipient>(),
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await sut.NotifyApplicantOfLarchSplitAsync(
            applicationId,
            applicantId,
            applicationDetails,
            larchDetails,
            applicationSummary,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformApplicantOfReturnedLarchApplicationDataModel>(model =>
                model.ApplicationReference == applicationDetails.ApplicationReference &&
                model.PropertyName == applicationDetails.PropertyName &&
                model.AdminHubFooter == AdminHubFooter &&
                model.Name == applicantAccount.FullName),
            expectedNotificationType,
            It.Is<NotificationRecipient>(recipient =>
                recipient.Name == applicantAccount.FullName &&
                recipient.Address == applicantAccount.Email),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task NotifyApplicantOfLarchAsync_ShouldCallSendNotificationAsync_ForMixLarchMixZone(
        Guid applicationId,
        Guid applicantId,
        ApplicationNotificationDetails applicationDetails,
        LarchCheckDetailsModel larchDetails,
        FellingLicenceApplicationSummaryModel applicationSummary,
        UserAccountModel applicantAccount)
    {
        // Arrange
        larchDetails.RecommendSplitApplicationDue = (int)RecommendSplitApplicationEnum.MixLarchMixZone;
        var expectedNotificationType = NotificationType.InformApplicantOfReturnedApplicationMixLarchMixZone;

        var sut = CreateSut();

        MockRetrieveUserAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));

        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<InformApplicantOfReturnedLarchApplicationDataModel>(),
                It.IsAny<NotificationType>(),
                It.IsAny<NotificationRecipient>(),
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await sut.NotifyApplicantOfLarchSplitAsync(
            applicationId,
            applicantId,
            applicationDetails,
            larchDetails,
            applicationSummary,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        MockSendNotifications.Verify(x => x.SendNotificationAsync(
            It.Is<InformApplicantOfReturnedLarchApplicationDataModel>(model =>
                model.ApplicationReference == applicationDetails.ApplicationReference &&
                model.PropertyName == applicationDetails.PropertyName &&
                model.AdminHubFooter == AdminHubFooter &&
                model.Name == applicantAccount.FullName),
            expectedNotificationType,
            It.Is<NotificationRecipient>(recipient =>
                recipient.Name == applicantAccount.FullName &&
                recipient.Address == applicantAccount.Email),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
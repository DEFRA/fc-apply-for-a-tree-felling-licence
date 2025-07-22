using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class CompleteWoodlandOfficerReviewAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase>
{
    [Theory, AutoMoqData]
    public async Task WhenUpdateWoodlandOfficerReviewServiceReturnsFailure(
        Guid applicationId,
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool recommendationForDecisionPublicRegister,
        string internalLinkToApplication,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId));
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.CompleteWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RecommendedLicenceDuration>(), It.IsAny<bool?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CompleteWoodlandOfficerReviewNotificationsModel>(error));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId,
            recommendedLicenceDuration,
            recommendationForDecisionPublicRegister,
            internalLinkToApplication,
            user,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        UpdateWoodlandOfficerReviewService.Verify(x => x.CompleteWoodlandOfficerReviewAsync(applicationId, user.UserAccountId!.Value, recommendedLicenceDuration, recommendationForDecisionPublicRegister, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();
        NotificationService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotRetrieveExternalApplicantDetails(
        Guid applicationId,
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool recommendationForDecisionPublicRegister,
        string internalLinkToApplication,
        CompleteWoodlandOfficerReviewNotificationsModel notificationsModel,
        UserDbErrorReason error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId));
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.CompleteWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RecommendedLicenceDuration>(), It.IsAny<bool?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notificationsModel));
        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>(error.ToString()));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId,
            recommendedLicenceDuration,
            recommendationForDecisionPublicRegister,
            internalLinkToApplication,
            user,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        UpdateWoodlandOfficerReviewService.Verify(x => x.CompleteWoodlandOfficerReviewAsync(applicationId, user.UserAccountId!.Value, recommendedLicenceDuration, recommendationForDecisionPublicRegister, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();
        NotificationService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.Verify(x => x.RetrieveUserAccountEntityByIdAsync(notificationsModel.ApplicantId, It.IsAny<CancellationToken>()), Times.Once);
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();

        var deleteMe = AuditingService.Invocations.First().Arguments[0] as AuditEvent;

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = error.ToString()
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    completedDate = Now.ToDateTimeUtc(),
                    recommendedLicenceDuration = recommendedLicenceDuration,
                    recommendationForDecisionPublicRegister = recommendationForDecisionPublicRegister
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotRetrieveWoodlandOfficerDetails(
        Guid applicationId,
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool recommendationForDecisionPublicRegister,
        string internalLinkToApplication,
        CompleteWoodlandOfficerReviewNotificationsModel notificationsModel,
        UserAccount applicantAccount)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId));
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.CompleteWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RecommendedLicenceDuration>(), It.IsAny<bool?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notificationsModel));
        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));
        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.None);

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId,
            recommendedLicenceDuration,
            recommendationForDecisionPublicRegister,
            internalLinkToApplication,
            user,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        UpdateWoodlandOfficerReviewService.Verify(x => x.CompleteWoodlandOfficerReviewAsync(applicationId, user.UserAccountId!.Value, recommendedLicenceDuration, recommendationForDecisionPublicRegister, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();
        NotificationService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.Verify(x => x.RetrieveUserAccountEntityByIdAsync(notificationsModel.ApplicantId, It.IsAny<CancellationToken>()), Times.Once);
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.Verify(x => x.GetUserAccountAsync(RequestContextUserId, It.IsAny<CancellationToken>()), Times.Once);
        InternalUserAccountService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = "Unable to find woodland officer to notify"
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    completedDate = Now.ToDateTimeUtc(),
                    recommendedLicenceDuration = recommendedLicenceDuration,
                    recommendationForDecisionPublicRegister = recommendationForDecisionPublicRegister
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotRetrieveFieldManagerDetails(
        Guid applicationId,
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool recommendationForDecisionPublicRegister,
        string internalLinkToApplication,
        CompleteWoodlandOfficerReviewNotificationsModel notificationsModel,
        UserAccount applicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount woodlandOfficerAccount)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId));
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.CompleteWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RecommendedLicenceDuration>(), It.IsAny<bool?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notificationsModel));
        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));
        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(RequestContextUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(woodlandOfficerAccount));
        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(notificationsModel.FieldManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.None);

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId,
            recommendedLicenceDuration,
            recommendationForDecisionPublicRegister,
            internalLinkToApplication,
            user,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        UpdateWoodlandOfficerReviewService.Verify(x => x.CompleteWoodlandOfficerReviewAsync(applicationId, user.UserAccountId!.Value, recommendedLicenceDuration, recommendationForDecisionPublicRegister, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();
        NotificationService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.Verify(x => x.RetrieveUserAccountEntityByIdAsync(notificationsModel.ApplicantId, It.IsAny<CancellationToken>()), Times.Once);
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.Verify(x => x.GetUserAccountAsync(RequestContextUserId, It.IsAny<CancellationToken>()), Times.Once);
        InternalUserAccountService.Verify(x => x.GetUserAccountAsync(notificationsModel.FieldManagerId, It.IsAny<CancellationToken>()), Times.Once);
        InternalUserAccountService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = "Unable to find field manager to notify"
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    completedDate = Now.ToDateTimeUtc(),
                    recommendedLicenceDuration = recommendedLicenceDuration,
                    recommendationForDecisionPublicRegister = recommendationForDecisionPublicRegister
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotSendNotificationToFieldManager(
        Guid applicationId,
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool recommendationForDecisionPublicRegister,
        string internalLinkToApplication,
        CompleteWoodlandOfficerReviewNotificationsModel notificationsModel,
        UserAccount applicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount woodlandOfficerAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount fieldManagerAccount,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId));
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.CompleteWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RecommendedLicenceDuration>(), It.IsAny<bool?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notificationsModel));
        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));
        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(RequestContextUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(woodlandOfficerAccount));
        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(notificationsModel.FieldManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(fieldManagerAccount));
        NotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformAssignedUserOfApplicationStatusTransitionDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId,
            recommendedLicenceDuration,
            recommendationForDecisionPublicRegister,
            internalLinkToApplication,
            user,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        UpdateWoodlandOfficerReviewService.Verify(x => x.CompleteWoodlandOfficerReviewAsync(applicationId, user.UserAccountId!.Value, recommendedLicenceDuration, recommendationForDecisionPublicRegister, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();
        NotificationService.Verify(x => x.SendNotificationAsync(
            It.Is<InformAssignedUserOfApplicationStatusTransitionDataModel>(
                m => m.Name == fieldManagerAccount.FullName(true) 
                     && m.ApplicationReference == notificationsModel.ApplicationReference 
                     && m.ViewApplicationURL == internalLinkToApplication 
                     && m.AdminHubFooter == AdminHubAddress
                     && m.PreviousAssignedUserName == woodlandOfficerAccount.FullName(true)
                     && m.PreviousAssignedEmailAddress == woodlandOfficerAccount.Email),
            NotificationType.InformFieldManagerOfWoodlandOfficerReviewCompletion,
            It.Is<NotificationRecipient>(r => r.Name == fieldManagerAccount.FullName(true) 
                                              && r.Address == fieldManagerAccount.Email),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        NotificationService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.Verify(x => x.RetrieveUserAccountEntityByIdAsync(notificationsModel.ApplicantId, It.IsAny<CancellationToken>()), Times.Once);
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.Verify(x => x.GetUserAccountAsync(RequestContextUserId, It.IsAny<CancellationToken>()), Times.Once);
        InternalUserAccountService.Verify(x => x.GetUserAccountAsync(notificationsModel.FieldManagerId, It.IsAny<CancellationToken>()), Times.Once);
        InternalUserAccountService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReviewNotificationFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == woodlandOfficerAccount.Id
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = "Failed to send notification to field manager"
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    completedDate = Now.ToDateTimeUtc(),
                    recommendedLicenceDuration = recommendedLicenceDuration,
                    recommendationForDecisionPublicRegister = recommendationForDecisionPublicRegister
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenProcessCompletesSuccessfully(
        Guid applicationId,
        RecommendedLicenceDuration? recommendedLicenceDuration,
        bool recommendationForDecisionPublicRegister,
        string internalLinkToApplication,
        CompleteWoodlandOfficerReviewNotificationsModel notificationsModel,
        UserAccount applicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount woodlandOfficerAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount fieldManagerAccount)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId));
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.CompleteWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RecommendedLicenceDuration>(), It.IsAny<bool?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notificationsModel));
        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantAccount));
        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(RequestContextUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(woodlandOfficerAccount));
        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(notificationsModel.FieldManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(fieldManagerAccount));
        NotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformAssignedUserOfApplicationStatusTransitionDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        NotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformApplicantOfApplicationReviewCompletionDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var result = await sut.CompleteWoodlandOfficerReviewAsync(
            applicationId,
            recommendedLicenceDuration,
            recommendationForDecisionPublicRegister,
            internalLinkToApplication,
            user,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        UpdateWoodlandOfficerReviewService.Verify(x => x.CompleteWoodlandOfficerReviewAsync(applicationId, user.UserAccountId!.Value, recommendedLicenceDuration, recommendationForDecisionPublicRegister, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();
        NotificationService.Verify(x => x.SendNotificationAsync(
            It.Is<InformAssignedUserOfApplicationStatusTransitionDataModel>(
                m => m.Name == fieldManagerAccount.FullName(true) 
                     && m.ApplicationReference == notificationsModel.ApplicationReference 
                     && m.ViewApplicationURL == internalLinkToApplication 
                     && m.AdminHubFooter == AdminHubAddress
                     && m.PreviousAssignedUserName == woodlandOfficerAccount.FullName(true)
                     && m.PreviousAssignedEmailAddress == woodlandOfficerAccount.Email),
            NotificationType.InformFieldManagerOfWoodlandOfficerReviewCompletion,
            It.Is<NotificationRecipient>(r => r.Name == fieldManagerAccount.FullName(true) && r.Address == fieldManagerAccount.Email),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        NotificationService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.Verify(x => x.RetrieveUserAccountEntityByIdAsync(notificationsModel.ApplicantId, It.IsAny<CancellationToken>()), Times.Once);
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.Verify(x => x.GetUserAccountAsync(RequestContextUserId, It.IsAny<CancellationToken>()), Times.Once);
        InternalUserAccountService.Verify(x => x.GetUserAccountAsync(notificationsModel.FieldManagerId, It.IsAny<CancellationToken>()), Times.Once);
        InternalUserAccountService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReviewNotificationSent
                && a.ActorType == ActorType.InternalUser
                && a.UserId == woodlandOfficerAccount.Id
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    recipient = "Field Manager",
                    recipientId = fieldManagerAccount.Id
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReviewNotificationSent
                && a.ActorType == ActorType.InternalUser
                && a.UserId == woodlandOfficerAccount.Id
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    recipient = "Applicant",
                    recipientId = applicantAccount.Id
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    completedDate = Now.ToDateTimeUtc(),
                    recommendedLicenceDuration = recommendedLicenceDuration,
                    recommendationForDecisionPublicRegister = recommendationForDecisionPublicRegister
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    private Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase CreateSut()
    {
        ResetMocks();

        return new Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            ActivityFeedItemProvider.Object,
            AuditingService.Object,
            NotificationService.Object,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            Clock.Object,
            RequestContext,
            new NullLogger<Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase>());
    }
}
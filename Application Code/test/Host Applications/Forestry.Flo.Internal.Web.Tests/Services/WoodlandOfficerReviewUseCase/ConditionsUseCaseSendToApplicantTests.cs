using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class ConditionsUseCaseSendToApplicantTests
{
    private readonly Mock<IGetWoodlandOfficerReviewService> _getWoodlandOfficerReviewService = new();
    private readonly Mock<ICalculateConditions> _conditionsService = new();
    private readonly Mock<IUpdateWoodlandOfficerReviewService> _updateWoodlandOfficerReviewService = new();
    private readonly Mock<IAuditService<ConditionsUseCase>> _auditService = new();
    private readonly Mock<IUpdateConfirmedFellingAndRestockingDetailsService> _fellingAndRestockingService = new();
    private readonly Mock<IRetrieveUserAccountsService> _externalApplicantService = new();
    private readonly Mock<ISendNotifications> _notificationsService = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnersService = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();

    private readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly Guid RequestContextUserId = Guid.NewGuid();
    private readonly Instant Now = new Instant();
    private readonly string _baseUri = "https://locahost:7900/";

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task WhenCannotGetDetailsToSendNotification(
        Guid applicationId,
        Guid userId,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetDetailsForConditionsNotificationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ApplicationDetailsForConditionsNotification>(error));

        var result = await sut.SendConditionsToApplicantAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getWoodlandOfficerReviewService.Verify(x => x.GetDetailsForConditionsNotificationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _externalApplicantService.VerifyNoOtherCalls();

        _conditionsService.VerifyNoOtherCalls();

        _notificationsService.VerifyNoOtherCalls();

        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions",
                    error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotGetApplicantDetails(
        Guid applicationId,
        Guid userId,
        ApplicationDetailsForConditionsNotification applicationDetails,
        UserDbErrorReason error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetDetailsForConditionsNotificationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationDetails));

        _externalApplicantService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>(error.ToString()));

        var result = await sut.SendConditionsToApplicantAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getWoodlandOfficerReviewService.Verify(x => x.GetDetailsForConditionsNotificationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _externalApplicantService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(applicationDetails.ApplicationAuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _externalApplicantService.VerifyNoOtherCalls();

        _conditionsService.VerifyNoOtherCalls();

        _notificationsService.VerifyNoOtherCalls();

        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions",
                    error = error.ToString()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotGetWoodlandOwnerDetails(
     Guid applicationId,
     Guid userId,
     ApplicationDetailsForConditionsNotification applicationDetails,
     UserAccount account,
     UserDbErrorReason error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetDetailsForConditionsNotificationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationDetails));

        _externalApplicantService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(account));

        _woodlandOwnersService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOwnerModel>(error.ToString()));

        var result = await sut.SendConditionsToApplicantAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getWoodlandOfficerReviewService.Verify(x => x.GetDetailsForConditionsNotificationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _externalApplicantService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(applicationDetails.ApplicationAuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _externalApplicantService.VerifyNoOtherCalls();

        _woodlandOwnersService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(applicationDetails.WoodlandOwnerId, It.IsAny<CancellationToken>()));
        _woodlandOwnersService.VerifyNoOtherCalls();

        _conditionsService.VerifyNoOtherCalls();

        _notificationsService.VerifyNoOtherCalls();

        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions",
                    error = error.ToString()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotSendNotification(
        Guid applicationId,
        Guid userId,
        ApplicationDetailsForConditionsNotification applicationDetails,
        UserAccount account,
        WoodlandOwnerModel woodlandOwner,
        string adminHubFooter,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetDetailsForConditionsNotificationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationDetails));

        _externalApplicantService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(account));

        _woodlandOwnersService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _conditionsService
            .Setup(x => x.RetrieveExistingConditionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionsResponse{Conditions = new List<CalculatedCondition>(0)});

        _notificationsService
            .Setup(x => x.SendNotificationAsync(It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(), It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        var result = await sut.SendConditionsToApplicantAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getWoodlandOfficerReviewService.Verify(x => x.GetDetailsForConditionsNotificationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _externalApplicantService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(applicationDetails.ApplicationAuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _externalApplicantService.VerifyNoOtherCalls();

        _woodlandOwnersService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(applicationDetails.WoodlandOwnerId, It.IsAny<CancellationToken>()));
        _woodlandOwnersService.VerifyNoOtherCalls();

        _conditionsService.Verify(x => x.RetrieveExistingConditionsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        var expectedConditionsText = "No conditions apply to the application";
        var expectedUrl = $"{_baseUri}FellingLicenceApplication/ApplicationTaskList/{applicationId}";

        _notificationsService.Verify(x => x.SendNotificationAsync(It.Is<ConditionsToApplicantDataModel>(
            m => m.ApplicationReference == applicationDetails.ApplicationReference 
                 && m.ConditionsText == expectedConditionsText 
                 && m.Name == account.FullName(true) 
                 && m.ViewApplicationURL == expectedUrl
                 && m.PropertyName == applicationDetails.PropertyName
                 && m.SenderEmail == user.EmailAddress
                 && m.AdminHubFooter == adminHubFooter
                 //&& m.SenderName == user.FullName
                 ),
            NotificationType.ConditionsToApplicant, It.Is<NotificationRecipient>(r => r.Name == account.FullName(true) && r.Address == account.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _notificationsService.VerifyNoOtherCalls();

        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions",
                    error = error.ToString()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotUpdateStatus(
        Guid applicationId,
        Guid userId,
        ApplicationDetailsForConditionsNotification applicationDetails,
        UserAccount account,
        WoodlandOwnerModel woodlandOwner,
        string adminHubFooter,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetDetailsForConditionsNotificationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationDetails));

        _externalApplicantService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(account));

        _woodlandOwnersService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _conditionsService
            .Setup(x => x.RetrieveExistingConditionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionsResponse { Conditions = new List<CalculatedCondition>(0) });

        _notificationsService
            .Setup(x => x.SendNotificationAsync(It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(), It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConditionalStatusAsync(It.IsAny<Guid>(), It.IsAny<ConditionsStatusModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        var result = await sut.SendConditionsToApplicantAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _getWoodlandOfficerReviewService.Verify(x => x.GetDetailsForConditionsNotificationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _externalApplicantService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(applicationDetails.ApplicationAuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _externalApplicantService.VerifyNoOtherCalls();

        _woodlandOwnersService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(applicationDetails.WoodlandOwnerId, It.IsAny<CancellationToken>()));
        _woodlandOwnersService.VerifyNoOtherCalls();

        _conditionsService.Verify(x => x.RetrieveExistingConditionsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        var expectedConditionsText = "No conditions apply to the application";
        var expectedUrl = $"{_baseUri}FellingLicenceApplication/ApplicationTaskList/{applicationId}";

        _notificationsService.Verify(x => x.SendNotificationAsync(It.Is<ConditionsToApplicantDataModel>(
            m => m.ApplicationReference == applicationDetails.ApplicationReference
                 && m.ConditionsText == expectedConditionsText
                 && m.Name == account.FullName(true)
                 && m.ViewApplicationURL == expectedUrl
                 && m.PropertyName == applicationDetails.PropertyName
                 && m.SenderEmail == user.EmailAddress
                 && m.SenderName == user.FullName
                 && m.AdminHubFooter == adminHubFooter),
            NotificationType.ConditionsToApplicant, It.Is<NotificationRecipient>(r => r.Name == account.FullName(true) && r.Address == account.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _notificationsService.VerifyNoOtherCalls();

        _updateWoodlandOfficerReviewService.Verify(x => x.UpdateConditionalStatusAsync(applicationId, It.Is<ConditionsStatusModel>(c => c.IsConditional == true && c.ConditionsToApplicantDate == Now.ToDateTimeUtc()), user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions",
                    error = error.ToString()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenNotificationIsSentWithNoConditions(
        Guid applicationId,
        Guid userId,
        ApplicationDetailsForConditionsNotification applicationDetails,
        UserAccount account,
        WoodlandOwnerModel woodlandOwner,
        string adminHubFooter)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetDetailsForConditionsNotificationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationDetails));

        _externalApplicantService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(account));

        _woodlandOwnersService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _conditionsService
            .Setup(x => x.RetrieveExistingConditionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionsResponse { Conditions = new List<CalculatedCondition>(0) });

        _notificationsService
            .Setup(x => x.SendNotificationAsync(It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(), It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConditionalStatusAsync(It.IsAny<Guid>(), It.IsAny<ConditionsStatusModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        var result = await sut.SendConditionsToApplicantAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _getWoodlandOfficerReviewService.Verify(x => x.GetDetailsForConditionsNotificationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _externalApplicantService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(applicationDetails.ApplicationAuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _externalApplicantService.VerifyNoOtherCalls();

        _woodlandOwnersService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(applicationDetails.WoodlandOwnerId, It.IsAny<CancellationToken>()));
        _woodlandOwnersService.VerifyNoOtherCalls();

        _conditionsService.Verify(x => x.RetrieveExistingConditionsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        var expectedConditionsText = "No conditions apply to the application";
        var expectedUrl = $"{_baseUri}FellingLicenceApplication/ApplicationTaskList/{applicationId}";

        _notificationsService.Verify(x => x.SendNotificationAsync(It.Is<ConditionsToApplicantDataModel>(
            m => m.ApplicationReference == applicationDetails.ApplicationReference
                 && m.ConditionsText == expectedConditionsText
                 && m.Name == account.FullName(true)
                 && m.ViewApplicationURL == expectedUrl
                 && m.PropertyName == applicationDetails.PropertyName
                 && m.SenderEmail == user.EmailAddress
                 && m.SenderName == user.FullName
                 && m.AdminHubFooter == adminHubFooter),
            NotificationType.ConditionsToApplicant, It.Is<NotificationRecipient>(r => r.Name == account.FullName(true) && r.Address == account.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _notificationsService.VerifyNoOtherCalls();

        _updateWoodlandOfficerReviewService.Verify(x => x.UpdateConditionalStatusAsync(applicationId, It.Is<ConditionsStatusModel>(c => c.IsConditional == true && c.ConditionsToApplicantDate == Now.ToDateTimeUtc()), user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenNotificationIsSentWithConditions(
        Guid applicationId,
        Guid userId,
        ApplicationDetailsForConditionsNotification applicationDetails,
        ConditionsResponse conditionsResponse,
        UserAccount account,
        WoodlandOwnerModel woodlandOwner,
        string adminHubFooter)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _getWoodlandOfficerReviewService
            .Setup(x => x.GetDetailsForConditionsNotificationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationDetails));

        _externalApplicantService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(account));

        _woodlandOwnersService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _conditionsService
            .Setup(x => x.RetrieveExistingConditionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conditionsResponse);

        _notificationsService
            .Setup(x => x.SendNotificationAsync(It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(), It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConditionalStatusAsync(It.IsAny<Guid>(), It.IsAny<ConditionsStatusModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        var result = await sut.SendConditionsToApplicantAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _getWoodlandOfficerReviewService.Verify(x => x.GetDetailsForConditionsNotificationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _getWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _externalApplicantService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(applicationDetails.ApplicationAuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _externalApplicantService.VerifyNoOtherCalls();

        _woodlandOwnersService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(applicationDetails.WoodlandOwnerId, It.IsAny<CancellationToken>()));
        _woodlandOwnersService.VerifyNoOtherCalls();

        _conditionsService.Verify(x => x.RetrieveExistingConditionsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        var expectedConditionsText = new List<string>();
        foreach (var condition in conditionsResponse.Conditions)
        {
            expectedConditionsText.AddRange(condition.ToFormattedArray());
            expectedConditionsText.Add(string.Empty);
        }
        var expectedUrl = $"{_baseUri}FellingLicenceApplication/ApplicationTaskList/{applicationId}";

        _notificationsService.Verify(x => x.SendNotificationAsync(It.Is<ConditionsToApplicantDataModel>(
            m => m.ApplicationReference == applicationDetails.ApplicationReference 
                 && m.ConditionsText == string.Join(Environment.NewLine, expectedConditionsText) 
                 && m.Name == account.FullName(true) 
                 && m.ViewApplicationURL == expectedUrl
                 && m.PropertyName == applicationDetails.PropertyName
                 && m.SenderEmail == user.EmailAddress
                 && m.SenderName == user.FullName
                 && m.AdminHubFooter == adminHubFooter),
            NotificationType.ConditionsToApplicant, It.Is<NotificationRecipient>(r => r.Name == account.FullName(true) && r.Address == account.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _notificationsService.VerifyNoOtherCalls();

        _updateWoodlandOfficerReviewService.Verify(x => x.UpdateConditionalStatusAsync(applicationId, It.Is<ConditionsStatusModel>(c => c.IsConditional == true && c.ConditionsToApplicantDate == Now.ToDateTimeUtc()), user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    private ConditionsUseCase CreateSut()
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);
        var requestContext = new RequestContext(
            RequestContextCorrelationId,
            new RequestUserModel(user));

        var clock = new Mock<IClock>();
        clock.Setup(x => x.GetCurrentInstant()).Returns(Now);

        _getWoodlandOfficerReviewService.Reset();
        _conditionsService.Reset();
        _updateWoodlandOfficerReviewService.Reset();
        _auditService.Reset();
        _fellingAndRestockingService.Reset();
        _agentAuthorityService.Reset();
        _woodlandOwnersService.Reset();
        _getConfiguredFcAreas.Reset();

        return new ConditionsUseCase(
            new Mock<IUserAccountService>().Object,
            _externalApplicantService.Object,
            new Mock<IFellingLicenceApplicationInternalRepository>().Object,
            _woodlandOwnersService.Object,
            _getWoodlandOfficerReviewService.Object,
            _updateWoodlandOfficerReviewService.Object,
            _auditService.Object,
            requestContext,
            _conditionsService.Object,
            _fellingAndRestockingService.Object,
            _notificationsService.Object,
            _agentAuthorityService.Object,
            _getConfiguredFcAreas.Object,
            clock.Object,
            new OptionsWrapper<ExternalApplicantSiteOptions>(new ExternalApplicantSiteOptions{BaseUrl = _baseUri}),
            new NullLogger<ConditionsUseCase>());
    }
}
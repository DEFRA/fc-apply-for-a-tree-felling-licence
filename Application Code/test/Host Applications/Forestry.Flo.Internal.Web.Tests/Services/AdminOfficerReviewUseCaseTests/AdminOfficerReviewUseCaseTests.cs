using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System.Text.Json;
using Forestry.Flo.Internal.Web.Services.MassTransit.Messages;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;

namespace Forestry.Flo.Internal.Web.Tests.Services.AdminOfficerReviewUseCaseTests;

public class AdminOfficerReviewUseCaseTests
{
    private AdminOfficerReviewUseCase _sut;

    private readonly Fixture _fixture;
    private readonly Mock<IAuditService<AdminOfficerReviewUseCaseBase>> _mockAuditService;
    private readonly Mock<ISendNotifications> _sendNotifications;
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerRepository;
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly Mock<IUserAccountService> _internalUserAccountService;
    private readonly Mock<IRetrieveUserAccountsService> _externalUserAccountService;
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceRepository;
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _externalFellingLicenceRepository;
    private readonly Mock<ILogger<AdminOfficerReviewUseCase>> _logger;
    private RequestContext _requestContext;
    private readonly Mock<IViewCaseNotesService> _viewCaseNotesService;
    private readonly Mock<IActivityFeedItemProvider> _activityFeedItemProvider;
    private readonly Mock<IUpdateAdminOfficerReviewService> _updateAdminOfficerReviewService;
    private readonly Mock<IClock> _clock;
    private readonly Mock<IUpdateFellingLicenceApplication> _updateFellingLicenceApplication;
    private readonly Mock<IGetAdminOfficerReview> _getAdminOfficerReview;
    private readonly Mock<IGetFellingLicenceApplicationForInternalUsers> _getFellingLicenceApplicationForInternalUsers;
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService;
    private readonly Mock<ILarchCheckService> _larchCheckService;
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreasService;
    private readonly Mock<IUpdateConfirmedFellingAndRestockingDetailsService> _updateConfirmedFellingAndRestockingDetailsService;
    private readonly Mock<ICalculateConditions> _calculateConditionsService;
    private readonly Mock<IUpdateWoodlandOfficerReviewService> _updateWoodlandOfficerReviewService;
    private readonly Mock<IBusControl> _mockBus;
    private readonly Mock<IWoodlandOfficerReviewSubStatusService> _woodlandOfficerReviewSubStatusService = new();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly string AdminHubFooter = "admin hub address";

    public AdminOfficerReviewUseCaseTests()
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: Flo.Services.Common.User.AccountTypeInternal.AdminOfficer);
        new InternalUser(userPrincipal);

        _fixture = new Fixture();
        _mockAuditService = new Mock<IAuditService<AdminOfficerReviewUseCaseBase>>();
        _sendNotifications = new Mock<ISendNotifications>();
        _woodlandOwnerRepository = new Mock<IRetrieveWoodlandOwners>();
        new Mock<IAgencyRepository>();
        _fellingLicenceRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
        _externalUserAccountService = new Mock<IRetrieveUserAccountsService>();
        _internalUserAccountService = new Mock<IUserAccountService>();
        _logger = new Mock<ILogger<AdminOfficerReviewUseCase>>();
        _externalFellingLicenceRepository = new Mock<IFellingLicenceApplicationExternalRepository>();
        _viewCaseNotesService = new Mock<IViewCaseNotesService>();
        _activityFeedItemProvider = new Mock<IActivityFeedItemProvider>();
        _updateAdminOfficerReviewService = new Mock<IUpdateAdminOfficerReviewService>();
        _clock = new Mock<IClock>();
        _updateFellingLicenceApplication = new Mock<IUpdateFellingLicenceApplication>();
        _getAdminOfficerReview = new Mock<IGetAdminOfficerReview>();
        _agentAuthorityService = new Mock<IAgentAuthorityService>();
        _getFellingLicenceApplicationForInternalUsers = new Mock<IGetFellingLicenceApplicationForInternalUsers>();
        _larchCheckService = new Mock<ILarchCheckService>();
        _getConfiguredFcAreasService = new Mock<IGetConfiguredFcAreas>();
        _updateConfirmedFellingAndRestockingDetailsService = new Mock<IUpdateConfirmedFellingAndRestockingDetailsService>();
        _calculateConditionsService = new Mock<ICalculateConditions>();
        _updateWoodlandOfficerReviewService = new Mock<IUpdateWoodlandOfficerReviewService>();
        _mockBus = new Mock<IBusControl>();

        _fixture.Build<WoodlandOwner>()
            .With(wo => wo.IsOrganisation, true)
            .With(wo => wo.Id, Guid.NewGuid)
            .Create();

        _sut = CreateSut(Guid.NewGuid());
    }

    private AdminOfficerReviewUseCase CreateSut(Guid userId)
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: userId,
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);
        _requestContext = new RequestContext(
            _requestContextCorrelationId,
            new RequestUserModel(user));

        _getConfiguredFcAreasService.Reset();
        _getConfiguredFcAreasService
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubFooter);

        return new AdminOfficerReviewUseCase(
            _sendNotifications.Object,
            _mockAuditService.Object,
            _internalUserAccountService.Object,
            _externalUserAccountService.Object,
            _logger.Object,
            _requestContext,
            _fellingLicenceRepository.Object,
            _woodlandOwnerRepository.Object,
            _viewCaseNotesService.Object,
            _activityFeedItemProvider.Object,
            _updateAdminOfficerReviewService.Object,
            _clock.Object,
            _updateFellingLicenceApplication.Object,
            _getAdminOfficerReview.Object,
            _agentAuthorityService.Object,
            new OptionsWrapper<ExternalApplicantSiteOptions>(new ExternalApplicantSiteOptions { BaseUrl = "https://localhost/" }),
            new OptionsWrapper<LarchOptions>(new LarchOptions()),
            _getFellingLicenceApplicationForInternalUsers.Object,
            _larchCheckService.Object,
            _getConfiguredFcAreasService.Object,
            _updateConfirmedFellingAndRestockingDetailsService.Object,
            _updateWoodlandOfficerReviewService.Object,
            _woodlandOfficerReviewSubStatusService.Object,
            _calculateConditionsService.Object,
            _mockBus.Object);
    }

    [Theory, AutoMoqData]
    public async Task ShouldConfirmReview_GivenUserIsAssigned(
        FellingLicenceApplication fellingLicenceApplication,
        UserAccount woodlandOfficer,
        UserAccount adminOfficer,
        Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount applicant,
        string internalLinkToApplication,
        DateTime dateReceived)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: adminOfficer.Id,
            accountTypeInternal: AccountTypeInternal.AdminOfficer);
        var user = new InternalUser(userPrincipal);

        _sut = CreateSut(user.UserAccountId!.Value);

        //arrange
        _updateAdminOfficerReviewService
            .Setup(x => x.CompleteAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new CompleteAdminOfficerReviewNotificationsModel(
                fellingLicenceApplication.ApplicationReference, fellingLicenceApplication.CreatedById, woodlandOfficer.Id, "admin hub")));

        var now = new Instant();
        _clock.Setup(x => x.GetCurrentInstant()).Returns(now);

        _internalUserAccountService
            .SetupSequence(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(woodlandOfficer))
            .ReturnsAsync(Maybe<UserAccount>.From(adminOfficer));

        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);
        _sendNotifications.Setup(x => x.SendNotificationAsync(It.IsAny<object>(), It.IsAny<NotificationType>(),
                It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        //act
        var result =
            await _sut.ConfirmAdminOfficerReview(fellingLicenceApplication.Id, user, internalLinkToApplication, dateReceived, false, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);

        _updateAdminOfficerReviewService.Verify(x => x.CompleteAdminOfficerReviewAsync(fellingLicenceApplication.Id, user.UserAccountId!.Value, now.ToDateTimeUtc(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _updateAdminOfficerReviewService.VerifyNoOtherCalls();

        //can't verify properly due to both test user accounts will have empty guid id as they are autodata
        _internalUserAccountService
            .Verify(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _internalUserAccountService.VerifyNoOtherCalls();

        _externalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(fellingLicenceApplication.CreatedById, It.IsAny<CancellationToken>()), Times.Once);
        _externalFellingLicenceRepository.VerifyNoOtherCalls();

        _sendNotifications.Verify(x => x.SendNotificationAsync(It.Is<InformAssignedUserOfApplicationStatusTransitionDataModel>(
            y => y.PreviousAssignedUserName == adminOfficer.FullName(true) 
                 && y.PreviousAssignedEmailAddress == adminOfficer.Email
                 && y.ApplicationReference == fellingLicenceApplication.ApplicationReference 
                 && y.Name == woodlandOfficer.FullName(true) 
                 && y.ViewApplicationURL == internalLinkToApplication
                 && y.AdminHubFooter == AdminHubFooter),
            NotificationType.InformWoodlandOfficerOfAdminOfficerReviewCompletion,
            It.Is<NotificationRecipient>(n => n.Name == woodlandOfficer.FullName(true) && n.Address == woodlandOfficer.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);

        _sendNotifications.VerifyNoOtherCalls();

        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmAdminOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == fellingLicenceApplication.Id
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && a.AuditData == null),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmAdminOfficerReviewNotificationSent
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == fellingLicenceApplication.Id
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    recipient = "Woodland Officer",
                    recipientId = woodlandOfficer.Id
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmAdminOfficerReviewNotificationSent
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == fellingLicenceApplication.Id
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    recipient = "Applicant",
                    recipientId = applicant.Id
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.VerifyNoOtherCalls();

        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        _updateConfirmedFellingAndRestockingDetailsService.VerifyNoOtherCalls();

        _mockBus.Verify(x => x.Publish(
            It.Is<GenerateSubmittedPdfPreviewMessage>(m =>
                m.ApplicationId == fellingLicenceApplication.Id &&
                m.InternalUserId == user.UserAccountId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailConfirmingReview_GivenApplicationUpdateFailure(
        FellingLicenceApplication fellingLicenceApplication,
        UserAccount adminOfficer,
        string internalLinkToApplication,
        string error,
        DateTime dateReceived)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: adminOfficer.Id,
            accountTypeInternal: AccountTypeInternal.AdminOfficer);
        var user = new InternalUser(userPrincipal);

        //arrange
        _updateAdminOfficerReviewService
            .Setup(x => x.CompleteAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CompleteAdminOfficerReviewNotificationsModel>(error));

        var now = new Instant();
        _clock.Setup(x => x.GetCurrentInstant()).Returns(now);

        //act
        var result =
            await _sut.ConfirmAdminOfficerReview(fellingLicenceApplication.Id, user, internalLinkToApplication, dateReceived, false, CancellationToken.None);

        Assert.True(result.IsFailure);

        _updateAdminOfficerReviewService.Verify(x => x.CompleteAdminOfficerReviewAsync(fellingLicenceApplication.Id, user.UserAccountId!.Value, now.ToDateTimeUtc(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _updateAdminOfficerReviewService.VerifyNoOtherCalls();

        _internalUserAccountService.VerifyNoOtherCalls();
        _externalFellingLicenceRepository.VerifyNoOtherCalls();
        _sendNotifications.VerifyNoOtherCalls();

        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmAdminOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == fellingLicenceApplication.Id
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.VerifyNoOtherCalls();

        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();
        _mockBus.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailConfirmingReview_GivenNotificationFailure(
        FellingLicenceApplication fellingLicenceApplication,
        UserAccount woodlandOfficer,
        UserAccount adminOfficer,
        Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount applicant,
        string internalLinkToApplication,
        string error,
        DateTime dateReceived)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: adminOfficer.Id,
            accountTypeInternal: AccountTypeInternal.AdminOfficer);
        var user = new InternalUser(userPrincipal);

        _sut = CreateSut(user.UserAccountId!.Value);

        //arrange
        _updateAdminOfficerReviewService
            .Setup(x => x.CompleteAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new CompleteAdminOfficerReviewNotificationsModel(
                fellingLicenceApplication.ApplicationReference, fellingLicenceApplication.CreatedById, woodlandOfficer.Id, "admin hub")));

        var now = new Instant();
        _clock.Setup(x => x.GetCurrentInstant()).Returns(now);

        _internalUserAccountService
            .SetupSequence(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(woodlandOfficer))
            .ReturnsAsync(Maybe<UserAccount>.From(adminOfficer));

        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _sendNotifications.Setup(x => x.SendNotificationAsync(It.IsAny<object>(), It.IsAny<NotificationType>(),
                It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));
        
        //act
        var result =
            await _sut.ConfirmAdminOfficerReview(fellingLicenceApplication.Id, user, internalLinkToApplication, dateReceived, false, CancellationToken.None);

        //verify

        _updateAdminOfficerReviewService.Verify(x => x.CompleteAdminOfficerReviewAsync(fellingLicenceApplication.Id, user.UserAccountId!.Value, now.ToDateTimeUtc(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _updateAdminOfficerReviewService.VerifyNoOtherCalls();

        //can't verify properly due to both test user accounts will have empty guid id as they are autodata
        _internalUserAccountService
            .Verify(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _internalUserAccountService.VerifyNoOtherCalls();

        _externalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(fellingLicenceApplication.CreatedById, It.IsAny<CancellationToken>()), Times.Once);
        _externalFellingLicenceRepository.VerifyNoOtherCalls();

        _sendNotifications.Verify(x => x.SendNotificationAsync(It.Is<InformAssignedUserOfApplicationStatusTransitionDataModel>(
                y => y.PreviousAssignedUserName == adminOfficer.FullName(true)
                     && y.PreviousAssignedEmailAddress == adminOfficer.Email
                     && y.ApplicationReference == fellingLicenceApplication.ApplicationReference 
                     && y.Name == woodlandOfficer.FullName(true) 
                     && y.ViewApplicationURL == internalLinkToApplication
                     && y.AdminHubFooter == AdminHubFooter),
            NotificationType.InformWoodlandOfficerOfAdminOfficerReviewCompletion,
            It.Is<NotificationRecipient>(n => n.Name == woodlandOfficer.FullName(true) && n.Address == woodlandOfficer.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _sendNotifications.VerifyNoOtherCalls();

        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmAdminOfficerReviewNotificationFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == fellingLicenceApplication.Id
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = "Failed to send notification to woodland officer"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.VerifyNoOtherCalls();

        //assert
        Assert.False(result.IsSuccess);

        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();
        _mockBus.Verify(x => x.Publish(
            It.Is<GenerateSubmittedPdfPreviewMessage>(m =>
                m.ApplicationId == fellingLicenceApplication.Id &&
                m.InternalUserId == user.UserAccountId),
            It.IsAny<CancellationToken>()), Times.Once);
    }



    [Theory, AutoMoqData]
    public async Task ShouldCompleteLarchCheckSuccessfully(
        Guid applicationId,
        Guid performingUserId)
    {
        // Arrange
        var now = new Instant();
        _clock.Setup(x => x.GetCurrentInstant()).Returns(now);

        _updateAdminOfficerReviewService
            .Setup(x => x.SetLarchCheckCompletionAsync(applicationId, It.IsAny<bool>(), performingUserId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // Act
        var result = await _sut.CompleteLarchCheckAsync(applicationId, performingUserId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _updateAdminOfficerReviewService.Verify(x => x.SetLarchCheckCompletionAsync(applicationId, It.IsAny<bool>(), performingUserId, true, It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateAdminOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailLarchCheckCompletion_WhenServiceFails(
        Guid applicationId,
        Guid performingUserId,
        string error)
    {
        // Arrange
        _updateAdminOfficerReviewService
            .Setup(x => x.SetLarchCheckCompletionAsync(applicationId, It.IsAny<bool>(), performingUserId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _sut.CompleteLarchCheckAsync(applicationId, performingUserId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        _updateAdminOfficerReviewService.Verify(x => x.SetLarchCheckCompletionAsync(applicationId, It.IsAny<bool>(), performingUserId, true, It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateAdminOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailConfirmingReview_WhenUpdateWOReviewForCompletedFAndRFails(
        FellingLicenceApplication fellingLicenceApplication,
        UserAccount adminOfficer,
        string internalLinkToApplication,
        string error,
        DateTime dateReceived)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: adminOfficer.Id,
            accountTypeInternal: AccountTypeInternal.AdminOfficer);
        var user = new InternalUser(userPrincipal);

        // Arrange: CBWrequireWOReview = false
        _getAdminOfficerReview
            .Setup(x => x.GetCBWReviewStatusAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Setup fellingAndRestocking retrieval to succeed
        _updateConfirmedFellingAndRestockingDetailsService
            .Setup(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CombinedConfirmedFellingAndRestockingDetailRecord([], [], true));

        // Setup HandleConfirmedFellingAndRestockingChangesAsync to fail
        _updateWoodlandOfficerReviewService
            .Setup(x => x.HandleConfirmedFellingAndRestockingChangesAsync(It.IsAny<Guid>(), user.UserAccountId!.Value, It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Failure(error));

        var now = new Instant();
        _clock.Setup(x => x.GetCurrentInstant()).Returns(now);

        // Act
        var result = await _sut.ConfirmAdminOfficerReview(fellingLicenceApplication.Id, user, internalLinkToApplication, dateReceived, false, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(error, result.Error);

        _updateWoodlandOfficerReviewService.Verify(x => x.HandleConfirmedFellingAndRestockingChangesAsync(
            fellingLicenceApplication.Id, user.UserAccountId!.Value, true, It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailConfirmingReview_WhenGenerateConditionsFails(
        FellingLicenceApplication fellingLicenceApplication,
        UserAccount adminOfficer,
        string internalLinkToApplication,
        string error,
        DateTime dateReceived)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: adminOfficer.Id,
            accountTypeInternal: AccountTypeInternal.AdminOfficer);
        var user = new InternalUser(userPrincipal);

        // Arrange: CBWrequireWOReview = false
        _getAdminOfficerReview
            .Setup(x => x.GetCBWReviewStatusAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Setup fellingAndRestocking retrieval to succeed
        _updateConfirmedFellingAndRestockingDetailsService
            .Setup(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CombinedConfirmedFellingAndRestockingDetailRecord([], [], true));

        // Setup HandleConfirmedFellingAndRestockingChangesAsync to succeed
        _updateWoodlandOfficerReviewService
            .Setup(x => x.HandleConfirmedFellingAndRestockingChangesAsync(It.IsAny<Guid>(), user.UserAccountId!.Value, It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success());

        // Setup CalculateConditionsAsync to fail
        _calculateConditionsService
            .Setup(x => x.CalculateConditionsAsync(It.IsAny<CalculateConditionsRequest>(), user.UserAccountId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConditionsResponse>(error));

        var now = new Instant();
        _clock.Setup(x => x.GetCurrentInstant()).Returns(now);

        // Act
        var result = await _sut.ConfirmAdminOfficerReview(fellingLicenceApplication.Id, user, internalLinkToApplication, dateReceived, false, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(error, result.Error);

        _updateConfirmedFellingAndRestockingDetailsService.Verify(x =>
            x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()), Times.Once);
        _calculateConditionsService.Verify(x =>
            x.CalculateConditionsAsync(It.IsAny<CalculateConditionsRequest>(), user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _updateWoodlandOfficerReviewService.Verify(x => x.HandleConfirmedFellingAndRestockingChangesAsync(
            fellingLicenceApplication.Id, user.UserAccountId!.Value, true, It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldFailConfirmingReview_WhenRetrieveConfirmedFellingAndRestockingDetailModelFails(
        FellingLicenceApplication fellingLicenceApplication,
        UserAccount adminOfficer,
        string internalLinkToApplication,
        string error,
        DateTime dateReceived)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: adminOfficer.Id,
            accountTypeInternal: AccountTypeInternal.AdminOfficer);
        var user = new InternalUser(userPrincipal);

        // Arrange: CBWrequireWOReview = false
        _getAdminOfficerReview
            .Setup(x => x.GetCBWReviewStatusAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _updateConfirmedFellingAndRestockingDetailsService
            .Setup(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CombinedConfirmedFellingAndRestockingDetailRecord>(error));
        
        // Setup HandleConfirmedFellingAndRestockingChangesAsync to succeed
        _updateWoodlandOfficerReviewService
            .Setup(x => x.HandleConfirmedFellingAndRestockingChangesAsync(It.IsAny<Guid>(), user.UserAccountId!.Value, It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success());
        
        var now = new Instant();
        _clock.Setup(x => x.GetCurrentInstant()).Returns(now);

        // Act
        var result = await _sut.ConfirmAdminOfficerReview(fellingLicenceApplication.Id, user, internalLinkToApplication, dateReceived, false, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(error, result.Error);

        _updateConfirmedFellingAndRestockingDetailsService.Verify(x =>
            x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()), Times.Once);
        _calculateConditionsService.VerifyNoOtherCalls();
        _updateWoodlandOfficerReviewService.Verify(x => x.HandleConfirmedFellingAndRestockingChangesAsync(
            fellingLicenceApplication.Id, user.UserAccountId!.Value, true, It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldConfirmReview_AndPerformSideEffectsWhenCbw(
        FellingLicenceApplication fellingLicenceApplication,
        UserAccount woodlandOfficer,
        UserAccount adminOfficer,
        Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount applicant,
        string internalLinkToApplication,
        DateTime dateReceived,
        List<FellingAndRestockingDetailModel> compartments,
        List<CalculatedCondition> calculatedConditions)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: adminOfficer.Id,
            accountTypeInternal: AccountTypeInternal.AdminOfficer);
        var user = new InternalUser(userPrincipal);

        _sut = CreateSut(user.UserAccountId!.Value);

        _getAdminOfficerReview
            .Setup(x => x.GetCBWReviewStatusAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Setup fellingAndRestocking retrieval to succeed
        _updateConfirmedFellingAndRestockingDetailsService
            .Setup(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CombinedConfirmedFellingAndRestockingDetailRecord(compartments, [], true));

        // Setup HandleConfirmedFellingAndRestockingChangesAsync to succeed
        _updateWoodlandOfficerReviewService
            .Setup(x => x.HandleConfirmedFellingAndRestockingChangesAsync(It.IsAny<Guid>(), user.UserAccountId!.Value, It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Success());

        // Setup CalculateConditionsAsync to succeed
        _calculateConditionsService
            .Setup(x => x.CalculateConditionsAsync(It.IsAny<CalculateConditionsRequest>(), user.UserAccountId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionsResponse
            {
                Conditions = calculatedConditions
            });

        //arrange
        _updateAdminOfficerReviewService
            .Setup(x => x.CompleteAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new CompleteAdminOfficerReviewNotificationsModel(
                fellingLicenceApplication.ApplicationReference, fellingLicenceApplication.CreatedById, woodlandOfficer.Id, "admin hub")));

        var now = new Instant();
        _clock.Setup(x => x.GetCurrentInstant()).Returns(now);

        _internalUserAccountService
            .SetupSequence(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(woodlandOfficer))
            .ReturnsAsync(Maybe<UserAccount>.From(adminOfficer));

        _externalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);
        _sendNotifications.Setup(x => x.SendNotificationAsync(It.IsAny<object>(), It.IsAny<NotificationType>(),
                It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        //act
        var result =
            await _sut.ConfirmAdminOfficerReview(fellingLicenceApplication.Id, user, internalLinkToApplication, dateReceived, false, CancellationToken.None);

        //verify

        _updateAdminOfficerReviewService.Verify(x => x.CompleteAdminOfficerReviewAsync(fellingLicenceApplication.Id, user.UserAccountId!.Value, now.ToDateTimeUtc(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _updateAdminOfficerReviewService.VerifyNoOtherCalls();

        //can't verify properly due to both test user accounts will have empty guid id as they are autodata
        _internalUserAccountService
            .Verify(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _internalUserAccountService.VerifyNoOtherCalls();

        _externalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(fellingLicenceApplication.CreatedById, It.IsAny<CancellationToken>()), Times.Once);
        _externalFellingLicenceRepository.VerifyNoOtherCalls();

        _sendNotifications.Verify(x => x.SendNotificationAsync(It.Is<InformAssignedUserOfApplicationStatusTransitionDataModel>(
            y => y.PreviousAssignedUserName == adminOfficer.FullName(true)
                 && y.PreviousAssignedEmailAddress == adminOfficer.Email
                 && y.ApplicationReference == fellingLicenceApplication.ApplicationReference
                 && y.Name == woodlandOfficer.FullName(true)
                 && y.ViewApplicationURL == internalLinkToApplication
                 && y.AdminHubFooter == AdminHubFooter),
            NotificationType.InformWoodlandOfficerOfAdminOfficerReviewCompletion,
            It.Is<NotificationRecipient>(n => n.Name == woodlandOfficer.FullName(true) && n.Address == woodlandOfficer.Email),
            null, null, null, It.IsAny<CancellationToken>()), Times.Once);

        _sendNotifications.VerifyNoOtherCalls();

        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmAdminOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == fellingLicenceApplication.Id
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && a.AuditData == null),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmAdminOfficerReviewNotificationSent
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == fellingLicenceApplication.Id
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    recipient = "Woodland Officer",
                    recipientId = woodlandOfficer.Id
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConfirmAdminOfficerReviewNotificationSent
                && a.ActorType == ActorType.InternalUser
                && a.UserId == user.UserAccountId!.Value
                && a.SourceEntityId == fellingLicenceApplication.Id
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    recipient = "Applicant",
                    recipientId = applicant.Id
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.VerifyNoOtherCalls();

        _updateConfirmedFellingAndRestockingDetailsService.Verify(x =>
            x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fellingLicenceApplication.Id, It.IsAny<CancellationToken>()), Times.Once);

        _calculateConditionsService.Verify(x =>
            x.CalculateConditionsAsync(It.IsAny<CalculateConditionsRequest>(), user.UserAccountId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        
        _updateWoodlandOfficerReviewService.Verify(x => x.HandleConfirmedFellingAndRestockingChangesAsync(
            fellingLicenceApplication.Id, user.UserAccountId!.Value, true, It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
        _updateWoodlandOfficerReviewService.VerifyNoOtherCalls();
        //assert
        Assert.True(result.IsSuccess);
    }
}
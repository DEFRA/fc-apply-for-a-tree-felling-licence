using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System.Text.Json;
using ExternalAccountModel = Forestry.Flo.Services.Applicants.Models.UserAccountModel;
using VoluntaryWithdrawalNotificationOptions = Forestry.Flo.Internal.Web.Infrastructure.VoluntaryWithdrawalNotificationOptions;
using VoluntaryWithdrawalNotificationUseCase = Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api.VoluntaryWithdrawalNotificationUseCase;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class VoluntaryWithdrawalNotificationUseCaseTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IVoluntaryWithdrawalNotificationService> _withdrawalNotificationApplicationsMock;
    private readonly Mock<IRetrieveUserAccountsService> _externalAccountServiceMock;
    private readonly Mock<ISendNotifications> _notificationsMock;
    private readonly Mock<IOptions<VoluntaryWithdrawalNotificationOptions>> _settingsMock;
    private readonly Mock<IOptions<ExternalApplicantSiteOptions>> _externalSiteOptionsMock;
    private readonly Mock<IAuditService<VoluntaryWithdrawalNotificationUseCase>> _auditMock;
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerServiceMock;
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreasServiceMock;

    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly TimeSpan _threshold;
    private const string ExternalUrl = "externalUrl";
    private const string AdminHubFooter = "admin hub address";

    public VoluntaryWithdrawalNotificationUseCaseTests()
    {
        _clockMock = new Mock<IClock>();
        _externalAccountServiceMock = new Mock<IRetrieveUserAccountsService>();
        _withdrawalNotificationApplicationsMock = new Mock<IVoluntaryWithdrawalNotificationService>();
        _notificationsMock = new Mock<ISendNotifications>();
        _settingsMock = new Mock<IOptions<VoluntaryWithdrawalNotificationOptions>>();
        _externalSiteOptionsMock = new Mock<IOptions<ExternalApplicantSiteOptions>>();
        _auditMock = new Mock<IAuditService<VoluntaryWithdrawalNotificationUseCase>>();
        _woodlandOwnerServiceMock = new Mock<IRetrieveWoodlandOwners>();
        _getConfiguredFcAreasServiceMock = new Mock<IGetConfiguredFcAreas>();

        _threshold = new TimeSpan(14, 0, 0, 0);
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task ApplicantNotified_WhenNoFailures(
        List<VoluntaryWithdrawalNotificationModel> voluntaryWithdrawalNotificationModels,
        ExternalAccountModel applicant,
        WoodlandOwnerModel woodlandOwner)
    {
        var sut = CreateSut();

        var currentDate = DateTime.UtcNow.Date;

        foreach (var notificationModel in voluntaryWithdrawalNotificationModels)
        {
            notificationModel.CreatedById = applicant.UserAccountId;
            notificationModel.WoodlandOwnerId = (Guid)woodlandOwner.Id;
            notificationModel.WithApplicantDate = currentDate.Subtract(new TimeSpan(16, 0, 0, 0));
        }

        _withdrawalNotificationApplicationsMock.Setup(s =>
                s.GetApplicationsAfterThresholdForWithdrawalAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(voluntaryWithdrawalNotificationModels);
        
        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);

        _notificationsMock
            .Setup(x => x.SendNotificationAsync(It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(), It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await sut.SendNotificationForWithdrawalAsync("baseUrl", CancellationToken.None);

        _withdrawalNotificationApplicationsMock.Verify(v => v.GetApplicationsAfterThresholdForWithdrawalAsync(_threshold, CancellationToken.None), Times.Once);

        foreach (var notificationModel in voluntaryWithdrawalNotificationModels)
        {
            _auditMock.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.ApplicationVoluntaryWithdrawalNotification
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 ApplicationId = notificationModel.ApplicationId,
                                 DaysInWithApplicant = currentDate.Subtract(notificationModel.WithApplicantDate).Days,
                                 NotificationSentDate = currentDate.Date
                             }, _options)),
                    CancellationToken.None), Times.Once);

            _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationVoluntaryWithdrawOptionDataModel>(n => 
                    n.ApplicationReference == notificationModel.ApplicationReference
                    && n.PropertyName == notificationModel.PropertyName
                    && n.DaysInWithApplicantStatus == currentDate.Subtract(notificationModel.WithApplicantDate).Days
                    && n.WithApplicantDateTime == DateTimeDisplay.GetDateDisplayString(notificationModel.WithApplicantDate)
                    && n.AdminHubFooter == AdminHubFooter
                    && n.Name == applicant.FullName
                        .Trim()
                        .Replace("  ", " ")),
                NotificationType.InformApplicantOfApplicationVoluntaryWithdrawOption,
                It.IsAny<NotificationRecipient>(),
                new NotificationRecipient[]{new NotificationRecipient(woodlandOwner.ContactEmail!, woodlandOwner.ContactName)},
                null,
                null,
                CancellationToken.None), Times.Once);
        }
    }
    
    private VoluntaryWithdrawalNotificationUseCase CreateSut()
    {
        _clockMock.Reset();
        _withdrawalNotificationApplicationsMock.Reset();
        _externalAccountServiceMock.Reset();
        _notificationsMock.Reset();
        _woodlandOwnerServiceMock.Reset();
        _getConfiguredFcAreasServiceMock.Reset();

        _settingsMock.Setup(x => x.Value).Returns(new VoluntaryWithdrawalNotificationOptions {ThresholdAfterWithApplicantStatusDate = _threshold });
        _externalSiteOptionsMock.Setup(s => s.Value).Returns(new ExternalApplicantSiteOptions{BaseUrl = ExternalUrl});
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));
        _getConfiguredFcAreasServiceMock
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubFooter);

        return new VoluntaryWithdrawalNotificationUseCase(
            _clockMock.Object,
            _withdrawalNotificationApplicationsMock.Object,
            _settingsMock.Object,
            new NullLogger<VoluntaryWithdrawalNotificationUseCase>(),
            _externalAccountServiceMock.Object,
            _notificationsMock.Object,
            new RequestContext(
                _requestContextCorrelationId,
                new RequestUserModel(UserFactory.CreateUnauthenticatedUser())),
            _auditMock.Object,
            _externalSiteOptionsMock.Object,
            _woodlandOwnerServiceMock.Object,
            _getConfiguredFcAreasServiceMock.Object);
    }
}
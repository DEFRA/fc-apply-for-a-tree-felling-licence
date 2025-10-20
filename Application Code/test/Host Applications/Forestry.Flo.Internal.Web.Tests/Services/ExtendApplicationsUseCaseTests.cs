using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
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
using System.Text.Json;
using Forestry.Flo.Services.Applicants.Models;
using ExternalAccountModel = Forestry.Flo.Services.Applicants.Models.UserAccountModel;
using UserAccountModel = Forestry.Flo.Services.InternalUsers.Models.UserAccountModel;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class ExtendApplicationsUseCaseTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IExtendApplications> _extendApplicationsMock;
    private readonly Mock<IRetrieveUserAccountsService> _externalAccountServiceMock;
    private readonly Mock<IUserAccountService> _internalAccountServiceMock;
    private readonly Mock<ISendNotifications> _notificationsMock;
    private readonly Mock<IOptions<ApplicationExtensionOptions>> _settingsMock;
    private readonly Mock<IOptions<ExternalApplicantSiteOptions>> _externalSiteOptionsMock;
    private readonly Mock<IAuditService<ExtendApplicationsUseCase>> _auditMock;
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerServiceMock;
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreasMock;

    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly TimeSpan _extensionLength;
    private readonly TimeSpan _threshold;
    private const string ExternalUrl = "externalUrl";
    private const string AdminHubFooter = "admin hub address";

    public ExtendApplicationsUseCaseTests()
    {
        _clockMock = new Mock<IClock>();
        _internalAccountServiceMock = new Mock<IUserAccountService>();
        _externalAccountServiceMock = new Mock<IRetrieveUserAccountsService>();
        _extendApplicationsMock = new Mock<IExtendApplications>();
        _notificationsMock = new Mock<ISendNotifications>();
        _settingsMock = new Mock<IOptions<ApplicationExtensionOptions>>();
        _externalSiteOptionsMock = new Mock<IOptions<ExternalApplicantSiteOptions>>();
        _auditMock = new Mock<IAuditService<ExtendApplicationsUseCase>>();
        _woodlandOwnerServiceMock = new Mock<IRetrieveWoodlandOwners>();
        _getConfiguredFcAreasMock = new Mock<IGetConfiguredFcAreas>();

        _extensionLength = new TimeSpan(90, 0, 0, 0);
        _threshold = new TimeSpan(10, 0, 0, 0);
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task ApplicantAndAssignedFCStaffMembersNotified_WhenNoFailures(
        List<ApplicationExtensionModel> extensionModels,
        List<UserAccountModel> internalUsers,
        ExternalAccountModel applicant,
        WoodlandOwnerModel woodlandOwner)
    {
        var sut = CreateSut();

        foreach (var extensionModel in extensionModels)
        {
            extensionModel.AssignedFCUserIds = internalUsers.Select(x => x.UserAccountId).ToList();
            extensionModel.CreatedById = applicant.UserAccountId;
            extensionModel.FinalActionDate = DateTime.UtcNow.Add(_extensionLength);
            extensionModel.ExtensionLength = _extensionLength;
        }

        _extendApplicationsMock.Setup(s =>
                s.ApplyApplicationExtensionsAsync(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(extensionModels);

        _internalAccountServiceMock.Setup(s =>
                s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(internalUsers);

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);

        _notificationsMock
            .Setup(x => x.SendNotificationAsync(It.IsAny<object>(), It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(), It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await sut.ExtendApplicationFinalActionDatesAsync("baseUrl", CancellationToken.None);

        _extendApplicationsMock.Verify(v => v.ApplyApplicationExtensionsAsync(_extensionLength, _threshold, CancellationToken.None), Times.Once);

        var currentDate = DateTime.UtcNow.Date;

        foreach (var extensionModel in extensionModels)
        {
            _auditMock.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.FellingLicenceApplicationFinalActionDateUpdated
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 SubmittedDate = extensionModel.SubmissionDate,
                                 FinalActionDate = extensionModel.FinalActionDate
                             }, _options)),
                    CancellationToken.None), Times.Once);

            _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationExtensionDataModel>(n => 
                    n.FinalActionDate == DateTimeDisplay.GetDateDisplayString(extensionModel.FinalActionDate)
                    && n.ApplicationReference == extensionModel.ApplicationReference
                    && n.ExtensionLength == _extensionLength.Days
                    && n.AdminHubFooter == AdminHubFooter
                    && n.Name == $"{applicant.FirstName} {applicant.LastName}"
                        .Trim()
                        .Replace("  ", " ")),
                NotificationType.InformApplicantOfApplicationExtension,
                It.IsAny<NotificationRecipient>(),
                new NotificationRecipient[]{new NotificationRecipient(woodlandOwner.ContactEmail!, woodlandOwner.ContactName)},
                null,
                null,
                CancellationToken.None), Times.Once);

            foreach (var internalUser in internalUsers)
            {
                _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformFCStaffOfFinalActionDateReachedDataModel>(
                    n =>
                        n.FinalActionDate == DateTimeDisplay.GetDateDisplayString(extensionModel.FinalActionDate)
                        && n.ApplicationReference == extensionModel.ApplicationReference
                        && n.ExtensionLength!.Value == _extensionLength.Days
                        && n.Name == $"{internalUser.Title} {internalUser.FirstName} {internalUser.LastName}"
                            .Trim()
                            .Replace("  ", " ")
                        && n.DaysUntilFinalActionDate == (currentDate - extensionModel.FinalActionDate).Days
                        && n.AdminHubFooter == AdminHubFooter),
                    NotificationType.InformFCStaffOfFinalActionDateReached,
                    It.IsAny<NotificationRecipient>(),
                    null,
                    null,
                    null,
                    CancellationToken.None), Times.Once);
            }

            _auditMock.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.ApplicationExtensionNotification
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 FinalActionDate = extensionModel.FinalActionDate,
                                 PreviousActionDate = extensionModel.FinalActionDate.Subtract(extensionModel.ExtensionLength ?? TimeSpan.Zero),
                                 ApplicationExtended = true,
                                 NumberOfFcStaffNotificationRecipients = internalUsers.Count
                             }, _options)),
                    CancellationToken.None), Times.Once);
        }
    }

    private ExtendApplicationsUseCase CreateSut()
    {
        _clockMock.Reset();
        _internalAccountServiceMock.Reset();
        _extendApplicationsMock.Reset();
        _externalAccountServiceMock.Reset();
        _notificationsMock.Reset();
        _woodlandOwnerServiceMock.Reset();
        _getConfiguredFcAreasMock.Reset();

        _settingsMock.Setup(x => x.Value).Returns(new ApplicationExtensionOptions {ExtensionLength = _extensionLength, ThresholdBeforeFinalActionDate = _threshold });
        _externalSiteOptionsMock.Setup(s => s.Value).Returns(new ExternalApplicantSiteOptions{BaseUrl = ExternalUrl});
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));
        _getConfiguredFcAreasMock.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubFooter);

        return new ExtendApplicationsUseCase(
            _clockMock.Object,
            _extendApplicationsMock.Object,
            _settingsMock.Object,
            new NullLogger<ExtendApplicationsUseCase>(),
            _internalAccountServiceMock.Object,
            _externalAccountServiceMock.Object,
            _notificationsMock.Object,
            new RequestContext(
                _requestContextCorrelationId,
                new RequestUserModel(UserFactory.CreateUnauthenticatedUser())),
            _auditMock.Object,
            _getConfiguredFcAreasMock.Object,
            _externalSiteOptionsMock.Object,
            _woodlandOwnerServiceMock.Object);
    }
}
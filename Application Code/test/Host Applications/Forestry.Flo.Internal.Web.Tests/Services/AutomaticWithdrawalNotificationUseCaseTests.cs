using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
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
using Forestry.Flo.Services.Gis.Interfaces;
using ExternalAccountModel = Forestry.Flo.Services.Applicants.Models.UserAccountModel;
using VoluntaryWithdrawalNotificationOptions = Forestry.Flo.Internal.Web.Infrastructure.VoluntaryWithdrawalNotificationOptions;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class AutomaticWithdrawalNotificationUseCaseTests
{
    private readonly Mock<IClock> _clockMock = new();
    private readonly Mock<IWithdrawalNotificationService> _withdrawalNotificationServiceMock = new();
    private readonly Mock<IRetrieveUserAccountsService> _externalAccountServiceMock = new();
    private readonly Mock<IUserAccountService> _internalAccountServiceMock = new();
    private readonly Mock<ISendNotifications> _notificationsMock = new();
    private readonly Mock<IOptions<VoluntaryWithdrawalNotificationOptions>> _settingsMock = new();
    private readonly Mock<IOptions<ExternalApplicantSiteOptions>> _externalSiteOptionsMock = new();
    private readonly Mock<IAuditService<AutomaticWithdrawalNotificationUseCase>> _auditMock = new();
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerServiceMock = new();
    private readonly Mock<IUserAccountRepository> _userAccountRepositoryMock = new();
    private readonly Mock<IWithdrawFellingLicenceService> _withdrawFellingLicenceService = new();
    private readonly Mock<IPublicRegister> _publicRegisterMock = new();
    private readonly Mock<IGetWoodlandOfficerReviewService> _woodlandOfficerReviewServiceMock = new();
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationExternalRepositoryMock = new();
    private readonly Mock<IDbContextTransaction> _transactionMock = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();

    private const string AdminHubAddress = "admin hub address";

    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly TimeSpan _threshold = new(14, 0, 0, 0);
    private const string ExternalUrl = "externalUrl";

    public AutomaticWithdrawalNotificationUseCaseTests()
    {
        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);
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
        voluntaryWithdrawalNotificationModels = voluntaryWithdrawalNotificationModels.Take(1).ToList();

        var currentDate = DateTime.UtcNow.Date;

        foreach (var notificationModel in voluntaryWithdrawalNotificationModels)
        {
            notificationModel.CreatedById = applicant.UserAccountId;
            notificationModel.WoodlandOwnerId = (Guid)woodlandOwner.Id;
            notificationModel.WithApplicantDate = currentDate.Subtract(new TimeSpan(16, 0, 0, 0));
        }

        _withdrawalNotificationServiceMock.Setup(s =>
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

        Guid userId1 = Guid.NewGuid();

        _withdrawFellingLicenceService.Setup(r => r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>
            {
                userId1
            });

        _userAccountRepositoryMock.Setup(x => x.GetUsersWithIdsInAsync(new List<Guid> { userId1 }, CancellationToken.None)).ReturnsAsync(new List<UserAccount>
        {
            new UserAccountProxy(userId1)
        });

        await sut.ProcessApplicationsAsync("baseUrl", _withdrawFellingLicenceService.Object, CancellationToken.None);

        _withdrawalNotificationServiceMock.Verify(v => v.GetApplicationsAfterThresholdForWithdrawalAsync(_threshold, CancellationToken.None), Times.Once);

        foreach (var notificationModel in voluntaryWithdrawalNotificationModels)
        {
            _withdrawFellingLicenceService.Verify(r => r.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), CancellationToken.None), Times.Once);

            _auditMock.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.WithdrawFellingLicenceApplication
                             && JsonSerializer.Serialize(e.AuditData, _options) ==
                             JsonSerializer.Serialize(new
                             {
                                 ApplicationId = notificationModel.ApplicationId,
                                 WithdrawalDate = currentDate.Date
                             }, _options)),
                    CancellationToken.None), Times.Once);

            _notificationsMock.Verify(v => v.SendNotificationAsync(It.IsAny<ApplicationWithdrawnConfirmationDataModel>(),
                NotificationType.ApplicationWithdrawnConfirmation,
                It.IsAny<NotificationRecipient>(),
                new NotificationRecipient[]{new NotificationRecipient(woodlandOwner.ContactEmail!, woodlandOwner.ContactName)},
                null,
                null,
                CancellationToken.None), Times.Once);

            _notificationsMock.Verify(v => v.SendNotificationAsync(It.IsAny<ApplicationWithdrawnConfirmationDataModel>(),
                NotificationType.ApplicationWithdrawn,
                It.IsAny<NotificationRecipient>(),
                It.IsAny<NotificationRecipient[]?>(),
                null,
                It.IsAny<string?>(),
                CancellationToken.None), Times.Once);
        }


        _transactionMock.Verify(
            t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ProcessApplicationsAsync_PublicRegisterRemoval_Success(
        List<VoluntaryWithdrawalNotificationModel> voluntaryWithdrawalNotificationModels,
        PublicRegisterModel publicRegister,
        ExternalAccountModel applicant,
        WoodlandOwnerModel woodlandOwner)
    {
        // Arrange
        var sut = CreateSut();
        voluntaryWithdrawalNotificationModels = voluntaryWithdrawalNotificationModels.Take(1).ToList();
        var application = voluntaryWithdrawalNotificationModels.First();
        application.ApplicationReference = "FLA123";

        _withdrawalNotificationServiceMock
            .Setup(s => s.GetApplicationsAfterThresholdForWithdrawalAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<VoluntaryWithdrawalNotificationModel>>(voluntaryWithdrawalNotificationModels));

        _withdrawFellingLicenceService
            .Setup(s => s.WithdrawApplication(application.ApplicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Guid>>(new List<Guid>()));

        _woodlandOfficerReviewServiceMock
            .Setup(s => s.GetPublicRegisterDetailsAsync(application.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe<PublicRegisterModel>.From(publicRegister)));

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwner);

        _withdrawFellingLicenceService.Setup(x =>
                x.UpdatePublicRegisterEntityToRemovedAsync(
                    application.ApplicationId,
                    null,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _userAccountRepositoryMock.Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<List<Guid>>(), CancellationToken.None)).ReturnsAsync(new List<UserAccount>());

        publicRegister.EsriId = 123;
        publicRegister.ConsultationPublicRegisterRemovedTimestamp = null;

        _publicRegisterMock
            .Setup(s => s.RemoveCaseFromConsultationRegisterAsync(publicRegister.EsriId.Value, application.ApplicationReference, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await sut.ProcessApplicationsAsync("baseUrl", _withdrawFellingLicenceService.Object, CancellationToken.None);

        // Assert
        _publicRegisterMock.Verify(
            v => v.RemoveCaseFromConsultationRegisterAsync(publicRegister.EsriId.Value, application.ApplicationReference, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(
            t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ProcessApplicationsAsync_PublicRegisterRemoval_Failure(
        List<VoluntaryWithdrawalNotificationModel> voluntaryWithdrawalNotificationModels,
        PublicRegisterModel publicRegister)
    {
        // Arrange
        var sut = CreateSut();
        voluntaryWithdrawalNotificationModels = voluntaryWithdrawalNotificationModels.Take(1).ToList();
        var application = voluntaryWithdrawalNotificationModels.First();
        application.ApplicationReference = "FLA123";

        _withdrawalNotificationServiceMock
            .Setup(s => s.GetApplicationsAfterThresholdForWithdrawalAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<VoluntaryWithdrawalNotificationModel>>(voluntaryWithdrawalNotificationModels));

        _withdrawFellingLicenceService
            .Setup(s => s.WithdrawApplication(application.ApplicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Guid>>(new List<Guid>()));

        _woodlandOfficerReviewServiceMock
            .Setup(s => s.GetPublicRegisterDetailsAsync(application.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe<PublicRegisterModel>.From(publicRegister)));

        publicRegister.EsriId = 123;
        publicRegister.ConsultationPublicRegisterRemovedTimestamp = null;

        _publicRegisterMock
            .Setup(s => s.RemoveCaseFromConsultationRegisterAsync(publicRegister.EsriId.Value, application.ApplicationReference, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Error removing case from public register"));

        // Act
        await sut.ProcessApplicationsAsync("baseUrl", _withdrawFellingLicenceService.Object, CancellationToken.None);

        // Assert
        _publicRegisterMock.Verify(
            v => v.RemoveCaseFromConsultationRegisterAsync(publicRegister.EsriId.Value, application.ApplicationReference, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                e => e.EventName == AuditEvents.WithdrawFellingLicenceApplicationFailure
                     && JsonSerializer.Serialize(e.AuditData, _options).Contains("Error removing case from public register")),
                It.IsAny<CancellationToken>()), Times.Once);

        _transactionMock.Verify(
            t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ProcessApplicationsAsync_PublicRegisterEntityUpdate_Failure(
        List<VoluntaryWithdrawalNotificationModel> voluntaryWithdrawalNotificationModels,
        PublicRegisterModel publicRegister)
    {
        // Arrange
        var sut = CreateSut();
        voluntaryWithdrawalNotificationModels = voluntaryWithdrawalNotificationModels.Take(1).ToList();
        var application = voluntaryWithdrawalNotificationModels.First();
        application.ApplicationReference = "FLA123";

        _withdrawalNotificationServiceMock
            .Setup(s => s.GetApplicationsAfterThresholdForWithdrawalAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<VoluntaryWithdrawalNotificationModel>>(voluntaryWithdrawalNotificationModels));

        _withdrawFellingLicenceService
            .Setup(s => s.WithdrawApplication(application.ApplicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Guid>>(new List<Guid>()));

        _woodlandOfficerReviewServiceMock
            .Setup(s => s.GetPublicRegisterDetailsAsync(application.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe<PublicRegisterModel>.From(publicRegister)));

        publicRegister.EsriId = 123;
        publicRegister.ConsultationPublicRegisterRemovedTimestamp = null;

        _publicRegisterMock
            .Setup(s => s.RemoveCaseFromConsultationRegisterAsync(publicRegister.EsriId.Value, application.ApplicationReference, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _withdrawFellingLicenceService.Setup(x =>
                x.UpdatePublicRegisterEntityToRemovedAsync(
                    application.ApplicationId,
                    null,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error updating entity"));

        // Act
        await sut.ProcessApplicationsAsync("baseUrl", _withdrawFellingLicenceService.Object, CancellationToken.None);

        // Assert
        _publicRegisterMock.Verify(
            v => v.RemoveCaseFromConsultationRegisterAsync(publicRegister.EsriId.Value, application.ApplicationReference, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _auditMock.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                e => e.EventName == AuditEvents.WithdrawFellingLicenceApplicationFailure
                     && JsonSerializer.Serialize(e.AuditData, _options).Contains("error updating entity")),
                It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(
            t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _withdrawFellingLicenceService.Verify(x =>
            x.UpdatePublicRegisterEntityToRemovedAsync(
                application.ApplicationId,
                null,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private AutomaticWithdrawalNotificationUseCase CreateSut()
    {
        _clockMock.Reset();
        _internalAccountServiceMock.Reset();
        _withdrawalNotificationServiceMock.Reset();
        _externalAccountServiceMock.Reset();
        _notificationsMock.Reset();
        _woodlandOwnerServiceMock.Reset();
        _withdrawFellingLicenceService.Reset();
        _publicRegisterMock.Reset();
        _auditMock.Reset();
        _fellingLicenceApplicationExternalRepositoryMock.Reset();
        _transactionMock.Reset();
        _userAccountRepositoryMock.Reset();

        _settingsMock.Setup(x => x.Value).Returns(new VoluntaryWithdrawalNotificationOptions { ThresholdAutomaticWithdrawal = _threshold });
        _externalSiteOptionsMock.Setup(s => s.Value).Returns(new ExternalApplicantSiteOptions { BaseUrl = ExternalUrl });
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));
        _fellingLicenceApplicationExternalRepositoryMock.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);
        _transactionMock.Setup(r => r.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _transactionMock.Setup(r => r.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new AutomaticWithdrawalNotificationUseCase(
            _clockMock.Object,
            _withdrawalNotificationServiceMock.Object,
            _settingsMock.Object,
            new NullLogger<AutomaticWithdrawalNotificationUseCase>(),
            _externalAccountServiceMock.Object,
            _notificationsMock.Object,
            new RequestContext(
                _requestContextCorrelationId,
                new RequestUserModel(UserFactory.CreateUnauthenticatedUser())),
            _auditMock.Object,
            _externalSiteOptionsMock.Object,
            _woodlandOwnerServiceMock.Object,
            _getConfiguredFcAreas.Object,
            _userAccountRepositoryMock.Object,
            _fellingLicenceApplicationExternalRepositoryMock.Object,
            _woodlandOfficerReviewServiceMock.Object,
            _publicRegisterMock.Object);
    }

    /// <summary>
    /// Proxy class facilitates setting of protected Id
    /// </summary>
    private class UserAccountProxy : UserAccount
    {
        public UserAccountProxy(Guid id)
        {
            this.Id = id;
        }
    }
}

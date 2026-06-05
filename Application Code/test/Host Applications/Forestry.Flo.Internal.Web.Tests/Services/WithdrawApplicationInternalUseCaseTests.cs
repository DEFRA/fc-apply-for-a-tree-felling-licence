using CSharpFunctionalExtensions;
using Forestry.Flo.HostApplicationsCommon.Services;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System.Security.Claims;
using System.Text.Json;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class WithdrawApplicationInternalUseCaseTests
{
    private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _getFellingLicenceApplicationServiceForExternalUsers = new();
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationExternalRepository = new();
    private readonly Mock<IWithdrawFellingLicenceService> _withdrawFellingLicenceService = new();
    private readonly Mock<IAuditService<WithdrawApplicationUseCaseBase>> _auditService = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IPublicRegister> _publicRegisterService = new();
    private readonly Mock<IGetPropertyProfiles> _getPropertyProfilesService = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreasService = new();
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerService = new();
    private readonly Mock<IRetrieveUserAccountsService> _retrieveExternalAccountsService = new();
    private readonly Mock<IUserAccountService> _internalUserAccountService = new();
    private readonly Mock<ISendNotifications> _sendNotifications = new();
    private ExternalApplicantSiteOptions _externalUserSiteOptions;
    private readonly RequestContext _requestContext = new("test", new RequestUserModel(new ClaimsPrincipal()));
    private readonly DateTime _now = DateTime.UtcNow;
    private readonly Mock<IDbContextTransaction> _dbContextTransaction = new();

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task WhenUnableToWithdrawApplication(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        string error)
    {
        var sut = CreateSut();

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<Guid>>(error));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        _dbContextTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawFailure
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        Section = "Withdraw FLA",
                        Error = error
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();
        _clock.VerifyNoOtherCalls();
        _publicRegisterService.VerifyNoOtherCalls();
        _getPropertyProfilesService.VerifyNoOtherCalls();
        _getConfiguredFcAreasService.VerifyNoOtherCalls();
        _woodlandOwnerService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUnableToRetrieveApplication(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        List<Guid> assignedInternalUsers,
        string error)
    {
        var sut = CreateSut();

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignedInternalUsers));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        _getFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>(error));

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        _dbContextTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawFailure
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        Section = "Withdraw FLA",
                        Error = $"Application {applicationId} could not be retrieved"
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();
        _clock.VerifyNoOtherCalls();
        _publicRegisterService.VerifyNoOtherCalls();
        _getPropertyProfilesService.VerifyNoOtherCalls();
        _getConfiguredFcAreasService.VerifyNoOtherCalls();
        _woodlandOwnerService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoLinkedPropertyProfile(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        List<Guid> assignedInternalUsers,
        FellingLicenceApplication application)
    {
        application.LinkedPropertyProfile = null;

        var sut = CreateSut();

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignedInternalUsers));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        _getFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        _dbContextTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawFailure
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        Section = "Withdraw FLA",
                        Error = $"Application {applicationId} has no linked property profile"
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();
        _clock.VerifyNoOtherCalls();
        _publicRegisterService.VerifyNoOtherCalls();
        _getPropertyProfilesService.VerifyNoOtherCalls();
        _getConfiguredFcAreasService.VerifyNoOtherCalls();
        _woodlandOwnerService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationRemovalFromPublicRegisterFails(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        List<Guid> assignedInternalUsers,
        FellingLicenceApplication application,
        string error)
    {
        application.PublicRegister.ConsultationPublicRegisterRemovedTimestamp = null;

        var sut = CreateSut();

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignedInternalUsers));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        _getFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        _dbContextTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawFailure
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        WoodlandOwnerId = application.WoodlandOwnerId,
                        Section = "Withdraw FLA",
                        Error = error
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();

        _clock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _clock.VerifyNoOtherCalls();

        _publicRegisterService
            .Verify(x => x.RemoveCaseFromConsultationRegisterAsync(application.PublicRegister.EsriId!.Value, application.ApplicationReference, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _publicRegisterService.VerifyNoOtherCalls();
        _getPropertyProfilesService.VerifyNoOtherCalls();
        _getConfiguredFcAreasService.VerifyNoOtherCalls();
        _woodlandOwnerService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationUpdatePublicRegisterRecordFails(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        List<Guid> assignedInternalUsers,
        FellingLicenceApplication application,
        string error)
    {
        TestUtils.SetProtectedProperty(application, nameof(FellingLicenceApplication.Id), applicationId);
        application.PublicRegister.ConsultationPublicRegisterRemovedTimestamp = null;

        var sut = CreateSut();

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignedInternalUsers));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        _getFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _withdrawFellingLicenceService
            .Setup(x => x.UpdatePublicRegisterEntityToRemovedAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        _dbContextTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once); 
        _withdrawFellingLicenceService
            .Verify(x => x.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawFailure
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        WoodlandOwnerId = application.WoodlandOwnerId,
                        Section = "Withdraw FLA",
                        Error = error
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();

        _clock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _clock.VerifyNoOtherCalls();

        _publicRegisterService
            .Verify(x => x.RemoveCaseFromConsultationRegisterAsync(application.PublicRegister.EsriId!.Value, application.ApplicationReference, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _publicRegisterService.VerifyNoOtherCalls();
        _getPropertyProfilesService.VerifyNoOtherCalls();
        _getConfiguredFcAreasService.VerifyNoOtherCalls();
        _woodlandOwnerService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotLoadApplicantToSendNotificationInSubmittableState(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        List<Guid> assignedInternalUsers,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        string adminHubFooter,
        WoodlandOwnerModel woodlandOwnerModel,
        string error)
    {
        TestUtils.SetProtectedProperty(application, nameof(FellingLicenceApplication.Id), applicationId);
        application.PublicRegister.ConsultationPublicRegisterRemovedTimestamp = null;
        application.StatusHistories =
        [
            new StatusHistory
            {
                Created = DateTime.Today,
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus
                    .ReturnedToApplicant // returned so it needs to look up property name on property profile
            },
            new StatusHistory
            {
                Created = DateTime.Today.AddSeconds(1),
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus.Withdrawn  // application entity is loaded after withdrawn status is applied in service
            }
        ];

        var sut = CreateSut();

        _retrieveExternalAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccountModel>(error));

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignedInternalUsers));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        _getFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _withdrawFellingLicenceService
            .Setup(x => x.UpdatePublicRegisterEntityToRemovedAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _getPropertyProfilesService
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _getConfiguredFcAreasService
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        _woodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwnerModel));

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveExternalAccountsService
            .Verify(x => x.RetrieveUserAccountByIdAsync(application.CreatedById, It.IsAny<CancellationToken>()),
                Times.Once);
        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        // withdraw completes and we only failed notifications, so transaction is committed not rolled back
        _dbContextTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService
            .Verify(x => x.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawNotificationSentFailed
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        RecipientId = application.CreatedById,
                        Error = error
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawComplete
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = application.WoodlandOwnerId
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();

        _clock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _clock.VerifyNoOtherCalls();

        _publicRegisterService
            .Verify(x => x.RemoveCaseFromConsultationRegisterAsync(application.PublicRegister.EsriId!.Value, application.ApplicationReference, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _publicRegisterService.VerifyNoOtherCalls();

        _getPropertyProfilesService
            .Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getPropertyProfilesService.VerifyNoOtherCalls();

        _getConfiguredFcAreasService
            .Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()),
                Times.Once);
        _getConfiguredFcAreasService.VerifyNoOtherCalls();

        _woodlandOwnerService
            .Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _woodlandOwnerService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenFailsToSendApplicantNotificationInSubmittableStateAndHasNoInternalUsers(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        string adminHubFooter,
        WoodlandOwnerModel woodlandOwnerModel,
        UserAccountModel applicantModel,
        string error)
    {
        TestUtils.SetProtectedProperty(application, nameof(FellingLicenceApplication.Id), applicationId);
        application.PublicRegister.ConsultationPublicRegisterRemovedTimestamp = null;
        application.StatusHistories =
        [
            new StatusHistory
            {
                Created = DateTime.Today,
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus
                    .ReturnedToApplicant // returned so it needs to look up property name on property profile
            },
            new StatusHistory
            {
                Created = DateTime.Today.AddSeconds(1),
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus.Withdrawn  // application entity is loaded after withdrawn status is applied in service
            }
        ];

        var sut = CreateSut();

        _retrieveExternalAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantModel));

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Guid>()));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        _getFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _withdrawFellingLicenceService
            .Setup(x => x.UpdatePublicRegisterEntityToRemovedAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _getPropertyProfilesService
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _getConfiguredFcAreasService
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        _woodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwnerModel));

        _sendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<ApplicationWithdrawnConfirmationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(),
                It.IsAny<NotificationAttachment[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(error));

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveExternalAccountsService
            .Verify(x => x.RetrieveUserAccountByIdAsync(application.CreatedById, It.IsAny<CancellationToken>()),
                Times.Once);
        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        // withdraw completes and we only failed notifications, so transaction is committed not rolled back
        _dbContextTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService
            .Verify(x => x.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawNotificationSentFailed
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        RecipientId = application.CreatedById,
                        RecipientName = applicantModel.FullName,
                        RecipientEmail = applicantModel.Email,
                        RecipientRole = AssignedUserRole.Author,
                        Error = error
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawComplete
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = application.WoodlandOwnerId
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();

        _clock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _clock.VerifyNoOtherCalls();

        _publicRegisterService
            .Verify(x => x.RemoveCaseFromConsultationRegisterAsync(application.PublicRegister.EsriId!.Value, application.ApplicationReference, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _publicRegisterService.VerifyNoOtherCalls();

        _getPropertyProfilesService
            .Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getPropertyProfilesService.VerifyNoOtherCalls();

        _getConfiguredFcAreasService
            .Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()),
                Times.Once);
        _getConfiguredFcAreasService.VerifyNoOtherCalls();

        _woodlandOwnerService
            .Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _woodlandOwnerService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();

        var externalLinkToApplication =
            $"{_externalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={applicationId}";
        _sendNotifications
            .Verify(x => x.SendNotificationAsync(It.Is<ApplicationWithdrawnConfirmationDataModel>(m =>
                m.ApplicationReference == application.ApplicationReference
                && m.PropertyName == propertyProfile.Name
                && m.Name == applicantModel.FullName
                && m.ViewApplicationURL == externalLinkToApplication
                && m.AdminHubFooter == adminHubFooter
                && m.ApplicationId == applicationId
                && string.Join(", ", m.ReasonForWithdrawal) == withdrawalReason.GetDisplayName()),
                NotificationType.ApplicationWithdrawnConfirmation,
                It.Is<NotificationRecipient>(r => r.Name == applicantModel.FullName && r.Address == applicantModel.Email),
                It.Is<NotificationRecipient[]>(cc => cc.Single().Name == woodlandOwnerModel.ContactName && cc.Single().Address == woodlandOwnerModel.ContactEmail),
                null,
                null,
                It.IsAny<CancellationToken>()), Times.Once);
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSuccessfullySendsApplicantNotificationInSubmittableStateAndHasNoInternalUsers(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        string adminHubFooter,
        WoodlandOwnerModel woodlandOwnerModel,
        UserAccountModel applicantModel,
        string error)
    {
        TestUtils.SetProtectedProperty(application, nameof(FellingLicenceApplication.Id), applicationId);
        application.PublicRegister.ConsultationPublicRegisterRemovedTimestamp = null;
        application.StatusHistories =
        [
            new StatusHistory
            {
                Created = DateTime.Today,
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus
                    .ReturnedToApplicant // returned so it needs to look up property name on property profile
            },
            new StatusHistory
            {
                Created = DateTime.Today.AddSeconds(1),
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus.Withdrawn  // application entity is loaded after withdrawn status is applied in service
            }
        ];

        var sut = CreateSut();

        _retrieveExternalAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantModel));

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Guid>()));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        _getFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _withdrawFellingLicenceService
            .Setup(x => x.UpdatePublicRegisterEntityToRemovedAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _getPropertyProfilesService
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _getConfiguredFcAreasService
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        _woodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwnerModel));

        _sendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<ApplicationWithdrawnConfirmationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(),
                It.IsAny<NotificationAttachment[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveExternalAccountsService
            .Verify(x => x.RetrieveUserAccountByIdAsync(application.CreatedById, It.IsAny<CancellationToken>()),
                Times.Once);
        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        // withdraw completes and we only failed notifications, so transaction is committed not rolled back
        _dbContextTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService
            .Verify(x => x.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawNotificationSent
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        RecipientId = application.CreatedById,
                        RecipientName = applicantModel.FullName,
                        RecipientEmail = applicantModel.Email,
                        RecipientRole = AssignedUserRole.Author
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawComplete
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = application.WoodlandOwnerId
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();

        _clock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _clock.VerifyNoOtherCalls();

        _publicRegisterService
            .Verify(x => x.RemoveCaseFromConsultationRegisterAsync(application.PublicRegister.EsriId!.Value, application.ApplicationReference, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _publicRegisterService.VerifyNoOtherCalls();

        _getPropertyProfilesService
            .Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getPropertyProfilesService.VerifyNoOtherCalls();

        _getConfiguredFcAreasService
            .Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()),
                Times.Once);
        _getConfiguredFcAreasService.VerifyNoOtherCalls();

        _woodlandOwnerService
            .Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _woodlandOwnerService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();

        var externalLinkToApplication =
            $"{_externalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={applicationId}";
        _sendNotifications
            .Verify(x => x.SendNotificationAsync(It.Is<ApplicationWithdrawnConfirmationDataModel>(m =>
                m.ApplicationReference == application.ApplicationReference
                && m.PropertyName == propertyProfile.Name
                && m.Name == applicantModel.FullName
                && m.ViewApplicationURL == externalLinkToApplication
                && m.AdminHubFooter == adminHubFooter
                && m.ApplicationId == applicationId
                && string.Join(", ", m.ReasonForWithdrawal) == withdrawalReason.GetDisplayName()),
                NotificationType.ApplicationWithdrawnConfirmation,
                It.Is<NotificationRecipient>(r => r.Name == applicantModel.FullName && r.Address == applicantModel.Email),
                It.Is<NotificationRecipient[]>(cc => cc.Single().Name == woodlandOwnerModel.ContactName && cc.Single().Address == woodlandOwnerModel.ContactEmail),
                null,
                null,
                It.IsAny<CancellationToken>()), Times.Once);
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSuccessfullySendsApplicantNotificationInInternalStateAndHasNoInternalUsers(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        FellingLicenceApplication application,
        string adminHubFooter,
        WoodlandOwnerModel woodlandOwnerModel,
        UserAccountModel applicantModel,
        string error)
    {
        TestUtils.SetProtectedProperty(application, nameof(FellingLicenceApplication.Id), applicationId);
        application.PublicRegister.ConsultationPublicRegisterRemovedTimestamp = null;
        application.StatusHistories =
        [
            new StatusHistory
            {
                Created = DateTime.Today,
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus
                    .WoodlandOfficerReview// internal state so it uses the property name on the snapshot
            },
            new StatusHistory
            {
                Created = DateTime.Today.AddSeconds(1),
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus.Withdrawn  // application entity is loaded after withdrawn status is applied in service
            }
        ];

        var sut = CreateSut();

        _retrieveExternalAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantModel));

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Guid>()));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        _getFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _withdrawFellingLicenceService
            .Setup(x => x.UpdatePublicRegisterEntityToRemovedAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _getConfiguredFcAreasService
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        _woodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwnerModel));

        _sendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<ApplicationWithdrawnConfirmationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(),
                It.IsAny<NotificationAttachment[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveExternalAccountsService
            .Verify(x => x.RetrieveUserAccountByIdAsync(application.CreatedById, It.IsAny<CancellationToken>()),
                Times.Once);
        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        // withdraw completes and we only failed notifications, so transaction is committed not rolled back
        _dbContextTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService
            .Verify(x => x.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawNotificationSent
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        RecipientId = application.CreatedById,
                        RecipientName = applicantModel.FullName,
                        RecipientEmail = applicantModel.Email,
                        RecipientRole = AssignedUserRole.Author
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawComplete
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = application.WoodlandOwnerId
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();

        _clock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _clock.VerifyNoOtherCalls();

        _publicRegisterService
            .Verify(x => x.RemoveCaseFromConsultationRegisterAsync(application.PublicRegister.EsriId!.Value, application.ApplicationReference, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _publicRegisterService.VerifyNoOtherCalls();

        _getPropertyProfilesService.VerifyNoOtherCalls();

        _getConfiguredFcAreasService
            .Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()),
                Times.Once);
        _getConfiguredFcAreasService.VerifyNoOtherCalls();

        _woodlandOwnerService
            .Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _woodlandOwnerService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();

        var externalLinkToApplication =
            $"{_externalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={applicationId}";
        _sendNotifications
            .Verify(x => x.SendNotificationAsync(It.Is<ApplicationWithdrawnConfirmationDataModel>(m =>
                m.ApplicationReference == application.ApplicationReference
                && m.PropertyName == application.SubmittedFlaPropertyDetail.Name
                && m.Name == applicantModel.FullName
                && m.ViewApplicationURL == externalLinkToApplication
                && m.AdminHubFooter == adminHubFooter
                && m.ApplicationId == applicationId
                && string.Join(", ", m.ReasonForWithdrawal) == withdrawalReason.GetDisplayName()),
                NotificationType.ApplicationWithdrawnConfirmation,
                It.Is<NotificationRecipient>(r => r.Name == applicantModel.FullName && r.Address == applicantModel.Email),
                It.Is<NotificationRecipient[]>(cc => cc.Single().Name == woodlandOwnerModel.ContactName && cc.Single().Address == woodlandOwnerModel.ContactEmail),
                null,
                null,
                It.IsAny<CancellationToken>()), Times.Once);
        _sendNotifications.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task WhenFailsToRetrieveAssignedInternalUserDetailsToSendNotification(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        string adminHubFooter,
        WoodlandOwnerModel woodlandOwnerModel,
        UserAccountModel applicantModel,
        Guid assignedInternalUserId,
        string error)
    {
        TestUtils.SetProtectedProperty(application, nameof(FellingLicenceApplication.Id), applicationId);
        application.PublicRegister.ConsultationPublicRegisterRemovedTimestamp = null;
        application.StatusHistories =
        [
            new StatusHistory
            {
                Created = DateTime.Today,
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus
                    .ReturnedToApplicant // returned so it needs to look up property name on property profile
            },
            new StatusHistory
            {
                Created = DateTime.Today.AddSeconds(1),
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus.Withdrawn  // application entity is loaded after withdrawn status is applied in service
            }
        ];

        var sut = CreateSut();

        _retrieveExternalAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantModel));

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Guid> { assignedInternalUserId }));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        _getFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _withdrawFellingLicenceService
            .Setup(x => x.UpdatePublicRegisterEntityToRemovedAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _getPropertyProfilesService
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _getConfiguredFcAreasService
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        _woodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwnerModel));

        _sendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<ApplicationWithdrawnConfirmationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(),
                It.IsAny<NotificationAttachment[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        _internalUserAccountService
            .Setup(x => x.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<Forestry.Flo.Services.InternalUsers.Models.UserAccountModel>>(error));

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveExternalAccountsService
            .Verify(x => x.RetrieveUserAccountByIdAsync(application.CreatedById, It.IsAny<CancellationToken>()),
                Times.Once);
        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        // withdraw completes and we only failed notifications, so transaction is committed not rolled back
        _dbContextTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService
            .Verify(x => x.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawNotificationSent
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        RecipientId = application.CreatedById,
                        RecipientName = applicantModel.FullName,
                        RecipientEmail = applicantModel.Email,
                        RecipientRole = AssignedUserRole.Author
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawComplete
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = application.WoodlandOwnerId
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();

        _clock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _clock.VerifyNoOtherCalls();

        _publicRegisterService
            .Verify(x => x.RemoveCaseFromConsultationRegisterAsync(application.PublicRegister.EsriId!.Value, application.ApplicationReference, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _publicRegisterService.VerifyNoOtherCalls();

        _getPropertyProfilesService
            .Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getPropertyProfilesService.VerifyNoOtherCalls();

        _getConfiguredFcAreasService
            .Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()),
                Times.Once);
        _getConfiguredFcAreasService.VerifyNoOtherCalls();

        _woodlandOwnerService
            .Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _woodlandOwnerService.VerifyNoOtherCalls();

        _internalUserAccountService
            .Verify(x => x.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(l => l.Single() == assignedInternalUserId), It.IsAny<CancellationToken>()),
                Times.Once);
        _internalUserAccountService.VerifyNoOtherCalls();

        var externalLinkToApplication =
            $"{_externalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={applicationId}";
        _sendNotifications
            .Verify(x => x.SendNotificationAsync(It.Is<ApplicationWithdrawnConfirmationDataModel>(m =>
                m.ApplicationReference == application.ApplicationReference
                && m.PropertyName == propertyProfile.Name
                && m.Name == applicantModel.FullName
                && m.ViewApplicationURL == externalLinkToApplication
                && m.AdminHubFooter == adminHubFooter
                && m.ApplicationId == applicationId
                && string.Join(", ", m.ReasonForWithdrawal) == withdrawalReason.GetDisplayName()),
                NotificationType.ApplicationWithdrawnConfirmation,
                It.Is<NotificationRecipient>(r => r.Name == applicantModel.FullName && r.Address == applicantModel.Email),
                It.Is<NotificationRecipient[]>(cc => cc.Single().Name == woodlandOwnerModel.ContactName && cc.Single().Address == woodlandOwnerModel.ContactEmail),
                null,
                null,
                It.IsAny<CancellationToken>()), Times.Once);
        _sendNotifications.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSuccessfullySendsAllNotifications(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        string adminHubFooter,
        WoodlandOwnerModel woodlandOwnerModel,
        UserAccountModel applicantModel,
        Guid assignedInternalUserId,
        Forestry.Flo.Services.InternalUsers.Models.UserAccountModel assignedInternalUser)
    {
        TestUtils.SetProtectedProperty(application, nameof(FellingLicenceApplication.Id), applicationId);
        application.PublicRegister.ConsultationPublicRegisterRemovedTimestamp = null;
        application.StatusHistories =
        [
            new StatusHistory
            {
                Created = DateTime.Today,
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus
                    .ReturnedToApplicant // returned so it needs to look up property name on property profile
            },
            new StatusHistory
            {
                Created = DateTime.Today.AddSeconds(1),
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus.Withdrawn  // application entity is loaded after withdrawn status is applied in service
            }
        ];

        var sut = CreateSut();

        _retrieveExternalAccountsService
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicantModel));

        _withdrawFellingLicenceService
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<List<WithdrawalReason>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Guid> { assignedInternalUserId }));

        _fellingLicenceApplicationExternalRepository
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dbContextTransaction.Object);

        _getFellingLicenceApplicationServiceForExternalUsers
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _publicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _withdrawFellingLicenceService
            .Setup(x => x.UpdatePublicRegisterEntityToRemovedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _getPropertyProfilesService
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _getConfiguredFcAreasService
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        _woodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwnerModel));

        _sendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<ApplicationWithdrawnConfirmationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]>(),
                It.IsAny<NotificationAttachment[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        _internalUserAccountService
            .Setup(x => x.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Flo.Services.InternalUsers.Models.UserAccountModel> { assignedInternalUser }));

        var result = await sut.WithdrawApplicationAsync(applicationId, withdrawalReason, linkToApplication, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveExternalAccountsService
            .Verify(x => x.RetrieveUserAccountByIdAsync(application.CreatedById, It.IsAny<CancellationToken>()),
                Times.Once);
        _retrieveExternalAccountsService.VerifyNoOtherCalls();

        _getFellingLicenceApplicationServiceForExternalUsers
            .Verify(x => x.GetApplicationByIdAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getFellingLicenceApplicationServiceForExternalUsers.VerifyNoOtherCalls();

        _fellingLicenceApplicationExternalRepository
            .Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        // withdraw completes and we only failed notifications, so transaction is committed not rolled back
        _dbContextTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once());
        _dbContextTransaction.Verify(x => x.DisposeAsync(), Times.Once);
        _dbContextTransaction.VerifyNoOtherCalls();

        _withdrawFellingLicenceService
            .Verify(x => x.WithdrawApplicationAsync(applicationId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.Is<List<WithdrawalReason>>(r => r.Single() == withdrawalReason), null, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService
            .Verify(x => x.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _withdrawFellingLicenceService.VerifyNoOtherCalls();

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawNotificationSent
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                    JsonSerializer.Serialize(new
                    {
                        RecipientId = application.CreatedById,
                        RecipientName = applicantModel.FullName,
                        RecipientEmail = applicantModel.Email,
                        RecipientRole = AssignedUserRole.Author
                    }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.FellingLicenceApplicationWithdrawComplete
                && x.ActorType == ActorType.ExternalApplicant
                && x.UserId == Guid.Empty
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == applicationId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = application.WoodlandOwnerId
                }, _options)),
            CancellationToken.None), Times.Once);
        _auditService.VerifyNoOtherCalls();

        _clock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _clock.VerifyNoOtherCalls();

        _publicRegisterService
            .Verify(x => x.RemoveCaseFromConsultationRegisterAsync(application.PublicRegister.EsriId!.Value, application.ApplicationReference, _now, It.IsAny<CancellationToken>()),
                Times.Once);
        _publicRegisterService.VerifyNoOtherCalls();

        _getPropertyProfilesService
            .Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _getPropertyProfilesService.VerifyNoOtherCalls();

        _getConfiguredFcAreasService
            .Verify(x => x.TryGetAdminHubAddress(application.AdministrativeRegion, It.IsAny<CancellationToken>()),
                Times.Once);
        _getConfiguredFcAreasService.VerifyNoOtherCalls();

        _woodlandOwnerService
            .Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()),
                Times.Once);
        _woodlandOwnerService.VerifyNoOtherCalls();

        _internalUserAccountService
            .Verify(x => x.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(l => l.Single() == assignedInternalUserId), It.IsAny<CancellationToken>()),
                Times.Once);
        _internalUserAccountService.VerifyNoOtherCalls();

        var externalLinkToApplication =
            $"{_externalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={applicationId}";
        _sendNotifications
            .Verify(x => x.SendNotificationAsync(It.Is<ApplicationWithdrawnConfirmationDataModel>(m =>
                m.ApplicationReference == application.ApplicationReference
                && m.PropertyName == propertyProfile.Name
                && m.Name == applicantModel.FullName
                && m.ViewApplicationURL == externalLinkToApplication
                && m.AdminHubFooter == adminHubFooter
                && m.ApplicationId == applicationId
                && string.Join(", ", m.ReasonForWithdrawal) == withdrawalReason.GetDisplayName()),
                NotificationType.ApplicationWithdrawnConfirmation,
                It.Is<NotificationRecipient>(r => r.Name == applicantModel.FullName && r.Address == applicantModel.Email),
                It.Is<NotificationRecipient[]>(cc => cc.Single().Name == woodlandOwnerModel.ContactName && cc.Single().Address == woodlandOwnerModel.ContactEmail),
                null,
                null,
                It.IsAny<CancellationToken>()), Times.Once);

        _sendNotifications
            .Verify(x => x.SendNotificationAsync(It.Is<ApplicationWithdrawnConfirmationDataModel>(m =>
                    m.ApplicationReference == application.ApplicationReference
                    && m.PropertyName == propertyProfile.Name
                    && m.Name == assignedInternalUser.FullName
                    && m.ViewApplicationURL == linkToApplication
                    && m.AdminHubFooter == adminHubFooter
                    && m.ApplicationId == applicationId
                    && string.Join(", ", m.ReasonForWithdrawal) == withdrawalReason.GetDisplayName()),
                NotificationType.ApplicationWithdrawn,
                It.Is<NotificationRecipient>(r => r.Name == assignedInternalUser.FullName && r.Address == assignedInternalUser.Email),
                null,
                null,
                null,
                It.IsAny<CancellationToken>()), Times.Once);

        _sendNotifications.VerifyNoOtherCalls();
    }

    private WithdrawApplicationInternalUseCase CreateSut()
    {
        _getFellingLicenceApplicationServiceForExternalUsers.Reset();
        _fellingLicenceApplicationExternalRepository.Reset();
        _withdrawFellingLicenceService.Reset();
        _auditService.Reset();
        _clock.Reset();
        _publicRegisterService.Reset();
        _getPropertyProfilesService.Reset();
        _getConfiguredFcAreasService.Reset();
        _woodlandOwnerService.Reset();
        _retrieveExternalAccountsService.Reset();
        _internalUserAccountService.Reset();
        _sendNotifications.Reset();
        _externalUserSiteOptions = new ExternalApplicantSiteOptions()
        {
            BaseUrl = "https://localhost"
        };

        _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(_now));

        _dbContextTransaction.Reset();

        return new WithdrawApplicationInternalUseCase(
            _getFellingLicenceApplicationServiceForExternalUsers.Object,
            _fellingLicenceApplicationExternalRepository.Object,
            _withdrawFellingLicenceService.Object,
            _auditService.Object,
            _clock.Object,
            _publicRegisterService.Object,
            _getPropertyProfilesService.Object,
            _getConfiguredFcAreasService.Object,
            _woodlandOwnerService.Object,
            _retrieveExternalAccountsService.Object,
            _internalUserAccountService.Object,
            _sendNotifications.Object,
            new OptionsWrapper<ExternalApplicantSiteOptions>(_externalUserSiteOptions),
            _requestContext,
            new NullLogger<WithdrawApplicationUseCaseBase>());
    }
}
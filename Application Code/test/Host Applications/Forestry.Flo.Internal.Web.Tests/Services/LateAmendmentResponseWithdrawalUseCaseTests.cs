using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common; // RequestContext, UserDbErrorReason
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services; // IWithdrawFellingLicenceService
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System.Text.Json;
using Forestry.Flo.HostApplicationsCommon.Infrastructure;
using ApplicantsUserAccountModel = Forestry.Flo.Services.Applicants.Models.UserAccountModel;
using InternalUserAccountModel = Forestry.Flo.Services.InternalUsers.Models.UserAccountModel;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class LateAmendmentResponseWithdrawalUseCaseTests
{
    private readonly Mock<ILateAmendmentResponseWithdrawalService> _lateService = new();
    private readonly Mock<IRetrieveUserAccountsService> _externalAccounts = new();
    private readonly Mock<ISendNotifications> _notifications = new();
    private readonly Mock<IGetConfiguredFcAreas> _configuredAreas = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IAuditService<LateAmendmentResponseWithdrawalUseCase>> _audit = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _flaRepo = new();
    private readonly Mock<IUserAccountService> _internalUserAccounts = new();
    private readonly Mock<IOptions<ExternalApplicantSiteOptions>> _extSiteOptions = new();
    private readonly Mock<IWithdrawApplicationInternalUseCase> _withdrawApplicationInternalUseCase = new();
    private readonly RequestContext _requestContext = new(Guid.NewGuid().ToString(), new RequestUserModel(UserFactory.CreateUnauthenticatedUser()));
    private readonly InternalUserSiteOptions _internalUserSiteOptions = new() { BaseUrl = "https://internal/" };

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private LateAmendmentResponseWithdrawalUseCase CreateSut()
    {
        _extSiteOptions.Setup(x => x.Value).Returns(new ExternalApplicantSiteOptions { BaseUrl = "https://external/" });
        _clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));
        _withdrawApplicationInternalUseCase.Reset();
        _lateService.Reset();
        _audit.Reset();

        return new LateAmendmentResponseWithdrawalUseCase(
            _lateService.Object,
            _externalAccounts.Object,
            _notifications.Object,
            _configuredAreas.Object,
            _clock.Object,
            _requestContext,
            _audit.Object,
            _extSiteOptions.Object,
            new OptionsWrapper<InternalUserSiteOptions>(_internalUserSiteOptions),
            new NullLogger<LateAmendmentResponseWithdrawalUseCase>(),
            _flaRepo.Object,
            _internalUserAccounts.Object,
            _withdrawApplicationInternalUseCase.Object);
    }

    [Theory, AutoMoqData]
    public async Task SendLateAmendmentResponseRemindersAsync_SendsAndCountsSuccessful(
        LateAmendmentResponseWithdrawalModel model,
        ApplicantsUserAccountModel applicant,
        InternalUserAccountModel internalUser)
    {
        var sut = CreateSut();
        
        // arrange single item list
        model.CreatedById = applicant.UserAccountId;
        model.WoodlandOfficerReviewLastUpdatedById = internalUser.UserAccountId;
        _lateService.Setup(s => s.GetLateAmendmentResponseForReminderApplicationsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<LateAmendmentResponseWithdrawalModel>>(new List<LateAmendmentResponseWithdrawalModel>{ model }));

        _externalAccounts.Setup(s => s.RetrieveUserAccountByIdAsync(model.CreatedById, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicant));

        _internalUserAccounts.Setup(s => s.GetUserAccountAsync(model.WoodlandOfficerReviewLastUpdatedById, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.None); // we only need name optional, leaving empty

        _notifications.Setup(n => n.SendNotificationAsync(
                It.IsAny<AmendmentsSentToApplicantDataModel>(),
                NotificationType.ReminderForApplicantToRespondToAmendments,
                It.IsAny<NotificationRecipient>(),
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        _lateService.Setup(s => s.UpdateReminderNotificationTimeStampAsync(model.ApplicationId, model.AmendmentReviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var transactionMock = new Mock<IDbContextTransaction>();
        _flaRepo.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transactionMock.Object);
        transactionMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);



        // act
        var count = await sut.SendLateAmendmentResponseRemindersAsync(CancellationToken.None);

        // assert
        Assert.Equal(1, count);
        _notifications.Verify(n => n.SendNotificationAsync(
            It.IsAny<AmendmentsSentToApplicantDataModel>(),
            NotificationType.ReminderForApplicantToRespondToAmendments,
            It.IsAny<NotificationRecipient>(),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        _lateService.Verify(s => s.UpdateReminderNotificationTimeStampAsync(model.ApplicationId, model.AmendmentReviewId, It.IsAny<CancellationToken>()), Times.Once);
        transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task SendLateAmendmentResponseRemindersAsync_NotificationFailure_DoesNotUpdateTimestamp(
        LateAmendmentResponseWithdrawalModel model,
        ApplicantsUserAccountModel applicant)
    {
        var sut = CreateSut();

        model.CreatedById = applicant.UserAccountId;
        _lateService.Setup(s => s.GetLateAmendmentResponseForReminderApplicationsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<LateAmendmentResponseWithdrawalModel>>(new List<LateAmendmentResponseWithdrawalModel>{ model }));

        _externalAccounts.Setup(s => s.RetrieveUserAccountByIdAsync(model.CreatedById, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicant));

        _notifications.Setup(n => n.SendNotificationAsync(
                It.IsAny<AmendmentsSentToApplicantDataModel>(),
                NotificationType.ReminderForApplicantToRespondToAmendments,
                It.IsAny<NotificationRecipient>(),
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>("fail"));

        var transactionMock = new Mock<IDbContextTransaction>();
        _flaRepo.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transactionMock.Object);
        transactionMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var count = await sut.SendLateAmendmentResponseRemindersAsync(CancellationToken.None);

        Assert.Equal(0, count);
        _lateService.Verify(s => s.UpdateReminderNotificationTimeStampAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // New tests for withdrawal
    [Fact]
    public async Task WithdrawLateAmendmentApplicationsAsync_AllSuccessful()
    {
        var sut = CreateSut();

        var apps = new List<LateAmendmentResponseWithdrawalModel>
        {
            new() { ApplicationId = Guid.NewGuid(), AmendmentReviewId = Guid.NewGuid() },
            new() { ApplicationId = Guid.NewGuid(), AmendmentReviewId = Guid.NewGuid() }
        };

        _lateService.Setup(s => s.GetLateAmendmentResponseForWithdrawalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<LateAmendmentResponseWithdrawalModel>>(apps));

        _withdrawApplicationInternalUseCase
            .Setup(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<WithdrawalReason>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var count = await sut.WithdrawLateAmendmentApplicationsAsync(CancellationToken.None);

        Assert.Equal(apps.Count, count);

        _lateService.Verify(x => x.GetLateAmendmentResponseForWithdrawalAsync(It.IsAny<CancellationToken>()), Times.Once);
        _lateService.VerifyNoOtherCalls();

        foreach (var app in apps)
        {
            var expectedLink = $"{_internalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{app.ApplicationId}";
            _withdrawApplicationInternalUseCase
                .Verify(x => x.WithdrawApplicationAsync(app.ApplicationId, WithdrawalReason.ExceededAmendmentsResponseDeadline, expectedLink, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
        _withdrawApplicationInternalUseCase.VerifyNoOtherCalls();

        _audit.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WithdrawLateAmendmentApplicationsAsync_MixOfSuccessAndFailure(
        string error)
    {
        var sut = CreateSut();

        var apps = new List<LateAmendmentResponseWithdrawalModel>
        {
            new() { ApplicationId = Guid.NewGuid(), AmendmentReviewId = Guid.NewGuid() },
            new() { ApplicationId = Guid.NewGuid(), AmendmentReviewId = Guid.NewGuid() }
        };

        _lateService.Setup(s => s.GetLateAmendmentResponseForWithdrawalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<LateAmendmentResponseWithdrawalModel>>(apps));

        _withdrawApplicationInternalUseCase
            .SetupSequence(x => x.WithdrawApplicationAsync(It.IsAny<Guid>(), It.IsAny<WithdrawalReason>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success)
            .ReturnsAsync(Result.Failure(error));

        var count = await sut.WithdrawLateAmendmentApplicationsAsync(CancellationToken.None);

        Assert.Equal(1, count);

        _lateService.Verify(x => x.GetLateAmendmentResponseForWithdrawalAsync(It.IsAny<CancellationToken>()), Times.Once);
        _lateService.VerifyNoOtherCalls();

        foreach (var app in apps)
        {
            var expectedLink = $"{_internalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{app.ApplicationId}";
            _withdrawApplicationInternalUseCase
                .Verify(x => x.WithdrawApplicationAsync(app.ApplicationId, WithdrawalReason.ExceededAmendmentsResponseDeadline, expectedLink, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
        _withdrawApplicationInternalUseCase.VerifyNoOtherCalls();

        _audit.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task WithdrawLateAmendmentApplicationsAsync_FailsToRetrieveApplications(
        string error)
    {
        var sut = CreateSut();

        _lateService.Setup(s => s.GetLateAmendmentResponseForWithdrawalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<LateAmendmentResponseWithdrawalModel>>(error));

        var count = await sut.WithdrawLateAmendmentApplicationsAsync(CancellationToken.None);

        Assert.Equal(0, count);

        _audit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.LateAmendmentResponseNotificationFailure
                && x.ActorType == _requestContext.ActorType
                && x.UserId == null
                && x.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && x.SourceEntityId == null
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    ApplicationId = (Guid?)null,
                    ResponseDeadline = (DateTime?)null
                }, _options)),
            CancellationToken.None), Times.Once);
        _audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task WithdrawLateAmendmentApplicationsAsync_NoApplicationsFound()
    {
        var sut = CreateSut();

        _lateService.Setup(s => s.GetLateAmendmentResponseForWithdrawalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<LateAmendmentResponseWithdrawalModel>>(new List<LateAmendmentResponseWithdrawalModel>()));

        var count = await sut.WithdrawLateAmendmentApplicationsAsync(CancellationToken.None);

        Assert.Equal(0, count);

        _audit.VerifyNoOtherCalls();
    }
}

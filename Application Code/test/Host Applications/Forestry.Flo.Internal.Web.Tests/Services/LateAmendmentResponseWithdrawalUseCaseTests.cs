using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common; // RequestContext, UserDbErrorReason
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models; // UserAccessModel
using Forestry.Flo.Services.Common.User;
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

    private LateAmendmentResponseWithdrawalUseCase CreateSut()
    {
        _extSiteOptions.Setup(x => x.Value).Returns(new ExternalApplicantSiteOptions { BaseUrl = "https://external/" });
        _clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));
        return new LateAmendmentResponseWithdrawalUseCase(
            _lateService.Object,
            _externalAccounts.Object,
            _notifications.Object,
            _configuredAreas.Object,
            _clock.Object,
            new RequestContext(Guid.NewGuid().ToString(), new RequestUserModel(UserFactory.CreateUnauthenticatedUser())),
            _audit.Object,
            _extSiteOptions.Object,
            new NullLogger<LateAmendmentResponseWithdrawalUseCase>(),
            _flaRepo.Object,
            _internalUserAccounts.Object);
    }

    [Theory, AutoMoqData]
    public async Task SendLateAmendmentResponseRemindersAsync_SendsAndCountsSuccessful(
        LateAmendmentResponseWithdrawalModel model,
        ApplicantsUserAccountModel applicant,
        InternalUserAccountModel internalUser)
    {
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
            .ReturnsAsync(Result.Success());

        _lateService.Setup(s => s.UpdateReminderNotificationTimeStampAsync(model.ApplicationId, model.AmendmentReviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var transactionMock = new Mock<IDbContextTransaction>();
        _flaRepo.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transactionMock.Object);
        transactionMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();

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
            .ReturnsAsync(Result.Failure("fail"));

        var transactionMock = new Mock<IDbContextTransaction>();
        _flaRepo.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transactionMock.Object);
        transactionMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var count = await sut.SendLateAmendmentResponseRemindersAsync(CancellationToken.None);

        Assert.Equal(0, count);
        _lateService.Verify(s => s.UpdateReminderNotificationTimeStampAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task NotifyApplicantAsync_ReturnsFailure_WhenApplicantLookupFails(
        LateAmendmentResponseWithdrawalModel model)
    {
        _externalAccounts.Setup(s => s.RetrieveUserAccountByIdAsync(model.CreatedById, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ApplicantsUserAccountModel>("not found"));

        var sut = CreateSut();
        var result = await sut.NotifyApplicantAsync(model, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task NotifyApplicantAsync_SendsNotification_Success(
        LateAmendmentResponseWithdrawalModel model,
        ApplicantsUserAccountModel applicant)
    {
        model.CreatedById = applicant.UserAccountId;
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
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();
        var result = await sut.NotifyApplicantAsync(model, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _notifications.Verify(n => n.SendNotificationAsync(
            It.IsAny<AmendmentsSentToApplicantDataModel>(),
            NotificationType.ReminderForApplicantToRespondToAmendments,
            It.IsAny<NotificationRecipient>(),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // New tests for withdrawal
    [Fact]
    public async Task WithdrawLateAmendmentApplicationsAsync_AllSuccessful()
    {
        var apps = new List<LateAmendmentResponseWithdrawalModel>
        {
            new() { ApplicationId = Guid.NewGuid(), AmendmentReviewId = Guid.NewGuid() },
            new() { ApplicationId = Guid.NewGuid(), AmendmentReviewId = Guid.NewGuid() }
        };

        _lateService.Setup(s => s.GetLateAmendmentResponseForWithdrawalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<LateAmendmentResponseWithdrawalModel>>(apps));

        var withdrawService = new Mock<IWithdrawFellingLicenceService>();
        withdrawService.Setup(w => w.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Guid>>(new List<Guid>()));

        _flaRepo.Setup(r => r.SetAmendmentReviewCompletedAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var transactionMock = new Mock<IDbContextTransaction>();
        _flaRepo.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transactionMock.Object);
        transactionMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var count = await sut.WithdrawLateAmendmentApplicationsAsync(withdrawService.Object, CancellationToken.None);

        Assert.Equal(apps.Count, count);
        withdrawService.Verify(w => w.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Exactly(apps.Count));
        _flaRepo.Verify(r => r.SetAmendmentReviewCompletedAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()), Times.Exactly(apps.Count));
        transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Exactly(apps.Count));
    }

    [Fact]
    public async Task WithdrawLateAmendmentApplicationsAsync_WithdrawFailure_RollsBack()
    {
        var app = new LateAmendmentResponseWithdrawalModel { ApplicationId = Guid.NewGuid(), AmendmentReviewId = Guid.NewGuid() };
        _lateService.Setup(s => s.GetLateAmendmentResponseForWithdrawalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<LateAmendmentResponseWithdrawalModel>>(new List<LateAmendmentResponseWithdrawalModel>{ app }));

        var withdrawService = new Mock<IWithdrawFellingLicenceService>();
        withdrawService.Setup(w => w.WithdrawApplication(app.ApplicationId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<Guid>>("withdraw error"));

        var transactionMock = new Mock<IDbContextTransaction>();
        _flaRepo.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transactionMock.Object);
        transactionMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var count = await sut.WithdrawLateAmendmentApplicationsAsync(withdrawService.Object, CancellationToken.None);

        Assert.Equal(0, count);
        _flaRepo.Verify(r => r.SetAmendmentReviewCompletedAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()), Times.Never);
        transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WithdrawLateAmendmentApplicationsAsync_ReviewCompleteFailure_RollsBack()
    {
        var app = new LateAmendmentResponseWithdrawalModel { ApplicationId = Guid.NewGuid(), AmendmentReviewId = Guid.NewGuid() };
        _lateService.Setup(s => s.GetLateAmendmentResponseForWithdrawalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<LateAmendmentResponseWithdrawalModel>>(new List<LateAmendmentResponseWithdrawalModel>{ app }));

        var withdrawService = new Mock<IWithdrawFellingLicenceService>();
        withdrawService.Setup(w => w.WithdrawApplication(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Guid>>(new List<Guid>()));

        _flaRepo.Setup(r => r.SetAmendmentReviewCompletedAsync(app.AmendmentReviewId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var transactionMock = new Mock<IDbContextTransaction>();
        _flaRepo.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transactionMock.Object);
        transactionMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var count = await sut.WithdrawLateAmendmentApplicationsAsync(withdrawService.Object, CancellationToken.None);

        Assert.Equal(0, count);
        transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

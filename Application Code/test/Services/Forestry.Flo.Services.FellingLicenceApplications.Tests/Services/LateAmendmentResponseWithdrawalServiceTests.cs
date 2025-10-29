using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Moq;
using NodaTime;
using Xunit;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class LateAmendmentResponseWithdrawalServiceTests
    {
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _repo = new();
        private readonly Mock<IClock> _clock = new();
        private readonly Mock<ILogger<LateAmendmentResponseWithdrawalService>> _logger = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private DateTime _now;

        private LateAmendmentResponseWithdrawalService CreateSut(DateTime? nowOverride = null)
        {
            _now = nowOverride ?? new DateTime(2025, 09, 30, 10, 00, 00, DateTimeKind.Utc);
            _clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(_now));
            _repo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
            return new LateAmendmentResponseWithdrawalService(_repo.Object, _clock.Object, _logger.Object);
        }

        private static FellingLicenceApplication CreateApplication(Guid? id = null, WoodlandOfficerReview? wo = null)
        {
            return new FellingLicenceApplication
            {
                Id = id ?? Guid.NewGuid(),
                ApplicationReference = "REF-123",
                CreatedTimestamp = DateTime.UtcNow.AddDays(-30),
                CreatedById = Guid.NewGuid(),
                WoodlandOwnerId = Guid.NewGuid(),
                WoodlandOfficerReview = wo
            };
        }

        private static WoodlandOfficerReview CreateWoReview(params FellingAndRestockingAmendmentReview[] reviews)
        {
            return new WoodlandOfficerReview
            {
                Id = Guid.NewGuid(),
                LastUpdatedById = Guid.NewGuid(),
                LastUpdatedDate = DateTime.UtcNow.AddDays(-1),
                FellingAndRestockingAmendmentReviews = reviews.ToList()
            };
        }

        private static FellingAndRestockingAmendmentReview Review(DateTime sent, DateTime deadline, bool? completed = null, DateTime? reminderTs = null)
        {
            return new FellingAndRestockingAmendmentReview
            {
                WoodlandOfficerReviewId = Guid.NewGuid(),
                AmendmentsSentDate = sent,
                ResponseDeadline = deadline,
                AmendmentReviewCompleted = completed,
                ReminderNotificationTimeStamp = reminderTs
            };
        }

        // --- GetLateAmendmentResponseForReminderApplicationsAsync tests ---

        [Fact]
        public async Task GetLateAmendmentResponseForReminderApplicationsAsync_ReturnsExpectedCandidate()
        {
            var sut = CreateSut();

            var reminderPeriod = TimeSpan.FromDays(7);

            // Review A: outside window
            var reviewA = Review(_now.AddDays(-15), _now.AddDays(10));
            // Review B: inside window (deadline within 7 days)
            var reviewB = Review(_now.AddDays(-10), _now.AddDays(6));
            // Review C: newest but already reminded -> excluded
            var reviewC = Review(_now.AddDays(-5), _now.AddDays(5), reminderTs: _now.AddDays(-1));

            var wo = CreateWoReview(reviewA, reviewB, reviewC);
            var app = CreateApplication(wo: wo);

            _repo.Setup(r => r.GetApplicationsForLateAmendmentNotificationAsync(_now, reminderPeriod, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FellingLicenceApplication> { app });

            var result = await sut.GetLateAmendmentResponseForReminderApplicationsAsync(reminderPeriod, CancellationToken.None);

            Assert.True(result.IsSuccess);
            var models = result.Value;
            Assert.Single(models);
            Assert.Equal(reviewB.Id, models[0].AmendmentReviewId);
            Assert.Equal(app.Id, models[0].ApplicationId);
        }

        [Fact]
        public async Task GetLateAmendmentResponseForReminderApplicationsAsync_SkipsApplicationWithoutWoReview()
        {
            var sut = CreateSut();
            var reminderPeriod = TimeSpan.FromDays(7);

            var app = CreateApplication(wo: null);

            _repo.Setup(r => r.GetApplicationsForLateAmendmentNotificationAsync(_now, reminderPeriod, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FellingLicenceApplication> { app });

            var result = await sut.GetLateAmendmentResponseForReminderApplicationsAsync(reminderPeriod, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetLateAmendmentResponseForReminderApplicationsAsync_NoMatchingReviews_ReturnsEmpty()
        {
            var sut = CreateSut();
            var reminderPeriod = TimeSpan.FromDays(7);

            // Only a completed review
            var completedReview = Review(_now.AddDays(-12), _now.AddDays(3), completed: true);
            var wo = CreateWoReview(completedReview);
            var app = CreateApplication(wo: wo);

            _repo.Setup(r => r.GetApplicationsForLateAmendmentNotificationAsync(_now, reminderPeriod, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FellingLicenceApplication> { app });

            var result = await sut.GetLateAmendmentResponseForReminderApplicationsAsync(reminderPeriod, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetLateAmendmentResponseForReminderApplicationsAsync_PicksLatestBySentDate()
        {
            var sut = CreateSut();
            var reminderPeriod = TimeSpan.FromDays(7);

            var olderQualifying = Review(_now.AddDays(-20), _now.AddDays(6));
            var newerQualifying = Review(_now.AddDays(-5), _now.AddDays(6)); // later AmendmentsSentDate -> should be picked
            var wo = CreateWoReview(olderQualifying, newerQualifying);
            var app = CreateApplication(wo: wo);

            _repo.Setup(r => r.GetApplicationsForLateAmendmentNotificationAsync(_now, reminderPeriod, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FellingLicenceApplication> { app });

            var result = await sut.GetLateAmendmentResponseForReminderApplicationsAsync(reminderPeriod, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal(newerQualifying.Id, result.Value[0].AmendmentReviewId);
        }

        // --- GetLateAmendmentResponseForWithdrawalAsync tests ---

        [Fact]
        public async Task GetLateAmendmentResponseForWithdrawalAsync_ReturnsExpectedCandidate()
        {
            var sut = CreateSut();

            var passedEarlier = Review(_now.AddDays(-15), _now.AddDays(-2)); // older
            var passedLater = Review(_now.AddDays(-5), _now.AddDays(-1));     // later AmendmentsSentDate -> should be chosen
            var notYet = Review(_now.AddDays(-6), _now.AddDays(1));          // future deadline

            var wo = CreateWoReview(passedEarlier, passedLater, notYet);
            var app = CreateApplication(wo: wo);

            _repo.Setup(r => r.GetApplicationsForLateAmendmentWithdrawalAsync(_now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FellingLicenceApplication> { app });

            var result = await sut.GetLateAmendmentResponseForWithdrawalAsync(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal(passedLater.Id, result.Value[0].AmendmentReviewId);
        }

        [Fact]
        public async Task GetLateAmendmentResponseForWithdrawalAsync_NoPassedDeadlines_ReturnsEmpty()
        {
            var sut = CreateSut();
            var future = Review(_now.AddDays(-3), _now.AddDays(2));
            var wo = CreateWoReview(future);
            var app = CreateApplication(wo: wo);

            _repo.Setup(r => r.GetApplicationsForLateAmendmentWithdrawalAsync(_now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FellingLicenceApplication> { app });

            var result = await sut.GetLateAmendmentResponseForWithdrawalAsync(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetLateAmendmentResponseForWithdrawalAsync_SkipsCompletedReview()
        {
            var sut = CreateSut();
            var completedPassed = Review(_now.AddDays(-10), _now.AddDays(-1), completed: true);
            var wo = CreateWoReview(completedPassed);
            var app = CreateApplication(wo: wo);

            _repo.Setup(r => r.GetApplicationsForLateAmendmentWithdrawalAsync(_now, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FellingLicenceApplication> { app });

            var result = await sut.GetLateAmendmentResponseForWithdrawalAsync(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        // --- UpdateReminderNotificationTimeStampAsync tests ---

        [Fact]
        public async Task UpdateReminderNotificationTimeStampAsync_ApplicationNotFound_Fails()
        {
            var sut = CreateSut();
            _repo.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

            var result = await sut.UpdateReminderNotificationTimeStampAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

            Assert.True(result.IsFailure);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReminderNotificationTimeStampAsync_ReviewNotFound_Fails()
        {
            var sut = CreateSut();
            var app = CreateApplication(wo: CreateWoReview()); // no reviews
            _repo.Setup(r => r.GetAsync(app.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.From(app));

            var result = await sut.UpdateReminderNotificationTimeStampAsync(app.Id, Guid.NewGuid(), CancellationToken.None);

            Assert.True(result.IsFailure);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReminderNotificationTimeStampAsync_AlreadySet_NoSaveAndSuccess()
        {
            var sut = CreateSut();
            var review = Review(_now.AddDays(-8), _now.AddDays(2));
            review.ReminderNotificationTimeStamp = _now.AddDays(-1);
            var wo = CreateWoReview(review);
            var app = CreateApplication(wo: wo);

            _repo.Setup(r => r.GetAsync(app.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.From(app));

            var result = await sut.UpdateReminderNotificationTimeStampAsync(app.Id, review.Id, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReminderNotificationTimeStampAsync_SaveSuccess_SetsTimestamp()
        {
            var sut = CreateSut();
            var review = Review(_now.AddDays(-9), _now.AddDays(3));
            var wo = CreateWoReview(review);
            var app = CreateApplication(wo: wo);

            _repo.Setup(r => r.GetAsync(app.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.From(app));

            _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var result = await sut.UpdateReminderNotificationTimeStampAsync(app.Id, review.Id, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(review.ReminderNotificationTimeStamp);
            Assert.Equal(_now, review.ReminderNotificationTimeStamp!.Value);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateReminderNotificationTimeStampAsync_SaveFailure_ReturnsFailure()
        {
            var sut = CreateSut();
            var review = Review(_now.AddDays(-9), _now.AddDays(3));
            var wo = CreateWoReview(review);
            var app = CreateApplication(wo: wo);

            _repo.Setup(r => r.GetAsync(app.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.From(app));

            _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

            var result = await sut.UpdateReminderNotificationTimeStampAsync(app.Id, review.Id, CancellationToken.None);

            Assert.True(result.IsFailure);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
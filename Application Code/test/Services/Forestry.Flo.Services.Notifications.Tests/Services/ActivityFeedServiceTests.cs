using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.Notifications.Tests.Services
{
    public class ActivityFeedServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWOrkMock;
        private readonly Mock<IRetrieveNotificationHistory> _notificationService;

        public ActivityFeedServiceTests()
        {
            _unitOfWOrkMock = new Mock<IUnitOfWork>();
            _notificationService = new Mock<IRetrieveNotificationHistory>();
        }

        private ActivityFeedService CreateSut()
        {
            _unitOfWOrkMock.Reset();

            return new ActivityFeedService(
                new NullLogger<ActivityFeedService>(),
                _notificationService.Object);
        }


        [Theory, AutoMoqData]

        public async Task ShouldRetrieveAllActivityFeedItems_WhenAllDetailsPresent(
            List<NotificationHistoryModel> notificationHistories)
        {
            var _sut = CreateSut();

            var applicationId = Guid.NewGuid();
            var applicationRef = "test/reference";

            foreach (var notificationHistory in notificationHistories)
                notificationHistory.Type = NotificationType.PublicRegisterComment;

            _notificationService.Setup(r => r.RetrieveNotificationHistoryAsync(It.IsAny<string>(),
                    It.IsAny<NotificationType[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(notificationHistories);

            ActivityFeedItemType[] activityFeedItemTypes =
            {
                ActivityFeedItemType.PublicRegisterComment
            };

            var providerModel = new ActivityFeedItemProviderModel()
            {
                FellingLicenceId = applicationId,
                FellingLicenceReference = applicationRef,
                ItemTypes = activityFeedItemTypes,
            };

            var result = await _sut.RetrieveActivityFeedItemsAsync(
                providerModel,
                ActorType.InternalUser,
                CancellationToken.None);

            // verify

            _notificationService.Verify(r => r.RetrieveNotificationHistoryAsync(
                applicationRef,
                new[]
                {
                    NotificationType.PublicRegisterComment
                },
                CancellationToken.None));

            // assert

            result.IsSuccess.Should().BeTrue();
            foreach (var note in result.Value)
            {
                Assert.False(note.VisibleToApplicant);
                Assert.False(note.VisibleToConsultee);
                Assert.Equal(note.FellingLicenceApplicationId, applicationId);
            }

            Assert.Equal(notificationHistories.FindAll(x => x.Type == NotificationType.PublicRegisterComment).Count,
                result.Value.Count(x => x.ActivityFeedItemType == ActivityFeedItemType.PublicRegisterComment));
        }


        [Fact]

        public async Task ShouldReturnFailure_WhenCommentNotFound()
        {
            var _sut = CreateSut();

            var applicationId = Guid.NewGuid();
            var applicationRef = "test/reference";

            _notificationService.Setup(r => r.RetrieveNotificationHistoryAsync(It.IsAny<string>(),
                    It.IsAny<NotificationType[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<List<NotificationHistoryModel>>("Unable to retrieve public register/consultee comments for application"));

            ActivityFeedItemType[] activityFeedItemTypes =
            {
                ActivityFeedItemType.PublicRegisterComment
            };

            var providerModel = new ActivityFeedItemProviderModel()
            {
                FellingLicenceId = applicationId,
                FellingLicenceReference = applicationRef,
                ItemTypes = activityFeedItemTypes,
                VisibleToApplicant = true,
                VisibleToConsultee = true
            };

            var result = await _sut.RetrieveActivityFeedItemsAsync(
                providerModel,
                ActorType.InternalUser,
                CancellationToken.None);

            // verify

            _notificationService.Verify(r => r.RetrieveNotificationHistoryAsync(
                applicationRef,
                new[]
                {
                    NotificationType.PublicRegisterComment
                },
                CancellationToken.None));

            // assert

            result.IsFailure.Should().BeTrue();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.Common.Tests.Services
{
    public class ActivityFeedItemProviderTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWOrkMock;
        private readonly Mock<IViewCaseNotesService> _viewCaseNotes;
        private readonly IActivityFeedService[] _activityFeedServices;
        private readonly Mock<IActivityFeedService> _activityFeedService;
        private readonly Mock<IAuditService<ActivityFeedItemProvider>> _auditService;

        public ActivityFeedItemProviderTests()
        {
            _unitOfWOrkMock = new Mock<IUnitOfWork>();
            _viewCaseNotes = new Mock<IViewCaseNotesService>();
            _activityFeedService = new Mock<IActivityFeedService>();
            _activityFeedServices = new[] { _activityFeedService.Object };
            _auditService = new Mock<IAuditService<ActivityFeedItemProvider>>();
        }

        private ActivityFeedItemProvider CreateSut()
        {
            _unitOfWOrkMock.Reset();
            _viewCaseNotes.Reset();
            _activityFeedService.Reset();
            _auditService.Reset();

            return new ActivityFeedItemProvider(
                _activityFeedServices,
                new NullLogger<ActivityFeedItemProvider>(),
                _auditService.Object,
                new RequestContext("test", new RequestUserModel(new ClaimsPrincipal())));
        }


        [Theory, AutoMoqData]

        public async Task ShouldProvideAllActivityFeedItems_WhenAllServicesSuccessful(List<ActivityFeedItemModel> activityFeedItems)
        {
            var _sut = CreateSut();

            ActivityFeedItemType[] activityFeedItemTypes =
            {
                ActivityFeedItemType.CaseNote,
                ActivityFeedItemType.StatusHistoryNotification,
                ActivityFeedItemType.AssigneeHistoryNotification
            };

            _activityFeedService.Setup(r => r.SupportedItemTypes()).Returns(activityFeedItemTypes);

            _activityFeedService.Setup(r =>
                    r.RetrieveActivityFeedItemsAsync(It.IsAny<ActivityFeedItemProviderModel>(),
                        ActorType.InternalUser,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(activityFeedItems);


            var providerModel = new ActivityFeedItemProviderModel()
            {
                FellingLicenceId = Guid.NewGuid(),
                FellingLicenceReference = "App/Reference",
                ItemTypes = activityFeedItemTypes
            };

            var result = await _sut.RetrieveAllRelevantActivityFeedItemsAsync(
                providerModel,
                ActorType.InternalUser,
                CancellationToken.None);

            // verify
            int serviceCount = _activityFeedServices.Length;

            _activityFeedService.Verify(x => x.RetrieveActivityFeedItemsAsync(providerModel, ActorType.InternalUser, CancellationToken.None), Times.Exactly(serviceCount));
            _activityFeedService.Verify(x => x.SupportedItemTypes(), Times.Exactly(activityFeedItems.Count));

            // assert

            result.IsSuccess.Should().BeTrue();
        }
    }
}

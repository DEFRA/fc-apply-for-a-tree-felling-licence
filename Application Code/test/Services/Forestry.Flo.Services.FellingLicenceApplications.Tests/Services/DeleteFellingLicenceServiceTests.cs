using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Repositories;
using NodaTime;
using Xunit;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class DeleteFellingLicenceServiceTests
    {
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepository;
        private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationExternalRepository;
        private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _getFellingLicenceApplicationForExternalUsersService;

        private readonly Mock<IUserAccountRepository> _userAccountRepository;
        private readonly Mock<IAuditService<DeleteFellingLicenceService>> _auditService;
        private readonly Mock<IClock> _clock;
        private readonly Mock<IFileStorageService> _fileStorageService;

        public DeleteFellingLicenceServiceTests()
        {
            _userAccountRepository = new Mock<IUserAccountRepository>();
            _fellingLicenceApplicationInternalRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
            _fellingLicenceApplicationExternalRepository = new Mock<IFellingLicenceApplicationExternalRepository>();
            _clock = new Mock<IClock>();
            _auditService = new Mock<IAuditService<DeleteFellingLicenceService>>();
            _fileStorageService = new Mock<IFileStorageService>();
            _getFellingLicenceApplicationForExternalUsersService =
                new Mock<IGetFellingLicenceApplicationForExternalUsers>();
        }
        
        [Theory, AutoMoqData]
        public async Task ShouldReturnSuccess_WhenDeletingApplicationWithDraftStatus(
            FellingLicenceApplication fla)
        {
            var sut = CreateSut();

            // dummy data
            var applicationId = fla.Id;
            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = fla.WoodlandOwnerId,
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { fla.WoodlandOwnerId }
            };
            var lastDate = fla.StatusHistories.OrderByDescending(x => x.Created).First().Created;
            fla.StatusHistories.Add(new StatusHistory()
            {
                Created = lastDate.AddDays(10),
                FellingLicenceApplicationId = applicationId,
                Status = FellingLicenceStatus.Draft
            });

            // setup

            _fellingLicenceApplicationExternalRepository.Setup(r=>r.GetAsync(applicationId,
                It.IsAny<CancellationToken>())).ReturnsAsync(fla);

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                r.DeleteFlaAsync(fla,  It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<UserDbErrorReason>());

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                r.CheckUserCanAccessApplicationAsync(fla.Id, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));


            var result = await sut.DeleteDraftApplicationAsync(fla.Id, userAccessModel, CancellationToken.None);

            // assert
            Assert.True(result.IsSuccess);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.DeleteFlaAsync(fla, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task ShouldReturnFailure_WhenDeletingApplicationWithNonDraftStatus(
            FellingLicenceApplication fla,
            Guid woodlandOwnerId)
        {
            var sut = CreateSut();

            // dummy data
            var applicationId = fla.Id;
            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = fla.WoodlandOwnerId,
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { fla.WoodlandOwnerId }
            };
            var lastDate = fla.StatusHistories.OrderByDescending(x => x.Created).First().Created;
            fla.StatusHistories.Add(new StatusHistory()
            {
                Created = lastDate.AddDays(10),
                FellingLicenceApplicationId = applicationId,
                Status = FellingLicenceStatus.Submitted
            });

            // setup

            _fellingLicenceApplicationExternalRepository.Setup(r => r.GetAsync(applicationId,
                It.IsAny<CancellationToken>())).ReturnsAsync(fla);

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                    r.CheckUserCanAccessApplicationAsync(fla.Id, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));


            var result = await sut.DeleteDraftApplicationAsync(fla.Id, userAccessModel, CancellationToken.None);

            // assert
            Assert.True(result.IsFailure);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.DeleteFlaAsync(fla, It.IsAny<CancellationToken>()), Times.Never);
        }

        private DeleteFellingLicenceService CreateSut()
        {
            _auditService.Reset();
            _fellingLicenceApplicationInternalRepository.Reset();
            _fellingLicenceApplicationExternalRepository.Reset();
            _getFellingLicenceApplicationForExternalUsersService.Reset();

            _userAccountRepository.Reset();
            _clock.Reset();
            _fileStorageService.Reset();

            _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));

            return new DeleteFellingLicenceService(
                _auditService.Object,
                new RequestContext("test", new RequestUserModel(new ClaimsPrincipal())),
                new NullLogger<DeleteFellingLicenceService>(),
                _fellingLicenceApplicationExternalRepository.Object,
                new GetFellingLicenceApplicationForExternalUsersService(_fellingLicenceApplicationExternalRepository.Object),
                _fileStorageService.Object);
        }
    }
}

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
using System.Text.Json;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public partial class DeleteFellingLicenceServiceTests
    {
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepository = new();
        private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationExternalRepository = new();

        private readonly Mock<IUserAccountRepository> _userAccountRepository = new();
        private readonly Mock<IAuditService<DeleteFellingLicenceService>> _auditService = new();
        private readonly Mock<IClock> _clock = new();
        private readonly Mock<IFileStorageService> _fileStorageService = new();

        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private RequestContext _requestContext;

        [Theory, AutoMoqData]
        public async Task ShouldReturnFailure_WhenApplicationNotFound(
            Guid applicationId,
            UserAccessModel uam)
        {
            var sut = CreateSut();

            _fellingLicenceApplicationExternalRepository
                .Setup(r => r.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<bool>("error"));

            var result = await sut.DeleteDraftApplicationAsync(applicationId, uam, CancellationToken.None);

            Assert.True(result.IsFailure);

            _fellingLicenceApplicationExternalRepository
                .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task ShouldReturnFailure_WhenApplicationNotAccessibleToUser(
            Guid applicationId,
            UserAccessModel uam)
        {
            var sut = CreateSut();

            _fellingLicenceApplicationExternalRepository
                .Setup(r => r.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(false));

            var result = await sut.DeleteDraftApplicationAsync(applicationId, uam, CancellationToken.None);

            Assert.True(result.IsFailure);

            _fellingLicenceApplicationExternalRepository
                .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();
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
            _fellingLicenceApplicationExternalRepository
                .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.DeleteFlaAsync(fla, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();
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
            _fellingLicenceApplicationExternalRepository
                .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.DeleteFlaAsync(fla, It.IsAny<CancellationToken>()), Times.Never);
            _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task ShouldReturnSuccess_WhenDeletingApplicationFails(
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

            _fellingLicenceApplicationExternalRepository.Setup(r => r.GetAsync(applicationId,
                It.IsAny<CancellationToken>())).ReturnsAsync(fla);

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                r.DeleteFlaAsync(fla, It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                r.CheckUserCanAccessApplicationAsync(fla.Id, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));


            var result = await sut.DeleteDraftApplicationAsync(fla.Id, userAccessModel, CancellationToken.None);

            // assert
            Assert.True(result.IsFailure);

            _fellingLicenceApplicationExternalRepository
                .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.DeleteFlaAsync(fla, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();
        }

        private DeleteFellingLicenceService CreateSut()
        {
            _auditService.Reset();
            _fellingLicenceApplicationInternalRepository.Reset();
            _fellingLicenceApplicationExternalRepository.Reset();

            _userAccountRepository.Reset();
            _clock.Reset();
            _fileStorageService.Reset();

            _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));

            _requestContext = new RequestContext(
                Guid.NewGuid().ToString(),
                new RequestUserModel(
                    UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: Guid.NewGuid())));

            return new DeleteFellingLicenceService(
                _auditService.Object,
                _requestContext,
                new NullLogger<DeleteFellingLicenceService>(),
                _fellingLicenceApplicationExternalRepository.Object,
                new GetFellingLicenceApplicationForExternalUsersService(_fellingLicenceApplicationExternalRepository.Object),
                _fileStorageService.Object);
        }
    }
}

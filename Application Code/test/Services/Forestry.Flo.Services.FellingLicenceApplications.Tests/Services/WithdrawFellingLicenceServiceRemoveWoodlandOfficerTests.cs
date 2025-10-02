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
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.InternalUsers.Repositories;
using NodaTime;
using Xunit;
using Forestry.Flo.Services.Common.Models;
using AutoFixture;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class WithdrawFellingLicenceServiceRemoveWoodlandOfficerTests
    {
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepository = new();
        private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationExternalRepository = new();
        private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _getFellingLicenceApplicationForExternalUsersService = new();

        private readonly Mock<IUserAccountRepository> _userAccountRepository = new();
        private readonly Mock<IAuditService<WithdrawFellingLicenceService>> _auditService = new();
        private readonly Mock<IClock> _clock = new();
        private readonly IFixture _fixture = new Fixture().CustomiseFixtureForFellingLicenceApplications();

        [Theory, AutoMoqData]
        public async Task ShouldReturnSuccess_WhenInternalUsersAreRemoved(
            FellingLicenceApplication fla)
        {
            var sut = CreateSut();

            var internalUserIds = new List<Guid>()
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            foreach (var assignee in fla.AssigneeHistories)
            {
                assignee.Role = AssignedUserRole.AdminOfficer;
            }

            // setup

            _fellingLicenceApplicationInternalRepository.Setup(r => r.RemoveAssignedFellingLicenceApplicationStaffMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<UserDbErrorReason>());

            var result = await sut.RemoveAssignedWoodlandOfficerAsync(fla.Id, internalUserIds, CancellationToken.None);
            
            // assert
            Assert.True(result.IsSuccess);

            _fellingLicenceApplicationInternalRepository.Verify(v => v.RemoveAssignedFellingLicenceApplicationStaffMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ShouldReturnFailure_WhenInternalUserNotFound()
        {
            var sut = CreateSut();

            var flaId = Guid.NewGuid();

            var internalUserIds = new List<Guid>()
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // setup
            _fellingLicenceApplicationInternalRepository.Setup(r => r.RemoveAssignedFellingLicenceApplicationStaffMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotFound));

            var result = await sut.RemoveAssignedWoodlandOfficerAsync(flaId, internalUserIds, CancellationToken.None);

            Assert.True(result.IsFailure);

            _fellingLicenceApplicationInternalRepository.Verify(v => v.RemoveAssignedFellingLicenceApplicationStaffMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Theory, AutoMoqData]
        public async Task ShouldReturnSuccess_WhenWithdrawingApplicationWithApplicant(
            FellingLicenceApplication fla,
            Guid woodlandOwnerId,
            Guid applicantId)
        {
            var sut = CreateSut();

            // dummy data
            var applicationId = fla.Id;
            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = applicantId,
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { fla.WoodlandOwnerId }
            };

            fla.AssigneeHistories = new List<AssigneeHistory>()
            {
                new AssigneeHistory
                {
                    FellingLicenceApplicationId = applicationId,
                    AssignedUserId = Guid.NewGuid(),
                    TimestampAssigned = DateTime.Now.AddDays(-3),
                    Role = AssignedUserRole.AdminOfficer
                },
                new AssigneeHistory
                {
                FellingLicenceApplicationId = applicationId,
                AssignedUserId = Guid.NewGuid(),
                TimestampAssigned = DateTime.Now.AddDays(-2),
                Role = AssignedUserRole.WoodlandOfficer
                },
                new AssigneeHistory
                {
                    FellingLicenceApplicationId = applicationId,
                    AssignedUserId = Guid.NewGuid(),
                    TimestampAssigned = DateTime.Now.AddDays(-1),
                    Role = AssignedUserRole.FieldManager
                }
            };

            var lastDate = fla.StatusHistories.OrderByDescending(x => x.Created).First().Created;
            fla.StatusHistories.Add(new StatusHistory()
            {
                Created = lastDate.AddDays(10),
                FellingLicenceApplicationId = applicationId,
                Status = FellingLicenceStatus.WithApplicant,
                CreatedById = applicantId
            });

            // setup

            _fellingLicenceApplicationExternalRepository.Setup(r=>r.GetAsync(applicationId,
                It.IsAny<CancellationToken>())).ReturnsAsync(fla);

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                    r.CheckUserCanAccessApplicationAsync(fla.Id, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));


            _fellingLicenceApplicationExternalRepository.Setup(r =>
                r.AddStatusHistory(woodlandOwnerId, applicationId, FellingLicenceStatus.Withdrawn, It.IsAny<CancellationToken>()));
            
            var result = await sut.WithdrawApplication(fla.Id, userAccessModel, CancellationToken.None);

            // assert
            Assert.True(result.IsSuccess);
            Assert.Equal(fla.AssigneeHistories.First().AssignedUserId, result.Value.First());
            Assert.Equal(3, result.Value.Count);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.AddStatusHistory(applicantId, fla.Id, FellingLicenceStatus.Withdrawn, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task ShouldReturnSuccess_WhenWithdrawingApplicationReturnedToApplicant(
            FellingLicenceApplication fla,
            Guid woodlandOwnerId,
            Guid? applicantId)
        {
            var sut = CreateSut();

            // dummy data
            var applicationId = fla.Id;
            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = Guid.NewGuid(),
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { fla.WoodlandOwnerId }
            };

            fla.AssigneeHistories = new List<AssigneeHistory>()
            {
                new AssigneeHistory
                {
                    FellingLicenceApplicationId = applicationId,
                    AssignedUserId = Guid.NewGuid(),
                    TimestampAssigned = DateTime.Now.AddDays(-3),
                    Role = AssignedUserRole.AdminOfficer
                },
                new AssigneeHistory
                {
                FellingLicenceApplicationId = applicationId,
                AssignedUserId = Guid.NewGuid(),
                TimestampAssigned = DateTime.Now.AddDays(-2),
                Role = AssignedUserRole.WoodlandOfficer
                },
                new AssigneeHistory
                {
                    FellingLicenceApplicationId = applicationId,
                    AssignedUserId = Guid.NewGuid(),
                    TimestampAssigned = DateTime.Now.AddDays(-1),
                    Role = AssignedUserRole.FieldManager
                }
            };

            var lastDate = fla.StatusHistories.OrderByDescending(x => x.Created).First().Created;
            fla.StatusHistories.Add(new StatusHistory()
            {
                Created = lastDate.AddDays(10),
                FellingLicenceApplicationId = applicationId,
                Status = FellingLicenceStatus.ReturnedToApplicant,
                CreatedById = applicantId
            });

            // setup

            _fellingLicenceApplicationExternalRepository.Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(fla);

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                    r.CheckUserCanAccessApplicationAsync(fla.Id, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                r.AddStatusHistory(woodlandOwnerId, applicationId, FellingLicenceStatus.Withdrawn, It.IsAny<CancellationToken>()));

            var result = await sut.WithdrawApplication(fla.Id, userAccessModel, CancellationToken.None);

            // assert
            Assert.True(result.IsSuccess);
            Assert.Equal(fla.AssigneeHistories.First().AssignedUserId, result.Value.First());
            Assert.Equal(3, result.Value.Count);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.AddStatusHistory(userAccessModel.UserAccountId, fla.Id, FellingLicenceStatus.Withdrawn, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(FellingLicenceStatus.Submitted)]
        [InlineData(FellingLicenceStatus.AdminOfficerReview)]
        [InlineData(FellingLicenceStatus.WoodlandOfficerReview)]
        [InlineData(FellingLicenceStatus.SentForApproval)]
        public async Task ShouldReturnSuccess_WhenWithdrawingApplicationFromPermittedStatus(FellingLicenceStatus status)
        {
            var fla = _fixture.Create<FellingLicenceApplication>();
            var woodlandOwnerId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();
            var sut = CreateSut();

            // dummy data
            var applicationId = fla.Id;
            var userAccessModel = new UserAccessModel
            {
                IsFcUser = false,
                UserAccountId = Guid.NewGuid(),
                AgencyId = null,
                WoodlandOwnerIds = new List<Guid> { fla.WoodlandOwnerId }
            };

            fla.AssigneeHistories = new List<AssigneeHistory>()
            {
                new AssigneeHistory
                {
                    FellingLicenceApplicationId = applicationId,
                    AssignedUserId = Guid.NewGuid(),
                    TimestampAssigned = DateTime.Now.AddDays(-3),
                    Role = AssignedUserRole.AdminOfficer
                },
                new AssigneeHistory
                {
                FellingLicenceApplicationId = applicationId,
                AssignedUserId = Guid.NewGuid(),
                TimestampAssigned = DateTime.Now.AddDays(-2),
                Role = AssignedUserRole.WoodlandOfficer
                },
                new AssigneeHistory
                {
                    FellingLicenceApplicationId = applicationId,
                    AssignedUserId = Guid.NewGuid(),
                    TimestampAssigned = DateTime.Now.AddDays(-1),
                    Role = AssignedUserRole.FieldManager
                }
            };

            var lastDate = fla.StatusHistories.OrderByDescending(x => x.Created).First().Created;
            fla.StatusHistories.Add(new StatusHistory()
            {
                Created = lastDate.AddDays(10),
                FellingLicenceApplicationId = applicationId,
                Status = status,
                CreatedById = applicantId
            });

            // setup

            _fellingLicenceApplicationExternalRepository.Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(fla);

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                    r.CheckUserCanAccessApplicationAsync(fla.Id, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                r.AddStatusHistory(woodlandOwnerId, applicationId, FellingLicenceStatus.Withdrawn, It.IsAny<CancellationToken>()));

            var result = await sut.WithdrawApplication(fla.Id, userAccessModel, CancellationToken.None);

            // assert
            Assert.True(result.IsSuccess);
            Assert.Equal(fla.AssigneeHistories.First().AssignedUserId, result.Value.First());
            Assert.Equal(3, result.Value.Count);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.AddStatusHistory(userAccessModel.UserAccountId, fla.Id, FellingLicenceStatus.Withdrawn, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(FellingLicenceStatus.Approved)]
        [InlineData(FellingLicenceStatus.Refused)]
        [InlineData(FellingLicenceStatus.Draft)]
        [InlineData(FellingLicenceStatus.Received)]
        [InlineData(FellingLicenceStatus.ReferredToLocalAuthority)]
        public async Task ShouldReturnFailure_WhenWithdrawingApplicationInWrongState(FellingLicenceStatus status)
        {
            var sut = CreateSut();
            var externalUserId = Guid.NewGuid();
            var fla = _fixture.Create<FellingLicenceApplication>();
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
                Status = status
            });

            // setup

            _fellingLicenceApplicationExternalRepository.Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(fla);

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                r.AddStatusHistory(externalUserId, applicationId, FellingLicenceStatus.Withdrawn, It.IsAny<CancellationToken>()));

            _fellingLicenceApplicationExternalRepository.Setup(r =>
                    r.CheckUserCanAccessApplicationAsync(fla.Id, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(true));

            var result = await sut.WithdrawApplication(fla.Id, userAccessModel, CancellationToken.None);

            // assert
            Assert.True(result.IsFailure);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.Verify(v => v.AddStatusHistory(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<FellingLicenceStatus>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        public async Task RemoveFromPublicRegisterAsync_ShouldReturnSuccess_WhenPublicRegisterExistsAndIsUpdated(
            Guid applicationId,
            DateTime removedDateTime)
        {
            // Arrange
            var sut = CreateSut();
            var publicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = applicationId,
                ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow.AddDays(-10)
            };

            _fellingLicenceApplicationInternalRepository
                .Setup(r => r.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

            _fellingLicenceApplicationInternalRepository
                .Setup(r => r.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            // Act
            var result = await sut.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, removedDateTime, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(removedDateTime, publicRegister.ConsultationPublicRegisterRemovedTimestamp);
            _fellingLicenceApplicationInternalRepository.Verify(r => r.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveFromPublicRegisterAsync_ShouldReturnFailure_WhenPublicRegisterDoesNotExist()
        {
            // Arrange
            var sut = CreateSut();
            var applicationId = Guid.NewGuid();

            _fellingLicenceApplicationInternalRepository
                .Setup(r => r.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<PublicRegister>.None);

            // Act
            var result = await sut.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, DateTime.UtcNow, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Public register does not have a publication date.", result.Error);
            _fellingLicenceApplicationInternalRepository.Verify(r => r.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RemoveFromPublicRegisterAsync_ShouldReturnFailure_WhenSaveFails()
        {
            // Arrange
            var sut = CreateSut();
            var applicationId = Guid.NewGuid();
            var removedDateTime = DateTime.UtcNow;
            var publicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = applicationId,
                ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow.AddDays(-10)
            };

            _fellingLicenceApplicationInternalRepository
                .Setup(r => r.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

            _fellingLicenceApplicationInternalRepository
                .Setup(r => r.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

            // Act
            var result = await sut.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, removedDateTime, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            _fellingLicenceApplicationInternalRepository.Verify(r => r.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveFromPublicRegisterAsync_ShouldLogError_WhenExceptionIsThrown()
        {
            // Arrange
            var sut = CreateSut();
            var applicationId = Guid.NewGuid();
            var removedDateTime = DateTime.UtcNow;

            _fellingLicenceApplicationInternalRepository
                .Setup(r => r.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await sut.UpdatePublicRegisterEntityToRemovedAsync(applicationId, null, removedDateTime, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Test exception", result.Error);
            _fellingLicenceApplicationInternalRepository.Verify(r => r.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        private WithdrawFellingLicenceService CreateSut()
        {
            _auditService.Reset();
            _fellingLicenceApplicationInternalRepository.Reset();
            _fellingLicenceApplicationExternalRepository.Reset();
            _getFellingLicenceApplicationForExternalUsersService.Reset();
            _userAccountRepository.Reset();
            _clock.Reset();

            _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));

            return new WithdrawFellingLicenceService(
                new NullLogger<WithdrawFellingLicenceService>(),
                _fellingLicenceApplicationInternalRepository.Object,
                _fellingLicenceApplicationExternalRepository.Object,
                new GetFellingLicenceApplicationForExternalUsersService(_fellingLicenceApplicationExternalRepository.Object),
                _clock.Object);
        }
    }
}

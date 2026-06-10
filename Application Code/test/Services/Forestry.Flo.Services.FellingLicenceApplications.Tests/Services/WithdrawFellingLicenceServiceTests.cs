using AutoFixture;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class WithdrawFellingLicenceServiceTests
    {
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationInternalRepository = new();
        private readonly Mock<IFellingLicenceApplicationExternalRepository> _fellingLicenceApplicationExternalRepository = new();
        private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _getFellingLicenceApplicationForExternalUsersService = new();

        private readonly Mock<IClock> _clock = new();
        private readonly IFixture _fixture = new Fixture().CustomiseFixtureForFellingLicenceApplications();
        private readonly DateTime _timestamp = DateTime.UtcNow;

        [Theory, AutoMoqData]
        public async Task WithdrawWhenUnableToGetFellingLicence(
            Guid applicationId,
            UserAccessModel uam,
            List<WithdrawalReason> reasons,
            string? withdrawalReasonOtherDetails,
            string error)
        {
            var sut = CreateSut();

            _getFellingLicenceApplicationForExternalUsersService
                .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<FellingLicenceApplication>(error));

            var result = await sut.WithdrawApplicationAsync(applicationId, uam, reasons, withdrawalReasonOtherDetails, CancellationToken.None);

            Assert.True(result.IsFailure);

            _getFellingLicenceApplicationForExternalUsersService
                .Verify(x => x.GetApplicationByIdAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
            _getFellingLicenceApplicationForExternalUsersService.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();
            _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task WithdrawWhenApplicationHasNoLinkedPropertyProfile(
            Guid applicationId,
            UserAccessModel uam,
            List<WithdrawalReason> reasons,
            string? withdrawalReasonOtherDetails,
            FellingLicenceApplication application)
        {
            application.LinkedPropertyProfile = null;

            var sut = CreateSut();

            _getFellingLicenceApplicationForExternalUsersService
                .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(application));

            var result = await sut.WithdrawApplicationAsync(applicationId, uam, reasons, withdrawalReasonOtherDetails, CancellationToken.None);

            Assert.True(result.IsFailure);

            _getFellingLicenceApplicationForExternalUsersService
                .Verify(x => x.GetApplicationByIdAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
            _getFellingLicenceApplicationForExternalUsersService.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();
            _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();
        }

        [Theory, AutoMoqData]
        public async Task WithdrawWhenApplicationIsNotInAStateToWithdraw(
            Guid applicationId,
            UserAccessModel uam,
            List<WithdrawalReason> reasons,
            string? withdrawalReasonOtherDetails,
            FellingLicenceApplication application)
        {
            foreach (var status in Enum.GetValues(typeof(FellingLicenceStatus)).Cast<FellingLicenceStatus>()
                         .Where(x => !FellingLicenceStatusConstants.WithdrawalStatuses.Contains(x)))
            {
                application.StatusHistories =
                [
                    new StatusHistory
                    {
                        Created = DateTime.Today.ToUniversalTime(),
                        FellingLicenceApplication = application,
                        Status = status
                    }
                ];

                var sut = CreateSut();

                _getFellingLicenceApplicationForExternalUsersService
                    .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success(application));

                var result = await sut.WithdrawApplicationAsync(applicationId, uam, reasons, withdrawalReasonOtherDetails, CancellationToken.None);

                Assert.True(result.IsFailure);

                _getFellingLicenceApplicationForExternalUsersService
                    .Verify(x => x.GetApplicationByIdAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
                _getFellingLicenceApplicationForExternalUsersService.VerifyNoOtherCalls();

                _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();
                _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

            }
        }

        [Theory, AutoMoqData] public async Task WithdrawWhenRepositoryCallFails(
            Guid applicationId,
            UserAccessModel uam,
            List<WithdrawalReason> reasons,
            string? withdrawalReasonOtherDetails,
            FellingLicenceApplication application,
            UserDbErrorReason error)
        {
            application.StatusHistories =
            [
                new StatusHistory
                {
                    Created = DateTime.Today.ToUniversalTime(),
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.ReturnedToApplicant
                }
            ];

            var sut = CreateSut();

            _getFellingLicenceApplicationForExternalUsersService
                .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(application));

            _fellingLicenceApplicationExternalRepository.Setup(x => x.WithdrawApplicationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<List<WithdrawalReason>>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(error));

            var result = await sut.WithdrawApplicationAsync(applicationId, uam, reasons, withdrawalReasonOtherDetails, CancellationToken.None);

            Assert.True(result.IsFailure);

            _getFellingLicenceApplicationForExternalUsersService
                .Verify(x => x.GetApplicationByIdAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
            _getFellingLicenceApplicationForExternalUsersService.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();
            
            _fellingLicenceApplicationExternalRepository
                .Verify(x => x.WithdrawApplicationAsync(
                    applicationId, uam.UserAccountId, _timestamp, reasons, withdrawalReasonOtherDetails, 
                    It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        }

        [Theory, AutoMoqData]
        public async Task WithdrawWhenRepositoryCallSucceeds(
            Guid applicationId,
            UserAccessModel uam,
            List<WithdrawalReason> reasons,
            string? withdrawalReasonOtherDetails,
            FellingLicenceApplication application)
        {
            application.StatusHistories =
            [
                new StatusHistory
                {
                    Created = DateTime.Today.ToUniversalTime(),
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.ReturnedToApplicant
                }
            ];

            var expectedUsers = application.AssigneeHistories
                .Where(x =>
                    x.TimestampUnassigned is null &&
                    x.Role is not (AssignedUserRole.Author or AssignedUserRole.Applicant))
                .Select(x => x.AssignedUserId).ToList();

            var sut = CreateSut();

            _getFellingLicenceApplicationForExternalUsersService
                .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(application));

            _fellingLicenceApplicationExternalRepository.Setup(x => x.WithdrawApplicationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<List<WithdrawalReason>>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var result = await sut.WithdrawApplicationAsync(applicationId, uam, reasons, withdrawalReasonOtherDetails, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equivalent(expectedUsers, result.Value);

            _getFellingLicenceApplicationForExternalUsersService
                .Verify(x => x.GetApplicationByIdAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
            _getFellingLicenceApplicationForExternalUsersService.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationExternalRepository
                .Verify(x => x.WithdrawApplicationAsync(
                    applicationId, uam.UserAccountId, _timestamp, reasons, withdrawalReasonOtherDetails,
                    It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        }

        [Theory, AutoMoqData]
        public async Task WithdrawWhenRepositoryCallSucceedsAsSystemUser(
            Guid applicationId,
            List<WithdrawalReason> reasons,
            string? withdrawalReasonOtherDetails,
            FellingLicenceApplication application)
        {
            application.StatusHistories =
            [
                new StatusHistory
                {
                    Created = DateTime.Today.ToUniversalTime(),
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.ReturnedToApplicant
                }
            ];

            var expectedUsers = application.AssigneeHistories
                .Where(x =>
                    x.TimestampUnassigned is null &&
                    x.Role is not (AssignedUserRole.Author or AssignedUserRole.Applicant))
                .Select(x => x.AssignedUserId).ToList();

            var sut = CreateSut();

            _getFellingLicenceApplicationForExternalUsersService
                .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(application));

            _fellingLicenceApplicationExternalRepository.Setup(x => x.WithdrawApplicationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<List<WithdrawalReason>>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var uam = UserAccessModel.SystemUserAccessModel;

            var result = await sut.WithdrawApplicationAsync(applicationId, uam, reasons, withdrawalReasonOtherDetails, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equivalent(expectedUsers, result.Value);

            _getFellingLicenceApplicationForExternalUsersService
                .Verify(x => x.GetApplicationByIdAsync(applicationId, uam, It.IsAny<CancellationToken>()), Times.Once);
            _getFellingLicenceApplicationForExternalUsersService.VerifyNoOtherCalls();

            _fellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

            _fellingLicenceApplicationExternalRepository
                .Verify(x => x.WithdrawApplicationAsync(
                    applicationId, null, _timestamp, reasons, withdrawalReasonOtherDetails,
                    It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationExternalRepository.VerifyNoOtherCalls();

        }

        private WithdrawFellingLicenceService CreateSut()
        {
            _fellingLicenceApplicationInternalRepository.Reset();
            _fellingLicenceApplicationExternalRepository.Reset();
            _getFellingLicenceApplicationForExternalUsersService.Reset();
            _clock.Reset();

            _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(_timestamp));

            return new WithdrawFellingLicenceService(
                new NullLogger<WithdrawFellingLicenceService>(),
                _fellingLicenceApplicationInternalRepository.Object,
                _fellingLicenceApplicationExternalRepository.Object,
                _getFellingLicenceApplicationForExternalUsersService.Object,
                _clock.Object);
        }
    }
}

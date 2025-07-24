using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class SubmitFellingLicenceServiceAutoAssignWoodlandOfficerTests
    {
        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository;
        private readonly Mock<IUserAccountRepository> _userAccountRepository;
        private readonly Mock<IAuditService<SubmitFellingLicenceService>> _auditService;
        private readonly Mock<IForesterServices> _foresterServices;
        private readonly Mock<IForestryServices> _forestryServices;
        private readonly Mock<ISendNotifications> _notificationsService;
        private readonly Mock<IClock> _clock;
        private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreasMock;

        public SubmitFellingLicenceServiceAutoAssignWoodlandOfficerTests()
        {
            _userAccountRepository = new Mock<IUserAccountRepository>();
            _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
            _foresterServices = new ();
            _forestryServices = new();
            _clock = new Mock<IClock>();
            _auditService = new Mock<IAuditService<SubmitFellingLicenceService>>();
            _notificationsService = new Mock<ISendNotifications>();
            _getConfiguredFcAreasMock = new Mock<IGetConfiguredFcAreas>();
        }

        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Theory, AutoMoqData]
        public async Task ShouldReturnSuccess_WhenAllDetailsCorrect(
            FellingLicenceApplication fla,
            Point centrePoint,
            UserAccount woodlandOfficerAccount,
            WoodlandOfficer woodlandOfficer)
        {
            var sut = CreateSut();

            fla.CentrePoint = JsonConvert.SerializeObject(centrePoint);

            var unassignedId = Guid.NewGuid();

            var externalApplicantId = Guid.NewGuid();

            woodlandOfficer.OfficerName = woodlandOfficerAccount.FullName(false);

            foreach (var assignee in fla.AssigneeHistories)
            {
                assignee.Role = AssignedUserRole.AdminOfficer;
            }

            // setup

            _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            _fellingLicenceApplicationRepository.Setup(r => r.AssignFellingLicenceApplicationToStaffMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AssignedUserRole>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((false, Maybe.From(unassignedId)));


            _foresterServices.Setup(r => r.GetWoodlandOfficerAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(woodlandOfficer);

            _userAccountRepository.Setup(r => r.GetByFullnameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserAccount> { woodlandOfficerAccount });

            var (isSuccess, _, result) = await sut.AutoAssignWoodlandOfficerAsync(fla.Id, externalApplicantId, "link", CancellationToken.None);

            isSuccess.Should().BeTrue();

            // assert returned record holds correct data

            result.AssignedUserEmail.Should().Be(woodlandOfficerAccount.Email);
            result.AssignedUserFirstName.Should().Be(woodlandOfficerAccount.FirstName);
            result.AssignedUserLastName.Should().Be(woodlandOfficerAccount.LastName);
            result.AssignedUserId.Should().Be(woodlandOfficerAccount.Id);
            result.UnassignedUserId.Should().Be(unassignedId);

            _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
            _foresterServices.Verify(v => v.GetWoodlandOfficerAsync(It.Is<Point>(e => JsonConvert.SerializeObject(e) == fla.CentrePoint), CancellationToken.None), Times.Once);
            _userAccountRepository.Verify(v => v.GetByFullnameAsync(woodlandOfficerAccount.FirstName!, woodlandOfficerAccount.LastName!, CancellationToken.None), Times.Once);
            _fellingLicenceApplicationRepository.Verify(v => v.AssignFellingLicenceApplicationToStaffMemberAsync(fla.Id, woodlandOfficerAccount.Id, AssignedUserRole.WoodlandOfficer, It.IsAny<DateTime>(), CancellationToken.None), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task ShouldReturnFailure_WhenWoodlandOwnerIsAlreadyAssigned(
            FellingLicenceApplication fla,
            Point centrePoint,
            UserAccount woodlandOfficerAccount,
            WoodlandOfficer woodlandOfficer)
        {
            var sut = CreateSut();

            fla.CentrePoint = JsonConvert.SerializeObject(centrePoint);

            var externalApplicantId = Guid.NewGuid();

            woodlandOfficer.OfficerName = woodlandOfficerAccount.FullName(false);

            foreach (var assignee in fla.AssigneeHistories)
            {
                assignee.Role = AssignedUserRole.AdminOfficer;
            }

            fla.AssigneeHistories[0].AssignedUserId = woodlandOfficerAccount.Id;
            fla.AssigneeHistories[0].Role = AssignedUserRole.WoodlandOfficer;
            fla.AssigneeHistories[0].TimestampUnassigned = null;

            // setup

            _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            _foresterServices.Setup(r => r.GetWoodlandOfficerAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(woodlandOfficer);

            _userAccountRepository.Setup(r => r.GetByFullnameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserAccount> { woodlandOfficerAccount });

            var result = await sut.AutoAssignWoodlandOfficerAsync(fla.Id, externalApplicantId, "link", CancellationToken.None);

            result.IsFailure.Should().BeTrue();

            _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
            _foresterServices.Verify(v => v.GetWoodlandOfficerAsync(It.Is<Point>(e => JsonConvert.SerializeObject(e) == fla.CentrePoint), CancellationToken.None), Times.Once);
            _userAccountRepository.Verify(v => v.GetByFullnameAsync(woodlandOfficerAccount.FirstName!, woodlandOfficerAccount.LastName!, CancellationToken.None), Times.Once);
            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
            _notificationsService.VerifyNoOtherCalls();
            _auditService.VerifyNoOtherCalls();
        }


        [Theory, AutoMoqData]

        public async Task ShouldReturnFailure_WhenUserAccountCannotBeMatched(
            FellingLicenceApplication fla,
            Point centrePoint,
            UserAccount woodlandOfficerAccount,
            WoodlandOfficer woodlandOfficer)
        {
            var sut = CreateSut();


            var externalApplicantId = Guid.NewGuid();

            woodlandOfficerAccount.FirstName = "John Marc Andrew";
            woodlandOfficerAccount.LastName = "Llywelyn Dafydd Sion";

            woodlandOfficer.OfficerName = woodlandOfficerAccount.FullName(false);

            // setup
            fla.CentrePoint = JsonConvert.SerializeObject(centrePoint);

            _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);


            _foresterServices.Setup(r => r.GetWoodlandOfficerAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(woodlandOfficer);

            _userAccountRepository.Setup(r => r.GetByFullnameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserAccount>());

            var result = await sut.AutoAssignWoodlandOfficerAsync(fla.Id, externalApplicantId, "link", CancellationToken.None);

            result.IsFailure.Should().BeTrue();

            _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
            _foresterServices.Verify(v => v.GetWoodlandOfficerAsync(It.Is<Point>(e => JsonConvert.SerializeObject(e) == fla.CentrePoint), CancellationToken.None), Times.Once);
            _userAccountRepository.Verify(v => v.GetByFullnameAsync(woodlandOfficerAccount.FirstName!, woodlandOfficerAccount.LastName!, CancellationToken.None), Times.Once);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]

        public async Task ShouldReturnFailure_WhenWoodlandOfficerResponseFailure(
            FellingLicenceApplication fla,
            Point centrePoint)
        {
            var sut = CreateSut();

            fla.CentrePoint = JsonConvert.SerializeObject(centrePoint);

            var externalApplicantId = Guid.NewGuid();

            // setup

            _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);


            _foresterServices.Setup(r => r.GetWoodlandOfficerAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<WoodlandOfficer>("Cannot find woodland officer response"));

            var result = await sut.AutoAssignWoodlandOfficerAsync(fla.Id, externalApplicantId, "link", CancellationToken.None);

            result.IsFailure.Should().BeTrue();

            _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
            _foresterServices.Verify(v => v.GetWoodlandOfficerAsync(It.Is<Point>(e => JsonConvert.SerializeObject(e) == fla.CentrePoint), CancellationToken.None), Times.Once);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]

        public async Task ShouldReturnFailure_WhenBadCentrePointNot(
            FellingLicenceApplication fla)
        {
            var sut = CreateSut();
            var externalApplicantId = Guid.NewGuid();

            _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            var result = await sut.AutoAssignWoodlandOfficerAsync(fla.Id, externalApplicantId, "link", CancellationToken.None);

            result.IsFailure.Should().BeTrue();

            _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task ShouldReturnFailure_WhenCentrePointNotSet(
            FellingLicenceApplication fla)
        {
            var sut = CreateSut();
            var externalApplicantId = Guid.NewGuid();
            fla.CentrePoint = "";

            _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fla);

            var result = await sut.AutoAssignWoodlandOfficerAsync(fla.Id, externalApplicantId, "link", CancellationToken.None);

            result.IsFailure.Should().BeTrue();

            _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure),
                    It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]

        public async Task ShouldReturnFailure_WhenFLANotFound()
        {
            var sut = CreateSut();

            var externalApplicantId = Guid.NewGuid();
            var flaId = Guid.NewGuid();

            // setup

            _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

            var result = await sut.AutoAssignWoodlandOfficerAsync(flaId, externalApplicantId, "link", CancellationToken.None);

            result.IsFailure.Should().BeTrue();

            _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(flaId, CancellationToken.None), Times.Once);

            _auditService.Verify(s =>
                s.PublishAuditEventAsync(It.Is<AuditEvent>(
                        e => e.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task CalculateCentrePointForApplicationAsync_Fails(Guid applicationId)
        {

            var sut = CreateSut();

            _forestryServices.Setup(a => a.CalculateCentrePointAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<Point>("Error"));


            var result =
                    await sut.CalculateCentrePointForApplicationAsync(applicationId, new List<string>(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be($"Unable to calculate centre point for application of id {applicationId}");
            _forestryServices.Verify(a => a.CalculateCentrePointAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task CalculateCentrePointForApplicationAsync_Passes(Point point)
        {
            var sut = CreateSut();
            _forestryServices.Setup(a => a.CalculateCentrePointAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(point));

            var result = await sut.CalculateCentrePointForApplicationAsync(Guid.NewGuid(), new List<string>(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(point.GetGeometrySimple());
        }


        [Theory, AutoMoqData]
        public async Task CalculateOSGridAsync_Fails(Point centrePoint)
        {
            var sut = CreateSut();
            _forestryServices.Setup(a => a.GetOSGridReferenceAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<string>("Error"));

            var result =
                    await sut.CalculateOSGridAsync(centrePoint.GetGeometrySimple() , CancellationToken.None);

            result.IsFailure.Should().BeTrue();

            _forestryServices.Verify(a => a.GetOSGridReferenceAsync(It.Is<Point>(e => e.GetGeometrySimple() == centrePoint.GetGeometrySimple()), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task CalculateOSGridAsync_FailsEmptyString()
        {

            var sut = CreateSut();
            _forestryServices.Setup(a => a.GetOSGridReferenceAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<string>("Error"));

            var result =
                    await sut.CalculateOSGridAsync(string.Empty, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _forestryServices.Verify(a => a.GetOSGridReferenceAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        [Theory, AutoMoqData]
        public async Task CalculateOSGridAsync_FailBadString(string val)
        {

            var sut = CreateSut();
            _forestryServices.Setup(a => a.GetOSGridReferenceAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<string>("Error"));

            var result =
                    await sut.CalculateOSGridAsync(val, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _forestryServices.Verify(a => a.GetOSGridReferenceAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        public async Task CalculateOSGridAsync_Pass(Point centrePoint)
        {

            var sut = CreateSut();
            _forestryServices.Setup(a => a.GetOSGridReferenceAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success("UT 999 999"));

            var result =
                    await sut.CalculateOSGridAsync(centrePoint.GetGeometrySimple(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _forestryServices.Verify(a => a.GetOSGridReferenceAsync(It.Is<Point>(e => e.GetGeometrySimple() == centrePoint.GetGeometrySimple()), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task GetAreaCodeAsync_FailsEmptyString()
        {

            var sut = CreateSut();
            _foresterServices.Setup(a => a.GetAdminBoundaryIdAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<AdminBoundary>("Error"));

            var result =
                    await sut.GetConfiguredFcAreaAsync(string.Empty, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _foresterServices.Verify(a => a.GetAdminBoundaryIdAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        [Theory, AutoMoqData]
        public async Task GetAreaCodeAsync_FailBadString(string val)
        {

            var sut = CreateSut();
            _foresterServices.Setup(a => a.GetAdminBoundaryIdAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<AdminBoundary>("Error"));

            var result =
                    await sut.GetConfiguredFcAreaAsync(val, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _foresterServices.Verify(a => a.GetAdminBoundaryIdAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory, AutoMoqData]
        public async Task GetAreaCodeAsync_Pass(Point centrePoint)
        {
            var value = new AdminBoundary();
            value.Code = "010";

            var sut = CreateSut();
            _foresterServices.Setup(a => a.GetAdminBoundaryIdAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(value));

            var result =
                    await sut.GetConfiguredFcAreaAsync(centrePoint.GetGeometrySimple(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _foresterServices.Verify(a => a.GetAdminBoundaryIdAsync(It.Is<Point>(e => e.GetGeometrySimple() == centrePoint.GetGeometrySimple()), It.IsAny<CancellationToken>()), Times.Once);
        }

        private SubmitFellingLicenceService CreateSut()
        {
            _auditService.Reset();
            _fellingLicenceApplicationRepository.Reset();
            _userAccountRepository.Reset();
            _clock.Reset();
            _foresterServices.Reset();
            _notificationsService.Reset();
            _getConfiguredFcAreasMock.Reset();

            _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));

            _getConfiguredFcAreasMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
                Result.Success(
                    new List<ConfiguredFcArea>
                    {
                        new (new AreaModel { Code = "010", Id = Guid.NewGuid(), Name = "North West" }, "010", "TestHub1"),
                        new (new AreaModel { Code = "018", Id = Guid.NewGuid(), Name = "South West" }, "018", "TestHub2")
                    }));

            return new SubmitFellingLicenceService(
                _auditService.Object,
                new RequestContext("test", new RequestUserModel(new ClaimsPrincipal())),
                new NullLogger<SubmitFellingLicenceService>(),
                _fellingLicenceApplicationRepository.Object,
                _userAccountRepository.Object,
                _foresterServices.Object,
                _forestryServices.Object,
                _clock.Object,
                _notificationsService.Object,
                _getConfiguredFcAreasMock.Object);
        }
    }
}

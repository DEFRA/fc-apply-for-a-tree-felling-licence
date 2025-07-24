using System.Security.Claims;
using System.Text.Json;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.MassTransit.Messages;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.External.Web.Tests.Services;

public class AssignWoodlandOfficerAsyncUseCaseTests
{
    private readonly Mock<IGetFellingLicenceApplicationForExternalUsers> _getFellingLicenceService;
    private readonly Mock<ISendNotifications> _notificationsMock;
    private readonly Mock<IAuditService<AssignWoodlandOfficerAsyncUseCase>> _auditMock;
    private readonly Mock<ISubmitFellingLicenceService> _submitFellingLicenceService;
    private readonly Mock<IUpdateCentrePoint> _updateCentrePointMock;
    private readonly Mock<IOptions<InternalUserSiteOptions>> _optionsMock;
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreasMock;

    private readonly Fixture _fixtureInstance;

    private const string AdminHubAddress = "admin hub address";

    public AssignWoodlandOfficerAsyncUseCaseTests()
    {
        _auditMock = new Mock<IAuditService<AssignWoodlandOfficerAsyncUseCase>>();
        _getFellingLicenceService = new Mock<IGetFellingLicenceApplicationForExternalUsers>();
        _notificationsMock = new Mock<ISendNotifications>();
        _submitFellingLicenceService = new Mock<ISubmitFellingLicenceService>();
        _updateCentrePointMock = new Mock<IUpdateCentrePoint>();
        _optionsMock = new Mock<IOptions<InternalUserSiteOptions>>();
        _getConfiguredFcAreasMock = new();
        _fixtureInstance = new Fixture();
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string BaseUrl = "testUrl";

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WhenOfficerCorrectlySetAndNotificationSuccessful(
        FellingLicenceApplication application,
        AutoAssignWoRecord autoAssignWoRecord)
    {
        var testUrl = $"{BaseUrl}FellingLicenceApplication/ApplicationSummary/{application.Id}";

        var message = new AssignWoodlandOfficerMessage(
            testUrl,
            application.WoodlandOwnerId,
            application.CreatedById,
            false,
            null,
            application.Id);

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s => s.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _submitFellingLicenceService.Setup(s => s.AutoAssignWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(autoAssignWoRecord);

        _notificationsMock.Setup(s=>s.SendNotificationAsync(
            It.IsAny<UserAssignedToApplicationDataModel>(),
            Flo.Services.Notifications.Entities.NotificationType.UserAssignedToApplication,
            It.IsAny<NotificationRecipient>(),
            It.IsAny<NotificationRecipient[]?>(),
            It.IsAny<NotificationAttachment[]?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var result = await sut.AssignWoodlandOfficerAsync(message, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _getFellingLicenceService.Verify(v => v.GetApplicationByIdAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count == 1
            ),
            It.IsAny<CancellationToken>()));

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMember
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    AssignedUserRole = AssignedUserRole.WoodlandOfficer.GetDisplayName()!,
                    AssignedStaffMemberId = autoAssignWoRecord.AssignedUserId,
                    UnassignedStaffMemberId = autoAssignWoRecord.UnassignedUserId,
                    NotificationSent = true
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WhenOfficerCorrectlySetAndNotificationFails(
        FellingLicenceApplication application, 
        AutoAssignWoRecord autoAssignWoRecord)
    {
        var testUrl = $"{BaseUrl}FellingLicenceApplication/ApplicationSummary/{application.Id}";

        var message = new AssignWoodlandOfficerMessage(
            testUrl,
            application.WoodlandOwnerId,
            application.CreatedById,
            false,
            null,
            application.Id);

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s => s.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _submitFellingLicenceService.Setup(s => s.AutoAssignWoodlandOfficerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(autoAssignWoRecord);

        _notificationsMock.Setup(s => s.SendNotificationAsync(
            It.IsAny<UserAssignedToApplicationDataModel>(),
            Flo.Services.Notifications.Entities.NotificationType.UserAssignedToApplication,
            It.IsAny<NotificationRecipient>(),
            It.IsAny<NotificationRecipient[]?>(),
            It.IsAny<NotificationAttachment[]?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("test no notification sent"));

        var result = await sut.AssignWoodlandOfficerAsync(message, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _getFellingLicenceService.Verify(v => v.GetApplicationByIdAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count == 1
            ),
            It.IsAny<CancellationToken>()));

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMember
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    AssignedUserRole = AssignedUserRole.WoodlandOfficer.GetDisplayName()!,
                    AssignedStaffMemberId = autoAssignWoRecord.AssignedUserId,
                    UnassignedStaffMemberId = autoAssignWoRecord.UnassignedUserId,
                    NotificationSent = false
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenApplicationCannotBeRetrieved(
        FellingLicenceApplication application)
    {
        var testUrl = $"{BaseUrl}FellingLicenceApplication/ApplicationSummary/{application.Id}";

        var message = new AssignWoodlandOfficerMessage(
            testUrl,
            application.WoodlandOwnerId,
            application.CreatedById,
            false,
            null,
            application.Id);

        var userAccessModel = new UserAccessModel
        {
            AgencyId = message.AgencyId,
            IsFcUser = message.IsFcUser,
            UserAccountId = message.UserId,
            WoodlandOwnerIds = new List<Guid> { message.WoodlandOwnerId }
        };

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s => s.GetApplicationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>("test"));

        var result = await sut.AssignWoodlandOfficerAsync(message, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _getFellingLicenceService.Verify(v => v.GetApplicationByIdAsync(
            application.Id,
            It.Is<UserAccessModel>(x => x.IsFcUser == message.IsFcUser &&
                                        x.AgencyId == message.AgencyId &&
                                        x.UserAccountId == message.UserId 
                                        && x.WoodlandOwnerIds!.Contains(message.WoodlandOwnerId)
                                        && x.WoodlandOwnerIds.Count ==1
                                        ),
            It.IsAny<CancellationToken>()));

        _auditMock.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure
                && x.SourceEntityId == message.ApplicationId
                && x.UserId == message.UserId
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = $"Unable to retrieve application with identifier {message.ApplicationId}"
                }, _options)), 
            CancellationToken.None), Times.Once);
        _submitFellingLicenceService.VerifyNoOtherCalls();
        _updateCentrePointMock.VerifyNoOtherCalls();
    }
    
    private AssignWoodlandOfficerAsyncUseCase CreateSut()
    {
        _notificationsMock.Reset();
        _optionsMock.Setup(x => x.Value).Returns(new InternalUserSiteOptions{ BaseUrl = BaseUrl});
        _getFellingLicenceService.Reset();
        _updateCentrePointMock.Reset();
        _auditMock.Reset();
        _getConfiguredFcAreasMock.Reset();

        _getConfiguredFcAreasMock.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        return new AssignWoodlandOfficerAsyncUseCase(
            _auditMock.Object,
            new RequestContext("test", new RequestUserModel(new ClaimsPrincipal())),
            _submitFellingLicenceService.Object,
            _getFellingLicenceService.Object,
            _notificationsMock.Object,
            _getConfiguredFcAreasMock.Object,
            new NullLogger<AssignWoodlandOfficerAsyncUseCase>(),
            _optionsMock.Object);
    }
}
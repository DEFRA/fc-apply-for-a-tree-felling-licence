using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System.Text.Json;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using UserAccountModel = Forestry.Flo.Services.InternalUsers.Models.UserAccountModel;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class PublicRegisterExpiryUseCaseTests
{
    private readonly Mock<IGetFellingLicenceApplicationForInternalUsers> _getFellingLicenceService;
    private readonly Mock<IUpdateFellingLicenceApplication> _updateFellingLicenceService;
    private readonly Mock<IUserAccountService> _internalAccountServiceMock;
    private readonly Mock<IPublicRegister> _publicRegisterMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Instant _now = new();
    private readonly Mock<ISendNotifications> _notificationsMock;
    private readonly Mock<IAuditService<PublicRegisterExpiryUseCase>> _auditMock;
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreasMock;
    private readonly Fixture _fixtureInstance;

    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();
    private const string InternalBaseUrl = "baseUrl";
    private const string AdminHubAddress = "admin hub address";

    public PublicRegisterExpiryUseCaseTests()
    {
        _getFellingLicenceService = new Mock<IGetFellingLicenceApplicationForInternalUsers>();
        _updateFellingLicenceService = new Mock<IUpdateFellingLicenceApplication>();
        _publicRegisterMock = new Mock<IPublicRegister>();
        _internalAccountServiceMock = new Mock<IUserAccountService>();
        _notificationsMock = new Mock<ISendNotifications>();
        _auditMock = new Mock<IAuditService<PublicRegisterExpiryUseCase>>();
        _clockMock = new Mock<IClock>();
        _getConfiguredFcAreasMock = new();
        _fixtureInstance = new Fixture();
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task NoProcessing_WhenZeroApplicationsRetrieved()
    {
        var sut = CreateSut();

        _getFellingLicenceService.Setup(s =>
                s.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel>());

        await sut.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(InternalBaseUrl, CancellationToken.None);

        _getFellingLicenceService.Verify(v => v.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(CancellationToken.None), Times.Once);

        _internalAccountServiceMock.VerifyNoOtherCalls();
        _updateFellingLicenceService.VerifyNoOtherCalls();
        _publicRegisterMock.VerifyNoOtherCalls();
        _notificationsMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory]
    [AutoMoqData]
    public async Task WhenApplicationFound_ButNoPREntity(List<UserAccountModel> users)
    {
        //arrange
        var sut = CreateSut();
        var model = _fixtureInstance.Build<PublicRegisterPeriodEndModel>().Without(x => x.PublicRegister).Create();

        _getFellingLicenceService.Setup(s =>
                s.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel>(new[] { model }));

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(users));

        // act
        await sut.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(InternalBaseUrl, CancellationToken.None);

        // assert
        _getFellingLicenceService.Verify(v => v.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(CancellationToken.None), Times.Once);

        var expectedUserIds = model.AssignedUserIds!.Distinct().ToList();
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(userIds => userIds.SequenceEqual(expectedUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);

        _updateFellingLicenceService.VerifyNoOtherCalls();
        _publicRegisterMock.VerifyNoOtherCalls();
        _notificationsMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory]
    [AutoMoqData]
    public async Task WhenApplicationFound_ButNoEsriObjectIdExistsInPREntity(List<UserAccountModel> users)
    {
        //arrange
        _fixtureInstance.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixtureInstance.Behaviors.Remove(b));
        _fixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());

        var prModel = _fixtureInstance.Build<PublicRegister>().Without(x => x.EsriId).Create();
        var model = _fixtureInstance.Build<PublicRegisterPeriodEndModel>()
            .With(x => x.PublicRegister, prModel)
            .Create();

        var sut = CreateSut();

        _getFellingLicenceService.Setup(s =>
                s.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel>(new[] { model }));

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(users));

        // act
        await sut.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(InternalBaseUrl, CancellationToken.None);

        // assert
        _getFellingLicenceService.Verify(v => v.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(CancellationToken.None), Times.Once);

        var expectedUserIds = model.AssignedUserIds!.Distinct().ToList();

        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(userIds => userIds.SequenceEqual(expectedUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);

        _updateFellingLicenceService.VerifyNoOtherCalls();
        _publicRegisterMock.VerifyNoOtherCalls();
        _notificationsMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory]
    [AutoMoqData]
    public async Task WhenCouldNotRetrieveApproverUsersForNotification_ShouldStillContinueWithRemovalFromPublicRegister(PublicRegisterPeriodEndModel model)
    {
        // arrange
        var sut = CreateSut();

        _getFellingLicenceService.Setup(s =>
                s.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel>(new[] { model }));

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<UserAccountModel>>("Error"));

        _publicRegisterMock
            .Setup(service => service.RemoveCaseFromConsultationRegisterAsync(model.PublicRegister.EsriId.Value, model.ApplicationReference, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // act
        await sut.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(InternalBaseUrl, CancellationToken.None);

        // assert 
        _getFellingLicenceService.Verify(v => v.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(CancellationToken.None), Times.Once);

        var expectedUserIds = model.AssignedUserIds!.Distinct().ToList();

        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(userIds => userIds.SequenceEqual(expectedUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);

        _publicRegisterMock.Verify(service =>
            service.RemoveCaseFromConsultationRegisterAsync(model.PublicRegister.EsriId.Value, model.ApplicationReference, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);

        _clockMock.Verify(x => x.GetCurrentInstant(), Times.Once);

        _updateFellingLicenceService.Verify(v => v.SetRemovalDateOnConsultationPublicRegisterEntryAsync(
            model.PublicRegister.FellingLicenceApplicationId, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);

        MustAuditRemovalFromConsultationPublicRegisterOnSuccess(model);

        _updateFellingLicenceService.VerifyNoOtherCalls();
        _publicRegisterMock.VerifyNoOtherCalls();
        _notificationsMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory]
    [AutoMoqData]
    public async Task WhenFailsToBeRemovedFromConsultationPR_ShouldSendUsersNotification(PublicRegisterPeriodEndModel model)
    {
        // arrange
        var sut = CreateSut();

        var expectedUsers = model.AssignedUserIds!.Distinct()
            .Select(userAccountModel => _fixtureInstance.Build<UserAccountModel>()
                .With(x => x.UserAccountId, userAccountModel).Create()).ToList();

        _getFellingLicenceService.Setup(s =>
                s.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel>(new[] { model }));

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedUsers));

        _publicRegisterMock
            .Setup(service => service.RemoveCaseFromConsultationRegisterAsync(model.PublicRegister.EsriId.Value, model.ApplicationReference, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Error"));

        // act
        await sut.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(InternalBaseUrl, CancellationToken.None);

        // assert 
        _getFellingLicenceService.Verify(v => v.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(CancellationToken.None), Times.Once);

        var expectedUserIds = model.AssignedUserIds!.Distinct().ToList();

        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(userIds => userIds.SequenceEqual(expectedUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);

        _publicRegisterMock.Verify(service =>
            service.RemoveCaseFromConsultationRegisterAsync(model.PublicRegister.EsriId.Value, model.ApplicationReference, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);

        _clockMock.Verify(x => x.GetCurrentInstant(), Times.Once);

        _updateFellingLicenceService.Verify(v => v.SetRemovalDateOnConsultationPublicRegisterEntryAsync(
            model.PublicRegister.FellingLicenceApplicationId, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Never);


        _updateFellingLicenceService.VerifyNoOtherCalls();
        _publicRegisterMock.VerifyNoOtherCalls();

        MustAuditRemovalFromConsultationPublicRegisterOnFailure(model);
        MustAuditNotificationsSent(model, false);

        foreach (var internalUser in expectedUsers)
        {
            MustSendNotificationOnFailure(model, internalUser);
        }

        _notificationsMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory]
    [AutoMoqData]
    public async Task WhenSuccessfullyRemovedFromConsultationPR_ButCouldNotSetLocalRemovalFlagOnPrEntity(PublicRegisterPeriodEndModel model)
    {
        // arrange
        var sut = CreateSut();

        _getFellingLicenceService.Setup(s =>
                s.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel>(new[] { model }));

        var expectedUsers = model.AssignedUserIds!.Distinct()
            .Select(userAccountModel => _fixtureInstance.Build<UserAccountModel>()
                .With(x => x.UserAccountId, userAccountModel).Create()).ToList();

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedUsers));

        _publicRegisterMock
            .Setup(service => service.RemoveCaseFromConsultationRegisterAsync(model.PublicRegister.EsriId.Value, model.ApplicationReference, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateFellingLicenceService.Setup(v => v.SetRemovalDateOnConsultationPublicRegisterEntryAsync(
            It.IsAny<Guid>(), _now.ToDateTimeUtc(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("error"));

        // act
        await sut.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(InternalBaseUrl, CancellationToken.None);

        // assert 
        _getFellingLicenceService.Verify(v => v.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(CancellationToken.None), Times.Once);

        var expectedUserIds = model.AssignedUserIds!.Distinct().ToList();

        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(userIds => userIds.SequenceEqual(expectedUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);

        _publicRegisterMock.Verify(service =>
            service.RemoveCaseFromConsultationRegisterAsync(model.PublicRegister.EsriId.Value, model.ApplicationReference, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);

        _clockMock.Verify(x => x.GetCurrentInstant(), Times.Once);

        _updateFellingLicenceService.Verify(v => v.SetRemovalDateOnConsultationPublicRegisterEntryAsync(
            model.PublicRegister.FellingLicenceApplicationId, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);

        MustAuditRemovalFromConsultationPublicRegisterOnSuccess(model, false);

        _notificationsMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory]
    [AutoMoqData]
    public async Task WhenSuccessfullyRemovedFromConsultationPR(PublicRegisterPeriodEndModel model)
    {
        // arrange
        var sut = CreateSut();

        _getFellingLicenceService.Setup(s =>
                s.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel>(new[] { model }));

        var expectedUsers = model.AssignedUserIds!.Distinct()
            .Select(userAccountModel => _fixtureInstance.Build<UserAccountModel>()
                .With(x => x.UserAccountId, userAccountModel).Create()).ToList();

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedUsers));

        _publicRegisterMock
            .Setup(service => service.RemoveCaseFromConsultationRegisterAsync(model.PublicRegister.EsriId.Value, model.ApplicationReference, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // act
        await sut.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(InternalBaseUrl, CancellationToken.None);

        // assert 
        _getFellingLicenceService.Verify(v => v.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(CancellationToken.None), Times.Once);

        var expectedUserIds = model.AssignedUserIds!.Distinct().ToList();

        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(userIds => userIds.SequenceEqual(expectedUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);

        _publicRegisterMock.Verify(service =>
            service.RemoveCaseFromConsultationRegisterAsync(model.PublicRegister.EsriId.Value, model.ApplicationReference, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);

        _clockMock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _updateFellingLicenceService.Verify(v => v.SetRemovalDateOnConsultationPublicRegisterEntryAsync(
            model.PublicRegister.FellingLicenceApplicationId, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);

        MustAuditRemovalFromConsultationPublicRegisterOnSuccess(model);

        _notificationsMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory]
    [AutoMoqData]
    public async Task WhenMixOfSuccessAndFailureWhenRemovingFromConsultationPR(List<PublicRegisterPeriodEndModel> models)
    {
        // arrange
        var sut = CreateSut();

        _getFellingLicenceService.Setup(s =>
                s.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicRegisterPeriodEndModel>(models));

        var expectedUsers = models.SelectMany(x => x.AssignedUserIds).Distinct()
            .Select(userAccountModel => _fixtureInstance.Build<UserAccountModel>()
                .With(x => x.UserAccountId, userAccountModel).Create()).ToList();

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedUsers));

        _publicRegisterMock
            .SetupSequence(service => service.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(), _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success())
            .ReturnsAsync(Result.Failure("error"))
            .ReturnsAsync(Result.Success);

        // act
        await sut.RemoveExpiredApplicationsFromConsultationPublicRegisterAsync(InternalBaseUrl, CancellationToken.None);

        // assert 
        _getFellingLicenceService.Verify(v => v.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(CancellationToken.None), Times.Once);

        var expectedUserIds = models.SelectMany(x => x.AssignedUserIds).Distinct().Distinct().ToList();

        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(userIds => userIds.SequenceEqual(expectedUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);

        _publicRegisterMock.Verify(service =>
            service.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(), _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Exactly(models.Count));

        _clockMock.Verify(x => x.GetCurrentInstant(), Times.Once);

        _updateFellingLicenceService.Verify(v => v.SetRemovalDateOnConsultationPublicRegisterEntryAsync(
            models.First().PublicRegister.FellingLicenceApplicationId, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Exactly(1));

        _updateFellingLicenceService.Verify(v => v.SetRemovalDateOnConsultationPublicRegisterEntryAsync(
            models.Last().PublicRegister.FellingLicenceApplicationId, _now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Exactly(1));

        _updateFellingLicenceService.VerifyNoOtherCalls();

        foreach (var internalUser in expectedUsers.Where(x => models[1].AssignedUserIds.Contains(x.UserAccountId)))
        {
            MustSendNotificationOnFailure(models[1], internalUser);
        }

        _notificationsMock.VerifyNoOtherCalls();

        MustAuditRemovalFromConsultationPublicRegisterOnSuccess(models[0]);
        MustAuditRemovalFromConsultationPublicRegisterOnSuccess(models[2]);
        MustAuditRemovalFromConsultationPublicRegisterOnFailure(models[1]);

        MustAuditNotificationsSent(models[1], false);

        _auditMock.VerifyNoOtherCalls();
    }

    private PublicRegisterExpiryUseCase CreateSut()
    {
        _internalAccountServiceMock.Reset();
        _publicRegisterMock.Reset();
        _clockMock.Reset();
        _notificationsMock.Reset();
        _getFellingLicenceService.Reset();
        _auditMock.Reset();
        _getConfiguredFcAreasMock.Reset();

        _clockMock.Setup(x => x.GetCurrentInstant()).Returns(_now);
        _getConfiguredFcAreasMock.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        return new PublicRegisterExpiryUseCase(
            _getFellingLicenceService.Object,
            _updateFellingLicenceService.Object,
            _publicRegisterMock.Object,
            new NullLogger<PublicRegisterExpiryUseCase>(),
            _internalAccountServiceMock.Object,
            _notificationsMock.Object,
            new RequestContext(
                _requestContextCorrelationId,
                new RequestUserModel(UserFactory.CreateUnauthenticatedUser())),
            _auditMock.Object,
            _getConfiguredFcAreasMock.Object,
            _clockMock.Object
        );
    }

    private void MustAuditRemovalFromConsultationPublicRegisterOnSuccess(PublicRegisterPeriodEndModel model, bool? localRemovalFlagSet = true)
    {
        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ConsultationPublicRegisterAutomatedExpirationRemovalSuccess
                         && e.SourceEntityId == model.PublicRegister!.FellingLicenceApplicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             CaseReference = model.ApplicationReference,
                             ConsultationPublicRegisterPeriodEndDate = model.PublicRegister!.ConsultationPublicRegisterExpiryTimestamp,
                             LocalApplicationSetToRemoved = localRemovalFlagSet
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    private void MustAuditRemovalFromConsultationPublicRegisterOnFailure(PublicRegisterPeriodEndModel model)
    {
        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ConsultationPublicRegisterAutomatedExpirationRemovalFailure
                         && e.SourceEntityId == model.PublicRegister!.FellingLicenceApplicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             CaseReference = model.ApplicationReference,
                             ConsultationPublicRegisterPeriodEndDate = model.PublicRegister!.ConsultationPublicRegisterExpiryTimestamp,
                             LocalApplicationSetToRemoved = false
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    private void MustAuditNotificationsSent(PublicRegisterPeriodEndModel model, bool? applicationRemovalSuccess = true)
    {
        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ConsultationPublicRegisterApplicationRemovalNotification
                         && e.SourceEntityId == model.PublicRegister!.FellingLicenceApplicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             PublicRegisterPeriodEndDate = model.PublicRegister!.ConsultationPublicRegisterExpiryTimestamp,
                             NumberOfFcStaffNotificationRecipients = model.AssignedUserIds.Count,
                             ApplicationRemovalSuccess = applicationRemovalSuccess
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    private void MustSendNotificationOnFailure(PublicRegisterPeriodEndModel model, UserAccountModel internalUser)
    {
        _notificationsMock.Verify(v => v.SendNotificationAsync(
            It.Is<InformFCStaffOfDecisionPublicRegisterAutomaticRemovalOnExpiryDataModel>(
                x => x.ApplicationReference == model.ApplicationReference
                     && x.Name == internalUser.FullName
                     && x.AdminHubFooter == AdminHubAddress
                     && x.DecisionPublicRegisterExpiryDate == DateTimeDisplay.GetDateDisplayString(model.PublicRegister!.ConsultationPublicRegisterExpiryTimestamp)
                     && x.RegisterName == "Consultation"),
            NotificationType.InformFcStaffOfApplicationRemovedFromDecisionPublicRegisterFailure,
            It.Is<NotificationRecipient>(x => x.Name == internalUser.FullName && x.Address == internalUser.Email),
            null, null, null, CancellationToken.None), Times.Once);
    }
}
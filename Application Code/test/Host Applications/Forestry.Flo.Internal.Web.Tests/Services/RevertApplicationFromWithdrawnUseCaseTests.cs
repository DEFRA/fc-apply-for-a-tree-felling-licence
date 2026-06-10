using System.Text.Json;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class RevertApplicationFromWithdrawnUseCaseTests
{
    private readonly Mock<IUpdateFellingLicenceApplication> _updateFellingLicenceService = new();
    private readonly Mock<IAuditService<RevertApplicationFromWithdrawnUseCase>> _auditMock = new();
    private readonly Mock<IGetPropertyProfiles> _getPropertyProfilesMock = new();
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserAccountsServiceMock = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreasMock = new();
    private readonly Mock<ISendNotifications> _sendNotificationsMock = new();

    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly string _adminHub = "admin hub address";

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ExternalApplicantSiteOptions _externalApplicantSiteOptions = new()
    {
        BaseUrl = "https://external.applicant.site"
    };

    private RevertApplicationFromWithdrawnUseCase CreateSut()
    {
        _auditMock.Reset();
        _updateFellingLicenceService.Reset();
        _getConfiguredFcAreasMock.Reset();
        _getPropertyProfilesMock.Reset();
        _retrieveUserAccountsServiceMock.Reset();
        _sendNotificationsMock.Reset();

        _getConfiguredFcAreasMock.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_adminHub);


        return new RevertApplicationFromWithdrawnUseCase(
            _auditMock.Object,
            new RequestContext(
                _requestContextCorrelationId,
                new RequestUserModel(UserFactory.CreateUnauthenticatedUser())),
            _updateFellingLicenceService.Object,
            _getPropertyProfilesMock.Object,
                        _retrieveUserAccountsServiceMock.Object,
            _getConfiguredFcAreasMock.Object,
            _sendNotificationsMock.Object,
            new OptionsWrapper<ExternalApplicantSiteOptions>(_externalApplicantSiteOptions),
            new NullLogger<RevertApplicationFromWithdrawnUseCase>());
    }

    [Theory, AutoMoqData]
    public async Task SuccessfulRevert_CannotLoadAuthorForNotification(
        ReopenApplicationResultModel response,
        string error)
    {
        // Arrange
        var sut = CreateSut();
        
        var performingUser =
            new InternalUser(
                UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: Guid.NewGuid(),
                    accountTypeInternal: AccountTypeInternal.AccountAdministrator));
        
        var applicationId = Guid.NewGuid();

        _updateFellingLicenceService
            .Setup(x => x.TryRevertApplicationFromWithdrawnAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        _retrieveUserAccountsServiceMock
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccountModel>(error));

        // Act
        var result = await sut.RevertApplicationFromWithdrawnAsync(performingUser, applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _updateFellingLicenceService.Verify(x => x.TryRevertApplicationFromWithdrawnAsync(
            performingUser.UserAccountId!.Value,
            applicationId,
            It.IsAny<CancellationToken>()), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnSuccess
                         && e.SourceEntityId == applicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new { }, _options)),
                CancellationToken.None), Times.Once);

        _retrieveUserAccountsServiceMock
            .Verify(x => x.RetrieveUserAccountByIdAsync(response.AuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsServiceMock.VerifyNoOtherCalls();

        _getPropertyProfilesMock.VerifyNoOtherCalls();

        _getConfiguredFcAreasMock.VerifyNoOtherCalls();

        _sendNotificationsMock.VerifyNoOtherCalls();

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnNotificationFailure
                         && e.SourceEntityId == response.ApplicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             RecipientName = (string?)null,
                             RecipientEmail = (string?)null,
                             Error = error
                         }, _options)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SuccessfulRevert_CannotRetrievePropertyForNotification(
        ReopenApplicationResultModel response,
        UserAccountModel author,
        string error)
    {
        // Arrange

        response.PropertyName = null;  // simulate "with applicant" state requiring a lookup for property name

        var sut = CreateSut();

        var performingUser =
            new InternalUser(
                UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: Guid.NewGuid(),
                    accountTypeInternal: AccountTypeInternal.AccountAdministrator));

        var applicationId = Guid.NewGuid();

        _updateFellingLicenceService
            .Setup(x => x.TryRevertApplicationFromWithdrawnAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        _retrieveUserAccountsServiceMock
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(author));

        _getPropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PropertyProfile>(error));

        // Act
        var result = await sut.RevertApplicationFromWithdrawnAsync(performingUser, applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _updateFellingLicenceService.Verify(x => x.TryRevertApplicationFromWithdrawnAsync(
            performingUser.UserAccountId!.Value,
            applicationId,
            It.IsAny<CancellationToken>()), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnSuccess
                         && e.SourceEntityId == applicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new { }, _options)),
                CancellationToken.None), Times.Once);

        _retrieveUserAccountsServiceMock
            .Verify(x => x.RetrieveUserAccountByIdAsync(response.AuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsServiceMock.VerifyNoOtherCalls();

        _getPropertyProfilesMock
            .Verify(x => x.GetPropertyByIdAsync(response.LinkedPropertyProfileId.Value, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()), Times.Once);
        _getPropertyProfilesMock.VerifyNoOtherCalls();

        _getConfiguredFcAreasMock.VerifyNoOtherCalls();

        _sendNotificationsMock.VerifyNoOtherCalls();

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnNotificationFailure
                         && e.SourceEntityId == response.ApplicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             RecipientName = author.FullName,
                             RecipientEmail = author.Email,
                             Error = error
                         }, _options)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SuccessfulRevert_FailsToSendNotification(
        ReopenApplicationResultModel response,
        UserAccountModel author,
        PropertyProfile property,
        string error)
    {
        // Arrange

        response.PropertyName = null;  // simulate "with applicant" state requiring a lookup for property name

        var sut = CreateSut();

        var performingUser =
            new InternalUser(
                UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: Guid.NewGuid(),
                    accountTypeInternal: AccountTypeInternal.AccountAdministrator));

        var applicationId = Guid.NewGuid();

        _updateFellingLicenceService
            .Setup(x => x.TryRevertApplicationFromWithdrawnAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        _retrieveUserAccountsServiceMock
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(author));

        _getPropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(property));

        _sendNotificationsMock
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformApplicantOfApplicationReopenedDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(error));

        // Act
        var result = await sut.RevertApplicationFromWithdrawnAsync(performingUser, applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _updateFellingLicenceService.Verify(x => x.TryRevertApplicationFromWithdrawnAsync(
            performingUser.UserAccountId!.Value,
            applicationId,
            It.IsAny<CancellationToken>()), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnSuccess
                         && e.SourceEntityId == applicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new { }, _options)),
                CancellationToken.None), Times.Once);

        _retrieveUserAccountsServiceMock
            .Verify(x => x.RetrieveUserAccountByIdAsync(response.AuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsServiceMock.VerifyNoOtherCalls();

        _getPropertyProfilesMock
            .Verify(x => x.GetPropertyByIdAsync(response.LinkedPropertyProfileId.Value, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()), Times.Once);
        _getPropertyProfilesMock.VerifyNoOtherCalls();

        _getConfiguredFcAreasMock.Verify(x => x.TryGetAdminHubAddress(response.AdminHubName, It.IsAny<CancellationToken>()), Times.Once);
        _getConfiguredFcAreasMock.VerifyNoOtherCalls();

        _sendNotificationsMock.Verify(x => x.SendNotificationAsync(It.Is<InformApplicantOfApplicationReopenedDataModel>(m =>
            m.ApplicationId == response.ApplicationId
            && m.ApplicationReference == response.ApplicationReference
            && m.Name == author.FullName
            && m.PropertyName == property.Name
            && m.SubmittedDate == DateTimeDisplay.GetDateDisplayString(response.SubmittedDate)
            && m.AdminHubFooter == _adminHub
            && m.ViewApplicationURL == $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={response.ApplicationId}"),
            NotificationType.InformApplicantOfApplicationReopened,
            It.Is<NotificationRecipient>(r => r.Name == author.FullName && r.Address == author.Email),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        _sendNotificationsMock.VerifyNoOtherCalls();

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnNotificationFailure
                         && e.SourceEntityId == response.ApplicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             RecipientName = author.FullName,
                             RecipientEmail = author.Email,
                             Error = error
                         }, _options)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SuccessfulRevert_SuccessfulNotification_WithApplicant(
        ReopenApplicationResultModel response,
        UserAccountModel author,
        PropertyProfile property)
    {
        // Arrange

        response.PropertyName = null;  // simulate "with applicant" state requiring a lookup for property name

        var sut = CreateSut();

        var performingUser =
            new InternalUser(
                UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: Guid.NewGuid(),
                    accountTypeInternal: AccountTypeInternal.AccountAdministrator));

        var applicationId = Guid.NewGuid();

        _updateFellingLicenceService
            .Setup(x => x.TryRevertApplicationFromWithdrawnAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        _retrieveUserAccountsServiceMock
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(author));

        _getPropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(property));

        _sendNotificationsMock
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformApplicantOfApplicationReopenedDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        // Act
        var result = await sut.RevertApplicationFromWithdrawnAsync(performingUser, applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _updateFellingLicenceService.Verify(x => x.TryRevertApplicationFromWithdrawnAsync(
            performingUser.UserAccountId!.Value,
            applicationId,
            It.IsAny<CancellationToken>()), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnSuccess
                         && e.SourceEntityId == applicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new { }, _options)),
                CancellationToken.None), Times.Once);

        _retrieveUserAccountsServiceMock
            .Verify(x => x.RetrieveUserAccountByIdAsync(response.AuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsServiceMock.VerifyNoOtherCalls();

        _getPropertyProfilesMock
            .Verify(x => x.GetPropertyByIdAsync(response.LinkedPropertyProfileId.Value, It.Is<UserAccessModel>(u => u.IsSystemUser), It.IsAny<CancellationToken>()), Times.Once);
        _getPropertyProfilesMock.VerifyNoOtherCalls();

        _getConfiguredFcAreasMock.Verify(x => x.TryGetAdminHubAddress(response.AdminHubName, It.IsAny<CancellationToken>()), Times.Once);
        _getConfiguredFcAreasMock.VerifyNoOtherCalls();

        _sendNotificationsMock.Verify(x => x.SendNotificationAsync(It.Is<InformApplicantOfApplicationReopenedDataModel>(m =>
            m.ApplicationId == response.ApplicationId
            && m.ApplicationReference == response.ApplicationReference
            && m.Name == author.FullName
            && m.PropertyName == property.Name
            && m.SubmittedDate == DateTimeDisplay.GetDateDisplayString(response.SubmittedDate)
            && m.AdminHubFooter == _adminHub
            && m.ViewApplicationURL == $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={response.ApplicationId}"),
            NotificationType.InformApplicantOfApplicationReopened,
            It.Is<NotificationRecipient>(r => r.Name == author.FullName && r.Address == author.Email),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        _sendNotificationsMock.VerifyNoOtherCalls();

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnNotificationSent
                         && e.SourceEntityId == response.ApplicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             RecipientName = author.FullName,
                             RecipientEmail = author.Email
                         }, _options)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task SuccessfulRevert_SuccessfulNotification_WithFC(
        ReopenApplicationResultModel response,
        UserAccountModel author)
    {
        // Arrange

        response.LinkedPropertyProfileId = null;  // simulate "with FC" state not requiring a lookup for property name

        var sut = CreateSut();

        var performingUser =
            new InternalUser(
                UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: Guid.NewGuid(),
                    accountTypeInternal: AccountTypeInternal.AccountAdministrator));

        var applicationId = Guid.NewGuid();

        _updateFellingLicenceService
            .Setup(x => x.TryRevertApplicationFromWithdrawnAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        _retrieveUserAccountsServiceMock
            .Setup(x => x.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(author));

        _sendNotificationsMock
            .Setup(x => x.SendNotificationAsync(It.IsAny<InformApplicantOfApplicationReopenedDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        // Act
        var result = await sut.RevertApplicationFromWithdrawnAsync(performingUser, applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _updateFellingLicenceService.Verify(x => x.TryRevertApplicationFromWithdrawnAsync(
            performingUser.UserAccountId!.Value,
            applicationId,
            It.IsAny<CancellationToken>()), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnSuccess
                         && e.SourceEntityId == applicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new { }, _options)),
                CancellationToken.None), Times.Once);

        _retrieveUserAccountsServiceMock
            .Verify(x => x.RetrieveUserAccountByIdAsync(response.AuthorId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsServiceMock.VerifyNoOtherCalls();

        _getPropertyProfilesMock.VerifyNoOtherCalls();

        _getConfiguredFcAreasMock.Verify(x => x.TryGetAdminHubAddress(response.AdminHubName, It.IsAny<CancellationToken>()), Times.Once);
        _getConfiguredFcAreasMock.VerifyNoOtherCalls();

        _sendNotificationsMock.Verify(x => x.SendNotificationAsync(It.Is<InformApplicantOfApplicationReopenedDataModel>(m =>
            m.ApplicationId == response.ApplicationId
            && m.ApplicationReference == response.ApplicationReference
            && m.Name == author.FullName
            && m.PropertyName == response.PropertyName
            && m.SubmittedDate == DateTimeDisplay.GetDateDisplayString(response.SubmittedDate)
            && m.AdminHubFooter == _adminHub
            && m.ViewApplicationURL == $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={response.ApplicationId}"),
            NotificationType.InformApplicantOfApplicationReopened,
            It.Is<NotificationRecipient>(r => r.Name == author.FullName && r.Address == author.Email),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        _sendNotificationsMock.VerifyNoOtherCalls();

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnNotificationSent
                         && e.SourceEntityId == response.ApplicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             RecipientName = author.FullName,
                             RecipientEmail = author.Email
                         }, _options)),
                CancellationToken.None), Times.Once);
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, CombinatorialData]
    public async Task ShouldNotRevertApplicationFromWithdrawn_WhenUserIsNotAdmin(AccountTypeInternal role)
    {
        if (role is AccountTypeInternal.AccountAdministrator)
        {
            return;
        }

        // Arrange
        var sut = CreateSut();
        var performingUser =
            new InternalUser(
                UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: Guid.NewGuid(),
                    accountTypeInternal: role));
        var applicationId = Guid.NewGuid();
        const string error = "You do not have permission to revert applications from withdrawn";

        // Act
        var result = await sut.RevertApplicationFromWithdrawnAsync(performingUser, applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        _updateFellingLicenceService.Verify(x => x.TryRevertApplicationFromWithdrawnAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnFailure
                         && e.SourceEntityId == applicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             Error = error,
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ShouldLogErrorAndAuditFailure_WhenRevertFails()
    {
        // Arrange
        var sut = CreateSut();
        var performingUser =
            new InternalUser(
                UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: Guid.NewGuid(),
                    accountTypeInternal: AccountTypeInternal.AccountAdministrator));
        var applicationId = Guid.NewGuid();
        const string errorMessage = "Revert failed";

        _updateFellingLicenceService
            .Setup(x => x.TryRevertApplicationFromWithdrawnAsync(
                performingUser.UserAccountId!.Value,
                applicationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ReopenApplicationResultModel>(errorMessage));

        // Act
        var result = await sut.RevertApplicationFromWithdrawnAsync(performingUser, applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
        _updateFellingLicenceService.Verify(x => x.TryRevertApplicationFromWithdrawnAsync(
            performingUser.UserAccountId!.Value,
            applicationId,
            It.IsAny<CancellationToken>()), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RevertApplicationFromWithdrawnFailure
                         && e.SourceEntityId == applicationId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             Error = errorMessage,
                         }, _options)),
                CancellationToken.None), Times.Once);
    }
}

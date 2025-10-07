using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using AutoFixture;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.AspNetCore.Http;
using MassTransit;
using UserAccountModel = Forestry.Flo.Services.InternalUsers.Models.UserAccountModel;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class RegisterUserAccountUseCaseTests
{
    private readonly Mock<IAuditService<RegisterUserAccountUseCase>> _auditService;
    private readonly Mock<ISignInInternalUser> _signInInternalUser;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<IUserAccountService> _userAccountService;
    private readonly Mock<ISendNotifications> _sendNotifications;
    private readonly Mock<IPublishEndpoint> _publishEndpoint;

    private static readonly Fixture FixtureInstance = new();

    private readonly InternalUser _internalUser;
    private readonly InternalUser _performingUser;

    private readonly string BaseUrlToConfirmUser = "https://localhost:7254/AdminAccount/ReviewUnconfirmedUserAccount";

    public RegisterUserAccountUseCaseTests()
    {
        _auditService = new Mock<IAuditService<RegisterUserAccountUseCase>>();
        _signInInternalUser = new Mock<ISignInInternalUser>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _userAccountService = new Mock<IUserAccountService>();
        _sendNotifications = new Mock<ISendNotifications>();
        _publishEndpoint = new Mock<IPublishEndpoint>();

        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<Guid>(),
            FixtureInstance.Create<string>(),
            AccountTypeInternal.AccountAdministrator);
        _internalUser = new InternalUser(user);

        var performingUser = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<Guid>(),
            FixtureInstance.Create<string>(),
            AccountTypeInternal.AdminOfficer);
        _performingUser = new InternalUser(performingUser);
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task ShouldReturnPopulatedModel_WhenUserAccountRetrieved(UserAccount userAccount)
    {
        var sut = CreateSut();

        _userAccountService.Setup(s =>
                s.GetUserAccountByIdentityProviderIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        var result = await sut.GetUserAccountModelAsync(_internalUser, CancellationToken.None);

        // assert user account is retrieved

        Assert.True(result.HasValue);

        // assert mapped values correct

        Assert.Equal(userAccount.FirstName, result.Value.FirstName);
        Assert.Equal(userAccount.LastName, result.Value.LastName);
        Assert.Equal(userAccount.Title, result.Value.Title);
        Assert.Equal(userAccount.AccountType, result.Value.RequestedAccountType);
        Assert.Single(result.Value.DisallowedRoles);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnEmpty_WhenUserAccountNotRetrieved(UserAccount userAccount)
    {
        var sut = CreateSut();

        _userAccountService.Setup(s =>
                s.GetUserAccountByIdentityProviderIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.None);

        var result = await sut.GetUserAccountModelAsync(_internalUser, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnPopulatedModel_WhenUserAccountRetrievedById(UserAccount userAccount)
    {
        var actingUser =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(accountTypeInternal: AccountTypeInternal.AccountAdministrator);

        var sut = CreateSut();

        _userAccountService.Setup(s =>
                s.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        var result = await sut.GetUserAccountModelByIdAsync(userAccount.Id, new InternalUser(actingUser), CancellationToken.None);

        // assert user account is retrieved

        Assert.True(result.IsSuccess);

        // assert mapped values correct

        Assert.Equal(userAccount.FirstName, result.Value.FirstName);
        Assert.Equal(userAccount.LastName, result.Value.LastName);
        Assert.Equal(userAccount.Title, result.Value.Title);
        Assert.Equal(userAccount.AccountType, result.Value.RequestedAccountType);
        Assert.Equal(userAccount.AccountTypeOther, result.Value.RequestedAccountTypeOther);
        Assert.True(result.Value.AllowRoleChange);
        Assert.Contains(AccountTypeInternal.FcStaffMember, result.Value.DisallowedRoles);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnModelDisallowingAdminRole_WhenUserAccountRetrievedByIdIsNotAlreadyAdmin(UserAccount userAccount)
    {
        userAccount.AccountType = AccountTypeInternal.FieldManager;

        var actingUser =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(accountTypeInternal: AccountTypeInternal.AccountAdministrator);

        var sut = CreateSut();

        _userAccountService.Setup(s =>
                s.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        var result = await sut.GetUserAccountModelByIdAsync(userAccount.Id, new InternalUser(actingUser), CancellationToken.None);

        // assert user account is retrieved

        Assert.True(result.IsSuccess);

        // assert disallowed roles includes account admin

        Assert.Contains(AccountTypeInternal.AccountAdministrator, result.Value.DisallowedRoles);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnModelNotDisallowingAdminRole_WhenUserAccountRetrievedByIdIsAlreadyAdmin(UserAccount userAccount)
    {
        userAccount.AccountType = AccountTypeInternal.AccountAdministrator;

        var actingUser =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(accountTypeInternal: AccountTypeInternal.AccountAdministrator);

        var sut = CreateSut();

        _userAccountService.Setup(s =>
                s.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        var result = await sut.GetUserAccountModelByIdAsync(userAccount.Id, new InternalUser(actingUser), CancellationToken.None);

        // assert user account is retrieved

        Assert.True(result.IsSuccess);

        // assert disallowed roles includes account admin

        Assert.DoesNotContain(AccountTypeInternal.AccountAdministrator, result.Value.DisallowedRoles);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnModelDisallowingRoleChange_WhenUserAccountRetrievedByIdIsActingUser(UserAccount userAccount)
    {
        userAccount.AccountType = AccountTypeInternal.FieldManager;

        var actingUser =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userAccount.Id, accountTypeInternal: AccountTypeInternal.AccountAdministrator);

        var sut = CreateSut();

        _userAccountService.Setup(s =>
                s.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        var result = await sut.GetUserAccountModelByIdAsync(userAccount.Id, new InternalUser(actingUser), CancellationToken.None);

        // assert user account is retrieved

        Assert.True(result.IsSuccess);

        // assert disallowed roles includes account admin

        Assert.False(result.Value.AllowRoleChange);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnEmpty_WhenUserAccountNotRetrievedById(UserAccount userAccount)
    {
        var actingUser =
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(accountTypeInternal: AccountTypeInternal.AccountAdministrator);

        var sut = CreateSut();

        _userAccountService.Setup(s =>
                s.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.None);

        var result = await sut.GetUserAccountModelByIdAsync(userAccount.Id, new InternalUser(actingUser), CancellationToken.None);

        // assert user account is retrieved

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(AccountTypeInternal.AdminOfficer)]
    [InlineData(AccountTypeInternal.FieldManager)]
    [InlineData(AccountTypeInternal.AccountAdministrator)]
    [InlineData(AccountTypeInternal.AdminHubManager)]
    [InlineData(AccountTypeInternal.Other)]
    [InlineData(AccountTypeInternal.FcStaffMember)]
    [InlineData(AccountTypeInternal.WoodlandOfficer)]
    public async Task ReturnsSuccessAndAudits_WhenAccountUpdatedById(AccountTypeInternal accountType)
    {
        var sut = CreateSut();

        var userAccount = FixtureInstance.Create<UserAccount>();
        
        var updateUserModel = FixtureInstance.Create<UpdateUserRegistrationDetailsModel>();
        updateUserModel.RequestedAccountType = accountType;

        _userAccountService.Setup(s =>
                s.UpdateUserAccountDetailsAsync(It.IsAny<UpdateRegistrationDetailsModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        var result = await 
            sut.UpdateAccountRegistrationDetailsByIdAsync(_internalUser, updateUserModel, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _userAccountService.Verify(v => v.UpdateUserAccountDetailsAsync(
            It.Is<UpdateRegistrationDetailsModel>(x => 
                x.AccountType == accountType
                && x.FirstName == updateUserModel.FirstName
                && x.LastName == updateUserModel.LastName
                && x.Title == updateUserModel.Title
                && x.UserAccountId == updateUserModel.Id
                && x.Roles.Contains(Roles.FcAdministrator) == (accountType == AccountTypeInternal.AccountAdministrator)),
            CancellationToken.None),
            Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.ActorType == ActorType.InternalUser
                && x.SourceEntityId == updateUserModel.Id
                && x.UserId == _internalUser.UserAccountId
                && x.EventName == AuditEvents.AccountAdministratorUpdateExistingAccount
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    UpdatedAccountType = updateUserModel.RequestedAccountType,
                    UpdatedFullName = $"{updateUserModel.Title} {updateUserModel.FirstName} {updateUserModel.LastName}".Trim().Replace("  ", " ")

                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailureAndAudits_WhenAccountNotUpdatedById(UpdateUserRegistrationDetailsModel updateUserModel)
    {
        var sut = CreateSut();

        var error = FixtureInstance.Create<string>();
        
        _userAccountService.Setup(s =>
                s.UpdateUserAccountDetailsAsync(It.IsAny<UpdateRegistrationDetailsModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>(error));

        var result = await
            sut.UpdateAccountRegistrationDetailsByIdAsync(_internalUser, updateUserModel, CancellationToken.None);

        Assert.True(result.IsFailure);

        _userAccountService.Verify(v => v.UpdateUserAccountDetailsAsync(
            It.Is<UpdateRegistrationDetailsModel>(x =>
                x.AccountType == updateUserModel.RequestedAccountType
                && x.FirstName == updateUserModel.FirstName
                && x.LastName == updateUserModel.LastName
                && x.Title == updateUserModel.Title
                && x.UserAccountId == updateUserModel.Id),
            CancellationToken.None),
            Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.ActorType == ActorType.InternalUser
                && x.SourceEntityId == updateUserModel.Id
                && x.UserId == _internalUser.UserAccountId
                && x.EventName == AuditEvents.AccountAdministratorUpdateExistingAccountFailure
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Error = error,
                    RequestedAccountType = updateUserModel.RequestedAccountType,
                    RequestedFullName = $"{updateUserModel.Title} {updateUserModel.FirstName} {updateUserModel.LastName}".Trim().Replace("  ", " ")

                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Theory]
    [InlineData(AccountTypeInternal.AdminOfficer, false)]
    [InlineData(AccountTypeInternal.FieldManager, true)]
    [InlineData(AccountTypeInternal.AccountAdministrator, false)]
    [InlineData(AccountTypeInternal.AdminHubManager, false)]
    [InlineData(AccountTypeInternal.Other, false)]
    [InlineData(AccountTypeInternal.FcStaffMember, false)]
    [InlineData(AccountTypeInternal.WoodlandOfficer, false)]
    public async Task ReturnsSuccessAndAudits_WhenAccountUpdatedByInternalUser(AccountTypeInternal accountType, bool canApproveApplications)
    {
        var sut = CreateSut();

        var adminForNotification = FixtureInstance.Create<UserAccountModel>();

        var userAccount = FixtureInstance.Create<UserAccount>();

        var updateUserModel = FixtureInstance.Create<UpdateUserRegistrationDetailsModel>();
        updateUserModel.RequestedAccountType = accountType;

        _userAccountService.Setup(s =>
                s.UpdateUserAccountDetailsAsync(It.IsAny<UpdateRegistrationDetailsModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccount);

        _userAccountService
            .Setup(x => x.GetConfirmedUsersByAccountTypeAsync(It.IsAny<AccountTypeInternal>(),
                It.IsAny<AccountTypeInternalOther?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserAccountModel> { adminForNotification });

        _signInInternalUser.Setup(s => s.CreateClaimsIdentityFromUserAccount(It.IsAny<UserAccount>()))
            .Returns(_internalUser.Principal.Identities.First);

        var result = await
            sut.UpdateAccountRegistrationDetailsAsync(_internalUser, updateUserModel, BaseUrlToConfirmUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _userAccountService.Verify(v => v.UpdateUserAccountDetailsAsync(
            It.Is<UpdateRegistrationDetailsModel>(x =>
                x.AccountType == accountType
                && x.FirstName == updateUserModel.FirstName
                && x.LastName == updateUserModel.LastName
                && x.Title == updateUserModel.Title
                && x.UserAccountId == _internalUser.UserAccountId
                && x.Roles.Contains(Roles.FcAdministrator) == (accountType == AccountTypeInternal.AccountAdministrator)
                && x.CanApproveApplications == canApproveApplications),
            CancellationToken.None),
            Times.Once);

        _userAccountService.Verify(x => x.GetConfirmedUsersByAccountTypeAsync(AccountTypeInternal.AccountAdministrator, null, It.IsAny<CancellationToken>()), Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.ActorType == ActorType.InternalUser
                && x.SourceEntityId == userAccount.Id
                && x.UserId == _internalUser.UserAccountId
                && x.EventName == AuditEvents.UpdateAccountEvent
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    AccountType = _internalUser.AccountType,
                    IdentityProviderId = _internalUser.IdentityProviderId

                }, _options)),
            CancellationToken.None), Times.Once);

        _sendNotifications
            .Verify(x => x.SendNotificationAsync(
                It.Is<InformAdminOfNewAccountSignupDataModel>(m => m.AccountEmail == _internalUser.EmailAddress && m.AccountName == $"{updateUserModel.FirstName} {updateUserModel.LastName}" && m.ConfirmAccountUrl == BaseUrlToConfirmUser + $"?userAccountId={_internalUser.UserAccountId}"), 
                NotificationType.InformAdminOfNewAccountSignup,
                It.Is<NotificationRecipient>(r => r.Name == adminForNotification.FullName && r.Address == adminForNotification.Email),
                null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailureAndAudits_WhenAccountNotUpdatedByInternalUser(UpdateUserRegistrationDetailsModel updateUserModel)
    {
        var sut = CreateSut();

        var error = FixtureInstance.Create<string>();

        _userAccountService.Setup(s =>
                s.UpdateUserAccountDetailsAsync(It.IsAny<UpdateRegistrationDetailsModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>(error));

        var result = await
            sut.UpdateAccountRegistrationDetailsAsync(_internalUser, updateUserModel, BaseUrlToConfirmUser, CancellationToken.None);

        Assert.True(result.IsFailure);

        _userAccountService.Verify(v => v.UpdateUserAccountDetailsAsync(
            It.Is<UpdateRegistrationDetailsModel>(x =>
                x.AccountType == updateUserModel.RequestedAccountType
                && x.FirstName == updateUserModel.FirstName
                && x.LastName == updateUserModel.LastName
                && x.Title == updateUserModel.Title
                && x.UserAccountId == _internalUser.UserAccountId),
            CancellationToken.None),
            Times.Once);

        _auditService.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(x =>
                x.ActorType == ActorType.InternalUser
                && x.SourceEntityId == _internalUser.UserAccountId
                && x.UserId == _internalUser.UserAccountId
                && x.EventName == AuditEvents.UpdateAccountFailureEvent
                && JsonSerializer.Serialize(x.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    AccountType = updateUserModel.RequestedAccountType,
                    IdentityProviderId = _internalUser.IdentityProviderId,
                    Error = error

                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Theory]
    [InlineData(Status.Denied, null)]
    [InlineData(Status.Confirmed, true)]
    [InlineData(Status.Confirmed, false)]
    public async Task ShouldUpdateAccountStatus(Status status, bool? canApprove)
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();
        user.AccountType = AccountTypeInternal.WoodlandOfficer;
        var loginUrl = FixtureInstance.Create<string>();

        _userAccountService.Setup(s => s.GetUserAccountAsync(
                It.IsAny<Guid>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await sut.UpdateUserAccountStatusAsync(_performingUser, user.Id, status, canApprove, loginUrl, CancellationToken.None);

        Assert.True(result.IsSuccess);

        if (status is Status.Confirmed)
        {
            _userAccountService.Verify(v => v.UpdateUserAccountConfirmedAsync(user.Id, canApprove, CancellationToken.None), Times.Once);

            _userAccountService.Verify(v => v.GetUserAccountAsync(user.Id, CancellationToken.None), Times.Once);

            _sendNotifications.Verify(v => v.SendNotificationAsync(
                It.Is<InformInternalUserOfAccountApprovalDataModel>(x => 
                    x.Name == user.FullName(true) 
                    && x.LoginUrl == loginUrl),
                NotificationType.InformInternalUserOfAccountApproval,
                It.Is<NotificationRecipient>(x => 
                    x.Name == user.FullName(true)
                    && x.Address == user.Email),
                null,
                null,
                null,
                CancellationToken.None), Times.Once);

            _publishEndpoint.Verify(v => v.Publish(It.Is<InternalFcUserAccountApprovedEvent>(x =>
                        x.EmailAddress == user.Email &&
                        x.IdentityProviderId == user.IdentityProviderId &&
                        x.FirstName == user.FirstName &&
                        x.LastName == user.LastName
                        && x.ApprovedByInternalFcUserId == _performingUser.UserAccountId)
                    , It.IsAny<CancellationToken>()),
                Times.Once);
        }
        else
        {
            _userAccountService.Verify(v => v.UpdateUserAccountDeniedAsync(user.Id, CancellationToken.None), Times.Once);

            _publishEndpoint.Verify(x=>x.Publish(It.IsAny<InternalFcUserAccountApprovedEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        _sendNotifications.VerifyNoOtherCalls();
        _userAccountService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnSuccess_WhenNotificationRecipientNotRetrieved()
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();
        var loginUrl = FixtureInstance.Create<string>();

        _userAccountService.Setup(s => s.GetUserAccountAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.None);

        var result = await sut.UpdateUserAccountStatusAsync(_performingUser, user.Id, Status.Confirmed, false, loginUrl, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _userAccountService.Verify(v => v.UpdateUserAccountConfirmedAsync(user.Id, false, CancellationToken.None), Times.Once);

        _userAccountService.Verify(v => v.GetUserAccountAsync(user.Id, CancellationToken.None), Times.Once);

        _sendNotifications.VerifyNoOtherCalls();
        _userAccountService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnSuccess_WhenNotificationCannotBeSent()
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();
        var loginUrl = FixtureInstance.Create<string>();

        _userAccountService.Setup(s => s.GetUserAccountAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _sendNotifications.Setup(s => s.SendNotificationAsync(
                It.IsAny<InformInternalUserOfAccountApprovalDataModel>(),
                It.IsAny<NotificationType>(),
                It.IsAny<NotificationRecipient>(),
                It.IsAny<NotificationRecipient[]>(),
                It.IsAny<NotificationAttachment[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("test"));

        var result = await sut.UpdateUserAccountStatusAsync(_performingUser, user.Id, Status.Confirmed, false, loginUrl, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _userAccountService.Verify(v => v.UpdateUserAccountConfirmedAsync(user.Id, false, CancellationToken.None), Times.Once);

        _userAccountService.Verify(v => v.GetUserAccountAsync(user.Id, CancellationToken.None), Times.Once);

        _sendNotifications.Verify(v => v.SendNotificationAsync(
            It.Is<InformInternalUserOfAccountApprovalDataModel>(x =>
                x.Name == user.FullName(true)
                && x.LoginUrl == loginUrl),
            NotificationType.InformInternalUserOfAccountApproval,
            It.Is<NotificationRecipient>(x =>
                x.Name == user.FullName(true)
                && x.Address == user.Email),
            null,
            null,
            null,
            CancellationToken.None), Times.Once);

        _publishEndpoint.Verify(v => v.Publish(It.Is<InternalFcUserAccountApprovedEvent>(x =>
                    x.EmailAddress == user.Email &&
                    x.IdentityProviderId == user.IdentityProviderId &&
                    x.FirstName == user.FirstName &&
                    x.LastName == user.LastName
                    && x.ApprovedByInternalFcUserId == _performingUser.UserAccountId)
                , It.IsAny<CancellationToken>()),
            Times.Once);

        _sendNotifications.VerifyNoOtherCalls();
        _userAccountService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnFailure_WhenExceptionThrown()
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();
        var loginUrl = FixtureInstance.Create<string>();
        var exceptionMessage = FixtureInstance.Create<string>();

        _userAccountService
            .Setup(v => v.UpdateUserAccountConfirmedAsync(It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception(exceptionMessage));

        var result = await sut.UpdateUserAccountStatusAsync(_performingUser, user.Id, Status.Confirmed, false, loginUrl, CancellationToken.None);

        Assert.True(result.IsFailure);

        _userAccountService.Verify(v => v.UpdateUserAccountConfirmedAsync(user.Id, false, CancellationToken.None), Times.Once);

        _sendNotifications.VerifyNoOtherCalls();
        _userAccountService.VerifyNoOtherCalls();
        _publishEndpoint.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(Status.Closed)]
    [InlineData(Status.Requested)]
    public async Task ShouldReturnFailure_WhenIncorrectStatusRequested(Status status)
    {
        var sut = CreateSut();

        var user = FixtureInstance.Create<UserAccount>();
        var loginUrl = FixtureInstance.Create<string>();

        _userAccountService.Setup(s => s.GetUserAccountAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await sut.UpdateUserAccountStatusAsync(_performingUser, user.Id, status, false, loginUrl, CancellationToken.None);

        Assert.True(result.IsFailure);

        _userAccountService.VerifyNoOtherCalls();
        _sendNotifications.VerifyNoOtherCalls();
        _publishEndpoint.VerifyNoOtherCalls();
    }

    private RegisterUserAccountUseCase CreateSut()
    {
        _auditService.Reset();
        _signInInternalUser.Reset();
        _httpContextAccessor.Reset();
        _userAccountService.Reset();
        _publishEndpoint.Reset();
        _sendNotifications.Reset();

        return new RegisterUserAccountUseCase(
            _httpContextAccessor.Object,
            _signInInternalUser.Object,
            _auditService.Object,
            new RequestContext("test", new RequestUserModel(_internalUser.Principal)),
            new NullLogger<RegisterUserAccountUseCase>(),
            _userAccountService.Object,
            _sendNotifications.Object,
            _publishEndpoint.Object);
    }
}
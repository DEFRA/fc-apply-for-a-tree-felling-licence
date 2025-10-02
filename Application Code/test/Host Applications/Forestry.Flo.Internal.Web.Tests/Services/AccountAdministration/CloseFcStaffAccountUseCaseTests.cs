using AutoFixture;
using AutoFixture.Xunit2;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.AccountAdministration;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using CSharpFunctionalExtensions;
using CancellationToken = System.Threading.CancellationToken;

namespace Forestry.Flo.Internal.Web.Tests.Services.AccountAdministration;

public class CloseFcStaffAccountUseCaseTests
{
    private readonly Mock<IUserAccountService> _internalAccountServiceMock;
    private readonly Mock<IAuditService<CloseFcStaffAccountUseCase>> _auditMock;

    private static readonly Fixture FixtureInstance = new();
    private readonly InternalUser _accountAdmin;

    public CloseFcStaffAccountUseCaseTests()
    {
        _internalAccountServiceMock = new Mock<IUserAccountService>();
        _auditMock = new Mock<IAuditService<CloseFcStaffAccountUseCase>>();
        _accountAdmin = CreateInternalUser(AccountTypeInternal.AccountAdministrator);
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_WhenAccountClosed(UserAccountModel user)
    {
        var sut = CreateSut();

        _internalAccountServiceMock.Setup(s => s.SetUserAccountStatusAsync(It.IsAny<Guid>(), It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await sut.CloseFcStaffAccountAsync(user.UserAccountId, _accountAdmin, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _internalAccountServiceMock.Verify(v => v.SetUserAccountStatusAsync(user.UserAccountId, Status.Closed, CancellationToken.None), Times.Once);
        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.AccountAdministratorCloseAccount
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             ClosedAccountFullName = user.FullName,
                             ClosedAccountType = user.AccountType
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenAccountNotClosed(UserAccountModel user)
    {
        var sut = CreateSut();

        var error = FixtureInstance.Create<string>();

        _internalAccountServiceMock.Setup(s => s.SetUserAccountStatusAsync(It.IsAny<Guid>(), It.IsAny<Status>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccountModel>(error));

        var result = await sut.CloseFcStaffAccountAsync(user.UserAccountId, _accountAdmin, CancellationToken.None);

        Assert.True(result.IsFailure);

        _internalAccountServiceMock.Verify(v => v.SetUserAccountStatusAsync(user.UserAccountId, Status.Closed, CancellationToken.None), Times.Once);
        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.AccountAdministratorCloseAccountFailure
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             Error = error
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailure_WhenClosingOwnAccount(UserAccountModel user)
    {
        var sut = CreateSut();

        user.UserAccountId = _accountAdmin.UserAccountId!.Value;

        var result = await sut.CloseFcStaffAccountAsync(user.UserAccountId, _accountAdmin, CancellationToken.None);

        Assert.True(result.IsFailure);

        _internalAccountServiceMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }


    [Theory, AutoData]
    public async Task ShouldReturnFailure_UserIsNotAccountAdmin(UserAccountModel user)
    {
        var sut = CreateSut();

        var notAdminAccount = CreateInternalUser(AccountTypeInternal.FieldManager);

        var result = await sut.CloseFcStaffAccountAsync(user.UserAccountId, notAdminAccount, CancellationToken.None);

        Assert.True(result.IsFailure);

        _internalAccountServiceMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    private static InternalUser CreateInternalUser(AccountTypeInternal accountType)
    {
        var claimsPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<Guid>(),
            FixtureInstance.Create<string>(),
            accountType);

        return new InternalUser(claimsPrincipal);
    }

    private CloseFcStaffAccountUseCase CreateSut()
    {
        _internalAccountServiceMock.Reset();
        _auditMock.Reset();

        return new CloseFcStaffAccountUseCase(
            new NullLogger<CloseFcStaffAccountUseCase>(),
            _internalAccountServiceMock.Object,
            _auditMock.Object,
            new RequestContext("test", new RequestUserModel(_accountAdmin.Principal)));
    }
}
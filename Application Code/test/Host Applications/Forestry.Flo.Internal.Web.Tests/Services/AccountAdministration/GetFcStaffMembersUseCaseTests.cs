using AutoFixture;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.AccountAdministration;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CancellationToken = System.Threading.CancellationToken;

namespace Forestry.Flo.Internal.Web.Tests.Services.AccountAdministration;

public class GetFcStaffMembersUseCaseTests
{
    private readonly Mock<IUserAccountService> _internalAccountServiceMock;

    private static readonly Fixture FixtureInstance = new();

    public GetFcStaffMembersUseCaseTests()
    {
        _internalAccountServiceMock = new Mock<IUserAccountService>();
    }

    [Theory]
    [InlineData(AccountTypeInternal.AdminOfficer)]
    [InlineData(AccountTypeInternal.FieldManager)]
    [InlineData(AccountTypeInternal.AccountAdministrator)]
    [InlineData(AccountTypeInternal.AdminHubManager)]
    [InlineData(AccountTypeInternal.Other)]
    [InlineData(AccountTypeInternal.FcStaffMember)]
    [InlineData(AccountTypeInternal.WoodlandOfficer)]
    public async Task ShouldOnlyRetrieveListForAccountAdministrator(AccountTypeInternal accountType)
    {
        var sut = CreateSut();

        var user = CreateInternalUser(accountType);

        _internalAccountServiceMock
            .Setup(s => s.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(FixtureInstance.CreateMany<UserAccount>(3));

        var result = await sut.GetAllFcStaffMembersAsync(user, "url", true, CancellationToken.None);

        Assert.Equal(accountType is AccountTypeInternal.AccountAdministrator, result.IsSuccess);

        _internalAccountServiceMock.Verify(v => 
            v.ListConfirmedUserAccountsAsync(CancellationToken.None, null),
            accountType is AccountTypeInternal.AccountAdministrator 
                ? Times.Once 
                : Times.Never);
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

    private GetFcStaffMembersUseCase CreateSut()
    {
        _internalAccountServiceMock.Reset();

        return new GetFcStaffMembersUseCase(
            new NullLogger<GetFcStaffMembersUseCase>(),
            _internalAccountServiceMock.Object);
    }
}
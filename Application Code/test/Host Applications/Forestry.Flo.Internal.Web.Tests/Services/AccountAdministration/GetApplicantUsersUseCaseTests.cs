using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.AccountAdministration;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.AccountAdministration;

public class GetApplicantUsersUseCaseTests
{
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserServiceMock;

    private static readonly Fixture FixtureInstance = new();
    private readonly InternalUser _accountAdmin;

    public GetApplicantUsersUseCaseTests()
    {
        _retrieveUserServiceMock = new Mock<IRetrieveUserAccountsService>();
        _accountAdmin = CreateInternalUser(AccountTypeInternal.AccountAdministrator);
    }

    [Theory, AutoData]
    public async Task ShouldPopulateModel_WhenListOfUsersRetrieved(List<UserAccount> users)
    {
        var sut = CreateSut();

        var returnUrl = "returnUrl";

        foreach (var user in users)
        {
            user.AgencyId = user.Agency.Id;
        }

        users.First().Agency.IsFcAgency = true;

        _retrieveUserServiceMock.Setup(s => s.RetrieveActiveExternalUsersByAccountTypeAsync(It.IsAny<List<AccountTypeExternal>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await sut.RetrieveListOfActiveExternalUsersAsync(_accountAdmin, returnUrl, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReturnUrl.Should().Be(returnUrl);
        result.Value.SelectedUserAccountId.Should().BeNull();
        result.Value.ExternalUserList.Count().Should().Be(users.Count);

        foreach (var user in users)
        {
            // assert model mapped correctly
            var correspondingEntry  = result.Value.ExternalUserList.FirstOrDefault(x => x.ExternalUser.Email == user.Email);
            correspondingEntry.Should().NotBeNull();

            correspondingEntry!.ExternalUser.FullName.Should().Be(user.FullName());
            correspondingEntry!.ExternalUser.Email.Should().Be(user.Email);
            if (user.Agency?.IsFcAgency is true)
            {
                correspondingEntry!.ExternalUser.AccountType.Should().Be(AccountTypeExternal.FcUser);
            }
            else
            {
                correspondingEntry!.ExternalUser.AccountType.Should().Be(user.AccountType);
            }
            

            if (user.Agency is not null)
            {
                correspondingEntry.AgencyModel.ContactEmail.Should().Be(user.Agency.ContactEmail);
                correspondingEntry.AgencyModel.ContactName.Should().Be(user.Agency.ContactName);
                correspondingEntry.AgencyModel.Address.Should().Be(user.Agency.Address);
                correspondingEntry.AgencyModel.AgencyId.Should().Be(user.AgencyId);
                correspondingEntry.AgencyModel.OrganisationName.Should().Be(user.Agency.OrganisationName);
            }
        }

        _retrieveUserServiceMock.Verify(v => v.RetrieveActiveExternalUsersByAccountTypeAsync(
            It.Is<List<AccountTypeExternal>>(x =>
                x.Contains(AccountTypeExternal.AgentAdministrator)
                && x.Contains(AccountTypeExternal.Agent)
                && x.Contains(AccountTypeExternal.WoodlandOwner)
                && x.Contains(AccountTypeExternal.FcUser)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ShouldReturnEmptyUserList_WhenNoUsersRetrieved()
    {
        var sut = CreateSut();

        var returnUrl = "returnUrl";

        _retrieveUserServiceMock.Setup(s => s.RetrieveActiveExternalUsersByAccountTypeAsync(It.IsAny<List<AccountTypeExternal>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserAccount>());

        var result = await sut.RetrieveListOfActiveExternalUsersAsync(_accountAdmin, returnUrl, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReturnUrl.Should().Be(returnUrl);
        result.Value.SelectedUserAccountId.Should().BeNull();

        result.Value.ExternalUserList.Count().Should().Be(0);

        _retrieveUserServiceMock.Verify(v => v.RetrieveActiveExternalUsersByAccountTypeAsync(It.Is<List<AccountTypeExternal>>(x =>
                x.Contains(AccountTypeExternal.AgentAdministrator)
                && x.Contains(AccountTypeExternal.Agent)
                && x.Contains(AccountTypeExternal.WoodlandOwner)
                && x.Contains(AccountTypeExternal.FcUser)), 
                CancellationToken.None),
            Times.Once);
    }

    [Theory]
    [InlineData(AccountTypeInternal.AdminOfficer)]
    [InlineData(AccountTypeInternal.FieldManager)]
    [InlineData(AccountTypeInternal.AccountAdministrator)]
    [InlineData(AccountTypeInternal.AdminHubManager)]
    [InlineData(AccountTypeInternal.Other)]
    [InlineData(AccountTypeInternal.FcStaffMember)]
    [InlineData(AccountTypeInternal.WoodlandOfficer)]
    public async Task ShouldReturnFailure_WhenUserIsNotAccountAdmin(AccountTypeInternal accountType)
    {
        var sut = CreateSut();

        var returnUrl = "returnUrl";

        _retrieveUserServiceMock.Setup(s => s.RetrieveActiveExternalUsersByAccountTypeAsync(It.IsAny<List<AccountTypeExternal>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserAccount>());

        var result = await sut.RetrieveListOfActiveExternalUsersAsync(CreateInternalUser(accountType), returnUrl, CancellationToken.None);

        result.IsSuccess.Should().Be(accountType is AccountTypeInternal.AccountAdministrator);

        _retrieveUserServiceMock.Verify(v => v.RetrieveActiveExternalUsersByAccountTypeAsync(It.Is<List<AccountTypeExternal>>(x => 
                    x.Contains(AccountTypeExternal.AgentAdministrator)
                    && x.Contains(AccountTypeExternal.Agent)
                    && x.Contains(AccountTypeExternal.WoodlandOwner)
                    && x.Contains(AccountTypeExternal.FcUser)),
                CancellationToken.None),
            accountType is AccountTypeInternal.AccountAdministrator ? Times.Once : Times.Never);
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
    private GetApplicantUsersUseCase CreateSut()
    {
        _retrieveUserServiceMock.Reset();

        return new GetApplicantUsersUseCase(
            new NullLogger<GetApplicantUsersUseCase>(),
            _retrieveUserServiceMock.Object);
    }
}
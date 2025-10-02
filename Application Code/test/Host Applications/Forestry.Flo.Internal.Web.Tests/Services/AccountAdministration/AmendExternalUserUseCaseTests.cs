using AutoFixture;
using AutoFixture.Xunit2;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.AccountAdministration;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.Extensions;
using UserAccountModel = Forestry.Flo.Services.Applicants.Models.UserAccountModel;

namespace Forestry.Flo.Internal.Web.Tests.Services.AccountAdministration;

public class AmendExternalUserUseCaseTests
{
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserServiceMock;
    private readonly Mock<IAmendUserAccounts> _amendUsersMock;
    private readonly Mock<IAuditService<AmendExternalUserUseCase>> _auditMock;

    private static readonly Fixture FixtureInstance = new();
    private readonly InternalUser _accountAdmin;

    public AmendExternalUserUseCaseTests()
    {
        _retrieveUserServiceMock = new Mock<IRetrieveUserAccountsService>();
        _amendUsersMock = new Mock<IAmendUserAccounts>();
        _auditMock = new Mock<IAuditService<AmendExternalUserUseCase>>();

        _accountAdmin = CreateInternalUser(AccountTypeInternal.AccountAdministrator);
    }

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoData]
    public async Task ModelMappedCorrectly_WhenRetrievingUserForAmendment(UserAccount user)
    {
        var sut = CreateSut();

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await sut.RetrieveExternalUserAccountAsync(user.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // assert model values correctly set

        Assert.Equal(user.Id, result.Value.UserId);

        Assert.Equal(user.ContactAddress.Line1, result.Value.PersonContactsDetails.ContactAddress.Line1);
        Assert.Equal(user.ContactAddress.Line2, result.Value.PersonContactsDetails.ContactAddress.Line2);
        Assert.Equal(user.ContactAddress.Line3, result.Value.PersonContactsDetails.ContactAddress.Line3);
        Assert.Equal(user.ContactAddress.Line4, result.Value.PersonContactsDetails.ContactAddress.Line4);
        Assert.Equal(user.ContactAddress.PostalCode, result.Value.PersonContactsDetails.ContactAddress.PostalCode);

        Assert.Equal(user.ContactMobileTelephone, result.Value.PersonContactsDetails.ContactMobileNumber);
        Assert.Equal(user.ContactTelephone, result.Value.PersonContactsDetails.ContactTelephoneNumber);
        Assert.Equal(user.PreferredContactMethod, result.Value.PersonContactsDetails.PreferredContactMethod);

        Assert.Equal(user.FirstName, result.Value.PersonName.FirstName);
        Assert.Equal(user.LastName, result.Value.PersonName.LastName);
        Assert.Equal(user.Title, result.Value.PersonName.Title);

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(user.Id, CancellationToken.None),Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFailure_WhenUserCannotBeRetrievedForAmendment(UserAccount user)
    {
        var sut = CreateSut();

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>("test"));

        var result = await sut.RetrieveExternalUserAccountAsync(user.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoData]
    public async Task ReturnsSuccessAndAudits_WhenUserSuccessfullyAmended(AmendExternalUserAccountModel model)
    {
        var sut = CreateSut();

        _amendUsersMock.Setup(s =>
                s.UpdateUserAccountDetailsAsync(It.IsAny<UpdateUserAccountModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.UpdateExternalAccountDetailsAsync(_accountAdmin, model, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _amendUsersMock.Verify(v => v.UpdateUserAccountDetailsAsync(
            It.Is<UpdateUserAccountModel>(
                x => x.ContactAddress.Line1 == model.PersonContactsDetails.ContactAddress.Line1
                && x.ContactAddress.Line2 == model.PersonContactsDetails.ContactAddress.Line2
                && x.ContactAddress.Line3 == model.PersonContactsDetails.ContactAddress.Line3
                && x.ContactAddress.Line4 == model.PersonContactsDetails.ContactAddress.Line4
                && x.ContactAddress.PostalCode == model.PersonContactsDetails.ContactAddress.PostalCode
                && x.ContactMobileNumber == model.PersonContactsDetails.ContactMobileNumber
                && x.ContactTelephoneNumber == model.PersonContactsDetails.ContactTelephoneNumber
                && x.PreferredContactMethod == model.PersonContactsDetails.PreferredContactMethod
                && x.FirstName == model.PersonName.FirstName
                && x.LastName == model.PersonName.LastName
                && x.Title == model.PersonName.Title
                && x.UserAccountId == model.UserId),
            CancellationToken.None), Times.Once);

        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AdministratorUpdateExternalAccount
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _accountAdmin.UserAccountId
                && a.SourceEntityId == model.UserId
                && a.SourceEntityType == SourceEntityType.UserAccount
                && a.CorrelationId == "test"
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    UpdatedFullName = $"{model.PersonName.Title} {model.PersonName.FirstName} {model.PersonName.LastName}".Trim().Replace("  ", " "),
                    AdministratorAccountType = _accountAdmin.AccountType.GetDisplayName(),
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsSuccessAndDoesNotAudit_WhenNoDetailsChanged(AmendExternalUserAccountModel model)
    {
        var sut = CreateSut();

        _amendUsersMock.Setup(s =>
                s.UpdateUserAccountDetailsAsync(It.IsAny<UpdateUserAccountModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await sut.UpdateExternalAccountDetailsAsync(_accountAdmin, model, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _amendUsersMock.Verify(v => v.UpdateUserAccountDetailsAsync(
            It.Is<UpdateUserAccountModel>(
                x => x.ContactAddress.Line1 == model.PersonContactsDetails.ContactAddress.Line1
                && x.ContactAddress.Line2 == model.PersonContactsDetails.ContactAddress.Line2
                && x.ContactAddress.Line3 == model.PersonContactsDetails.ContactAddress.Line3
                && x.ContactAddress.Line4 == model.PersonContactsDetails.ContactAddress.Line4
                && x.ContactAddress.PostalCode == model.PersonContactsDetails.ContactAddress.PostalCode
                && x.ContactMobileNumber == model.PersonContactsDetails.ContactMobileNumber
                && x.ContactTelephoneNumber == model.PersonContactsDetails.ContactTelephoneNumber
                && x.PreferredContactMethod == model.PersonContactsDetails.PreferredContactMethod
                && x.FirstName == model.PersonName.FirstName
                && x.LastName == model.PersonName.LastName
                && x.Title == model.PersonName.Title
                && x.UserAccountId == model.UserId),
            CancellationToken.None), Times.Once);

        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureAndAudits_WhenUserNotAmended(AmendExternalUserAccountModel model)
    {
        var sut = CreateSut();

        var error =  FixtureInstance.Create<string>();
        var errorMessage = $"Unable to update external user account {model.UserId}, error: {error}";

        _amendUsersMock.Setup(s =>
                s.UpdateUserAccountDetailsAsync(It.IsAny<UpdateUserAccountModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(error));

        var result = await sut.UpdateExternalAccountDetailsAsync(_accountAdmin, model, CancellationToken.None);

        Assert.True(result.IsFailure);

        _amendUsersMock.Verify(v => v.UpdateUserAccountDetailsAsync(
            It.Is<UpdateUserAccountModel>(
                x => x.ContactAddress.Line1 == model.PersonContactsDetails.ContactAddress.Line1
                     && x.ContactAddress.Line2 == model.PersonContactsDetails.ContactAddress.Line2
                     && x.ContactAddress.Line3 == model.PersonContactsDetails.ContactAddress.Line3
                     && x.ContactAddress.Line4 == model.PersonContactsDetails.ContactAddress.Line4
                     && x.ContactAddress.PostalCode == model.PersonContactsDetails.ContactAddress.PostalCode
                     && x.ContactMobileNumber == model.PersonContactsDetails.ContactMobileNumber
                     && x.ContactTelephoneNumber == model.PersonContactsDetails.ContactTelephoneNumber
                     && x.PreferredContactMethod == model.PersonContactsDetails.PreferredContactMethod
                     && x.FirstName == model.PersonName.FirstName
                     && x.LastName == model.PersonName.LastName
                     && x.Title == model.PersonName.Title
                     && x.UserAccountId == model.UserId),
            CancellationToken.None), Times.Once);

        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AdministratorUpdateExternalAccountFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _accountAdmin.UserAccountId
                && a.SourceEntityId == model.UserId
                && a.SourceEntityType == SourceEntityType.UserAccount
                && a.CorrelationId == "test"
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = errorMessage,
                    AdministratorAccountType = _accountAdmin.AccountType.GetDisplayName()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsSuccessAndAudits_WhenAccountClosed(UserAccountModel user)
    {
        var sut = CreateSut();

        _amendUsersMock.Setup(s =>
                s.UpdateApplicantAccountStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<UserAccountStatus>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await sut.CloseExternalUserAccountAsync(user.UserAccountId, _accountAdmin, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _amendUsersMock.Verify(v => v.UpdateApplicantAccountStatusAsync(user.UserAccountId, UserAccountStatus.Deactivated, CancellationToken.None), Times.Once);

        _auditMock.Verify(v => v.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AccountAdministratorCloseAccount
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _accountAdmin.UserAccountId
                && a.SourceEntityId == user.UserAccountId
                && a.SourceEntityType == SourceEntityType.UserAccount
                && a.CorrelationId == "test"
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    ClosedAccountFullName = $"{user.FirstName} {user.LastName}".Trim().Replace("  ", " "),
                    ClosedAccountType = user.AccountType
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFailureAndAudits_WhenAccountNotClosed(UserAccountModel user)
    {
        var sut = CreateSut();

        var errorMessage = FixtureInstance.Create<string>();

        _amendUsersMock.Setup(s =>
                s.UpdateApplicantAccountStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<UserAccountStatus>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccountModel>(errorMessage));

        var result = await sut.CloseExternalUserAccountAsync(user.UserAccountId, _accountAdmin, CancellationToken.None);

        Assert.True(result.IsFailure);

        _amendUsersMock.Verify(v => v.UpdateApplicantAccountStatusAsync(user.UserAccountId, UserAccountStatus.Deactivated, CancellationToken.None), Times.Once);

        _auditMock.Verify(v => v.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AccountAdministratorCloseAccountFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == _accountAdmin.UserAccountId
                && a.SourceEntityId == user.UserAccountId
                && a.SourceEntityType == SourceEntityType.UserAccount
                && a.CorrelationId == "test"
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = errorMessage,
                    AdministratorAccountType = _accountAdmin.AccountType.GetDisplayName()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFailure_WhenPerformingUserIsNotAccountAdministrator(UserAccountModel user)
    {
        var sut = CreateSut();

        var result = await sut.CloseExternalUserAccountAsync(user.UserAccountId, CreateInternalUser(AccountTypeInternal.AdminOfficer), CancellationToken.None);

        Assert.True(result.IsFailure);

        _amendUsersMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailure_WhenAgentUserIdIsSameAsPerformingUser(UserAccountModel user)
    {
        var sut = CreateSut();

        var internalUser = CreateInternalUser(AccountTypeInternal.AdminOfficer);

        user.UserAccountId = internalUser.UserAccountId.Value;

        var result = await sut.CloseExternalUserAccountAsync(user.UserAccountId, internalUser, CancellationToken.None);

        Assert.True(result.IsFailure);

        _amendUsersMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsTrue_WhenAgentUserCanBeClosed(UserAccount user, List<UserAccount> agencyUsers)
    {
        var sut = CreateSut();

        agencyUsers[0].AccountType = AccountTypeExternal.AgentAdministrator;
        user.Status = UserAccountStatus.Active;

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUsersLinkedToAgencyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agencyUsers);

        var result = await sut.VerifyAgentCanBeClosedAsync(user.Id, user.AgencyId!.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(user.Id, CancellationToken.None), Times.Once);
        _retrieveUserServiceMock.Verify(v => v.RetrieveUsersLinkedToAgencyAsync(user.AgencyId.Value, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFalse_WhenAgentUserIsOnlyAdministrator(UserAccount user, List<UserAccount> agencyUsers)
    {
        var sut = CreateSut();

        foreach (var agent in agencyUsers)
        {
            agent.AccountType = AccountTypeExternal.Agent;
        }

        user.AccountType = AccountTypeExternal.AgentAdministrator;
        user.Status = UserAccountStatus.Active;

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUsersLinkedToAgencyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agencyUsers);

        var result = await sut.VerifyAgentCanBeClosedAsync(user.Id, user.AgencyId!.Value, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("There must be at least one agent administrator account at the agency.", result.Error);

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(user.Id, CancellationToken.None), Times.Once);
        _retrieveUserServiceMock.Verify(v => v.RetrieveUsersLinkedToAgencyAsync(user.AgencyId.Value, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFalse_WhenUserAccountNotRetrieved(UserAccount user, List<UserAccount> agencyUsers)
    {
        var sut = CreateSut();

        foreach (var agent in agencyUsers)
        {
            agent.AccountType = AccountTypeExternal.Agent;
        }

        user.AccountType = AccountTypeExternal.AgentAdministrator;
        user.Status = UserAccountStatus.Active;

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>("test"));

        var result = await sut.VerifyAgentCanBeClosedAsync(user.Id, user.AgencyId!.Value, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Agent user account could not be retrieved.", result.Error);

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(user.Id, CancellationToken.None), Times.Once);
        _retrieveUserServiceMock.Verify(v => v.RetrieveUsersLinkedToAgencyAsync(user.AgencyId.Value, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFalse_WhenAgencyUsersNotRetrieved(UserAccount user, List<UserAccount> agencyUsers)
    {
        var sut = CreateSut();

        foreach (var agent in agencyUsers)
        {
            agent.AccountType = AccountTypeExternal.Agent;
        }

        user.AccountType = AccountTypeExternal.AgentAdministrator;
        user.Status = UserAccountStatus.Active;

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUsersLinkedToAgencyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<UserAccount>>("test"));

        var result = await sut.VerifyAgentCanBeClosedAsync(user.Id, user.AgencyId!.Value, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Agent user account could not be retrieved.", result.Error);

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(user.Id, CancellationToken.None), Times.Once);
        _retrieveUserServiceMock.Verify(v => v.RetrieveUsersLinkedToAgencyAsync(user.AgencyId.Value, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsTrue_WhenWoodlandOwnerUserCanBeClosed(UserAccount user, List<UserAccountModel> woodlandOwnerUsers)
    {
        var sut = CreateSut();

        woodlandOwnerUsers[0].AccountType = AccountTypeExternal.WoodlandOwnerAdministrator;
        user.Status = UserAccountStatus.Active;

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountsForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerUsers);

        var result = await sut.VerifyWoodlandOwnerCanBeClosedAsync(user.Id, user.WoodlandOwnerId!.Value, CancellationToken.None);

        Assert.True(result);

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(user.Id, CancellationToken.None), Times.Once);
        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountsForWoodlandOwnerAsync(user.WoodlandOwnerId.Value, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFalse_WhenWoodlandOwnerUserNotRetrieved(UserAccount user, List<UserAccountModel> woodlandOwnerUsers)
    {
        var sut = CreateSut();

        woodlandOwnerUsers[0].AccountType = AccountTypeExternal.WoodlandOwnerAdministrator;
        user.Status = UserAccountStatus.Active;

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>("test"));

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountsForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerUsers);

        var result = await sut.VerifyWoodlandOwnerCanBeClosedAsync(user.Id, user.WoodlandOwnerId!.Value, CancellationToken.None);

        Assert.False(result);

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(user.Id, CancellationToken.None), Times.Once);
        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountsForWoodlandOwnerAsync(user.WoodlandOwnerId.Value, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFalse_WhenUsersLinkedToWoNotRetrieved(UserAccount user)
    {
        var sut = CreateSut();
        user.Status = UserAccountStatus.Active;

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>("test"));

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountsForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserAccountModel>());

        var result = await sut.VerifyWoodlandOwnerCanBeClosedAsync(user.Id, user.WoodlandOwnerId!.Value, CancellationToken.None);

        Assert.False(result);

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(user.Id, CancellationToken.None), Times.Once);
        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountsForWoodlandOwnerAsync(user.WoodlandOwnerId.Value, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFalse_WhenWoUserIsOnlyAdministrator(UserAccount user, List<UserAccountModel> woodlandOwnerUsers)
    {
        var sut = CreateSut();

        foreach (var woodlandOwner in woodlandOwnerUsers)
        {
            woodlandOwner.AccountType = AccountTypeExternal.WoodlandOwner;
        }

        user.AccountType = AccountTypeExternal.WoodlandOwnerAdministrator;
        user.Status = UserAccountStatus.Active;

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountsForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerUsers);

        var result = await sut.VerifyWoodlandOwnerCanBeClosedAsync(user.Id, user.WoodlandOwnerId!.Value, CancellationToken.None);

        Assert.False(result);

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(user.Id, CancellationToken.None), Times.Once);
        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountsForWoodlandOwnerAsync(user.WoodlandOwnerId.Value, CancellationToken.None), Times.Once);
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
    private AmendExternalUserUseCase CreateSut()
    {
        _retrieveUserServiceMock.Reset();

        return new AmendExternalUserUseCase(
            new NullLogger<AmendExternalUserUseCase>(),
            _amendUsersMock.Object,
            _retrieveUserServiceMock.Object,
            _auditMock.Object,
            new RequestContext("test", new RequestUserModel(_accountAdmin.Principal)));
    }
}
using System.Security.Claims;
using System.Text.Json;
using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.AccountAdministration;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.AccountAdministration;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using UserAccountModel = Forestry.Flo.Services.Applicants.Models.UserAccountModel;

namespace Forestry.Flo.External.Web.Tests.Services.AccountAdministration;

public class AmendExternalUserUseCaseTests
{
    private readonly Mock<IRetrieveUserAccountsService> _retrieveUserServiceMock;
    private readonly Mock<IAmendUserAccounts> _amendUsersMock;
    private readonly Mock<IAuditService<AmendExternalUserUseCase>> _auditMock;

    private const string ErrorMessage = "test";

    private static readonly Fixture FixtureInstance = new();

    public AmendExternalUserUseCaseTests()
    {
        _retrieveUserServiceMock = new Mock<IRetrieveUserAccountsService>();
        _amendUsersMock = new Mock<IAmendUserAccounts>();
        _auditMock = new Mock<IAuditService<AmendExternalUserUseCase>>();
    }

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoData]
    public async Task ReturnsAmendmentModel_WhenUserRetrieved(UserAccount user)
    {
        var sut = CreateSut();

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await sut.RetrieveExternalUserAccountAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // assert model mapped correctly

        result.Value.PersonContactsDetails.ContactAddress.Line1.Should().Be(user.ContactAddress.Line1);
        result.Value.PersonContactsDetails.ContactAddress.Line2.Should().Be(user.ContactAddress.Line2);
        result.Value.PersonContactsDetails.ContactAddress.Line3.Should().Be(user.ContactAddress.Line3);
        result.Value.PersonContactsDetails.ContactAddress.Line4.Should().Be(user.ContactAddress.Line4);
        result.Value.PersonContactsDetails.ContactAddress.PostalCode.Should().Be(user.ContactAddress.PostalCode);

        result.Value.PersonContactsDetails.ContactMobileNumber.Should().Be(user.ContactMobileTelephone);
        result.Value.PersonContactsDetails.ContactTelephoneNumber.Should().Be(user.ContactTelephone);
        result.Value.PersonContactsDetails.PreferredContactMethod.Should().Be(user.PreferredContactMethod);

        result.Value.PersonName.FirstName.Should().Be(user.FirstName);
        result.Value.PersonName.LastName.Should().Be(user.LastName);
        result.Value.PersonName.Title.Should().Be(user.Title);

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(user.Id, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFailure_WhenUserCannotBeRetrievedForAmendment(UserAccount user)
    {
        var sut = CreateSut();

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>("test"));

        var result = await sut.RetrieveExternalUserAccountAsync(user.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task ReturnsSuccess_WhenUserAccountUpdatedSuccessfully(UserAccount user, AmendExternalUserAccountModel model)
    {
        var sut = CreateSut();

        var woodlandOwnerAdmin = CreateExternalUser(AccountTypeExternal.WoodlandOwnerAdministrator);

        user.WoodlandOwnerId = Guid.Parse(woodlandOwnerAdmin.WoodlandOwnerId);

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _amendUsersMock.Setup(s =>
                s.UpdateUserAccountDetailsAsync(It.IsAny<UpdateUserAccountModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.UpdateExternalAccountDetailsAsync(woodlandOwnerAdmin, model, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(model.UserId, CancellationToken.None), Times.Once);
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
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == woodlandOwnerAdmin.UserAccountId
                && a.SourceEntityId == model.UserId
                && a.SourceEntityType == SourceEntityType.UserAccount
                && a.CorrelationId == "test"
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    UpdatedFullName = $"{model.PersonName.Title} {model.PersonName.FirstName} {model.PersonName.LastName}".Trim().Replace("  ", " "),
                    AdministratorAccountType = woodlandOwnerAdmin.AccountType.GetDisplayName()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFailure_WhenUserAccountNotRetrieved(UserAccount user, AmendExternalUserAccountModel model)
    {
        var sut = CreateSut();

        var woodlandOwnerAdmin = CreateExternalUser(AccountTypeExternal.WoodlandOwnerAdministrator);

        user.WoodlandOwnerId = Guid.Parse(woodlandOwnerAdmin.WoodlandOwnerId);

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>(ErrorMessage));

        var result = await sut.UpdateExternalAccountDetailsAsync(woodlandOwnerAdmin, model, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(model.UserId, CancellationToken.None), Times.Once);
        _amendUsersMock.VerifyNoOtherCalls();
        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AdministratorUpdateExternalAccountFailure
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == woodlandOwnerAdmin.UserAccountId
                && a.SourceEntityId == model.UserId
                && a.SourceEntityType == SourceEntityType.UserAccount
                && a.CorrelationId == "test"
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = $"Unable to retrieve user account with id {model.UserId}, error: {ErrorMessage}",
                    AdministratorAccountType = woodlandOwnerAdmin.AccountType.GetDisplayName()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(AccountTypeExternal.WoodlandOwnerAdministrator, true, false)]
    [InlineData(AccountTypeExternal.WoodlandOwnerAdministrator, false, true)]
    [InlineData(AccountTypeExternal.AgentAdministrator, true, false)]
    [InlineData(AccountTypeExternal.AgentAdministrator, false, true)]
    public async Task ReturnsFailure_WhenLoggedInUserNotAuthorisedToAmend(AccountTypeExternal accountType, bool correspondingIdMatch, bool matchingAccountType)
    {
        var sut = CreateSut();

        var adminUser = CreateExternalUser(accountType);

        var user = FixtureInstance.Create<UserAccount>();
        var model = FixtureInstance.Create<AmendExternalUserAccountModel>();

        if (accountType is AccountTypeExternal.WoodlandOwnerAdministrator)
        {
            user.AccountType = matchingAccountType
                ? AccountTypeExternal.WoodlandOwner
                : AccountTypeExternal.Agent;

            user.WoodlandOwnerId = correspondingIdMatch
                ? Guid.Parse(adminUser.WoodlandOwnerId)
                : Guid.NewGuid();
        } else if (accountType is AccountTypeExternal.AgentAdministrator)
        {
            user.AccountType = matchingAccountType
                ? AccountTypeExternal.Agent
                : AccountTypeExternal.WoodlandOwner;

            user.AgencyId = correspondingIdMatch
                ? Guid.Parse(adminUser.AgencyId)
                : Guid.NewGuid();
        }

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await sut.UpdateExternalAccountDetailsAsync(adminUser, model, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        var error = user.IsWoodlandOwner()
            ? $"Logged in user lacks authority to amend woodland owner user account with id {model.UserId}"
            : $"Logged in user lacks authority to amend agent user account with id {model.UserId}";

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(model.UserId, CancellationToken.None), Times.Once);
        _amendUsersMock.VerifyNoOtherCalls();
        _auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AdministratorUpdateExternalAccountFailure
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == adminUser.UserAccountId
                && a.SourceEntityId == model.UserId
                && a.SourceEntityType == SourceEntityType.UserAccount
                && a.CorrelationId == "test"
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = error,
                    AdministratorAccountType = adminUser.AccountType.GetDisplayName()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFailure_WhenUserAccountNotUpdatedSuccessfully(UserAccount user, AmendExternalUserAccountModel model)
    {
        var sut = CreateSut();

        var woodlandOwnerAdmin = CreateExternalUser(AccountTypeExternal.WoodlandOwnerAdministrator);

        user.WoodlandOwnerId = Guid.Parse(woodlandOwnerAdmin.WoodlandOwnerId);

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _amendUsersMock.Setup(s =>
                s.UpdateUserAccountDetailsAsync(It.IsAny<UpdateUserAccountModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(ErrorMessage));

        var result = await sut.UpdateExternalAccountDetailsAsync(woodlandOwnerAdmin, model, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(model.UserId, CancellationToken.None), Times.Once);
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
                && a.ActorType == ActorType.ExternalApplicant
                && a.UserId == woodlandOwnerAdmin.UserAccountId
                && a.SourceEntityId == model.UserId
                && a.SourceEntityType == SourceEntityType.UserAccount
                && a.CorrelationId == "test"
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = $"Unable to update external user account {model.UserId}, error: {ErrorMessage}",
                    AdministratorAccountType = woodlandOwnerAdmin.AccountType.GetDisplayName()
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsSuccessAndDoesNotAudit_WhenNoChangesMade(UserAccount user, AmendExternalUserAccountModel model)
    {
        var sut = CreateSut();

        var woodlandOwnerAdmin = CreateExternalUser(AccountTypeExternal.WoodlandOwnerAdministrator);

        user.WoodlandOwnerId = Guid.Parse(woodlandOwnerAdmin.WoodlandOwnerId);

        _retrieveUserServiceMock.Setup(s =>
                s.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _amendUsersMock.Setup(s =>
                s.UpdateUserAccountDetailsAsync(It.IsAny<UpdateUserAccountModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await sut.UpdateExternalAccountDetailsAsync(woodlandOwnerAdmin, model, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _retrieveUserServiceMock.Verify(v => v.RetrieveUserAccountEntityByIdAsync(model.UserId, CancellationToken.None), Times.Once);
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

    private static ExternalApplicant CreateExternalUser(AccountTypeExternal accountType)
    {
        var claimsPrincipal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<Guid>(),
            accountType is AccountTypeExternal.WoodlandOwnerAdministrator or AccountTypeExternal.WoodlandOwner
                ? FixtureInstance.Create<Guid>()
                : null,
            agencyId: accountType is AccountTypeExternal.AgentAdministrator or AccountTypeExternal.Agent 
                ? FixtureInstance.Create<Guid>() 
                : null,
            woodlandOwnerName: FixtureInstance.Create<string>(),
            accountTypeExternal: accountType);

        return new ExternalApplicant(claimsPrincipal);
    }
    private AmendExternalUserUseCase CreateSut()
    {
        _retrieveUserServiceMock.Reset();

        return new AmendExternalUserUseCase(
            new NullLogger<AmendExternalUserUseCase>(),
            _amendUsersMock.Object,
            _retrieveUserServiceMock.Object,
            _auditMock.Object,
            new RequestContext("test", new RequestUserModel(new ClaimsPrincipal())));
    }
}
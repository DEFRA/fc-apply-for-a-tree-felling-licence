using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.ValueTasks;
using Forestry.Flo.External.Web.Models.Agency;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;
using Forestry.Flo.External.Web.Models.WoodlandOwner;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants;
using Forestry.Flo.Services.Applicants.Configuration;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Testing;
using AccountType = Forestry.Flo.External.Web.Models.UserAccount.AccountType;
using ServiceTenantType = Forestry.Flo.Services.Applicants.Entities.WoodlandOwner.TenantType;

namespace Forestry.Flo.External.Web.Tests.Services;

public class RegisterUserAccountUseCaseTests
{
    private static readonly Fixture FixtureInstance = new Fixture();

    private ApplicantsContext _testDatabase = null!;
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IClock _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
    private Mock<IHttpContextAccessor> _mockHttpContextAccessor = null!;
    private Mock<IUserAccountRepository> _userAccountRepository = null!;
    private readonly Mock<IAuditService<RegisterUserAccountUseCase>> _mockAuditService = new();
    private readonly Mock<IAuditService<SignInApplicantWithEf>> _mockAuditServiceForSignIn = new();
    private readonly Mock<IPropertyProfileRepository> _mockPropertyProfileRepository = new();
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _mockFellingLicenceApplicationRepository = new();
    private Mock<IUnitOfWork> _unitOfWOrkMock = null!;
    private ExternalApplicant _externalApplicant;

    public RegisterUserAccountUseCaseTests()
    {
        _testDatabase = TestApplicantsDatabaseFactory.CreateDefaultTestContext();
        _userAccountRepository = new Mock<IUserAccountRepository>();
        _unitOfWOrkMock = new Mock<IUnitOfWork>();
    }

    [Fact]
    public async Task ReturnsNothingWhenUserHasNoLocalAccount()
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal();
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        var result = await sut.RetrieveExistingAccountAsync(_externalApplicant, CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Fact]
    public async Task CanRetrieveExistingAccountForCurrentUser()
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId, 
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id);
        
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        var result = await sut.RetrieveExistingAccountAsync(_externalApplicant, CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(_externalApplicant.IdentityProviderId, result.Value.IdentityProviderId);
        Assert.Equal(_externalApplicant.UserAccountId, result.Value.Id);
    }

    [Theory]
    [InlineData(AccountType.WoodlandOwner, false)]
    [InlineData(AccountType.WoodlandOwner, true)]
    [InlineData(AccountType.Agent, false)]
    public async Task CannotRegisterNewAccountIsUserAlreadyHasOne(AccountType accountType, bool isOrganisation)
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        var result = await sut.RegisterNewAccountAsync(
            _externalApplicant,
            new UserTypeModel
            {
                AccountType = accountType,
                IsOrganisation = isOrganisation
            },
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        _mockHttpContextAccessor.VerifyGet(x => x.HttpContext, Times.Never);
        _mockAuditService.VerifyAll();
    }

    [Theory]
    [InlineData(AccountType.WoodlandOwner, false)]
    [InlineData(AccountType.WoodlandOwner, true)]
    [InlineData(AccountType.Agent, false)]
    public async Task RegisterNewAccountShouldNotStoreNewEntityIfAccountWithSameIdentityProviderIdExists(AccountType accountType, bool isOrganisation)
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email);
        _externalApplicant = new ExternalApplicant(user);
        
        var sut = CreateSut();

        var result = await sut.RegisterNewAccountAsync(
            _externalApplicant, 
            new UserTypeModel
            {
                AccountType = accountType,
                IsOrganisation = isOrganisation
            }, 
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        if (accountType != AccountType.Agent)
        {
            _mockHttpContextAccessor.VerifyGet(x => x.HttpContext, Times.Once);
        }
        _mockAuditService.VerifyAll();

        Assert.Equal(1, _testDatabase.UserAccounts.Count());
    }

    [Theory]
    [InlineData(AccountType.WoodlandOwner, false)]
    [InlineData(AccountType.WoodlandOwner, true)]
    [InlineData(AccountType.Agent, false)]
    public async Task RegistrationReturnsFailureIfAccountWithSameIdentityProviderIdButDifferentEmailAddressExists(AccountType accountType, bool isOrganisation)
    {
        var account = accountType is AccountType.WoodlandOwner 
            ? await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance)
            : await TestApplicantsDatabaseFactory.AddTestAgentAccount(_testDatabase, FixtureInstance);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            FixtureInstance.Create<string>(),
            agencyId: accountType == AccountType.Agent ? account.Agency!.Id : null);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        var result = await sut.RegisterNewAccountAsync(
            _externalApplicant,
            new UserTypeModel 
            {
                AccountType = accountType,
                IsOrganisation = isOrganisation
            },
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        _mockHttpContextAccessor.VerifyGet(x => x.HttpContext, Times.Never);
        _mockAuditService.VerifyAll();
    }
    
    [Theory]
    [InlineData(AccountType.WoodlandOwner, false)]
    [InlineData(AccountType.WoodlandOwner, true)]
    [InlineData(AccountType.Agent, false)]
    public async Task CanRegisterNewWoodlandOwnerAccount(AccountType accountType, bool isOrganisation)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal();
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        var result = await sut.RegisterNewAccountAsync(
            _externalApplicant,
            new UserTypeModel
            {
                AccountType = accountType,
                IsOrganisation = isOrganisation
            }, 
            null, 
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);
        Assert.NotNull(storedAccount);

        Assert.Equal(_externalApplicant.IdentityProviderId, storedAccount.IdentityProviderId);
        Assert.Equal(_externalApplicant.EmailAddress, storedAccount.Email);

        switch (accountType)
        {
            case AccountType.WoodlandOwner when isOrganisation is false:
                Assert.Equal(AccountTypeExternal.WoodlandOwnerAdministrator, storedAccount.AccountType);
                Assert.False(storedAccount.WoodlandOwner!.IsOrganisation);
                Assert.Equal(_externalApplicant.EmailAddress, storedAccount.WoodlandOwner!.ContactEmail);
                break;
            case AccountType.WoodlandOwner when isOrganisation:
                Assert.Equal(AccountTypeExternal.WoodlandOwnerAdministrator, storedAccount.AccountType);
                Assert.True(storedAccount.WoodlandOwner!.IsOrganisation);
                break;
            case AccountType.Agent:
                Assert.Equal(AccountTypeExternal.AgentAdministrator, storedAccount.AccountType);
                Assert.True(storedAccount.IsAgent());
                Assert.NotNull(storedAccount.Agency);
                break;
            default:
                break;
        }

        _mockHttpContextAccessor.VerifyGet(x => x.HttpContext);

        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.SourceEntityType == SourceEntityType.UserAccount && a.SourceEntityId == storedAccount.Id
                                                               && a.EventName == AuditEvents.RegisterAuditEvent),
            It.IsAny<CancellationToken>()));
    }

    [Theory, AutoData]
    public async Task CannotUpdatePersonNameWhenNoLocalAccountExists(UserAccountModel input)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal();
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();
        var result = await sut.UpdateAccountPersonNameDetailsAsync(_externalApplicant, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        _mockAuditService.VerifyAll();
    }

    [Theory, AutoData]
    public async Task CanUpdatePersonName(UserAccountModel input)
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id);
        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var result = await sut.UpdateAccountPersonNameDetailsAsync(_externalApplicant, input, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);
        Assert.NotNull(storedAccount);

        Assert.Equal(input.PersonName!.Title, storedAccount.Title);
        Assert.Equal(input.PersonName.FirstName, storedAccount.FirstName);
        Assert.Equal(input.PersonName.LastName, storedAccount.LastName);

        _mockAuditService.Verify(x =>
            x.PublishAuditEventAsync(
                It.Is<AuditEvent>(a =>
                    a.SourceEntityType == SourceEntityType.UserAccount && 
                    a.SourceEntityId == storedAccount.Id &&
                    a.EventName == AuditEvents.UpdateAccountEvent && 
                    a.ActorType == ActorType.ExternalApplicant), It.IsAny<CancellationToken>()));
    }

    [Theory, AutoData]
    public async Task CannotUpdatePersonContactDetailsWhenNoLocalAccountExists(UserAccountModel input)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal();
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();
     
        var result = await sut.UpdateAccountPersonContactDetailsAsync(_externalApplicant, input, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockAuditService.VerifyAll();
    }

    [Theory]
    [InlineData(AccountType.WoodlandOwner, false)]
    [InlineData(AccountType.WoodlandOwner, true)]
    [InlineData(AccountType.Agent, false)]
    public async Task CanUpdatePersonContactDetails(AccountType accountType, bool isOrganisation)
    {

        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();
        var model = FixtureInstance.Create<UserAccountModel>();
        model.UserTypeModel.AccountType = accountType;

        var result = await sut.UpdateAccountPersonContactDetailsAsync(_externalApplicant, model, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);
        Assert.NotNull(storedAccount);

        Assert.Equal(model.PersonContactsDetails.ContactTelephoneNumber, storedAccount.ContactTelephone);
        Assert.Equal(model.PersonContactsDetails.ContactMobileNumber, storedAccount.ContactMobileTelephone);
        Assert.Equal(model.PersonContactsDetails.PreferredContactMethod, storedAccount.PreferredContactMethod);
        Assert.Equal(model.PersonContactsDetails.ContactAddress.Line1, storedAccount.ContactAddress.Line1);
        Assert.Equal(model.PersonContactsDetails.ContactAddress.Line2, storedAccount.ContactAddress.Line2);
        Assert.Equal(model.PersonContactsDetails.ContactAddress.Line3, storedAccount.ContactAddress.Line3);
        Assert.Equal(model.PersonContactsDetails.ContactAddress.Line4, storedAccount.ContactAddress.Line4);
        Assert.Equal(model.PersonContactsDetails.ContactAddress.PostalCode, storedAccount.ContactAddress.PostalCode);

        if (accountType == AccountType.WoodlandOwner)
        {
            Assert.Equal(model.PersonContactsDetails.ContactAddress.Line1, storedAccount.WoodlandOwner.ContactAddress.Line1);
            Assert.Equal(model.PersonContactsDetails.ContactAddress.Line2, storedAccount.WoodlandOwner.ContactAddress.Line2);
            Assert.Equal(model.PersonContactsDetails.ContactAddress.Line3, storedAccount.WoodlandOwner.ContactAddress.Line3);
            Assert.Equal(model.PersonContactsDetails.ContactAddress.Line4, storedAccount.WoodlandOwner.ContactAddress.Line4);
            Assert.Equal(model.PersonContactsDetails.ContactAddress.PostalCode, storedAccount.WoodlandOwner.ContactAddress.PostalCode);
        }
        else
        {
            Assert.NotEqual(model.PersonContactsDetails.ContactAddress.Line1, storedAccount.WoodlandOwner.ContactAddress.Line1);
            Assert.NotEqual(model.PersonContactsDetails.ContactAddress.Line2, storedAccount.WoodlandOwner.ContactAddress.Line2);
            Assert.NotEqual(model.PersonContactsDetails.ContactAddress.Line3, storedAccount.WoodlandOwner.ContactAddress.Line3);
            Assert.NotEqual(model.PersonContactsDetails.ContactAddress.Line4, storedAccount.WoodlandOwner.ContactAddress.Line4);
            Assert.NotEqual(model.PersonContactsDetails.ContactAddress.PostalCode, storedAccount.WoodlandOwner.ContactAddress.PostalCode);
        }

        _mockAuditService.Verify(x =>
            x.PublishAuditEventAsync(
                It.Is<AuditEvent>(a =>
                    a.SourceEntityType == SourceEntityType.UserAccount && 
                    a.SourceEntityId == storedAccount.Id && 
                    a.EventName == AuditEvents.UpdateAccountEvent && 
                    a.ActorType == ActorType.ExternalApplicant), It.IsAny<CancellationToken>()));
    }

    [Theory, AutoData]
    public async Task CannotUpdateWoodlandOwnerDetailsWhenNoLocalAccountExists(WoodlandOwnerModel input)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal();
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        var result = await sut.UpdateAccountWoodlandOwnerDetailsAsync(_externalApplicant, input, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockAuditService.VerifyAll();
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateAgencyDetails_GivenAgencyDetails(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.AgentAdministrator;
        _userAccountRepository.Setup(r => r.GetAsync(It.Is<Guid>(d => d == account.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: account.Agency?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.UpdateUserAgencyDetails(_externalApplicant, agencyModel, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        _userAccountRepository.Verify(r => r.Update(It.Is<UserAccount>( u => 
            u.Id == account.Id
            && u.Agency!.ShouldAutoApproveThinningApplications == agencyModel.ShouldAutoApproveThinningApplications
            && u.Agency!.ContactEmail == agencyModel.ContactEmail
            && u.Agency!.Address!.PostalCode == agencyModel!.Address!.PostalCode
            && u.Agency!.Address!.Line1 == agencyModel!.Address!.Line1
            && u.Agency!.Address!.Line2 == agencyModel!.Address!.Line2
            && u.Agency!.Address!.Line3 == agencyModel!.Address!.Line3
            && u.Agency!.Address!.Line4 == agencyModel!.Address!.Line4
            && u.Agency!.ContactName == agencyModel.ContactName
            && u.Agency.IsOrganisation
            )));
        _unitOfWOrkMock.Verify(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                e.EventName == AuditEvents.UpdateAccountEvent
                && e.SourceEntityId == account.Id
                && e.ActorType == ActorType.ExternalApplicant), It.IsAny<CancellationToken>()));
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldNotUpdateAgencyDetails_GivenUserIsNotAgencyAdmin(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.Agent;
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: account.Agency?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.UpdateUserAgencyDetails(_externalApplicant, agencyModel, CancellationToken.None);

        //assert
        Assert.True(result.IsFailure);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldNotUpdateAgencyDetails_GivenUserIsNotLinkedToAgency(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.Agent;
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: null);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.UpdateUserAgencyDetails(_externalApplicant, agencyModel, CancellationToken.None);

        //assert
        Assert.True(result.IsFailure);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldNotUpdateAgencyDetails_GivenUserDoesNotHaveLocalAccount(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.AgentAdministrator;
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            null,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: null);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.UpdateUserAgencyDetails(_externalApplicant, agencyModel, CancellationToken.None);

        //assert
        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotUpdateAgencyDetails_GivenNotExistingAgencyDetails(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.AgentAdministrator;
        _userAccountRepository.Setup(r => r.GetAsync(It.Is<Guid>(d => d == account.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: account.Agency?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.UpdateUserAgencyDetails(_externalApplicant, agencyModel, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        _mockAuditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                e.EventName == AuditEvents.UpdateAccountFailureEvent && e.ActorType == ActorType.ExternalApplicant), It.IsAny<CancellationToken>()));
    }


    
    [Theory, AutoMoqData]
    public async Task ShouldReturnError_WhenUpdateAgencyDetails_GivenFailedUpdateResult(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.AgentAdministrator;
        _userAccountRepository.Setup(r => r.GetAsync(It.Is<Guid>(d => d == account.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));
        _unitOfWOrkMock.Setup(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => UnitResult.Failure(UserDbErrorReason.General));
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: account.Agency?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.UpdateUserAgencyDetails(_externalApplicant, agencyModel, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        _mockAuditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                e.EventName == AuditEvents.UpdateAccountFailureEvent
                && e.ActorType == ActorType.ExternalApplicant), It.IsAny<CancellationToken>()));
    }


    

    [Theory, AutoData]
    public async Task CanUpdateWoodlandOwnerDetails(WoodlandOwnerModel input)
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        var result = await sut.UpdateAccountWoodlandOwnerDetailsAsync(_externalApplicant, input, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedAccount = await _testDatabase.UserAccounts
            .Include(x => x.WoodlandOwner)
            .SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);
        Assert.NotNull(storedAccount);

        Assert.Equal(input.ContactEmail, storedAccount.WoodlandOwner.ContactEmail);
        Assert.Equal(input.ContactName, storedAccount.WoodlandOwner.ContactName);
        Assert.Equal(input.OrganisationName, storedAccount.WoodlandOwner.OrganisationName);
        Assert.Equal(input.ContactAddress.Line1, storedAccount.WoodlandOwner.ContactAddress.Line1);
        Assert.Equal(input.ContactAddress.Line2, storedAccount.WoodlandOwner.ContactAddress.Line2);
        Assert.Equal(input.ContactAddress.Line3, storedAccount.WoodlandOwner.ContactAddress.Line3);
        Assert.Equal(input.ContactAddress.Line4, storedAccount.WoodlandOwner.ContactAddress.Line4);
        Assert.Equal(input.ContactAddress.PostalCode, storedAccount.WoodlandOwner.ContactAddress.PostalCode);
        Assert.Equal(input.OrganisationAddress.Line1, storedAccount.WoodlandOwner.OrganisationAddress.Line1);
        Assert.Equal(input.OrganisationAddress.Line2, storedAccount.WoodlandOwner.OrganisationAddress.Line2);
        Assert.Equal(input.OrganisationAddress.Line3, storedAccount.WoodlandOwner.OrganisationAddress.Line3);
        Assert.Equal(input.OrganisationAddress.Line4, storedAccount.WoodlandOwner.OrganisationAddress.Line4);
        Assert.Equal(input.OrganisationAddress.PostalCode, storedAccount.WoodlandOwner.OrganisationAddress.PostalCode);

        _mockAuditService.Verify(x =>
            x.PublishAuditEventAsync(
                It.Is<AuditEvent>(a =>
                    a.SourceEntityType == SourceEntityType.WoodlandOwner &&
                    a.SourceEntityId == storedAccount.WoodlandOwner.Id &&
                    a.EventName == AuditEvents.UpdateWoodlandOwnerEvent && 
                    a.ActorType == ActorType.ExternalApplicant
                    ), It.IsAny<CancellationToken>()));
    }

    [Theory, AutoData]
    public async Task CannotUpdateAcceptsTAndCsWhenNoLocalAccountExists(AccountTermsAndConditionsModel input)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal();
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        var result = await sut.UpdateUserAcceptsTermsAndConditionsAsync(_externalApplicant, input, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockAuditService.VerifyAll();
    }

    [Theory, AutoData]
    public async Task CanUpdateAcceptsTAndCs(AccountTermsAndConditionsModel input)
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(
            _testDatabase, 
            FixtureInstance, 
            acceptedTAndCs: !input.AcceptsTermsAndConditions,
            acceptedPrivacyPolicy: !input.AcceptsPrivacyPolicy);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id);
        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var result = await sut.UpdateUserAcceptsTermsAndConditionsAsync(_externalApplicant, input, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);
        Assert.NotNull(storedAccount);

        if (input.AcceptsTermsAndConditions)
        {
            Assert.Equal(_fixedTimeClock.GetCurrentInstant().ToDateTimeUtc(), storedAccount.DateAcceptedTermsAndConditions);
        }
        else
        {
            Assert.Null(storedAccount.DateAcceptedTermsAndConditions);
        }

        if (input.AcceptsPrivacyPolicy)
        {
            Assert.Equal(_fixedTimeClock.GetCurrentInstant().ToDateTimeUtc(), storedAccount.DateAcceptedPrivacyPolicy);
        }
        else
        {
            Assert.Null(storedAccount.DateAcceptedPrivacyPolicy);
        }
        

        _mockAuditService.Verify(x =>
            x.PublishAuditEventAsync(
                It.Is<AuditEvent>(a =>
                    a.SourceEntityType == SourceEntityType.UserAccount && a.SourceEntityId == storedAccount.Id &&
                    a.EventName == AuditEvents.UpdateAccountEvent && 
                    a.ActorType == ActorType.ExternalApplicant), It.IsAny<CancellationToken>()));
    }

    [Theory]
    [InlineData(AccountType.WoodlandOwner, false)]
    [InlineData(AccountType.Agent, false)]
    public async Task CanUpdateWoodlandOwnerAccountType(AccountType accountType, bool isOrganisation)
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance, isOrganisation:true);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            hasAcceptedTermsAndConditionsAndPrivacyPolicy:false //do not understand why has to be in state of not accepted for this test/use-case to work.
            );

        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        var result = await sut.UpdateAccountTypeAsync(
            _externalApplicant,
            new UserTypeModel
            {
                AccountType = accountType,
                IsOrganisation = isOrganisation
            }, 
            null,
            CancellationToken.None);


        Assert.True(result.IsSuccess);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);
        Assert.NotNull(storedAccount);

        switch (accountType)
        {
            case AccountType.Agent:
                Assert.Equal(AccountTypeExternal.AgentAdministrator, storedAccount.AccountType);
                Assert.Null(storedAccount.WoodlandOwner);
                Assert.Null(storedAccount.WoodlandOwnerId);
                Assert.True(storedAccount.IsAgent());
                break;
            case AccountType.WoodlandOwner:
                Assert.Equal(AccountTypeExternal.WoodlandOwnerAdministrator, storedAccount.AccountType);
                Assert.NotEqual(_externalApplicant.WoodlandOwnerId, storedAccount.WoodlandOwnerId.ToString());
                Assert.True(storedAccount.IsWoodlandOwner());

                Assert.Equal(storedAccount.Email, storedAccount.WoodlandOwner.ContactEmail);
                Assert.Equal(storedAccount.FullName(false), storedAccount.WoodlandOwner.ContactName);
                Assert.Equal(storedAccount.ContactAddress.Line1, storedAccount.WoodlandOwner.ContactAddress.Line1);
                Assert.Equal(storedAccount.ContactAddress.Line2, storedAccount.WoodlandOwner.ContactAddress.Line2);
                Assert.Equal(storedAccount.ContactAddress.Line3, storedAccount.WoodlandOwner.ContactAddress.Line3);
                Assert.Equal(storedAccount.ContactAddress.Line4, storedAccount.WoodlandOwner.ContactAddress.Line4);
                Assert.Equal(storedAccount.ContactAddress.PostalCode, storedAccount.WoodlandOwner.ContactAddress.PostalCode);
                break;
            default:
                break;
        }
    }

    [Theory]
    [InlineData(AccountType.WoodlandOwner, true)]
    public async Task CanUpdateToSameAccountType(AccountType accountType, bool isOrganisation)
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance, isOrganisation: true);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            account.AccountType);
        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var result = await sut.UpdateAccountTypeAsync(
            _externalApplicant,
            new UserTypeModel
            {
                AccountType = accountType,
                IsOrganisation = isOrganisation
            },
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);
        Assert.NotNull(storedAccount);

        Assert.Equal(_externalApplicant.WoodlandOwnerId, storedAccount.WoodlandOwnerId.ToString());
        Assert.Equal(AccountTypeExternal.WoodlandOwnerAdministrator, storedAccount.AccountType);
    }

    [Theory]
    [InlineData(AccountType.Agent, false)]
    [InlineData(AccountType.WoodlandOwner, true)]
    [InlineData(AccountType.WoodlandOwner, false)]
    public async Task CannotUpdateAccountTypeWhenNoLocalAccountExists(AccountType accountType, bool isOrganisation)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal();
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        var result = await sut.UpdateAccountTypeAsync(
            _externalApplicant, 
            new UserTypeModel 
            {
                AccountType = accountType,
                IsOrganisation = isOrganisation
            }, 
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(AccountType.Agent, false, false, AccountTypeExternal.AgentAdministrator)]
    [InlineData(AccountType.WoodlandOwner, true, false, AccountTypeExternal.WoodlandOwnerAdministrator)]
    [InlineData(AccountType.WoodlandOwner, false, false, AccountTypeExternal.WoodlandOwnerAdministrator)]
    [InlineData(AccountType.Trust, true, false, AccountTypeExternal.WoodlandOwnerAdministrator)]
    [InlineData(AccountType.Trust, false, false, AccountTypeExternal.WoodlandOwnerAdministrator)]
    [InlineData(AccountType.Tenant, false, false, AccountTypeExternal.WoodlandOwnerAdministrator)]
    [InlineData(AccountType.Tenant, false, true, AccountTypeExternal.WoodlandOwnerAdministrator)]
    public async Task ShouldUpdateWoodlandOwnerTypeBasedOnAccountType(
        AccountType accountType, 
        bool isOrganisation, 
        bool crownLandTenant, 
        AccountTypeExternal expectedAssignment)
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance, isOrganisation: true);

        var landlordDetails = FixtureInstance.Create<LandlordDetails>();

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            account.AccountType,
            hasAcceptedTermsAndConditionsAndPrivacyPolicy: false);
        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var shouldHaveLandlordDetails = accountType is AccountType.Tenant && crownLandTenant;

        ServiceTenantType? expectedTenantType = accountType switch
        {
            AccountType.Agent => null,
            AccountType.Tenant when shouldHaveLandlordDetails => ServiceTenantType.CrownLand,
            AccountType.Tenant when !shouldHaveLandlordDetails => ServiceTenantType.NonCrownLand,
            _ => ServiceTenantType.None
        };

        var result = await sut.UpdateAccountTypeAsync(
            _externalApplicant,
            new UserTypeModel
            {
                AccountType = accountType,
                IsOrganisation = isOrganisation
            },
            shouldHaveLandlordDetails
                ? landlordDetails 
                : null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);
        Assert.NotNull(storedAccount);

        // assert the correct account type is assigned to the account

        Assert.Equal(expectedAssignment, storedAccount.AccountType);

        // assert landlord details are only set for Crown Land tenants

        Assert.Equal(shouldHaveLandlordDetails, storedAccount.WoodlandOwner?.LandlordFirstName != null);
        Assert.Equal(shouldHaveLandlordDetails, storedAccount.WoodlandOwner?.LandlordLastName != null);

        Assert.Equal(expectedTenantType, storedAccount.WoodlandOwner?.TenantType);
    }

    [Theory]
    [InlineData(AccountType.Agent, false, false, AccountTypeExternal.AgentAdministrator)]
    [InlineData(AccountType.WoodlandOwner, true, false, AccountTypeExternal.WoodlandOwnerAdministrator)]
    [InlineData(AccountType.WoodlandOwner, false, false, AccountTypeExternal.WoodlandOwnerAdministrator)]
    [InlineData(AccountType.Trust, true, false, AccountTypeExternal.WoodlandOwnerAdministrator)]
    [InlineData(AccountType.Trust, false, false, AccountTypeExternal.WoodlandOwnerAdministrator)]
    [InlineData(AccountType.Tenant, false, false, AccountTypeExternal.WoodlandOwnerAdministrator)]
    [InlineData(AccountType.Tenant, false, true, AccountTypeExternal.WoodlandOwnerAdministrator)]
    public async Task ShouldRegisterWoodlandOwnerBasedOnAccountType(
        AccountType accountType,
        bool isOrganisation,
        bool crownLandTenant,
        AccountTypeExternal expectedAssignment)
    {
        var landlordDetails = FixtureInstance.Create<LandlordDetails>();

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal();
        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var shouldHaveLandlordDetails = accountType is AccountType.Tenant && crownLandTenant;

        var expectedTenantType = accountType is AccountType.Tenant
            ? shouldHaveLandlordDetails
                ? ServiceTenantType.CrownLand
                : ServiceTenantType.NonCrownLand
            : ServiceTenantType.None;

        var result = await sut.RegisterNewAccountAsync(
            _externalApplicant,
            new UserTypeModel
            {
                AccountType = accountType,
                IsOrganisation = isOrganisation
            },
            shouldHaveLandlordDetails
                ? landlordDetails
                : null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);
        Assert.NotNull(storedAccount);

        // assert the correct account type is assigned to the account

        Assert.Equal(expectedAssignment, storedAccount.AccountType);

        // assert landlord details are only set for Crown Land tenants

        Assert.Equal(shouldHaveLandlordDetails, storedAccount.WoodlandOwner?.LandlordFirstName != null);
        Assert.Equal(shouldHaveLandlordDetails, storedAccount.WoodlandOwner?.LandlordLastName != null);

        if (accountType is not AccountType.Agent)
        {
            Assert.Equal(expectedTenantType, storedAccount.WoodlandOwner?.TenantType);
        }
        else
        {
            Assert.Null(storedAccount.WoodlandOwner);
        }
    }

    [Fact]
    public async Task ShouldRemoveWoodlandOwnerWhenUpdatingAccount()
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(_testDatabase, FixtureInstance, isOrganisation: true);

        var landlordDetails = FixtureInstance.Create<LandlordDetails>();

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            account.AccountType,
            hasAcceptedTermsAndConditionsAndPrivacyPolicy: false);

        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var result = await sut.UpdateAccountTypeAsync(
            _externalApplicant,
            new UserTypeModel
            {
                AccountType = AccountType.Agent,
                IsOrganisation = false
            },
            null,
            CancellationToken.None);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);

        Assert.Null(storedAccount.WoodlandOwner);
        Assert.NotNull(storedAccount.Agency);
    }

    [Theory, CombinatorialData]
    public async Task ShouldRemoveAgencyWhenUpdatingAccount(bool isAgentAdministrator)
    {
        var account = await TestApplicantsDatabaseFactory.AddTestAgentAccount(
            _testDatabase, 
            FixtureInstance, 
            isOrganisation: true, 
            agentAdministrator: isAgentAdministrator,
            acceptedPrivacyPolicy: false,
            acceptedTAndCs: false);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            agencyId: account.AgencyId,
            accountTypeExternal: account.AccountType,
            hasAcceptedTermsAndConditionsAndPrivacyPolicy: false);

        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var result = await sut.UpdateAccountTypeAsync(
            _externalApplicant,
            new UserTypeModel
            {
                AccountType = AccountType.WoodlandOwner,
                IsOrganisation = false
            },
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);

        var storedAgencies = _testDatabase.Agencies.AsQueryable();

        Assert.Empty(storedAgencies);

        Assert.Null(storedAccount.Agency);
        Assert.NotNull(storedAccount.WoodlandOwner);
    }

    [Fact]
    public async Task ShouldNotRemoveAgency_WhenAgentAuthoritiesExist()
    {
        var account = await TestApplicantsDatabaseFactory.AddTestAgentAccount(
            _testDatabase,
            FixtureInstance,
            isOrganisation: true);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            agencyId: account.AgencyId,
            accountTypeExternal: account.AccountType,
            hasAcceptedTermsAndConditionsAndPrivacyPolicy: false);

        _testDatabase.AgentAuthorities.Add(new AgentAuthority
        {
            CreatedTimestamp = DateTime.UtcNow,
            ChangedTimestamp = DateTime.UtcNow,
            CreatedByUser = account,
            ChangedByUser = account,
            Status = AgentAuthorityStatus.Created,
            WoodlandOwner = FixtureInstance.Create<WoodlandOwner>(),
            Agency = account.Agency
        });

        await _testDatabase.SaveChangesAsync();

        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var result = await sut.UpdateAccountTypeAsync(
            _externalApplicant,
            new UserTypeModel
            {
                AccountType = AccountType.WoodlandOwner,
                IsOrganisation = false
            },
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);

        var storedAgencies = _testDatabase.Agencies.AsQueryable();

        Assert.Single(storedAgencies);

        Assert.NotNull(storedAccount.Agency);
        Assert.Null(storedAccount.WoodlandOwner);
    }

    [Fact]
    public async Task ShouldNotRemoveAgency_WhenUserIsInvited()
    {
        var account = await TestApplicantsDatabaseFactory.AddTestAgentAccount(
            _testDatabase,
            FixtureInstance,
            isOrganisation: true,
            invited: true);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            agencyId: account.AgencyId,
            accountTypeExternal: account.AccountType,
            hasAcceptedTermsAndConditionsAndPrivacyPolicy: false,
            invited: true);

        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var result = await sut.UpdateAccountTypeAsync(
            _externalApplicant,
            new UserTypeModel
            {
                AccountType = AccountType.WoodlandOwner,
                IsOrganisation = false
            },
            null,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        var storedAccount = await _testDatabase.UserAccounts.SingleAsync(x => x.IdentityProviderId == _externalApplicant.IdentityProviderId);

        var storedAgencies = _testDatabase.Agencies.AsQueryable();

        Assert.Single(storedAgencies);

        Assert.NotNull(storedAccount.Agency);
        Assert.Null(storedAccount.WoodlandOwner);
    }

    [Theory]
    [InlineData(UserAccountStatus.Migrated)]
    [InlineData(UserAccountStatus.Deactivated)]
    [InlineData(UserAccountStatus.Invited)]
    public async Task AccountSignupValidityCheckTests_WhenAccountIsFound(UserAccountStatus status)
    {
        var account = await TestApplicantsDatabaseFactory.AddTestWoodlandOwnerAccount(
            _testDatabase,
            FixtureInstance,
            acceptedPrivacyPolicy: false,
            acceptedTAndCs: false,
            userAccountStatus: status);

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            agencyId: account.AgencyId,
            accountTypeExternal: account.AccountType,
            hasAcceptedTermsAndConditionsAndPrivacyPolicy: false);

        _userAccountRepository.Setup(r => r.GetByEmailAsync(It.Is<string>(d => d == account.Email), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));

        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var result = await sut.AccountSignupValidityCheck(
            _externalApplicant,
            CancellationToken.None);

        switch (status)
        {
            case UserAccountStatus.Migrated:
                Assert.True(result == AccountSignupValidityCheckOutcome.IsMigratedUser);
                break;
            case UserAccountStatus.Invited:
                Assert.True(result == AccountSignupValidityCheckOutcome.IsAlreadyInvited);
                break;
            case UserAccountStatus.Deactivated:
                Assert.True(result == AccountSignupValidityCheckOutcome.IsDeactivated);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }

    [Fact]
    public async Task AccountSignupValidityCheckTests_WhenAccountIsNotFound()
    {
        _userAccountRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            hasAcceptedTermsAndConditionsAndPrivacyPolicy: false);

        _externalApplicant = new ExternalApplicant(user);

        var sut = CreateSut();

        var result = await sut.AccountSignupValidityCheck(
            _externalApplicant,
            CancellationToken.None);

        Assert.True(result == AccountSignupValidityCheckOutcome.IsValidSignUp);
    }

    [Theory, AutoMoqData]
    public async Task ShouldRevertAgencyToIndividual_GivenAgencyDetails(UserAccount account)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.AgentAdministrator;
        _userAccountRepository.Setup(r => r.GetAsync(It.Is<Guid>(d => d == account.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: account.Agency?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.RevertAgencyOrganisationToIndividualAsync(_externalApplicant, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        _userAccountRepository.Verify(r => r.Update(It.Is<UserAccount>(u =>
            u.Id == account.Id
            && u.Agency != null
            && u.Agency.IsOrganisation == false
            && u.Agency.OrganisationName == null)));
        _unitOfWOrkMock.Verify(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                e.EventName == AuditEvents.UpdateAccountEvent
                && e.SourceEntityId == account.Id
                && e.ActorType == ActorType.ExternalApplicant), It.IsAny<CancellationToken>()));
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotRevertAgencyToIndividual_GivenUserIsNotAgencyAdmin(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.Agent;
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: account.Agency?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.RevertAgencyOrganisationToIndividualAsync(_externalApplicant, CancellationToken.None);

        //assert
        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotRevertAgencyToIndividual_GivenUserIsNotLinkedToAgency(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.Agent;
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: null);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.RevertAgencyOrganisationToIndividualAsync(_externalApplicant, CancellationToken.None);

        //assert
        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotRevertAgencyToIndividual_GivenUserDoesNotHaveLocalAccount(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.AgentAdministrator;
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            null,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: null);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.RevertAgencyOrganisationToIndividualAsync(_externalApplicant, CancellationToken.None);

        //assert
        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotRevertAgencyToIndividual_GivenNotExistingAgencyDetails(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.AgentAdministrator;
        _userAccountRepository.Setup(r => r.GetAsync(It.Is<Guid>(d => d == account.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: account.Agency?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.RevertAgencyOrganisationToIndividualAsync(_externalApplicant, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        _mockAuditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                e.EventName == AuditEvents.UpdateAccountFailureEvent && e.ActorType == ActorType.ExternalApplicant), It.IsAny<CancellationToken>()));
    }



    [Theory, AutoMoqData]
    public async Task ShouldReturnError_WhenRevertAgencyToIndividual_GivenFailedUpdateResult(UserAccount account, AgencyModel agencyModel)
    {
        //arrange
        account.Agency = new Agency();
        account.AccountType = AccountTypeExternal.AgentAdministrator;
        _userAccountRepository.Setup(r => r.GetAsync(It.Is<Guid>(d => d == account.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(account));
        _unitOfWOrkMock.Setup(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => UnitResult.Failure(UserDbErrorReason.General));
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            account.IdentityProviderId,
            account.Email,
            account.Id,
            account.WoodlandOwner?.Id,
            accountTypeExternal: account.AccountType,
            agencyId: account.Agency?.Id);
        _externalApplicant = new ExternalApplicant(user);
        var sut = CreateSut();

        //act
        var result = await sut.RevertAgencyOrganisationToIndividualAsync(_externalApplicant, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        _mockAuditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                e.EventName == AuditEvents.UpdateAccountFailureEvent
                && e.ActorType == ActorType.ExternalApplicant), It.IsAny<CancellationToken>()));
    }

    private RegisterUserAccountUseCase CreateSut()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockFileStorageService = new Mock<IFileStorageService>();
        _userAccountRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
        _mockAuditService.Reset();

        var userAccountRepository = new UserAccountRepository(_testDatabase);

        return new RegisterUserAccountUseCase(
            TestDatabaseContextFactory<ApplicantsContext>.CreateDefaultTestContextFactory(_testDatabase),
            _mockHttpContextAccessor.Object,
            new SignInApplicantWithEf(
                userAccountRepository,
                new InvitedUserValidator(new NullLogger<InvitedUserValidator>(),
                    _fixedTimeClock),
                new NullLogger<SignInApplicantWithEf>(), 
                _mockAuditServiceForSignIn.Object,
                Options.Create(new FcAgencyOptions { PermittedEmailDomainsForFcAgent = new List<string> {"qxlva.com","forestrycommission.gov.uk"}}),
                Options.Create(new AuthenticationOptions { Provider = AuthenticationProvider.OneLogin}),
                new RequestContext("test", new RequestUserModel(_externalApplicant.Principal))),
            _fixedTimeClock,
            _mockAuditService.Object,
            new NullLogger<RegisterUserAccountUseCase>(),
            _mockPropertyProfileRepository.Object,
            _mockFellingLicenceApplicationRepository.Object,
            _userAccountRepository.Object,
            Options.Create(new FcAgencyOptions { PermittedEmailDomainsForFcAgent = new List<string> {"qxlva.com","forestrycommission.gov.uk"}}),
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
            new AgentAuthorityService(
                new AgencyRepository(_testDatabase), 
                userAccountRepository,
                mockFileStorageService.Object, 
                new FileTypesProvider(),
                _fixedTimeClock, 
                new NullLogger<AgentAuthorityService>()));
    }
}
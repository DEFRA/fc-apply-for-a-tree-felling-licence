using System.Security.Claims;
using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Configuration;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Testing;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class SignInApplicantWithEfTests
{
    private static readonly Fixture FixtureInstance = new();
    private readonly Mock<IUserAccountRepository> _userAccountRepository;
    private readonly Mock<IAuditService<SignInApplicantWithEf>> _mockAuditService;
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IClock _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
    private readonly Mock<IOptions<AuthenticationOptions>> _mockAuthenticationOptions = new();

    public SignInApplicantWithEfTests()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        _userAccountRepository = new Mock<IUserAccountRepository>();
        _userAccountRepository.SetupGet(r => r.UnitOfWork).Returns(unitOfWork.Object);
        _mockAuditService = new Mock<IAuditService<SignInApplicantWithEf>>();
    }

    [Theory]
    [CombinatorialData]
    public async Task WhenNoLocalUserAccountFound(AuthenticationProvider provider)
    {
        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(provider: provider);
        _userAccountRepository.Setup(r => r.GetByUserIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Failure<UserAccount,UserDbErrorReason>(UserDbErrorReason.NotFound));
        
        var sut = CreateSut(provider);
        await sut.HandleUserLoginAsync(principal, null, CancellationToken.None);

        Assert.DoesNotContain(principal.Identities, identity => identity.AuthenticationType == FloClaimTypes.ClaimsIdentityAuthenticationType);
    }

    [Theory]
    [CombinatorialData]
    public async Task WhenLocalAccountIsFound(AuthenticationProvider provider)
    {
        var identityProviderId = FixtureInstance.Create<string>();
        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(identityProviderId, provider: provider);

        var localAccount = FixtureInstance.Create<UserAccount>();
        localAccount.IdentityProviderId = identityProviderId;
        localAccount.Agency.IsFcAgency = false;
        _userAccountRepository.Setup(r => r.GetByUserIdentifierAsync(identityProviderId, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Success<UserAccount,UserDbErrorReason>(localAccount));

        var sut = CreateSut(provider);
        await sut.HandleUserLoginAsync(principal, null, CancellationToken.None);

        Assert.Contains(principal.Identities, identity => identity.AuthenticationType == FloClaimTypes.ClaimsIdentityAuthenticationType);

        Assert.Equal(localAccount.Id.ToString(), principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.LocalAccountId)?.Value);
        Assert.Equal(localAccount.WoodlandOwner?.Id.ToString(), principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.WoodlandOwnerId)?.Value);
        Assert.Equal(localAccount.AccountType.ToString(), principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.AccountType)?.Value);
    }

    [Theory]
    [CombinatorialData]
    public async Task ShouldAcceptInvitedUser_GivenInvitedUserFromB2C(AuthenticationProvider provider)
    {
        var userAccount = FixtureInstance.Create<UserAccount>();

        //arrange
        var token = Guid.NewGuid();
        userAccount.InviteToken = token;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(userAccount.IdentityProviderId, userAccount.Email, provider: provider);
        _userAccountRepository.Setup(r => r.GetByUserIdentifierAsync(userAccount.IdentityProviderId!, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Failure<UserAccount,UserDbErrorReason>(UserDbErrorReason.NotFound));
        _userAccountRepository.Setup(r => r.GetByEmailAsync(userAccount.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount,UserDbErrorReason>(userAccount));

        var sut = CreateSut(provider);

        //act
        await sut.HandleUserLoginAsync(principal, token.ToString(), CancellationToken.None);

        //assert
        _userAccountRepository.Verify(r =>
            r.Update(It.Is<UserAccount>(u => u.IdentityProviderId == userAccount.IdentityProviderId
            && u.Status == UserAccountStatus.Active
            && u.Email == userAccount.Email)));
        _mockAuditService.Verify(a =>
            a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                e.UserId == userAccount.Id
                && e.EventName == AuditEvents.AcceptInvitationSuccess), It.IsAny<CancellationToken>()));
    }

    [Theory]
    [CombinatorialData]
    public async Task ShouldNotAcceptInvitedUser_GivenNotFoundInvitedUserFromB2C(AuthenticationProvider provider)
    {
        var userAccount = FixtureInstance.Create<UserAccount>();
        //arrange
        var token = Guid.NewGuid();
        userAccount.InviteToken = token;
        userAccount.Status = UserAccountStatus.Invited;
        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(userAccount.IdentityProviderId, userAccount.Email, provider: provider);
        _userAccountRepository.Setup(r => r.GetByUserIdentifierAsync(userAccount.IdentityProviderId!, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Failure<UserAccount,UserDbErrorReason>(UserDbErrorReason.NotFound));
        _userAccountRepository.Setup(r => r.GetByEmailAsync(userAccount.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount,UserDbErrorReason>(UserDbErrorReason.NotFound));

        var sut = CreateSut(provider);
        //act
        await sut.HandleUserLoginAsync(principal, token.ToString(), CancellationToken.None);

        //assert
        _userAccountRepository.Verify(r =>
            r.Update(It.IsAny<UserAccount>()), Times.Never);
        _mockAuditService.Verify(a =>
            a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                e.UserId == null
                && e.EventName == AuditEvents.AcceptInvitationFailure), It.IsAny<CancellationToken>()));
    }

    [Theory]
    [CombinatorialData]
    public async Task ShouldExtractClaim_GivenInvitedUserFromB2C(AuthenticationProvider provider)
    {
        var userAccount = FixtureInstance.Create<UserAccount>();
        //arrange
        var token = Guid.NewGuid();
        userAccount.InviteToken = token;
        userAccount.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        userAccount.Status = UserAccountStatus.Invited;
        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(userAccount.IdentityProviderId, userAccount.Email, provider: provider);
        _userAccountRepository.Setup(r => r.GetByUserIdentifierAsync(userAccount.IdentityProviderId!, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Failure<UserAccount,UserDbErrorReason>(UserDbErrorReason.NotFound));
        _userAccountRepository.Setup(r => r.GetByEmailAsync(userAccount.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount,UserDbErrorReason>(userAccount));

        var sut = CreateSut(provider);
        //act
        await sut.HandleUserLoginAsync(principal, token.ToString(), CancellationToken.None);

        //assert
        Assert.Equal(userAccount.Id.ToString(), principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.LocalAccountId)?.Value);
        Assert.Equal(userAccount.WoodlandOwner?.Id.ToString(), principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.WoodlandOwnerId)?.Value);
        Assert.Equal(userAccount.AccountType.ToString(), principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.AccountType)?.Value);
    }

    [Theory]
    [InlineData("test@qxlva.com", AccountTypeExternal.FcUser, true, true)]
    [InlineData("test@qxlva.com", AccountTypeExternal.FcUser, true, true)]
    [InlineData("test@forestrycommission.gov.uk", AccountTypeExternal.FcUser, true, true)]
    [InlineData("test@forestrycommission.GOV.uk", AccountTypeExternal.FcUser, true, true)]
    [InlineData("test@qxlva.com", AccountTypeExternal.AgentAdministrator, false, false)]
    [InlineData("test@forestrycommission.gov.uk", AccountTypeExternal.Agent, false, false)]
    [InlineData("test@qxlva.com", AccountTypeExternal.WoodlandOwner, true, false)]
    [InlineData("test@qxlva.com", AccountTypeExternal.WoodlandOwnerAdministrator, true, false)]

    public async Task OnlyUsersWithCorrectAccountTypeAndEmailDomain_CanHaveFcUserClaim(
        string userEmailAddress, 
        AccountTypeExternal accountType,
        bool isTheFcAgency, 
        bool shouldHaveExpectedClaim)
    {
        var identityProviderId = FixtureInstance.Create<string>();
      
        var localAccount = FixtureInstance.Create<UserAccount>();
        localAccount.AccountType = accountType;
        localAccount.Email = userEmailAddress;
        localAccount.Agency = FixtureInstance.Create<Agency>();
        localAccount.Agency.IsFcAgency = isTheFcAgency;

        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(identityProviderId);
        
        localAccount.IdentityProviderId = identityProviderId;
        _userAccountRepository.Setup(r => r.GetByUserIdentifierAsync(identityProviderId, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Success<UserAccount,UserDbErrorReason>(localAccount));

        var sut = CreateSut(AuthenticationProvider.OneLogin);
        await sut.HandleUserLoginAsync(principal, null, CancellationToken.None);

        Assert.True(shouldHaveExpectedClaim
            ? bool.Parse(principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.FcUser)!.Value)
            : principal.Claims.NotAny(x => x.Type == FloClaimTypes.FcUser));
    }

    [Theory]
    [CombinatorialData]
    public async Task WhenLocalUserEmailAddressDomainIsNotOnePermittedToBeAnFCUser_Then_ShouldThrowError(AuthenticationProvider provider)
    {
        var identityProviderId = FixtureInstance.Create<string>();
      
        var localAccount = FixtureInstance.Create<UserAccount>();
        localAccount.AccountType = AccountTypeExternal.FcUser;
        localAccount.Email = "test@somesite.com";
        localAccount.Agency = FixtureInstance.Create<Agency>();
        localAccount.Agency.IsFcAgency = true;

        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(identityProviderId, provider: provider);
        
        localAccount.IdentityProviderId = identityProviderId;
        _userAccountRepository.Setup(r => r.GetByUserIdentifierAsync(identityProviderId, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Success<UserAccount,UserDbErrorReason>(localAccount));

        var sut = CreateSut(provider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleUserLoginAsync(principal, null, CancellationToken.None));
    }

    private SignInApplicantWithEf CreateSut(AuthenticationProvider provider)
    {
        SetAuthenticationScheme(provider);

        return new SignInApplicantWithEf(
            _userAccountRepository.Object,
            new InvitedUserValidator(new NullLogger<InvitedUserValidator>(),
                _fixedTimeClock),
            new NullLogger<SignInApplicantWithEf>(),
            _mockAuditService.Object,
            Options.Create(new FcAgencyOptions { PermittedEmailDomainsForFcAgent = new List<string> { "qxlva.com", "forestrycommission.gov.uk" } }),
            _mockAuthenticationOptions.Object,
            new RequestContext("test", new RequestUserModel(new ClaimsPrincipal())));
    }
    private void SetAuthenticationScheme(AuthenticationProvider provider)
    {
        _mockAuthenticationOptions.Reset();
        _mockAuthenticationOptions.SetupGet(a => a.Value).Returns(new AuthenticationOptions
        {
            Provider = provider
        });
    }
}
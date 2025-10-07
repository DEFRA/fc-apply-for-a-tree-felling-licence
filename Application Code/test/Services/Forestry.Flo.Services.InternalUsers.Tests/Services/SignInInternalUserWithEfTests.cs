using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.InternalUsers.Configuration;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Forestry.Flo.Services.InternalUsers.Tests.Services;

public class SignInInternalUserWithEfTests
{
    private static readonly Fixture FixtureInstance = new();
    private readonly Mock<IUserAccountService> _userAccountService;
    private readonly Mock<IUserAccountRepository> _userAccountRepository;
    private readonly Mock<IAuditService<SignInInternalUserWithEf>> _mockAuditService;
    private readonly Mock<IOptions<AuthenticationOptions>> _mockAuthenticationOptions = new();

    public SignInInternalUserWithEfTests()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        _userAccountService = new Mock<IUserAccountService>();
        _userAccountRepository = new Mock<IUserAccountRepository>();
        _userAccountRepository.SetupGet(r => r.UnitOfWork).Returns(unitOfWork.Object);
        _mockAuditService = new Mock<IAuditService<SignInInternalUserWithEf>>();
    }

    [Theory, CombinatorialData]
    public async Task WhenNoLocalUserAccountFound(AuthenticationProvider provider)
    {
        var identityProviderId = FixtureInstance.Create<string>();
        const string email = "example@forestrycommission.gov.uk";

        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            identityProviderId: identityProviderId, 
            emailAddress: email,
            provider: provider);
        
        _userAccountRepository.Setup(r => r.GetByIdentityProviderIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Failure<UserAccount,UserDbErrorReason>(UserDbErrorReason.NotFound));

        _userAccountService.Setup(r => r.CreateFcUserAccountAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(
            new UserAccount
            {
                IdentityProviderId = identityProviderId,
                Email = email
            });

        var sut = CreateSut(provider);

        await sut.HandleUserLoginAsync(principal, null, CancellationToken.None);

        Assert.Contains(principal.Identities, identity => identity.AuthenticationType == FloClaimTypes.ClaimsIdentityAuthenticationType);
        
        _userAccountService.Verify(r => r.CreateFcUserAccountAsync(identityProviderId, email), Times.Once);
    }

    [Theory, CombinatorialData]
    public async Task WhenNoLocalUserAccountFoundForInvalidEmailDomain(AuthenticationProvider provider)
    {
        var identityProviderId = FixtureInstance.Create<string>();
        const string email = "example@gmail.com";

        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            identityProviderId: identityProviderId,
            emailAddress: email,
            provider: provider);

        _userAccountRepository.Setup(r => r.GetByIdentityProviderIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var sut = CreateSut(provider);
        await sut.HandleUserLoginAsync(principal, null, CancellationToken.None);

        Assert.DoesNotContain(principal.Identities, identity => identity.AuthenticationType == FloClaimTypes.ClaimsIdentityAuthenticationType);

        _userAccountService.Verify(r => r.CreateFcUserAccountAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory, CombinatorialData]
    public async Task WhenLocalAccountIsFound(AuthenticationProvider provider)
    {
        var identityProviderId = FixtureInstance.Create<string>();
        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            identityProviderId,
            provider: provider);

        var localAccount = FixtureInstance.Create<UserAccount>();
        localAccount.IdentityProviderId = identityProviderId;
        _userAccountRepository.Setup(r => r.GetByIdentityProviderIdAsync(identityProviderId, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Success<UserAccount,UserDbErrorReason>(localAccount));

        var sut = CreateSut(provider);
        await sut.HandleUserLoginAsync(principal, null, CancellationToken.None);

        Assert.Contains(principal.Identities, identity => identity.AuthenticationType == FloClaimTypes.ClaimsIdentityAuthenticationType);

        Assert.Equal(localAccount.Id.ToString(), principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.LocalAccountId)?.Value);
        Assert.Equal(localAccount.AccountType.ToString(), principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.AccountType)?.Value);
    }

    private void SetAuthenticationScheme(AuthenticationProvider provider)
    {
        _mockAuthenticationOptions.Reset();
        _mockAuthenticationOptions.SetupGet(a => a.Value).Returns(new AuthenticationOptions
        {
            Provider = provider
        });
    }

    private SignInInternalUserWithEf CreateSut(AuthenticationProvider provider)
    {
        SetAuthenticationScheme(provider);

        return new SignInInternalUserWithEf(
            _userAccountRepository.Object,
            _userAccountService.Object,
            new OptionsWrapper<PermittedRegisteredUserOptions>(new PermittedRegisteredUserOptions()), new NullLogger<SignInInternalUserWithEf>(),
            _mockAuthenticationOptions.Object,
            _mockAuditService.Object);
    }
}
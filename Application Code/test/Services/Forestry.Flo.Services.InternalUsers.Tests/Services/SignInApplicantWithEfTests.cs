using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.InternalUsers.Configuration;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Forestry.Flo.Services.InternalUsers.Tests.Services;

public class SignInApplicantWithEfTests
{
    private static readonly Fixture FixtureInstance = new();

    private readonly ISignInInternalUser _sut;
    private readonly Mock<IUserAccountService> _userAccountService;
    private readonly Mock<IUserAccountRepository> _userAccountRepository;
    private readonly Mock<IAuditService<SignInInternalUserWithEf>> _mockAuditService;

    public SignInApplicantWithEfTests()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        _userAccountService = new Mock<IUserAccountService>();
        _userAccountRepository = new Mock<IUserAccountRepository>();
        _userAccountRepository.SetupGet(r => r.UnitOfWork).Returns(unitOfWork.Object);
        _mockAuditService = new Mock<IAuditService<SignInInternalUserWithEf>>();
        
        _sut = new SignInInternalUserWithEf(_userAccountRepository.Object, _userAccountService.Object, new OptionsWrapper<PermittedRegisteredUserOptions>(new PermittedRegisteredUserOptions()), new NullLogger<SignInInternalUserWithEf>(), _mockAuditService.Object);
    }

    [Theory, AutoMoqData]
    public async Task WhenNoLocalUserAccountFound(string identityProviderId)
    {
        const string email = "example@forestrycommission.gov.uk";

        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            identityProviderId: identityProviderId, 
            emailAddress: email);
        
        _userAccountRepository.Setup(r => r.GetByIdentityProviderIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount,UserDbErrorReason>(UserDbErrorReason.NotFound));

        _userAccountService.Setup(r => r.CreateFcUserAccountAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(
            new UserAccount
            {
                IdentityProviderId = identityProviderId,
                Email = email
            });

        await _sut.HandleUserLoginAsync(principal, null, CancellationToken.None);

        Assert.Contains(principal.Identities, identity => identity.AuthenticationType == "flo");
        
        _userAccountService.Verify(r => r.CreateFcUserAccountAsync(identityProviderId, email), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task WhenNoLocalUserAccountFoundForInvalidEmailDomain(string identityProviderId)
    {
        const string email = "example@gmail.com";

        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            identityProviderId: identityProviderId,
            emailAddress: email);

        _userAccountRepository.Setup(r => r.GetByIdentityProviderIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        await _sut.HandleUserLoginAsync(principal, null, CancellationToken.None);

        Assert.DoesNotContain(principal.Identities, identity => identity.AuthenticationType == "flo");

        _userAccountService.Verify(r => r.CreateFcUserAccountAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WhenLocalAccountIsFound()
    {
        var identityProviderId = FixtureInstance.Create<string>();
        var principal = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(identityProviderId);

        var localAccount = FixtureInstance.Create<UserAccount>();
        localAccount.IdentityProviderId = identityProviderId;
        _userAccountRepository.Setup(r => r.GetByIdentityProviderIdAsync(identityProviderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount,UserDbErrorReason>(localAccount));

        await _sut.HandleUserLoginAsync(principal, null, CancellationToken.None);

        Assert.Contains(principal.Identities, identity => identity.AuthenticationType == "flo");

        Assert.Equal(localAccount.Id.ToString(), principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.LocalAccountId)?.Value);
        Assert.Equal(localAccount.AccountType.ToString(), principal.Claims.SingleOrDefault(x => x.Type == FloClaimTypes.AccountType)?.Value);
    }
}
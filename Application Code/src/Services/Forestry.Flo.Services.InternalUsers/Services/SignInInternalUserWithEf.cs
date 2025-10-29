using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.InternalUsers.Configuration;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Forestry.Flo.Services.InternalUsers.Services;

/// <summary>
/// Implementation of <see cref="ISignInInternalUser"/>.
/// </summary>
public class SignInInternalUserWithEf : ISignInInternalUser
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IUserAccountService _userAccountService;
    private readonly PermittedRegisteredUserOptions _permittedUserOptions;
    private readonly AuthenticationOptions _authenticationOptions;
    private readonly ILogger<SignInInternalUserWithEf> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="SignInInternalUserWithEf"/>.
    /// </summary>
    /// <param name="userAccountRepository">A repository to manage internal user accounts.</param>
    /// <param name="userAccountService">A service to manage internal user accounts.</param>
    /// <param name="permittedUserOptions">Configuration options for internal user accounts.</param>
    /// <param name="logger">A logging instance.</param>
    /// <param name="authenticationOptions">Configuration options for the identity provider.</param>
    public SignInInternalUserWithEf(
        IUserAccountRepository userAccountRepository,
        IUserAccountService userAccountService,
        IOptions<PermittedRegisteredUserOptions> permittedUserOptions,
        ILogger<SignInInternalUserWithEf> logger,
        IOptions<AuthenticationOptions> authenticationOptions)
    {
        ArgumentNullException.ThrowIfNull(userAccountRepository);
        ArgumentNullException.ThrowIfNull(userAccountService);
        ArgumentNullException.ThrowIfNull(permittedUserOptions);
        ArgumentNullException.ThrowIfNull(authenticationOptions.Value);

        _userAccountRepository = userAccountRepository;
        _userAccountService = userAccountService;
        _permittedUserOptions = permittedUserOptions.Value;
        _logger = logger;
        _authenticationOptions = authenticationOptions.Value;
    }

    /// <inheritdoc />
    public async Task HandleUserLoginAsync(ClaimsPrincipal user, string? inviteToken, CancellationToken cancellationToken = default)
    {
        var userIdentifier = GetIdentityProviderId(user.Claims);
        var email = GetEmailFromClaims(user.Claims);

        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            return;
        }

        var result = await _userAccountRepository.GetByIdentityProviderIdAsync(userIdentifier, cancellationToken, email);

        if (result.IsSuccess)
        {
            var claimsIdentity = CreateClaimsIdentityFromUserAccount(result.Value);

            user.AddIdentity(claimsIdentity);
        }
        else
        {
            if (result.Error == UserDbErrorReason.NotFound)
            {
                var domain = email?.Split("@")[1];
                if (!_permittedUserOptions.PermittedEmailDomainsForRegisteredUser
                        .Any(x => x.Equals(domain, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.LogWarning("User with unauthorised email domain {Domain} is attempting to log in to Internal app", domain);
                    return;
                }

                var userAccount = await _userAccountService.CreateFcUserAccountAsync(userIdentifier, email!);

                var claimsIdentity = CreateClaimsIdentityFromUserAccount(userAccount);

                user.AddIdentity(claimsIdentity);
            }
            else
            {
                string errorMessage = $"Unexpected failure fetching user account in {nameof(HandleUserLoginAsync)}";

                throw new Exception(errorMessage);
            }
        }
    }

    /// <inheritdoc />
    public ClaimsIdentity CreateClaimsIdentityFromUserAccount(UserAccount userAccount)
    {
        var claims = new List<Claim>();

        AddIfNotNull(claims, FloClaimTypes.LocalAccountId, userAccount.Id.ToString());
        AddIfNotNull(claims, FloClaimTypes.AccountType, userAccount.AccountType.ToString());
        AddIfNotNull(claims, FloClaimTypes.AccountTypeOther, userAccount.AccountTypeOther?.ToString());
        AddIfNotNull(claims, FloClaimTypes.UserName, userAccount.FullName());
        AddIfNotNull(claims, FloClaimTypes.UserCanApproveApplications, userAccount.CanApproveApplications.ToString());

        return new ClaimsIdentity(claims, FloClaimTypes.ClaimsIdentityAuthenticationType);
    }

    private void AddIfNotNull(List<Claim> list, string claimType, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _logger.LogDebug("Skipping claim {ClaimType} as no value was found within local database", claimType);
        }
        else
        {
            _logger.LogDebug("Adding claim {ClaimType} as with value {ClaimValue} read from local database", claimType, value);

            list.Add(new Claim(claimType, value));
        }
    }

    private string? GetEmailFromClaims(IEnumerable<Claim> claims)
    {
        var claimType = _authenticationOptions.Provider switch
        {
            AuthenticationProvider.Azure => FloClaimTypes.Email,
            AuthenticationProvider.OneLogin => OneLoginPrincipalClaimTypes.EmailAddress,
            _ => ClaimTypes.Email,
        };

        return claims.SingleOrDefault(x => x.Type == claimType)?.Value;
    }

    private string? GetIdentityProviderId(IEnumerable<Claim> claims)
    {
        var claimType = _authenticationOptions.Provider switch
        {
            AuthenticationProvider.OneLogin => OneLoginPrincipalClaimTypes.NameIdentifier,
            _ => ClaimTypes.NameIdentifier,
        };

        return claims.SingleOrDefault(x => x.Type == claimType)?.Value;
    }
}
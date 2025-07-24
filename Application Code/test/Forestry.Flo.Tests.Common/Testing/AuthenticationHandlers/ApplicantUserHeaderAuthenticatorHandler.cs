using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;

namespace Forestry.Flo.Tests.Common.Testing.AuthenticationHandlers;

/// <summary>
/// This is a custom authentication implementation that is only wired-up within the development environment.
/// </summary>
public class ApplicantUserHeaderAuthenticatorHandler : AuthenticationHandler<HeaderAuthenticationOptions>
{
    private readonly IOptionsMonitor<HeaderAuthenticationOptions> _options;
    private readonly ILogger _logger;
    private readonly UrlEncoder _encoder;
    private readonly ISystemClock _clock;

    /// <inheritdoc />
    public ApplicantUserHeaderAuthenticatorHandler(
        IOptionsMonitor<HeaderAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger.CreateLogger(typeof(InternalUserHeaderAuthenticatorHandler));
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headerKey = _options.CurrentValue.Header;
        var headerLocalUserIdKey = _options.CurrentValue.HeaderLocalUserId;
        _logger.LogDebug("Starting authentication check using HTTP Header value {HeaderKey}", headerKey);

        if (Request.Headers.ContainsKey(headerKey))
        {
            var testUserIdentityProvider = Request.Headers[headerKey].First();
            var testLocalUserIdValue = Request.Headers[headerLocalUserIdKey].First();

            var testUserAccount = UserFactory
                .CreateExternalApplicantIdentityProviderClaimsPrincipal(
                    identityProviderId: testUserIdentityProvider,
                    localAccountId: Guid.Parse(testLocalUserIdValue),
                    woodlandOwnerId: Guid.NewGuid());

            _logger.LogInformation("Creating applicant authenticated user with username {Username}", testUserAccount.Identity!.Name);
            var result = AuthenticateResult.Success(new AuthenticationTicket(testUserAccount, Scheme.Name));
            return Task.FromResult(result);

        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
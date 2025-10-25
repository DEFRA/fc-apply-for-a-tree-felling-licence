using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.InternalUsers;
using Forestry.Flo.Services.InternalUsers.Configuration;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using GovUk.OneLogin.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using AuthenticationOptions = Forestry.Flo.Services.Common.Infrastructure.AuthenticationOptions;
using ClaimsPrincipalExtensions = Forestry.Flo.Internal.Web.Infrastructure.ClaimsPrincipalExtensions;

namespace Forestry.Flo.Internal.Web.Middleware
{
    public class UserAccountValidationMiddleware : IMiddleware
    {
        private IDbContextFactory<InternalUsersContext> _dbContextFactory;
        private IUserAccountService _userAccountService;
        private readonly ILogger<UserAccountValidationMiddleware> _logger;
        private readonly PermittedRegisteredUserOptions _permittedRegisteredUserOptions;
        private readonly AuthenticationOptions _authenticationOptions;

        private const string UserAccountRegisterAccountDetailsUrl = "/Account/RegisterAccountDetails";
        private const string UserAccountAwaitingConfirmationUrl = "/Account/UserAccountAwaitingConfirmation";
        private const string UserAccountClosedUrl = "/Home/AccountError";

        /// <summary>
        /// A collection of URLs that should not require user account validation.
        /// </summary>
        private static readonly string[] PermissibleUrls = 
        [
            "/Home/Logout", 
            "/Home/Error"
        ];

        public UserAccountValidationMiddleware(
            IDbContextFactory<InternalUsersContext> dbContextFactory, 
            IUserAccountService userAccountService, 
            IOptions<PermittedRegisteredUserOptions> permittedUserOptions,
            IOptions<AuthenticationOptions> authenticationOptions,
            ILogger<UserAccountValidationMiddleware> logger)
        {
            ArgumentNullException.ThrowIfNull(permittedUserOptions);

            _permittedRegisteredUserOptions = permittedUserOptions.Value;
            _dbContextFactory = dbContextFactory;
            _userAccountService = userAccountService;
            _logger = logger;
            _authenticationOptions = authenticationOptions.Value;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {            
            // don't run on permissible URLs
            if (PermissibleUrls.Any(url => context.Request.Path.StartsWithSegments(url, StringComparison.OrdinalIgnoreCase)))
            {
                await next(context);
                return;
            }


            if (context.User.Identity is not { IsAuthenticated: true })
            {
                // Handled by standard [Authorize] attributes
                await next(context);
                return;
            }

            var requestUrl = context.Request.GetDisplayUrl();

            CancellationToken cancellationToken = default;

            // determine if user email domain is permitted to access the system
            var email = GetEmailFromClaims(context.User.Claims);
            var domain = email?.Split("@")[1];
            if (!_permittedRegisteredUserOptions.PermittedEmailDomainsForRegisteredUser
                    .Any(x => x.Equals(domain, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogWarning("User with unauthorised email domain {Domain} is attempting to log in to Internal app", domain);
                if (!IsUserAccountErrorUrl(requestUrl))
                {
                    context.Response.Redirect(UserAccountClosedUrl);
                    return;
                }

                await next(context);
                return;
            }

            try
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                var identityProviderId = GetIdentityProviderId(context.User.Claims);

                // If user account cannot be identified by IdentityProviderId, we need to create a minimal record
                // Note that LocalAccountId is not persisted in claims between requests so use of IdentityProviderId is optimal

                var userAccount =
                    await dbContext.UserAccounts.SingleOrDefaultAsync(x => x.IdentityProviderId == identityProviderId,
                        cancellationToken);

                if (userAccount is null)
                {
                    // This should only occur if the user has existing authentication cookies but no user account record
                    // Cookies need to be cleared as sign in process needs to be triggered
                    context.Response.Headers.Append("Clear-Site-Data", "\"cookies\", \"storage\", \"cache\"");

                    switch (_authenticationOptions.Provider)
                    {
                        case AuthenticationProvider.OneLogin:
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            await context.SignOutAsync(OneLoginDefaults.AuthenticationScheme);
                            break;
                        case AuthenticationProvider.Azure:
                            await context.SignOutAsync("SignIn");
                            await context.SignOutAsync("SignUp");
                            break;
                        default:
                            await context.SignOutAsync();
                            break;
                    }

                    return;
                }

                if (UserAccountIsInvalid(userAccount!))
                {
                    if (!IsUserAccountErrorUrl(requestUrl))
                    {
                        context.Response.Redirect(UserAccountClosedUrl);
                        return;
                    }
                }
                else if (!UserRegistrationDetailsComplete(userAccount))
                {
                    if (!IsUserAccountRegisterAccountDetailsUrl(requestUrl))
                    {
                        // If user registration details not complete, redirect the user to update them.
                        // Placing check here allows us to add additional fields and run the check on later sign ins

                        context.Response.Redirect(UserAccountRegisterAccountDetailsUrl);
                        return;
                    }
                }
                else if (userAccount.Status is not Status.Confirmed)
                {
                    // Is FC user account type, but is not confirmed, return to awaiting confirmation screen

                    if (!IsUserAccountAwaitingConfirmationUrl(requestUrl))
                    {
                        // Account not confirmed, redirect to warning route

                        context.Response.Redirect(UserAccountAwaitingConfirmationUrl);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user account");
                context.Response.Redirect("/Home/Error");
                return;
            }

            await next(context);
        }

        private bool IsUserAccountRegisterAccountDetailsUrl(string url)
        {
            bool isUserAccountRegisterAccountDetailsUrl = url.Contains(UserAccountRegisterAccountDetailsUrl) || url.Contains("Home/Error");

            return isUserAccountRegisterAccountDetailsUrl;
        }

        private bool IsUserAccountAwaitingConfirmationUrl(string url)
        {
            bool isUserAccountValidationUrl = url.Contains(UserAccountAwaitingConfirmationUrl) || url.Contains("Home/Error");

            return isUserAccountValidationUrl;
        }

        private bool IsUserAccountErrorUrl(string url) => url.Contains(UserAccountClosedUrl) || url.Contains("Home/Logout");

        private bool UserRegistrationDetailsComplete(UserAccount userAccount)
        {
            return !string.IsNullOrWhiteSpace(userAccount.FirstName) && !string.IsNullOrWhiteSpace(userAccount.LastName);
        }

        private bool UserAccountIsInvalid(UserAccount userAccount)
        {
            return userAccount.Status is Status.Closed or Status.Denied;
        }

        private string? GetEmailFromClaims(IEnumerable<Claim> claims)
        {
            var claimType = _authenticationOptions.Provider switch
            {
                AuthenticationProvider.Azure => ClaimsPrincipalExtensions.IdentityProviderEmailClaimType,
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
}

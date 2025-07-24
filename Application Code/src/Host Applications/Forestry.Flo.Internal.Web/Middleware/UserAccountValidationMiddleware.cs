using System.Security.Claims;
using Forestry.Flo.Internal.Web.Extensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers;
using Forestry.Flo.Services.InternalUsers.Configuration;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Internal.Web.Middleware
{
    public class UserAccountValidationMiddleware : IMiddleware
    {
        private IDbContextFactory<InternalUsersContext> _dbContextFactory;
        private IUserAccountService _userAccountService;
        private readonly ILogger<UserAccountValidationMiddleware> _logger;
        private readonly PermittedRegisteredUserOptions _permittedRegisteredUserOptions;

        private const string UserAccountRegisterAccountDetailsUrl = "/Account/RegisterAccountDetails";
        private const string UserAccountAwaitingConfirmationUrl = "/Account/UserAccountAwaitingConfirmation";
        private const string UserAccountClosedUrl = "/Home/AccountError";

        public UserAccountValidationMiddleware(
            IDbContextFactory<InternalUsersContext> dbContextFactory, 
            IUserAccountService userAccountService, 
            IOptions<PermittedRegisteredUserOptions> permittedUserOptions,
            ILogger<UserAccountValidationMiddleware> logger)
        {
            ArgumentNullException.ThrowIfNull(permittedUserOptions);

            _permittedRegisteredUserOptions = permittedUserOptions.Value;
            _dbContextFactory = dbContextFactory;
            _userAccountService = userAccountService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {

            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                // Handled by standard [Authorize] attributes

                await next(context);
            }
            else
            {
                var requestUrl = context.Request.GetDisplayUrl();

                CancellationToken cancellationToken = default;

                // verify user logged into AD B2C has a valid email address
                var email = context.User.Claims.SingleOrDefault(x =>
                    x.Type == ClaimsPrincipalExtensions.IdentityProviderEmailClaimType)?.Value;
                var domain = email?.Split("@")[1];
                if (!_permittedRegisteredUserOptions.PermittedEmailDomainsForRegisteredUser
                        .Any(x => x.Equals(domain, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.LogWarning("User with unauthorised email domain {Domain} is attempting to log in to Internal app", domain);
                    if (!IsUserAccountErrorUrl(requestUrl))
                    {
                        context.Response.Redirect(UserAccountClosedUrl);
                    }
                }
                else
                {
                    await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                    var identityProviderId = context.User.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

                    // If user account cannot be identified by IdentityProviderId, we need to create a minimal record
                    // Note that LocalAccountId is not persisted in claims between requests so use of IdentityProviderId is optimal

                    var userAccount = await dbContext.UserAccounts.SingleOrDefaultAsync(x => x.IdentityProviderId == identityProviderId, cancellationToken);

                    if (userAccount == null)
                    {
                        await context.SignOutAsync();
                        throw new InvalidOperationException($"{nameof(userAccount)} is not expected to be null in {nameof(UserAccountValidationMiddleware)} {nameof(InvokeAsync)}");
                    }

                    if (UserAccountIsInvalid(userAccount))
                    {
                        if (!IsUserAccountErrorUrl(requestUrl))
                        {
                            context.Response.Redirect(UserAccountClosedUrl);
                        }
                    }
                    else if (!UserRegistrationDetailsComplete(userAccount))
                    {
                        if (!IsUserAccountRegisterAccountDetailsUrl(requestUrl))
                        {
                            // If user registration details not complete, redirect the user to update them.
                            // Placing check here allows us to add additional fields and run the check on later sign ins

                            context.Response.Redirect(UserAccountRegisterAccountDetailsUrl);
                        }
                    }
                    else if (userAccount.Status is not Status.Confirmed)
                    {
                        // Is FC user account type, but is not confirmed, return to awaiting confirmation screen

                        if (!IsUserAccountAwaitingConfirmationUrl(requestUrl))
                        {
                            // Account not confirmed, redirect to warning route

                            context.Response.Redirect(UserAccountAwaitingConfirmationUrl);
                        }
                    }
                }

                await next(context);
            }
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
    }
}

using Forestry.Flo.Services.InternalUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Internal.Web.Services
{
    /// <summary>
    /// Determine if user has role from permissible list of roles
    /// </summary>
    public class AuthorizationPolicyService
    {
        public static readonly Func<AuthorizationHandlerContext, IDbContextFactory<InternalUsersContext>, IList<Roles>, AuthenticationProvider, bool> 
            AssertHasAnyOfRoles = (context, dbContextFactory, permissibleRoles, provider) =>
        {
            bool hasAnyOfRoles = false;

            // Option to verify information on claims directly

            var identityProviderId = context.User.Claims.GetIdentityProviderId(provider);

            using var dbContext = dbContextFactory.CreateDbContext();

            if (identityProviderId != null)
            {
                var userAccount = dbContext.UserAccounts.SingleOrDefault(x => x.IdentityProviderId == identityProviderId);

                if (userAccount != null)
                {
                    hasAnyOfRoles = RolesService.RolesStringHasAnyOfRoles(userAccount.Roles, permissibleRoles);
                }
            }

            return hasAnyOfRoles;
        };
    }
}


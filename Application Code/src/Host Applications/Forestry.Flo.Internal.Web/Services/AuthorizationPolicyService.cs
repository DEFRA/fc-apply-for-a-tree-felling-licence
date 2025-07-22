using Forestry.Flo.Services.InternalUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Internal.Web.Services
{
    /// <summary>
    /// Determine if user has role from permissible list of roles
    /// </summary>
    public class AuthorizationPolicyService
    {
        public static Func<AuthorizationHandlerContext, IDbContextFactory<InternalUsersContext>, IList<Roles>, bool> AssertHasAnyOfRoles = (context, dbContextFactory, permissableRoles) =>
        {
            bool hasAnyOfRoles = false;

            // Option to verify information on claims directly

            var identityProviderId = context.User.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            using var dbContext = dbContextFactory.CreateDbContext();

            if (identityProviderId != null)
            {
                var userAccount = dbContext.UserAccounts.SingleOrDefault(x => x.IdentityProviderId == identityProviderId);

                if (userAccount != null)
                {
                    hasAnyOfRoles = RolesService.RolesStringHasAnyOfRoles(userAccount.Roles, permissableRoles);
                }
            }

            return hasAnyOfRoles;
        };
    }
}


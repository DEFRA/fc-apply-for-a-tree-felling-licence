using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using System.Security.Claims;

namespace Forestry.Flo.Services.Applicants.Services
{
    public static class ClaimsIdentityHelper
    {
        /// <summary>
        /// Generate a <see cref="ClaimsIdentity"/> representing the given applicant's <see cref="UserAccount"/>.
        /// </summary>
        /// <param name="userAccount">The local user account entity to generate claims for.</param>
        /// <param name="permittedEmailDomainsForFcAgent">The list of email domains which a users email address domain must exist in if
        /// to be given the FCUser claim when their Agent is the FC Super agent</param>
        /// <returns>The claims identity representing the given local user account.</returns>
        public static ClaimsIdentity CreateClaimsIdentityFromApplicantUserAccount(
            UserAccount userAccount,
            List<string> permittedEmailDomainsForFcAgent)
        {
            var claims = new List<Claim>();
            AddIfNotNull(claims, FloClaimTypes.LocalAccountId, userAccount.Id.ToString());
            AddIfNotNull(claims, FloClaimTypes.WoodlandOwnerId, userAccount.WoodlandOwner?.Id.ToString());
            AddIfNotNull(claims, FloClaimTypes.AgencyId, userAccount.AgencyId?.ToString());
            AddIfNotNull(claims, FloClaimTypes.AccountType, userAccount.AccountType.ToString());
            AddIfNotNull(claims, FloClaimTypes.AcceptedTermsAndConditions, userAccount.DateAcceptedTermsAndConditions.HasValue.ToString());
            AddIfNotNull(claims, FloClaimTypes.AcceptedPrivacyPolicy, userAccount.DateAcceptedPrivacyPolicy.HasValue.ToString());
            AddIfNotNull(claims, FloClaimTypes.AgencyId, userAccount.AgencyId.ToString());
            AddIfNotNull(claims, FloClaimTypes.UserName, userAccount.FullName());
            AddIfNotNull(claims, FloClaimTypes.Invited, userAccount.InviteToken.HasValue.ToString());
            AddIfNotNull(claims, FloClaimTypes.LastChanged, userAccount.LastChanged.ToString("o"));
            AddIfNotNull(claims, FloClaimTypes.AccountStatus, userAccount.Status.GetDisplayName());

            if (userAccount.AccountType is AccountTypeExternal.FcUser)
            {
                if (userAccount.Agency is { IsFcAgency: true })
                {
                    var userDomain = userAccount.Email.Split('@')[1];
                    if (permittedEmailDomainsForFcAgent.Any(x => x.Equals(userDomain, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        AddIfNotNull(claims, FloClaimTypes.FcUser, "true");
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Unable to grant FCUser Claim to this user account despite being linked to the FC Agency - " +
                            $"as the user email address [{userAccount.Email}] is not in the list of configured permitted email address domains.");
                    }
                }
            }

            string? woodlandOwnerName = null;
            if (userAccount.WoodlandOwner != null)
            {
                woodlandOwnerName = userAccount.WoodlandOwner!.IsOrganisation
                    ? userAccount.WoodlandOwner!.OrganisationName
                    : userAccount.WoodlandOwner!.ContactName;
            }
            AddIfNotNull(claims, FloClaimTypes.WoodlandOwnerName, woodlandOwnerName);

            return new ClaimsIdentity(claims, FloClaimTypes.ClaimsIdentityAuthenticationType);
        }

        private static void AddIfNotNull(List<Claim> list, string claimType, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                list.Add(new Claim(claimType, value));
            }
        }
    }
}

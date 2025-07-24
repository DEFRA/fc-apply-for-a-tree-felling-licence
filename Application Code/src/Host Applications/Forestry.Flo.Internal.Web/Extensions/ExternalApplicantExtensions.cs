using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Internal.Web.Extensions;

public static class ExternalApplicantExtensions
{
    public static ExternalApplicantType GetExternalApplicantType(this UserAccount externalAccount)
    {
        if (externalAccount.AccountType is AccountTypeExternal.WoodlandOwner or AccountTypeExternal.WoodlandOwnerAdministrator)
            return ExternalApplicantType.WoodlandOwner;

        if (externalAccount.AccountType is AccountTypeExternal.Agent or AccountTypeExternal.AgentAdministrator)
            return ExternalApplicantType.Agent;

        return ExternalApplicantType.FcStaffMember;
    }
}
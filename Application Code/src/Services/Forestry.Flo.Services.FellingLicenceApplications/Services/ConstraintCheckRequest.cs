using Forestry.Flo.Services.Common;
using System.Security.Claims;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class ConstraintCheckRequest
{
    public Guid ApplicationId { get; }
    public Guid UserAccountId { get; }
    public bool IsInternalUser { get; }

    private ConstraintCheckRequest(Guid applicationId, Guid userAccountId, bool isInternalUser)
    {
        ApplicationId = applicationId;
        UserAccountId = userAccountId;
        IsInternalUser = isInternalUser;
    }

    public static ConstraintCheckRequest Create(ClaimsPrincipal user, Guid applicationId)
    {
        var userId = user.Claims.FirstOrDefault(x => x.Type == FloClaimTypes.LocalAccountId)?.Value;
        var isInternal = user.Identities.Any(x => x.Label == FloClaimTypes.InternalUserLocalSourceClaimLabel);
        return new ConstraintCheckRequest(applicationId, Guid.Parse(userId!), isInternal);
    }
}
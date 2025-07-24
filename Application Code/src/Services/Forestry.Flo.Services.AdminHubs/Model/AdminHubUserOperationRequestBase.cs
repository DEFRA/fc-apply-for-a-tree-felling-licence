using Ardalis.GuardClauses;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.AdminHubs.Model;

public class AdminHubUserOperationRequestBase: AdminHubRequestBase
{
    /// <summary>
    /// The Unique Identifier of the Admin Hub to apply the operation to.
    /// </summary>
    public Guid AdminHubId { get; }

    /// <summary>
    /// The Unique Identifier of the Internal User to perform the operation on at the Admin Hub.
    /// </summary>
    public Guid UserId { get; }

    protected AdminHubUserOperationRequestBase(
        Guid performingUserId, 
        AccountTypeInternal performingPerformingUserAccountType, 
        Guid adminHubId, 
        Guid userId)
        : base(performingUserId, performingPerformingUserAccountType)
    {
        AdminHubId = Guard.Against.Null(adminHubId);
        UserId = Guard.Against.Null(userId);
    }

    protected AdminHubUserOperationRequestBase()
    {
    }
}
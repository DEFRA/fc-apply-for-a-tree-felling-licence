using Ardalis.GuardClauses;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.AdminHubs.Model;

public class AdminHubRequestBase
{
    /// <summary>
    /// The unique identifier of the internal user performing the operation.
    /// </summary>
    public Guid PerformingUserId { get; }

    /// <summary>
    /// The account type of the internal user performing the operation on the admin hub.
    /// </summary>
    public AccountTypeInternal PerformingUserAccountType { get; }

    protected AdminHubRequestBase(Guid performingUserId, AccountTypeInternal performingPerformingUserAccountType)
    {
        PerformingUserId = Guard.Against.Null(performingUserId);
        PerformingUserAccountType = Guard.Against.Null(performingPerformingUserAccountType);
    }

    protected AdminHubRequestBase()
    {
    }
}
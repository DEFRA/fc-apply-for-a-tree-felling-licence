namespace Forestry.Flo.Services.Common.Models;

/// <summary>
/// Representation of a user's access to woodland owners
/// </summary>
public class UserAccessModel
{
    /// <summary>
    /// Gets and inits the user account id.
    /// </summary>
    public Guid UserAccountId { get; init; }

    /// <summary>
    /// Gets and inits a flag indicating that the user is an FC user.
    /// </summary>
    public bool IsFcUser { get; init; }

    /// <summary>
    /// Gets and inits a list of woodland owner ids that this user can access.
    /// </summary>
    /// <remarks>
    /// Should be null if the user is an FC user - FC users can manage all woodland owners.
    /// </remarks>
    public List<Guid>? WoodlandOwnerIds { get; init; }

    /// <summary>
    /// Gets and inits the agency id of the user, if it is an agent user.
    /// </summary>
    public Guid? AgencyId { get; init; }


    /// <summary>
    /// Checks if the user represented by this <see cref="UserAccessModel"/> has access to manage the woodland
    /// owner with the provided id.
    /// </summary>
    /// <param name="woodlandOwnerId">The id of the woodland owner to check.</param>
    /// <returns>True if the user is allowed to access data for the woodland owner, otherwise false.</returns>
    public bool CanManageWoodlandOwner(Guid woodlandOwnerId) => IsFcUser
                                                                || (WoodlandOwnerIds is not null && WoodlandOwnerIds.Contains(woodlandOwnerId));
}
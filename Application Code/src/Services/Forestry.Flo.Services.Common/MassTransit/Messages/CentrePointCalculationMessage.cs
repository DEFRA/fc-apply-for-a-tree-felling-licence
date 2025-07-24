namespace Forestry.Flo.Services.Common.MassTransit.Messages;

/// <summary>
/// A message class containing the relevant properties for calculating centre points and executing various ESRI processes in a <see cref="CentrePointCalculationConsumer"/>
/// </summary>
public class CentrePointCalculationMessage
{
    /// <summary>
    /// Creates an instance of a <see cref="CentrePointCalculationMessage"/>.
    /// </summary>
    /// <param name="woodlandOwnerId">A textual representation of an identifier for the associated <see cref="WoodlandOwner"/></param>
    /// <param name="userId">An identifier for the <see cref="ExternalApplicant"/> requesting the submission.</param>
    /// <param name="applicationId">An identifier for the application.</param>
    /// <param name="isFcUser">A flag indicating whether the performing user is an FC User</param>
    /// <param name="agencyId">The agency Id of the user</param>
    public CentrePointCalculationMessage(
        Guid woodlandOwnerId,
        Guid userId,
        Guid applicationId,
        bool isFcUser,
        Guid? agencyId)
    {
        WoodlandOwnerId = woodlandOwnerId;
        UserId = userId;
        ApplicationId = applicationId;
        IsFcUser = isFcUser;
        AgencyId = agencyId;
    }

    /// <summary>
    /// Gets and inits an identifier for the relevant application.
    /// </summary>
    public Guid ApplicationId { get; }

    /// <summary>
    /// Gets and inits an identifier for the user that triggered centre point calculation.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets and inits an identifier for the woodland owner of the user that triggered centre point calculation.
    /// </summary>
    public Guid WoodlandOwnerId { get; }

    /// <summary>
    /// Gets and inits a flag indicating whether the submission from an FC User.
    /// </summary>
    public bool IsFcUser { get; }

    /// <summary>
    /// Gets and inits the AgencyId of this user, if there is one.
    /// </summary>
    public Guid? AgencyId { get; }
}
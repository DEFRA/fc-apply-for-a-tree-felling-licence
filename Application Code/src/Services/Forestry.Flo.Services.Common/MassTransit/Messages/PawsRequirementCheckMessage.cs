namespace Forestry.Flo.Services.Common.MassTransit.Messages;

/// <summary>
/// A message class containing the relevant properties for checking PAWS requirements in a <see cref="PawsRequirementCheckConsumer"/>
/// </summary>
public class PawsRequirementCheckMessage
{
    /// <summary>
    /// Creates an instance of a <see cref="PawsRequirementCheckMessage"/>.
    /// </summary>
    /// <param name="woodlandOwnerId">The identifier for the associated woodland owner of the context that triggered the message.</param>
    /// <param name="userId">An identifier for the external applicant user that triggered the message.</param>
    /// <param name="applicationId">An identifier for the application.</param>
    /// <param name="isFcUser">A flag indicating whether the user that triggered the message is an FC User.</param>
    /// <param name="agencyId">The agency Id of the user that triggered the message, if the user is an agent.</param>
    public PawsRequirementCheckMessage(
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
    /// Gets and inits an identifier for the user that triggered a PAWS requirement check.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets and inits an identifier for the woodland owner of the user that triggered a PAWS requirement check.
    /// </summary>
    public Guid WoodlandOwnerId { get; }

    /// <summary>
    /// Gets and inits a flag indicating whether the submission is from an FC User.
    /// </summary>
    public bool IsFcUser { get; }

    /// <summary>
    /// Gets and inits the AgencyId of this user, if there is one.
    /// </summary>
    public Guid? AgencyId { get; }
}
namespace Forestry.Flo.Services.Common.MassTransit.Messages;

/// <summary>
/// Event class representing the required detail of
/// a newly approved internal FC user.
/// </summary>
/// <remarks>This event class is used elsewhere in the system by the relevant consumer
/// class.</remarks>
public class InternalFcUserAccountApprovedEvent
{
    /// <summary>
    /// Gets and sets the email address of the FC internal user.
    /// </summary>
    public string EmailAddress { get; init; }

    /// <summary>
    /// Gets and sets the first name of the FC internal user.
    /// </summary>
    public string FirstName { get; init; }

    /// <summary>
    /// Gets and Sets the last name of the FC internal user.
    /// </summary>
    public string LastName { get; init; }

    /// <summary>
    /// Gets and Sets the AZ ADB2C Identity Provider of the FC internal user.
    /// </summary>
    public string IdentityProviderId { get; init; }

    /// <summary>
    /// The User Id of the internal user who approved the User Account
    /// </summary>
    public Guid ApprovedByInternalFcUserId { get; init; }

    public InternalFcUserAccountApprovedEvent(
        string emailAddress,
        string firstName,
        string lastName, 
        string identityProviderId,
        Guid approvedByInternalFcUserId)
    {
        EmailAddress = emailAddress;
        FirstName = firstName;
        LastName = lastName;
        IdentityProviderId = identityProviderId;
        ApprovedByInternalFcUserId = approvedByInternalFcUserId;
    }
}

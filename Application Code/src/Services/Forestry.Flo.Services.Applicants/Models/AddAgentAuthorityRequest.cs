using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing a request to create a new <see cref="AgentAuthority"/> within the system.
/// </summary>
public class AddAgentAuthorityRequest
{
    /// <summary>
    /// Gets and sets the id of the user adding the agent authority entry.
    /// </summary>
    public Guid CreatedByUser { get; set; }

    /// <summary>
    /// Gets and sets the details of the woodland owner for the agent authority entry.
    /// </summary>
    public WoodlandOwnerModel WoodlandOwner { get; set; }

    /// <summary>
    /// Gets and sets the id of the agency that this agent is working in.
    /// </summary>
    public Guid AgencyId { get; set; }

}
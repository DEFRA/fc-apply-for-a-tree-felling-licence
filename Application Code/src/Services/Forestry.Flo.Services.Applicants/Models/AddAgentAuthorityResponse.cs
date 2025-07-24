namespace Forestry.Flo.Services.Applicants.Models;

public class AddAgentAuthorityResponse
{
    /// <summary>
    /// Gets and sets the id of the agent authority entity.
    /// </summary>
    public Guid AgentAuthorityId { get; set; }

    /// <summary>
    /// Gets and sets the name of the Agency involved.
    /// </summary>
    public string AgencyName { get; set; }

    /// <summary>
    /// Gets and sets the id of the Agency involved.
    /// </summary>
    public Guid AgencyId { get; set; }
}
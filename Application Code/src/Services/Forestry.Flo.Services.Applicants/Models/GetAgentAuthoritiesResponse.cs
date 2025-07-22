namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing the list of agent authority entries returned in response to a request
/// to retrieve them for an agent user.
/// </summary>
public class GetAgentAuthoritiesResponse
{
    /// <summary>
    /// Gets and sets a list of <see cref="AgentAuthorityModel"/>.
    /// </summary>
    public List<AgentAuthorityModel> AgentAuthorities { get; set; }
}
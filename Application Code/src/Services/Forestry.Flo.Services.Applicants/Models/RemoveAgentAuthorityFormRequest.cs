using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;

namespace Forestry.Flo.Services.Applicants.Models;

public class RemoveAgentAuthorityFormRequest
{
    /// <summary>
    /// Gets and sets the id of the user removing the agent authority form entry.
    /// </summary>
    public Guid RemovedByUser { get; set; }

    /// <summary>
    /// Gets and sets the ID of the <see cref="AgentAuthority"/> entry that the AAF document is
    /// being removed from.
    /// </summary>
    public Guid AgentAuthorityId { get; set; }

    /// <summary>
    /// Gets and sets the ID of the <see cref="AgentAuthorityForm"/> entry to be removed.
    /// </summary>
    public Guid AgentAuthorityFormId { get; set; }
}
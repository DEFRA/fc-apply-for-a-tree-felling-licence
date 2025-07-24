using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Request model class for retrieving the documents related to a specific AAF.  Intended for
/// use in the Internal app only (no verification of user permissions).
/// </summary>
public class GetAgentAuthorityFormDocumentsInternalRequest
{
    /// <summary>
    /// Gets and sets the ID of the <see cref="AgentAuthority"/> entry that the AAF document is
    /// attached to.
    /// </summary>
    public Guid AgentAuthorityId { get; set; }

    /// <summary>
    /// Gets and sets the ID of the <see cref="AgentAuthorityForm"/> entry to retrieve the files for.
    /// </summary>
    public Guid AgentAuthorityFormId { get; set; }
}

/// <summary>
/// Request model class for retrieving the documents related to a specific AAF.  Intended for
/// use in the External app (includes the ID of the user in order to verify their permission to view
/// the AAF).
/// </summary>
public class GetAgentAuthorityFormDocumentsRequest : GetAgentAuthorityFormDocumentsInternalRequest
{
    /// <summary>
    /// Gets and sets the id of the user accessing the agent authority form entry files.
    /// </summary>
    public Guid AccessedByUser { get; set; }
}


using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.FileStorage.Model;

namespace Forestry.Flo.Services.Applicants.Models;

public class AddAgentAuthorityFormRequest
{
    /// <summary>
    /// Gets and sets the id of the user adding the agent authority form entry.
    /// </summary>
    public Guid UploadedByUser { get; set; }

    /// <summary>
    /// Gets and sets the ID of the <see cref="AgentAuthority"/> entry that the AAF document is
    /// being uploaded for.
    /// </summary>
    public Guid AgentAuthorityId { get; set; }

    /// <summary>
    /// Gets and sets the collection of document files that comprise the AAF.
    /// </summary>
    public IReadOnlyCollection<FileToStoreModel> AafDocuments { get; set; }
}
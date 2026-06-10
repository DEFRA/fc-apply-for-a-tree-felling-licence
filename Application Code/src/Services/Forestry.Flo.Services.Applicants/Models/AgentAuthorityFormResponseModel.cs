namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing an Agent Authority Form entry.
/// </summary>
public class AgentAuthorityFormResponseModel
{
    /// <summary>
    /// Gets and sets the id of the agent authority form in the repository.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the id of the woodland owner the agent authority form is associated with.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }

    /// <summary>
    /// Gets and sets the id of the agency the agent authority form is associated with.
    /// </summary>
    public Guid AgencyId { get; set; }

    /// <summary>
    /// Gets and sets the valid from date of the agent authority form.
    /// </summary>
    public DateTime ValidFromDate { get; set; }

    /// <summary>
    /// Gets and sets the valid to date for the agent authority form.
    /// </summary>
    public DateTime? ValidToDate { get; set; }

    /// <summary>
    /// Gets and sets the set of document files that constitute this AAF.
    /// </summary>
    public List<AafDocumentResponseModel> AafDocuments { get; set; }
}
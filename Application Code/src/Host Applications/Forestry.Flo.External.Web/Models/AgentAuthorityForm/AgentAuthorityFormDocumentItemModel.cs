using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.External.Web.Models.AgentAuthorityForm;

public class AgentAuthorityFormDocumentItemModel
{
    /// <summary>
    /// Gets and sets the id of the agent authority form in the repository.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets and sets the valid from date of the agent authority form.
    /// </summary>
    public DateTime ValidFromDate { get; init; }

    /// <summary>
    /// Gets and sets the valid to date for the agent authority form.
    /// </summary>
    public DateTime? ValidToDate { get; init; }

    /// <summary>
    /// Gets and sets the set of document files that constitute this AAF.
    /// </summary>
    public List<AafDocumentResponseModel> AafDocuments { get; init; }

    /// <summary>
    /// Concatenation of the file names, ordered by file name
    /// </summary>
    public string AafDocumentFileNamesList => string.Join(", ", AafDocuments.Select(x=>x.FileName).OrderBy(x=>x));
}
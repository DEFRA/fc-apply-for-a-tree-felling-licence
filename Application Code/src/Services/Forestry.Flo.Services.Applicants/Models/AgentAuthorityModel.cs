using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing an Agent Authority entry.
/// </summary>
public class AgentAuthorityModel
{
    /// <summary>
    /// Gets and sets the Id of the agent authority entry in the repository.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the date and time that the entry was created.
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Gets and sets the name of the user that created the entry.
    /// </summary>
    public string CreatedByName { get; set; }

    /// <summary>
    /// Gets and sets the <see cref="AgentAuthorityStatus"/> value for the entry.
    /// </summary>
    public AgentAuthorityStatus Status { get; set; }

    /// <summary>
    /// Gets and sets the name of the individual that processed the authority request, if there is one.
    /// </summary>
    public string? ChangedByName { get; set; }

    /// <summary>
    /// Gets and sets the date that the entry was last updated.
    /// </summary>
    public DateTime? ChangedDate { get; set; }

    /// <summary>
    /// Gets and sets a populated <see cref="WoodlandOwnerModel"/> representing the applicant this
    /// authority entry is for.
    /// </summary>
    public WoodlandOwnerModel WoodlandOwner { get; set; }

    /// <summary>
    /// Gets and sets a list of <see cref="AgentAuthorityFormResponseModel"/> entries representing
    /// the uploaded AAFs for this agent/woodland owner linkage.
    /// </summary>
    public List<AgentAuthorityFormResponseModel> AgentAuthorityForms { get; set; }
}
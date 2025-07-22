using Forestry.Flo.Services.Applicants.Entities.Agent;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Applicants.Entities.AgentAuthority;

/// <summary>
/// Entity class representing an agent authority record.
/// </summary>
public class AgentAuthority
{
    /// <summary>
    /// Gets the unique internal identifier for the agent authority record on the system.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or Sets the datetime that the record was created.
    /// </summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Gets or Sets the datetime that the record was last changed.
    /// </summary>
    public DateTime? ChangedTimestamp { get; set; }

    /// <summary>
    /// Gets or Sets the user account that created the authority record.
    /// </summary>
    [Required]
    public UserAccount.UserAccount CreatedByUser { get; set; }

    /// <summary>
    /// Gets or Sets the user account that last changed the authority record or its uploaded AAFs.
    /// </summary>
    public UserAccount.UserAccount? ChangedByUser { get; set; }

    /// <summary>
    /// Gets or Sets the approval status of the authority record.
    /// </summary>
    public AgentAuthorityStatus Status { get; set; }

    /// <summary>
    /// Gets or Sets the woodland owner associated with the agent authority.
    /// </summary>
    [Required]
    public WoodlandOwner.WoodlandOwner WoodlandOwner { get; set; }

    /// <summary>
    /// Gets or Sets the agency associated with the agent authority.
    /// </summary>
    [Required]
    public Agency Agency { get; set; }

    /// <summary>
    /// Gets and sets the list of uploaded <see cref="AgentAuthorityForm"/> entries for 
    /// this <see cref="AgentAuthority"/>.
    /// </summary>
    public IList<AgentAuthorityForm> AgentAuthorityForms { get; set; } = new List<AgentAuthorityForm>();
}
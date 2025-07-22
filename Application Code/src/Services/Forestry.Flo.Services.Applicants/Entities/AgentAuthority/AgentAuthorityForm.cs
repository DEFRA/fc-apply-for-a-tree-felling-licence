using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Applicants.Entities.AgentAuthority;

/// <summary>
/// Entity class representing an agent authority form.
/// </summary>
public class AgentAuthorityForm
{
    /// <summary>
    /// Gets the unique internal identifier for the uploaded AAF.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or Sets the user account that uploaded the AAF.
    /// </summary>
    [Required]
    public UserAccount.UserAccount UploadedBy { get; set; }

    /// <summary>
    /// Gets and sets the validity from date.
    /// </summary>
    [Required]
    public DateTime ValidFromDate { get; set; }

    /// <summary>
    /// Gets and sets the validity to date.
    /// </summary>
    public DateTime? ValidToDate { get; set; }

    /// <summary>
    /// Gets and sets the list of one or more document files representing this AAF.
    /// </summary>
    public IList<AafDocument> AafDocuments { get; set; } = new List<AafDocument>();

    /// <summary>
    /// Gets and sets the Id of the <see cref="AgentAuthority"/> this <see cref="AgentAuthorityForm"/>
    /// is for.
    /// </summary>
    public Guid AgentAuthorityId { get; set; }
}
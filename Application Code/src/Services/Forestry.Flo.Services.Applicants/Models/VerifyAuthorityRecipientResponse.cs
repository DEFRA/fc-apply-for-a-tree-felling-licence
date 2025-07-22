using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Response class encapsulating the details required for an authority confirmation recipient to
/// decide whether or not to approve an agency authority request.
/// </summary>
public class VerifyAuthorityRecipientResponse
{
    /// <summary>
    /// Gets and sets the id of the <see cref="AgentAuthority"/> entity in the repository.
    /// </summary>
    public Guid AgentAuthorityId { get; set; }
    
    ///// <summary>
    ///// Gets and sets the <see cref="AuthorityConfirmationEmailRecipientResponseModel"/> for the
    ///// specific authority confirmation recipient that made the request.
    ///// </summary>
    //public AuthorityConfirmationEmailRecipientResponseModel AuthorityRecipientDetails { get; set; }

    /// <summary>
    /// Gets and sets the details of the woodland owner involved in the agent authority request.
    /// </summary>
    public WoodlandOwnerModel WoodlandOwnerDetails { get; set; }

    /// <summary>
    /// Gets and sets the details of the Agency involved in the agent authority request.
    /// </summary>
    public AgencyModel AgencyDetails { get; set; }

    /// <summary>
    /// Gets and sets the current AAF status.
    /// </summary>
    public AgentAuthorityStatus AgentAuthorityStatus { get; set; }

    /// <summary>
    /// Gets or Sets the name of the individual that approved or declined the authority record.
    /// </summary>
    public string? ProcessedByName { get; set; }

    /// <summary>
    /// Gets or Sets the signature of the request processor in base64 format.
    /// </summary>
    public string? SignatureImageData { get; set; }
}
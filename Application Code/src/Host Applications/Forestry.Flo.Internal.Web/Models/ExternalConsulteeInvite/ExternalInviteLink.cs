using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public record ExternalInviteLink
{
    /// <summary>
    /// Gets and Sets the external access link ID.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets and Sets the link created time.
    /// </summary>
    public DateTime CreatedTimeStamp { get; init; }
    
    /// <summary>
    /// Gets and Sets the link expiration time.
    /// </summary>
    public DateTime ExpiresTimeStamp { get; init; }

    /// <summary>
    /// Gets and Sets the application access code.
    /// </summary>
    public Guid AccessCode { get; init; }

    /// <summary>
    /// Gets and Sets the link owner name.
    /// </summary>
    public string Name { get; init; } = null!;
    
    /// <summary>
    /// Gets and Sets the link owner email.
    /// </summary>
    public string ContactEmail { get; init; } = null!;

    /// <summary>
    /// Gets and Sets the external access link purpose.
    /// </summary>
    public string Purpose { get; init; } = null!;

    /// <summary>
    /// Gets and inits the type of the external access link.
    /// </summary>
    public ExternalAccessLinkType LinkType { get; init; }

    /// <summary>
    /// Gets and inits the list of shared supporting document Ids.
    /// </summary>
    public List<Guid> SharedSupportingDocuments { get; init; }

    /// <summary>
    /// Gets and sets whether the consultee has responded using this link.
    /// </summary>
    public bool HasResponded { get; set; }
}
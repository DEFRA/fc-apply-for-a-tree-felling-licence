using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

public class ExternalAccessLink
{
    /// <summary>
    /// Gets and Sets the external access link ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets and Sets the external link application ID.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the external access link type.
    /// </summary>
    [Required]
    public ExternalAccessLinkType LinkType { get; set; }

    /// <summary>
    /// Gets and Sets the created time stamp.
    /// </summary>
    [Required]
    public DateTime CreatedTimeStamp { get; set; }
    
    /// <summary>
    /// Gets and Sets the link expiration time.
    /// </summary>
    [Required]
    public DateTime ExpiresTimeStamp { get; set; }
    
    /// <summary>
    /// Gets and Sets the application access code.
    /// </summary>
    [Required]
    public Guid AccessCode { get; set; }
    
    /// <summary>
    /// Gets and Sets the link owner name.
    /// </summary>
    [Required]
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Gets and Sets the link owner email.
    /// </summary>
    [Required]
    public string ContactEmail { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the external access link purpose.
    /// </summary>
    [Required]
    public string Purpose { get; set; } = null!;

    /// <summary>
    /// Gets and Sets a flag indication is multiple link use is allowed.
    /// </summary>
    [Required]
    public bool IsMultipleUseAllowed { get; set; } = false;

    /// <summary>
    /// Gets and sets a set of supporting document ids that are shared with this
    /// external access link.
    /// </summary>
    public List<Guid> SharedSupportingDocuments { get; set; } = new();
}
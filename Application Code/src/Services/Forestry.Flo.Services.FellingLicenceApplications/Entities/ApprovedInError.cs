using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Represents the Approved In Error record for a felling licence application.
/// Captures the details when an approved application needs to be corrected or reversed.
/// </summary>
public class ApprovedInError
{
    /// <summary>
    /// Gets and sets the Id of this entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the felling licence application id.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the navigation property to the associated felling licence application.
    /// </summary>
    public FellingLicenceApplication FellingLicenceApplication { get; set; }

    /// <summary>
    /// Gets and sets the date and time this record was last updated.
    /// </summary>
    [Required]
    public DateTime LastUpdatedDate { get; set; }

    /// <summary>
    /// Gets and sets the id of the user that last updated this record.
    /// </summary>
    [Required]
    public Guid LastUpdatedById { get; set; }

    /// <summary>
    /// Gets and sets the old reference number of the application before the approved-in-error process.
    /// </summary>
    public string PreviousReference { get; set; } = string.Empty;

    /// <summary>
    /// Gets and sets a flag indicating whether the expiry date was a reason for marking this application as approved in error.
    /// </summary>
    public bool ReasonExpiryDate { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether supplementary points were a reason for marking this application as approved in error.
    /// </summary>
    public bool ReasonSupplementaryPoints { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether other reasons exist for marking this application as approved in error.
    /// </summary>
    public bool ReasonOther { get; set; }

    /// <summary>
    /// Gets and sets an optional case note providing additional context about why the application was approved in error.
    /// </summary>
    public string? CaseNote { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// RestockingOutcome entity class
/// </summary>
public class RestockingOutcome
{
    /// <summary>
    /// Gets and Sets the property document ID.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or sets the proposed Restocking details ID.
    /// </summary>
    [Required]
    public Guid ProposedRestockingDetailsId { get; set; }

    /// <summary>
    /// Gets or sets the Restocking licence application.
    /// </summary>
    [Required]
    public ProposedRestockingDetail ProposedRestockingDetail { get; set; }

    /// <summary>
    /// Gets or sets the area
    /// </summary>
    [Required]
    public double Area { get; set; } 

    /// <summary>
    /// Gets or sets the number of trees
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets the species
    /// </summary>
    [Required]
    public string Species { get; set; }

    /// <summary>
    /// Gets or sets the comment.
    /// </summary>
    public string? Comment { get; set; }
}
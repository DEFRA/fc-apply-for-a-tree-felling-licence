using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Proposed restocking species entity class.
/// </summary>
public class RestockingSpecies
{
    /// <summary>
    /// Gets and Sets the property document ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the proposed restocking details ID.
    /// </summary>
    [Required]
    public Guid ProposedRestockingDetailsId { get; set; }

    /// <summary>
    /// Gets or sets the proposed restocking details.
    /// </summary>
    [Required]
    public ProposedRestockingDetail ProposedRestockingDetail { get; set; }

    /// <summary>
    /// Gets or sets the species
    /// </summary>
    [Required]
    public string Species { get; set; }

    /// <summary>
    /// Gets or sets the percentage
    /// </summary>
    public double? Percentage { get; set; }
}
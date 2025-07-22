using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Confirmed restocking species entity class.
/// </summary>
public class ConfirmedRestockingSpecies
{
    /// <summary>
    /// Gets and Sets the confirmed restocking species ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the confirmed restocking details ID.
    /// </summary>
    [Required]
    public Guid ConfirmedRestockingDetailsId { get; set; }

    /// <summary>
    /// Gets and sets the confirmed restocking details.
    /// </summary>
    [Required]
    public ConfirmedRestockingDetail ConfirmedRestockingDetail { get; set; }

    /// <summary>
    /// Gets and sets the species.
    /// </summary>
    [Required]
    public string Species { get; set; }

    /// <summary>
    /// Gets and sets the percentage.
    /// </summary>
    public double? Percentage { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Confirmed felling species entity class.
/// </summary>
public class ConfirmedFellingSpecies
{
    /// <summary>
    /// Gets and Sets the confirmed felling species ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the confirmed felling detail id.
    /// </summary>
    [Required]
    public Guid ConfirmedFellingDetailId { get; set; }

    /// <summary>
    /// Gets and sets the confirmed felling detail.
    /// </summary>
    [Required]
    public ConfirmedFellingDetail ConfirmedFellingDetail { get; set; }

    /// <summary>
    /// Gets or sets the species
    /// </summary>
    [Required]
    public string Species { get; set; }
}
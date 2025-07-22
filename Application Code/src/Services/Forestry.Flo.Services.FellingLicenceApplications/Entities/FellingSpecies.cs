using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// FellingSpecies entity class
/// </summary>
public class FellingSpecies
{
    /// <summary>
    /// Gets and Sets the felling species ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the proposed felling details ID.
    /// </summary>
    [Required]
    public Guid ProposedFellingDetailsId { get; set; }

    /// <summary>
    /// Gets or sets the felling licence application.
    /// </summary>
    [Required]
    public ProposedFellingDetail ProposedFellingDetail { get; set; }

    /// <summary>
    /// Gets or sets the species
    /// </summary>
    [Required]
    public string Species { get; set; }
}
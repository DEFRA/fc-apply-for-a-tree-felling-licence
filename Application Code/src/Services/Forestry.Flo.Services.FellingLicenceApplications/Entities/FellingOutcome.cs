using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// FellingOutcome entity class
/// </summary>
public class FellingOutcome
{
    /// <summary>
    /// Gets and Sets the property document ID.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

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
    /// Gets or sets the volume
    /// </summary>
    public int? Volume{ get; set; }

    /// <summary>
    /// Gets or sets the number of trees
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets the tree marking.
    /// </summary>
    public string? TreeMarking { get; set; }

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
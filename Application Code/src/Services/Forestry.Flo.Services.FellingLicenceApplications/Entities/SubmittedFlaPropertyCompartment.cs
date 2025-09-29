using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// FellingLicenceApplication entity class
/// </summary>
public class SubmittedFlaPropertyCompartment
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the felling licence application ID.
    /// </summary>
    public Guid SubmittedFlaPropertyDetailId { get; set; }

    /// <summary>
    /// Gets and Sets the compartment id.
    /// </summary>
    public Guid CompartmentId { get; set; }

    /// <summary>
    /// Gets and Sets the compartment number.
    /// </summary>
    [Required]
    public string CompartmentNumber { get; set; }

    /// <summary>
    /// Gets and Sets the sub compartment name
    /// </summary>
    public string? SubCompartmentName { get; set; }

    /// <summary>
    /// Gets and Sets the total hectares number
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public double? TotalHectares { get; set; }

    /// <summary>
    /// Gets and Sets the confirmed (digitised) total hectares number
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public double? ConfirmedTotalHectares { get; set; }

    /// <summary>
    /// Gets and Sets the woodland name
    /// </summary>
    public string? WoodlandName { get; set; }

    /// <summary>
    /// Gets and Sets the designation
    /// </summary>
    public string? Designation { get; set; }

    /// <summary>
    /// Gets and Sets the GIS data
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public string? GISData { get; set; }

    /// <summary>
    /// Gets and Sets the id of the Property Profile
    /// </summary>
    public Guid PropertyProfileId { get; set; }

    /// <summary>
    /// Gets or sets the submitted fla property detail.
    /// </summary>
    public SubmittedFlaPropertyDetail SubmittedFlaPropertyDetail { get; set; }

    public string DisplayName => CompartmentNumber;

    /// <summary>
    /// Gets or sets the confirmed felling details.
    /// </summary>
    public IList<ConfirmedFellingDetail> ConfirmedFellingDetails { get; set; } = new List<ConfirmedFellingDetail>();

    public bool Zone1 { get; set; }
    public bool Zone2 { get; set; }
    public bool Zone3 { get; set; }

    /// <summary>
    /// Gets and sets the compartment designations.
    /// </summary>
    public SubmittedCompartmentDesignations? SubmittedCompartmentDesignations { get; set; }
}
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// FellingLicenceApplication entity class
/// </summary>
public class SubmittedFlaPropertyDetail
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the felling licence application ID.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and Sets the property profile id.
    /// </summary>
    public Guid PropertyProfileId { get; set; }

    /// <summary>
    /// Gets and Sets the property profile name.
    /// </summary>
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the Optional Name of the Wood
    /// </summary>
    public string? NameOfWood { get; set; }

    /// <summary>
    /// Gets and Sets the nearest town.
    /// </summary>
    public string? NearestTown { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating the property profile has Woodland Management Plan.
    /// </summary>
    [Required]
    public bool HasWoodlandManagementPlan { get; set; }

    /// <summary>
    /// Gets and Sets the Woodland Management Plan reference.
    /// </summary>
    public string? WoodlandManagementPlanReference { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating the property profile has Woodland Certification Scheme.
    /// </summary>
    public bool? IsWoodlandCertificationScheme { get; set; }

    /// <summary>
    /// Gets and Sets the Woodland Certification Scheme reference.
    /// </summary>
    public string? WoodlandCertificationSchemeReference { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating the Woodland Owner id.
    /// </summary>
    [Required]
    public Guid WoodlandOwnerId { get; set; }

    public FellingLicenceApplication FellingLicenceApplication { get; set; }

    public IList<SubmittedFlaPropertyCompartment>? SubmittedFlaPropertyCompartments { get; set; }
}
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;

/// <summary>
/// View model representing a single habitat restoration entry for an application's compartment.
/// </summary>
public class HabitatRestorationViewModel
{
    /// <summary>
    /// Unique identifier of the habitat restoration record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the linked property profile that owns this restoration record.
    /// </summary>
    public Guid LinkedPropertyProfileId { get; set; }

    /// <summary>
    /// Identifier of the compartment within the property profile.
    /// </summary>
    public Guid PropertyProfileCompartmentId { get; set; }

    /// <summary>
    /// Selected habitat type for restoration (optional).
    /// </summary>
    public HabitatType? HabitatType { get; set; }

    /// <summary>
    /// Free text description when <see cref="HabitatType.Other"/> is selected.
    /// </summary>
    public string? OtherHabitatDescription { get; set; }

    /// <summary>
    /// Selected woodland species type (optional).
    /// </summary>
    public WoodlandSpeciesType? WoodlandSpeciesType { get; set; }

    /// <summary>
    /// Indicates whether broadleaf woodland is native (optional, only applicable for broadleaf selections).
    /// </summary>
    public bool? NativeBroadleaf { get; set; }

    /// <summary>
    /// Indicates if the compartment is classified as productive woodland (optional).
    /// </summary>
    public bool? ProductiveWoodland { get; set; }

    /// <summary>
    /// Indicates whether the compartment was felled earlier than planned (optional).
    /// </summary>
    public bool? FelledEarly { get; set; }

    /// <summary>
    /// Indicates whether all restoration questions for the compartment are completed (optional).
    /// </summary>
    public bool? Completed { get; set; }
}

using Forestry.Flo.Services.FellingLicenceApplications.Models;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Represents habitat restoration details captured for a specific compartment within a linked property profile.
/// Used by external applicant UI to record habitat type and woodland species information during the Habitat Restoration task.
/// </summary>
public class HabitatRestoration
{
    /// <summary>
    /// Primary key identifier for the habitat restoration record.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the linked property profile this restoration record belongs to.
    /// </summary>
    public Guid LinkedPropertyProfileId { get; set; }

    /// <summary>
    /// Navigation to the linked property profile.
    /// </summary>
    public LinkedPropertyProfile LinkedPropertyProfile { get; set; }

    /// <summary>
    /// Identifier of the compartment within the property profile for which restoration data is recorded.
    /// </summary>
    public Guid PropertyProfileCompartmentId { get; set; }

    /// <summary>
    /// The priority open habitat type the compartment is predominantly being converted into.
    /// </summary>
    public HabitatType? HabitatType { get; set; }

    public string? OtherHabitatDescription { get; set; }

    /// <summary>
    /// The woodland species type describing the dominant tree composition within the compartment.
    /// </summary>
    public WoodlandSpeciesType? WoodlandSpeciesType { get; set; }

    /// <summary>
    /// Indicates if the selected woodland species is native broadleaf (applies when species type is BroadleafWoodland).
    /// </summary>
    public bool? NativeBroadleaf { get; set; }

    /// <summary>
    /// Indicates if the selected woodland species is productive woodland (applies when species type is not BroadleafWoodland).
    /// </summary>
    public bool? ProductiveWoodland { get; set; }

    /// <summary>
    /// Indicates if the compartment was felled early as part of habitat restoration.
    /// </summary>
    public bool? FelledEarly { get; set; }

    /// <summary>
    /// Marks the habitat restoration task for this compartment as completed by the applicant.
    /// Used to drive navigation to remaining compartments or return to task list.
    /// </summary>
    public bool? Completed { get; set; }
}
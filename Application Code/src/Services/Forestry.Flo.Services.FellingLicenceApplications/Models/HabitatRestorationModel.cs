using System;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// DTO model corresponding to the HabitatRestoration entity for external applicant updates.
/// </summary>
public class HabitatRestorationModel
{
    public Guid Id { get; set; }
    public Guid LinkedPropertyProfileId { get; set; }
    public Guid PropertyProfileCompartmentId { get; set; }
    public HabitatType? HabitatType { get; set; }
    public string? OtherHabitatDescription { get; set; }
    public WoodlandSpeciesType? WoodlandSpeciesType { get; set; }
    public bool? NativeBroadleaf { get; set; }
    public bool? ProductiveWoodland { get; set; }
    public bool? FelledEarly { get; set; }
    public bool? Completed { get; set; }
}

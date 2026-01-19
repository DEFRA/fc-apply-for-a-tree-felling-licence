using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class PriorityOpenHabitatViewModel : WoodlandOfficerReviewModelBase
{
    public Guid PropertyProfileCompartmentId { get; set; }
    public HabitatType? HabitatType { get; set; }
    public string? OtherHabitatDescription { get; set; }
    public WoodlandSpeciesType? WoodlandSpeciesType { get; set; }
    public bool? NativeBroadleaf { get; set; }
    public bool? ProductiveWoodland { get; set; }
    public bool? FelledEarly { get; set; }
    public bool? Completed { get; set; }
}

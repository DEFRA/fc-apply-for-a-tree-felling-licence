using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class PriorityOpenHabitatsViewModel : WoodlandOfficerReviewModelBase
{
    public Guid ApplicationId { get; set; }
    public IReadOnlyList<PriorityOpenHabitatViewModel> Habitats { get; set; } = new List<PriorityOpenHabitatViewModel>();
    public bool? AreDetailsCorrect { get; set; }
}

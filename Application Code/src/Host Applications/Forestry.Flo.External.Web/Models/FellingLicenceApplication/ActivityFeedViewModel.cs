using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class ActivityFeedViewModel
{
    /// <summary>
    /// The ID of the felling licence application to display the activity feed for.
    /// </summary>
    public Guid ApplicationId { get; set; }
    /// <summary>
    /// A flag indicating whether or not the filter dropdown is visible.
    /// </summary>
    public bool ShowFilters { get; set; } = true;
    /// <summary>
    /// The default filter for activity feed items.
    /// </summary>
    public CaseNoteType? DefaultCaseNoteFilter { get; set; } = null;
    /// <summary>
    /// A list of <see cref="ActivityFeedItemModel"/> to be displayed in the activity feed.
    /// </summary>
    public IList<ActivityFeedItemModel> ActivityFeedItemModels { get; set; }
    /// <summary>
    /// The title to be displayed at the top of the activity feed partial.
    /// </summary>
    public string ActivityFeedTitle { get; set; } = "Activity Feed";

    /// <summary>
    /// A list of <see cref="StatusHistory"/> to filter the activity feed for external users.
    /// </summary>
    public List<StatusHistory>? StatusHistories { get; set; }

}
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class ActivityFeedModel
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
    /// A flag indicating whether or not case notes can be added through this activity feed instance.
    /// </summary>
    public bool ShowAddCaseNote { get; set; } = true;

    /// <summary>
    /// The <see cref="CaseNoteType"/> of case notes added through this instance.
    /// </summary>
    public CaseNoteType NewCaseNoteType { get; set; } = CaseNoteType.CaseNote;
    
    /// <summary>
    /// The default filter for activity feed items.
    /// </summary>
    public CaseNoteType? DefaultCaseNoteFilter { get; set; } = null;

    /// <summary>
    /// A list of <see cref="ActivityFeedItemModel"/> to be displayed in the activity feed.
    /// </summary>
    public IList<ActivityFeedItemModel> ActivityFeedItemModels { get; set; } = [];
    
    /// <summary>
    /// The action to return to after adding a case note.
    /// </summary>
    public string HostingPage { get; set; } = string.Empty;

    /// <summary>
    /// The title to be displayed at the top of the activity feed partial.
    /// </summary>
    public string ActivityFeedTitle { get; set; } = "Activity feed";

    /// <summary>
    /// Gets and sets the actor type of the intended viewer of the activity feed items.
    /// </summary>
    public ActorType ViewingUserActorType { get; set; } = ActorType.InternalUser;

    /// <summary>
    /// Gets and sets the email of the intended viewer of the activity feed items.
    /// Only required for Consultee actor types.
    /// </summary>
    public string? ViewingUserEmail { get; set; }

    /// <summary>
    /// Gets and sets the authentication token of the intended viewer of the activity feed items.
    /// Only required for Consultee actor types.
    /// </summary>
    public Guid? ViewingUserAuthenticationToken { get; set; }
}
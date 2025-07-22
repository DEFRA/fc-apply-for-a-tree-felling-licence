using Forestry.Flo.Services.Common.Services;

namespace Forestry.Flo.Services.Common.Models;

/// <summary>
/// A model to provide <see cref="ActivityFeedItemProvider"/> with data pertaining to an instance of the activity feed.
/// </summary>
public class ActivityFeedItemProviderModel
{
    /// <summary>
    /// The application ID for the activity feed.
    /// </summary>
    public Guid FellingLicenceId { get; set; }
    /// <summary>
    /// The application reference for the activity feed.
    /// </summary>
    public string? FellingLicenceReference { get; set; }
    /// <summary>
    /// An array of <see cref="ActivityFeedItemType"/> to be displayed in the activity feed.
    /// </summary>
    public ActivityFeedItemType[]? ItemTypes { get; set; }
    /// <summary>
    /// A flag whether retrieved items must be visible to applicants.
    /// </summary>
    public bool? VisibleToApplicant { get; set; } = null;
    /// <summary>
    /// A flag whether retrieved items must be visible to consultees.
    /// </summary>
    public bool? VisibleToConsultee { get; set; } = null;
}
namespace Forestry.Flo.Services.Common.Models;

/// <summary>
/// A model representing an item in an activity feed.
/// </summary>
public class ActivityFeedItemModel
{
    /// <summary>
    /// The application ID for the relevant felling licence application.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }
    /// <summary>
    /// The <see cref="ActivityFeedItemType"/> of the item.
    /// </summary>
    public ActivityFeedItemType ActivityFeedItemType { get; set; }
    /// <summary>
    /// The text to be displayed in the item.
    /// </summary>
    public string Text { get; set; }
    /// <summary>
    /// A flag whether this item is visible to applicants.
    /// </summary>
    public bool VisibleToApplicant { get; set; }
    /// <summary>
    /// A flag whether this item is visible to consultees.
    /// </summary>
    public bool VisibleToConsultee { get; set; }
    /// <summary>
    /// The date when this item was created.
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }
    /// <summary>
    /// The Id of an associated record or entry.
    /// </summary>
    public Guid? AssociatedId { get; set; }
    /// <summary>
    /// The author of the item, if applicable.
    /// </summary>
    public ActivityFeedItemUserModel? CreatedByUser { get; set; }
    /// <summary>
    /// The source of a notification.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// The recipients of a notification.
    /// </summary>
    public string[]? Recipients { get; set; }
}
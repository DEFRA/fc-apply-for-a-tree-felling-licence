using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Services.Common.Infrastructure;

/// <summary>
/// The category of an activity feed item.
/// </summary>
[AttributeUsage(validOn: AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true)]
public class ActivityFeedItemTypeAttribute : Attribute
{
    public Maybe<ActivityFeedItemCategory> Category { get; private set; }
    public ActivityFeedItemTypeAttribute(ActivityFeedItemCategory category) 
    {
        this.Category = category;
    }
}
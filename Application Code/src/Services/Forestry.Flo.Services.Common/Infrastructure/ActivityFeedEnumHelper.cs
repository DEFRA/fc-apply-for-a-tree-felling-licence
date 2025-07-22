using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Services.Common.Infrastructure;

public static class ActivityFeedEnumHelper
{
    public static Maybe<ActivityFeedItemCategory> GetActivityFeedItemTypeAttribute<T>(this T enumValue)
    {
        var fieldInfo = enumValue?.GetType().GetField(enumValue.ToString() ?? string.Empty);

        var category = Maybe<ActivityFeedItemCategory>.None;

        if (fieldInfo == null) return category;

        var attrs = fieldInfo.GetCustomAttributes(typeof(ActivityFeedItemTypeAttribute), true);
        if (attrs.Length > 0)
        {
            category = ((ActivityFeedItemTypeAttribute)attrs[0]).Category;
        }

        return category;
    }
}


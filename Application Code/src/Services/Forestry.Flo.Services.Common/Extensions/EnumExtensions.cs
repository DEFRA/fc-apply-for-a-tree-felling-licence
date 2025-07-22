using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Forestry.Flo.Services.Common.Extensions;

public static class EnumExtensions
{
    public static string? GetDisplayName(this Enum enumValue)
    {
        var member = enumValue.GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault();

        return member?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? enumValue.ToString();
    }

    public static List<string> GetDisplayNames(this Type enumType)
    {
        var displayNames = new List<string>();
        foreach (var value in enumType.GetEnumValues())
        {
            var displayName = ((Enum)value).GetDisplayName();
            displayNames.Add(displayName ?? value.ToString()!);
        }
        return displayNames;
    }

    /// <summary>
    /// Retrieves the display name for the specified enum value based on the provided <see cref="ActorType"/> context.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The enum value for which to get the display name.</param>
    /// <param name="context">The actor type context used to determine which display name to return.</param>
    /// <returns>
    /// The display name defined by the <see cref="DisplayNamesAttribute"/> for the given context,
    /// or the enum <see cref="DisplayAttribute"/>'s name if no attribute is found.
    /// </returns>
    public static string GetDisplayNameByActorType<TEnum>(this TEnum value, ActorType context) where TEnum : Enum
    {
        var type = typeof(TEnum);
        var memInfo = type.GetMember(value.ToString());
        if (memInfo.Length > 0)
        {
            if (memInfo[0].GetCustomAttributes(typeof(DisplayNamesAttribute), false)
                    .FirstOrDefault() is DisplayNamesAttribute attr)
            {
                return context is ActorType.ExternalApplicant
                    ? attr.ExternalName
                    : attr.InternalName;
            }
        }

        // Fallback to the default display name if no attribute is found
        return value.GetDisplayName() ?? value.ToString();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forestry.Flo.Services.DataImport.Tests.Services;

/// <summary>
/// Extension methods to support the testing code for importing data into the Forestry FLOv2 system.
/// </summary>
public static class ImportExtensionMethods
{
    public static T RandomElement<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.RandomElementUsing<T>(new Random());
    }

    private static T RandomElementUsing<T>(this IEnumerable<T> enumerable, Random rand)
    {
        int index = rand.Next(0, enumerable.Count());
        return enumerable.ElementAt(index);
    }

    public static string ToDelimitedString<T>(this IEnumerable<T> enumerable, string delimiter = ",")
    {
        return string.Join(delimiter, enumerable);
    }

    public static DateTime GetRandomLaterDate(this DateTime start)
    {
        var daysLater = new Random().Next(1, 365);
        return start.AddDays(daysLater);
    }
}
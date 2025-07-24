namespace Forestry.Flo.Services.Common.Extensions;

/// <summary>
/// Extensions to aid readability of code, particularly around boolean comparisons.
/// </summary>
public static class ReadabilityExtensions
{
    /// <summary>
    /// Gets a value indicating whether the current <see cref="Nullable{T}"/> object does
    /// not have a valid value of its underlying type.
    /// </summary>
    /// <typeparam name="T">The type of the nullable object.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the current <see cref="Nullable{T}"/> object has no value;
    /// <see langword="false"/> if the current <see cref="Nullable{T}"/> object has a value.</returns>
    public static bool HasNoValue<T>(this T? value) where T : struct
    {
        return value.HasValue == false;
    }

    /// <summary>
    /// Determines whether a sequence contains no elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the source sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns><see langword="true"/> if the source sequence contains no elements; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool NotAny<T>(this IEnumerable<T> source)
    {
        return source.Any() == false;
    }

    /// <summary>
    /// Determines whether no elements of a sequence satisfy a condition.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="predicate"></param>
    /// <returns><see langword="true"/> if the source sequence is empty or none of its elements pass the test in the specified predicate; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool NotAny<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return source.Any(predicate) == false;
    }
}
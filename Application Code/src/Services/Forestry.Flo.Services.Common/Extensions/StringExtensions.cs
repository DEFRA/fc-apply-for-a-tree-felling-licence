using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Common.Extensions;

public static class StringExtensions
{
    public static (string? Title, string? FirstName, string? LastName) ParseFullName(this string fullName)
    {
        string? title = null;
        string? firstName = null;
        string? lastName = null;

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            // lastordefault as titles are in alphabetical order so e.g. Major comes before Major General
            var foundTitle = typeof(TitlesEnum).GetDisplayNames().LastOrDefault(fullName.StartsWith);

            if (!string.IsNullOrWhiteSpace(foundTitle))
            {
                title = foundTitle;
                fullName = fullName.Replace(title, string.Empty);
            }

            var nameParts = fullName.Split(' ')
                .ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (nameParts.Count > 1)
            {
                firstName = nameParts.First();
                nameParts.Remove(firstName);
            }

            lastName = string.Join(' ', nameParts).Trim();
        }

        return (title, firstName, lastName);
    }

    /// <summary>
    /// Formats a double value as a string with two decimal places.
    /// </summary>
    /// <param name="value">The double value to format.</param>
    /// <returns>A string representation of the value with two decimal places.</returns>
    public static string FormatDoubleForDisplay(this double value) => $"{value:0.00}";
}
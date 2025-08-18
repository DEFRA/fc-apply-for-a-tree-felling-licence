namespace Forestry.Flo.Internal.Web.Services;

/// <summary>
/// Provides functionality to manage and update a navigation history list for back-link navigation.
/// </summary>
public class BackLinkService
{
    /// <summary>
    /// The maximum size of the navigation history list.
    /// </summary>
    private const int MaxHistorySize = 10;

    /// <summary>
    /// Updates the navigation history list with the current URL.
    /// If the URL already exists in the history, truncates the history up to and including that URL.
    /// Otherwise, adds the URL to the end of the history.
    /// </summary>
    /// <param name="currentUrl">The current URL to add or check in the history.</param>
    /// <param name="existingHistory">The existing navigation history list.</param>
    /// <returns>
    /// A new list representing the updated navigation history.
    /// </returns>
    public List<string> UpdateHistoryList(string currentUrl, List<string>? existingHistory)
    {
        if (string.IsNullOrEmpty(currentUrl))
        {
            return existingHistory?.ToList() ?? [];
        }

        var lowerUrl = currentUrl.ToLowerInvariant();
        var history = existingHistory?.ToList() ?? [];

        if (history.Count == 0 || history[^1] != lowerUrl)
        {
            history.Add(lowerUrl);
        }

        if (history.Count > MaxHistorySize)
        {
            history = history.Skip(history.Count - MaxHistorySize).ToList();
        }

        return history;
    }

    /// <summary>
    /// Retrieves the previous URL from the navigation history list for back-link navigation.
    /// </summary>
    /// <param name="historyList">The navigation history list.</param>
    /// <returns>
    /// The previous URL if available; otherwise, an empty string.
    /// </returns>
    public string GetBackLinkUrl(List<string>? historyList)
    {
        return historyList?.Count > 1 ? historyList[^2] : string.Empty;
    }
}

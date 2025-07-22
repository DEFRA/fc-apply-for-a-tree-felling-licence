using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Forestry.Flo.External.Web.Infrastructure;

/// <summary>
/// Provides extension methods for working with <see cref="ViewDataDictionary"/> in Razor Pages.
/// </summary>
public static class ViewDataExtensions
{
    public const string BackLinkShowKey = "BackLinkShow";
    public const string BackLinkUrlKey = "BackLinkUrl";
    public const string BackLinkTypeKey = "BackLinkType";
    public const string OmitFromNavigationHistoryKey = "OmitFromNavigationHistory";

    /// <summary>
    /// Sets a flag in the <see cref="ViewDataDictionary"/> to indicate that the back link should be shown in the view.
    /// </summary>
    /// <param name="viewData">The view data dictionary to update.</param>
    /// <param name="backLinkType"> The type of back link to show. Defaults to <see cref="BackLinkType.Default"/>.</param>
    public static void ShowBackLink(this ViewDataDictionary viewData, BackLinkType backLinkType = BackLinkType.Default)
    {
        viewData[BackLinkShowKey] = true;
        viewData[BackLinkTypeKey] = backLinkType;
    }

    /// <summary>
    /// Sets or removes a manual back link URL in the <see cref="ViewDataDictionary"/>.
    /// If a non-empty URL is provided, it is set as the back link; otherwise, the back link is removed.
    /// </summary>
    /// <param name="viewData">The view data dictionary to update.</param>
    /// <param name="backLinkUrl">The URL to set as the back link. If null or whitespace, the back link is removed.</param>
    public static void ManuallySetBackLink(this ViewDataDictionary viewData, string? backLinkUrl)
    {
        if (string.IsNullOrWhiteSpace(backLinkUrl) is false)
        {
            viewData[BackLinkUrlKey] = backLinkUrl;
        }
        else
        {
            viewData.Remove(BackLinkUrlKey);
        }
    }

    /// <summary>
    /// Gets the type of back link from the <see cref="ViewDataDictionary"/>.
    /// Returns <see cref="BackLinkType.Default"/> if not set.
    /// </summary>
    /// <param name="viewData">The view data dictionary to read from.</param>
    /// <returns>The <see cref="BackLinkType"/> value stored in the dictionary, or <see cref="BackLinkType.Default"/> if not set.</returns>
    public static BackLinkType GetBackLinkType(this ViewDataDictionary viewData)
    {
        return viewData.TryGetValue(BackLinkTypeKey, out var value) && value is BackLinkType type
            ? type
            : BackLinkType.Default;
    }

    /// <summary>
    /// Sets a flag in the <see cref="ViewDataDictionary"/> to indicate that the current page should be omitted from the navigation history.
    /// </summary>
    /// <param name="viewData">The view data dictionary to update.</param>
    public static void OmitFromNavigationHistory(this ViewDataDictionary viewData)
    {
        viewData[OmitFromNavigationHistoryKey] = true;
    }

    /// <summary>
    /// Determines whether the current page is omitted from the navigation history by checking the <see cref="ViewDataDictionary"/>.
    /// </summary>
    /// <param name="viewData">The view data dictionary to check.</param>
    /// <returns><c>true</c> if the page is omitted from navigation history; otherwise, <c>false</c>.</returns>
    public static bool IsOmittedFromNavigationHistory(this ViewDataDictionary viewData)
    {
        return viewData.TryGetValue(OmitFromNavigationHistoryKey, out var value) && value is true;
    }
}

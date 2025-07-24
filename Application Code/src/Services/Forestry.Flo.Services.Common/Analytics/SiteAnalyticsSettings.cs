namespace Forestry.Flo.Services.Common.Analytics;

/// <summary>
/// Represents the settings for various tracking tools such as Google Analytics and Microsoft Clarity.
/// </summary>
public class SiteAnalyticsSettings
{
    /// <summary>
    /// Configuration key, used to simplify retrieval of the settings from the configuration.
    /// </summary>
    public static string ConfigurationKey => "SiteAnalytics";

    /// <summary>
    /// Configuration settings for Google Analytics.
    /// </summary>
    public GoogleAnalyticsSettings GoogleAnalytics { get; init; } = new();

    /// <summary>
    /// Configuration settings for Microsoft Clarity.
    /// </summary>
    public MicrosoftClaritySettings MicrosoftClarity { get; init; } = new();
}

/// <summary>
/// Represents the Google Analytics settings in the configuration.
/// </summary>
public class GoogleAnalyticsSettings
{
    /// <summary>
    /// Specifies whether Google Analytics tracking is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// The Google Analytics tracking ID used to send data to Google Analytics.
    /// </summary>
    public string TrackingId { get; init; } = "GA1";
}

/// <summary>
/// Represents the Microsoft Clarity settings in the configuration.
/// </summary>
public class MicrosoftClaritySettings
{
    /// <summary>
    /// Specifies whether Microsoft Clarity tracking is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// The Microsoft Clarity tracking ID used to send data to Microsoft Clarity.
    /// </summary>
    public string TrackingId { get; init; } = "MSC1";
}
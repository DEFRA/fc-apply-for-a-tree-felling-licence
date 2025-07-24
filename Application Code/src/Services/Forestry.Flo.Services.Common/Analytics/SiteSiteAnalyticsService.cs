using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.Common.Analytics;

/// <summary>
/// Service responsible for managing the tracking configuration for both Google Analytics and Microsoft Clarity.
/// This service checks if tracking is enabled based on the configuration.
/// </summary>
public class SiteSiteAnalyticsService
{
    private readonly SiteAnalyticsSettings _siteAnalyticsSettings;
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private const string MicrosoftClarityEnabledCacheKey = "MicrosoftClarityEnabled";
    private const string MicrosoftClarityTrackingIdCacheKey = "MicrosoftClarityTrackingId";
    private const string GoogleAnalyticsEnabledCacheKey = "GoogleAnalyticsEnabled";
    private const string GoogleAnalyticsTrackingIdCacheKey = "GoogleAnalyticsTrackingId";
    private const string AnalyticsConsentCookie = ".AspNet.Consent";
    private readonly TimeSpan _cachedConfigDuration = TimeSpan.FromHours(1); // Cache for 1 hour

    /// <summary>
    /// Initializes a new instance of the <see cref="SiteSiteAnalyticsService"/> class.
    /// </summary>
    /// <param name="siteAnalyticsOptions">An instance of <see cref="IOptions{SiteAnalyticsSettings}"/> that provides tracking configuration settings.</param>
    /// <param name="cache">An instance of <see cref="IMemoryCache"/> to cache configuration values and prevent repeated lookups.</param>
    /// <param name="httpContextAccessor">An instance of <see cref="IHttpContextAccessor"/> to access the current HTTP context, including cookies.</param>
    public SiteSiteAnalyticsService(
        IOptions<SiteAnalyticsSettings> siteAnalyticsOptions,
        IMemoryCache cache, 
        IHttpContextAccessor httpContextAccessor)
    {
        _siteAnalyticsSettings = siteAnalyticsOptions.Value;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;

        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cachedConfigDuration
        };
    }

    public bool HasUserConsented => _httpContextAccessor.HttpContext?.Request.Cookies[AnalyticsConsentCookie] == "yes";

    /// <summary>
    /// Determines whether Google Analytics tracking is enabled based on configuration.
    /// </summary>
    public bool IsGoogleAnalyticsEnabled()
    {
        // Check if Google Analytics is enabled in the configuration
        var isGoogleAnalyticsEnabled = _cache.GetOrCreate(GoogleAnalyticsEnabledCacheKey, entry =>
        {
            entry.SetOptions(_cacheOptions);
            return _siteAnalyticsSettings.GoogleAnalytics.Enabled;
        });

        return isGoogleAnalyticsEnabled;
    }

    /// <summary>
    /// Retrieves the Google Analytics tracking ID from the configuration.
    /// </summary>
    /// <returns>The Google Analytics tracking ID.</returns>
    public string GetGoogleAnalyticsTrackingId()
    {
        return _cache.GetOrCreate(GoogleAnalyticsTrackingIdCacheKey, entry =>
        {
            entry.SetOptions(_cacheOptions);
            return _siteAnalyticsSettings.GoogleAnalytics.TrackingId;
        });
    }

    /// <summary>
    /// Determines whether Microsoft Clarity tracking is enabled based on configuration.
    /// </summary>
    public bool IsMicrosoftClarityEnabled()
    {
        var isMicrosoftClarityEnabled = _cache.GetOrCreate(MicrosoftClarityEnabledCacheKey, entry =>
        {
            entry.SetOptions(_cacheOptions);
            return _siteAnalyticsSettings.MicrosoftClarity.Enabled;
        });

        return isMicrosoftClarityEnabled;
    }

    /// <summary>
    /// Retrieves the Microsoft Clarity tracking ID from the configuration.
    /// </summary>
    /// <returns>The Microsoft Clarity tracking ID.</returns>
    public string GetMicrosoftClarityTrackingId()
    {
        return _cache.GetOrCreate(MicrosoftClarityTrackingIdCacheKey, entry =>
        {
            entry.SetOptions(_cacheOptions);
            return _siteAnalyticsSettings.MicrosoftClarity.TrackingId;
        });
    }
}

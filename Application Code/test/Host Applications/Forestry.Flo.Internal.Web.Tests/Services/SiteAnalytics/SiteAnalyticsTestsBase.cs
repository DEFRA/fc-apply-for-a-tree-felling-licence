using System.Text.RegularExpressions;
using Forestry.Flo.Services.Common.Analytics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using RazorLight;

namespace Forestry.Flo.Internal.Web.Tests.Services.SiteAnalytics;

public class SiteAnalyticsTestsBase
{
    protected RazorLightEngine _razorLightEngine;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    protected Mock<IMemoryCache> _memoryCacheMock;
    protected SiteSiteAnalyticsService _sut;
    protected const string ExpectedGATrackingID = "GA1";
    protected const string ExpectedMSCTrackingID = "MSC1";

    protected SiteSiteAnalyticsService SetupTestAnalyticsService(
        bool consentProvided = true,
        bool googleAnalyticsEnabled = false,
        bool microsoftClarityEnabled = false,
        bool inCache = true)
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _memoryCacheMock = new Mock<IMemoryCache>();
        var siteAnalyticsOptions = Options.Create(new SiteAnalyticsSettings
        {
            GoogleAnalytics = new GoogleAnalyticsSettings { Enabled = googleAnalyticsEnabled, TrackingId = ExpectedGATrackingID },
            MicrosoftClarity = new MicrosoftClaritySettings { Enabled = microsoftClarityEnabled, TrackingId = ExpectedMSCTrackingID }
        });

        SetupHttpRequestForConsent(consentProvided);

        SetupCacheForGetOrCreate(inCache, "GoogleAnalyticsEnabled", googleAnalyticsEnabled);
        SetupCacheForGetOrCreate(inCache, "MicrosoftClarityEnabled", microsoftClarityEnabled);
        SetupCacheForGetOrCreate(inCache, "GoogleAnalyticsTrackingId", ExpectedGATrackingID);
        SetupCacheForGetOrCreate(inCache, "MicrosoftClarityTrackingId", ExpectedMSCTrackingID);

        var baseDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent!.Parent!.Parent!.Parent!.Parent!.Parent;
        var contentRootPath = Path.Combine(baseDirectory!.FullName, "src", "Host Applications",
            "Forestry.Flo.Internal.Web", "Views", "Shared");

        _razorLightEngine = new RazorLightEngineBuilder()
            .UseMemoryCachingProvider()
            .UseFileSystemProject(contentRootPath)
            .Build();

        return new SiteSiteAnalyticsService(siteAnalyticsOptions, _memoryCacheMock.Object, _httpContextAccessorMock.Object);
    }

    protected static string NormalizeHtml(string html)
    {
        // Remove extra whitespace and normalize new lines
        return Regex.Replace(html, @"\s+", " ").Trim();
    }

    private void SetupHttpRequestForConsent(bool consentProvided)
    {
        // Arrange
        var context = new DefaultHttpContext();

        var cookieCollection = new Mock<IRequestCookieCollection>();
        cookieCollection.Setup(c => c.ContainsKey(".AspNet.Consent")).Returns(true);
        cookieCollection.Setup(c => c[".AspNet.Consent"]).Returns(consentProvided ? "yes" : "no");

        context.Request.Cookies = cookieCollection.Object;
        _httpContextAccessorMock.Setup(m => m.HttpContext).Returns(context);
    }

    private void SetupCacheForGetOrCreate(bool cacheHit, string key, object value)
    {
        _memoryCacheMock
            .Setup(m => m.CreateEntry(key))
            .Returns(new Mock<ICacheEntry>().Object);

        if (cacheHit)
        {
            _memoryCacheMock
                .Setup(m => m.TryGetValue(key, out value))
                .Returns(true);
        }
        else
        {
            _memoryCacheMock
                .Setup(m => m.CreateEntry(key))
                .Returns(new Mock<ICacheEntry>().Object);
        }
    }
}
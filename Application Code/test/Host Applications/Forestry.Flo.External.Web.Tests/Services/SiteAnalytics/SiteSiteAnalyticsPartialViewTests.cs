namespace Forestry.Flo.External.Web.Tests.Services.SiteAnalytics;

public class SiteSiteAnalyticsPartialViewTests : SiteAnalyticsTestsBase
{
    private const string Sut = "_SiteAnalytics.cshtml";

    private const string ExpectedMicrosoftClarityHtml = @"
       <!--  Microsoft Clarity -->
         <script type=""text/javascript"">
             (function (c, l, a, r, i, t, y) {
                 c[a] = c[a] || function () { (c[a].q = c[a].q || []).push(arguments) };
                 t = l.createElement(r); t.async = 1; t.src = ""https://www.clarity.ms/tag/"" + i;
                 y = l.getElementsByTagName(r)[0]; y.parentNode.insertBefore(t, y);
             })(window, document, ""clarity"", ""script"", ""MSC1"");
         </script>";

    private const string ExpectedGoogleAnalyticsHtml = @"
        <!--  GA -->
             <script async src=""https://www.googletagmanager.com/gtag/js?id=GA1""></script>
             <script>
                 window.dataLayer = window.dataLayer || [];
                 function gtag() { dataLayer.push(arguments); }
                 gtag('js', new Date());
                 gtag('config', 'GA1');
             </script>";

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task WhenNoConsent_ShouldRenderNoAnalyticsHtml(bool googleAnalyticsEnabled, bool microsoftClarityEnabled)
    {
        // Arrange
        var siteAnalyticsService = SetupTestAnalyticsService(
            googleAnalyticsEnabled: googleAnalyticsEnabled, 
            microsoftClarityEnabled:microsoftClarityEnabled, 
            consentProvided:false);

        // Act
        
        var result = await _razorLightEngine.CompileRenderAsync(Sut, siteAnalyticsService);

        // Assert
        Assert.Equal(string.Empty, NormalizeHtml(result));
    }

    [Fact]
    public async Task WhenMicrosoftClarityEnabled_WithUserConsent_ShouldOnlyRenderGoogleAnalyticsHtml()
    {
        // Arrange
        var siteAnalyticsService = SetupTestAnalyticsService(microsoftClarityEnabled: true);

        // Act
        var result = await _razorLightEngine.CompileRenderAsync(Sut, siteAnalyticsService);

        // Assert
        Assert.Equal(NormalizeHtml(ExpectedMicrosoftClarityHtml), NormalizeHtml(result), StringComparer.OrdinalIgnoreCase);
        Assert.Contains(ExpectedMSCTrackingID, result);
    }

    [Fact]
    public async Task WhenGoogleAnalyticsEnabled_WithUserConsent_ShouldOnlyRenderMicrosoftClarityHtml()
    {
        // Arrange
        var siteAnalyticsService = SetupTestAnalyticsService(googleAnalyticsEnabled: true);
        
        // Act
        var result = await _razorLightEngine.CompileRenderAsync(Sut, siteAnalyticsService);

        // Assert
        Assert.Equal(NormalizeHtml(ExpectedGoogleAnalyticsHtml), NormalizeHtml(result), StringComparer.OrdinalIgnoreCase);
        Assert.Contains(ExpectedGATrackingID, result);
    }

    [Fact]
    public async Task WhenAnalyticsEnabled_WithUserConsent_ShouldRenderAllAnalyticsHtml()
    {
        // Arrange
        var siteAnalyticsService = SetupTestAnalyticsService(googleAnalyticsEnabled:true, microsoftClarityEnabled: true);

        // Act
        var result = await _razorLightEngine.CompileRenderAsync(Sut, siteAnalyticsService);

        // Assert
        Assert.Equal(NormalizeHtml(string.Concat(ExpectedGoogleAnalyticsHtml, ExpectedMicrosoftClarityHtml)), NormalizeHtml(result), StringComparer.OrdinalIgnoreCase);
        Assert.Contains(ExpectedGATrackingID, result);
        Assert.Contains(ExpectedMSCTrackingID, result);
    }
}

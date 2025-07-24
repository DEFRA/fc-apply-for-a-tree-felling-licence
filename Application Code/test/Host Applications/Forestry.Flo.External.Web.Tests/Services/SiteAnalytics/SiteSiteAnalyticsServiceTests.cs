namespace Forestry.Flo.External.Web.Tests.Services.SiteAnalytics;

public class SiteSiteAnalyticsServiceTests : SiteAnalyticsTestsBase
{
    [Theory]
    [CombinatorialData]
    public void GoogleAnalyticsEnabled_OnlyWhenIsEnabledInConfiguration(bool googleAnalyticsEnabled)
    {
        // Arrange
        _sut = SetupTestAnalyticsService(googleAnalyticsEnabled: googleAnalyticsEnabled);

        // Act
        var result = _sut.IsGoogleAnalyticsEnabled();

        // Assert
        Assert.Equal(googleAnalyticsEnabled, result);
    }

    [Theory]
    [CombinatorialData]
    public void MicrosoftClarityEnabled_OnlyWhenIsEnabledInConfiguration(
        bool microsoftClarityEnabled)
    {
        // Arrange
        _sut = SetupTestAnalyticsService(microsoftClarityEnabled: microsoftClarityEnabled);

        // Act
        var result = _sut.IsMicrosoftClarityEnabled();

        // Assert
        Assert.Equal(microsoftClarityEnabled, result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GettingGoogleAnalyticsEnabledUsesCacheWhenNeedsTo(bool inCache)
    {
        // Arrange
        _sut = SetupTestAnalyticsService(googleAnalyticsEnabled: true, inCache: inCache);

        // Act
        var result = _sut.IsGoogleAnalyticsEnabled();

        // Assert
        Assert.True(result);

        _memoryCacheMock.Verify(m => m.CreateEntry("GoogleAnalyticsEnabled"), inCache ? Times.Never : Times.Once);
      
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GettingMicrosoftClarityEnabledUsesCacheWhenNeedsTo(bool inCache)
    {
        // Arrange
        _sut = SetupTestAnalyticsService(microsoftClarityEnabled: true, inCache: inCache);

        // Act
        var result = _sut.IsMicrosoftClarityEnabled();

        // Assert
        Assert.True(result);

        _memoryCacheMock.Verify(m => m.CreateEntry("MicrosoftClarityEnabled"), inCache ? Times.Never : Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GettingMicrosoftClarityTrackingIdUsesCacheWhenNeedsTo(bool inCache)
    {
        // Arrange
        _sut = SetupTestAnalyticsService(inCache: inCache);

        // Act
        var result = _sut.GetMicrosoftClarityTrackingId();

        // Assert
        Assert.Equal(ExpectedMSCTrackingID, result);

        _memoryCacheMock.Verify(m => m.CreateEntry("MicrosoftClarityTrackingId"), inCache? Times.Never: Times.Once);
     }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GettingGoogleAnalyticsTrackingIdUsesCacheWhenNeedsTo(bool inCache)
    {
        // Arrange
        _sut = SetupTestAnalyticsService(inCache: inCache);

        // Act
        var result = _sut.GetGoogleAnalyticsTrackingId();

        // Assert
        Assert.Equal(ExpectedGATrackingID, result);

        _memoryCacheMock.Verify(m => m.CreateEntry("GoogleAnalyticsTrackingId"), inCache ? Times.Never : Times.Once);
    }

    [Fact]
    public void GetGoogleAnalyticsTrackingId_ShouldReturnTrackingId()
    {
        // Arrange
        _sut = SetupTestAnalyticsService();

        // Act
        var trackingId = _sut.GetGoogleAnalyticsTrackingId();

        // Assert
        Assert.Equal(ExpectedGATrackingID, trackingId);
    }

    [Fact]
    public void GetMicrosoftClarityTrackingId_ShouldReturnTrackingId()
    {
        // Arrange
        _sut = SetupTestAnalyticsService();

        // Act
        var trackingId = _sut.GetMicrosoftClarityTrackingId();

        // Assert
        Assert.Equal(ExpectedMSCTrackingID, trackingId);
    }

   
}
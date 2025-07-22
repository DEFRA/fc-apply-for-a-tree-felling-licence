using Forestry.Flo.External.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Forestry.Flo.External.Web.Tests.Infrastructure;

public class ViewDataExtensionsTests
{
    private ViewDataDictionary CreateViewData()
    {
        var metadataProvider = new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider();
        return new ViewDataDictionary(metadataProvider, new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
    }

    [Fact]
    public void ShowBackLink_SetsBackLinkShowAndType()
    {
        var viewData = CreateViewData();

        viewData.ShowBackLink();

        Assert.NotNull(viewData[ViewDataExtensions.BackLinkShowKey]);
        Assert.True((bool?)viewData[ViewDataExtensions.BackLinkShowKey]);
        Assert.Equal(BackLinkType.Default, viewData[ViewDataExtensions.BackLinkTypeKey]);
    }

    [Fact]
    public void ShowBackLink_SetsCustomBackLinkType()
    {
        var viewData = CreateViewData();

        viewData.ShowBackLink();

        Assert.NotNull(viewData[ViewDataExtensions.BackLinkShowKey]);
        Assert.True((bool?)viewData[ViewDataExtensions.BackLinkShowKey]);
        Assert.Equal(BackLinkType.Default, viewData[ViewDataExtensions.BackLinkTypeKey]);
    }

    [Fact]
    public void ManuallySetBackLink_SetsUrl_WhenNotNullOrWhitespace()
    {
        var viewData = CreateViewData();
        const string url = "/test-url";

        viewData.ManuallySetBackLink(url);

        Assert.Equal(url, viewData[ViewDataExtensions.BackLinkUrlKey]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ManuallySetBackLink_RemovesUrl_WhenNullOrWhitespace(string? url)
    {
        var viewData = CreateViewData();
        viewData[ViewDataExtensions.BackLinkUrlKey] = "/existing";

        viewData.ManuallySetBackLink(url);

        Assert.False(viewData.ContainsKey(ViewDataExtensions.BackLinkUrlKey));
    }

    [Fact]
    public void GetBackLinkType_ReturnsType_WhenSet()
    {
        var viewData = CreateViewData();
        viewData[ViewDataExtensions.BackLinkTypeKey] = BackLinkType.Default;

        var result = viewData.GetBackLinkType();

        Assert.Equal(BackLinkType.Default, result);
    }

    [Fact]
    public void GetBackLinkType_ReturnsDefault_WhenNotSet()
    {
        var viewData = CreateViewData();

        var result = viewData.GetBackLinkType();

        Assert.Equal(BackLinkType.Default, result);
    }

    [Fact]
    public void OmitFromNavigationHistory_SetsFlag()
    {
        var viewData = CreateViewData();

        viewData.OmitFromNavigationHistory();
        
        Assert.NotNull(viewData[ViewDataExtensions.OmitFromNavigationHistoryKey]);
        Assert.True((bool?)viewData[ViewDataExtensions.OmitFromNavigationHistoryKey]);
    }

    [Fact]
    public void IsOmittedFromNavigationHistory_ReturnsTrue_WhenSet()
    {
        var viewData = CreateViewData();
        viewData[ViewDataExtensions.OmitFromNavigationHistoryKey] = true;

        var result = viewData.IsOmittedFromNavigationHistory();

        Assert.True(result);
    }

    [Fact]
    public void IsOmittedFromNavigationHistory_ReturnsFalse_WhenNotSet()
    {
        var viewData = CreateViewData();

        var result = viewData.IsOmittedFromNavigationHistory();

        Assert.False(result);
    }

    [Fact]
    public void IsOmittedFromNavigationHistory_ReturnsFalse_WhenSetToFalse()
    {
        var viewData = CreateViewData();
        viewData[ViewDataExtensions.OmitFromNavigationHistoryKey] = false;

        var result = viewData.IsOmittedFromNavigationHistory();

        Assert.False(result);
    }
}
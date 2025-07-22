using Forestry.Flo.External.Web.Services;

namespace Forestry.Flo.External.Web.Tests.Services;

public class BackLinkServiceTests
{
    [Fact]
    public void HistoryList_Is_Updated_With_New_Url()
    {
        // Arrange
        var service = new BackLinkService();
        var existing = new List<string> { "/home", "/form/step1" };
        const string newUrl = "/form/step2?id=3";

        // Act
        var updated = service.UpdateHistoryList(newUrl, existing);

        // Assert
        Assert.Equal(3, updated.Count);
        Assert.Contains("/form/step2?id=3", updated);
    }

    [Fact]
    public void GetBackLink_Returns_Previous_Url()
    {
        // Arrange
        var service = new BackLinkService();
        var list = new List<string> { "/page1", "/page2", "/page3" };

        // Act
        var backUrl = service.GetBackLinkUrl(list);

        // Assert
        Assert.Equal("/page2", backUrl);
    }

    [Fact]
    public void GetBackLink_Returns_Empty_If_Only_One_Item()
    {
        var service = new BackLinkService();
        var list = new List<string> { "/page1" };

        var backUrl = service.GetBackLinkUrl(list);

        Assert.Equal(string.Empty, backUrl);
    }

    [Fact]
    public void UpdateHistoryList_Returns_Empty_When_CurrentUrl_Is_Null_Or_Empty()
    {
        var service = new BackLinkService();

        var resultNull = service.UpdateHistoryList(null, null);
        var resultEmpty = service.UpdateHistoryList(string.Empty, new List<string>());

        Assert.Empty(resultNull);
        Assert.Empty(resultEmpty);
    }

    [Fact]
    public void UpdateHistoryList_Adds_Url_When_History_Is_Empty()
    {
        var service = new BackLinkService();
        var updated = service.UpdateHistoryList("/first", new List<string>());

        Assert.Single(updated);
        Assert.Equal("/first", updated[0]);
    }

    [Fact]
    public void UpdateHistoryList_Does_Not_Add_Duplicate_Consecutive_Url()
    {
        var service = new BackLinkService();
        var existing = new List<string> { "/home", "/form/step1" };
        var updated = service.UpdateHistoryList("/form/step1", existing);

        Assert.Equal(2, updated.Count);
        Assert.Equal("/form/step1", updated[^1]);
    }

    [Fact]
    public void UpdateHistoryList_Is_Case_Insensitive()
    {
        var service = new BackLinkService();
        var existing = new List<string> { "/home", "/form/step1" };
        var updated = service.UpdateHistoryList("/FORM/STEP2", existing);

        Assert.Contains("/form/step2", updated);
    }

    [Fact]
    public void GetBackLink_Returns_Empty_If_History_Is_Null_Or_Empty()
    {
        var service = new BackLinkService();

        Assert.Equal(string.Empty, service.GetBackLinkUrl(null));
        Assert.Equal(string.Empty, service.GetBackLinkUrl(new List<string>()));
    }
}

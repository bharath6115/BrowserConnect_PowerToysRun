using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;

namespace BrowserConnect.Tests.Utils;

public class UrlUtilsTests
{
    [Theory]
    [InlineData("https://example.com/search?q=%s", "cats and dogs", "https://example.com/search?q=cats%20and%20dogs")]
    [InlineData("https://example.com/search?q=%s", "", "https://example.com")]
    [InlineData("https://example.com/docs", "ignored", "https://example.com/docs")]
    public void BuildSearchUrl_BuildsExpectedUrl(string template, string query, string expected)
    {
        Assert.Equal(expected, UrlUtils.BuildSearchUrl(query, template));
    }

    [Theory]
    [InlineData("example.com", true, "https://example.com")]
    [InlineData("https://example.com", true, "https://example.com")]
    [InlineData("http://example.com", true, "http://example.com")]
    [InlineData("file:///C:/Windows/System32/cmd.exe", false, "file:///C:/Windows/System32/cmd.exe")]
    public void TryNormalizeWebUrl_AllowsOnlyHttpAndHttps(string input, bool expectedResult, string expectedNormalized)
    {
        var result = UrlUtils.TryNormalizeWebUrl(input, out var normalized);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedNormalized, normalized);
    }

    [Theory]
    [InlineData("-i https://example.com", "https://example.com", true)]
    [InlineData("https://example.com -i", "https://example.com", true)]
    [InlineData("https://example.com/watch-i", "https://example.com/watch-i", false)]
    public void ParseURL_RemovesOnlyEdgeIncognitoFlag(string rawInput, string expectedUrl, bool expectedIncognito)
    {
        var (url, isIncognitoMentioned) = UrlUtils.ParseURL(rawInput);

        Assert.Equal(expectedUrl, url);
        Assert.Equal(expectedIncognito, isIncognitoMentioned);
    }

    [Fact]
    public void GetCopyUrlForMultiEngineSearch_JoinsGeneratedUrls()
    {
        var engines = new Dictionary<string, string>
        {
            ["google"] = "https://www.google.com/search?q=%s",
            ["yt"] = "https://www.youtube.com/results?search_query=%s",
        };

        string copyUrl = UrlUtils.GetCopyUrlForMultiEngineSearch("lo fi", ["google", "yt"], engines);

        Assert.Equal("https://www.google.com/search?q=lo%20fi, https://www.youtube.com/results?search_query=lo%20fi", copyUrl);
    }
}

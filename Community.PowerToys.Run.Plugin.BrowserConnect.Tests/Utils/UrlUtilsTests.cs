using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrowserConnect.Tests.Utils;

[TestClass]
public class UrlUtilsTests
{
    [DataTestMethod]
    [DataRow("https://example.com/search?q=%s", "cats and dogs", "https://example.com/search?q=cats%20and%20dogs")]
    [DataRow("https://example.com/search?q=%s", "", "https://example.com")]
    [DataRow("https://example.com/docs", "ignored", "https://example.com/docs")]
    public void BuildSearchUrl_BuildsExpectedUrl(string template, string query, string expected)
    {
        Assert.AreEqual(expected, UrlUtils.BuildSearchUrl(query, template));
    }

    [DataTestMethod]
    [DataRow("example.com", true, "https://example.com")]
    [DataRow("https://example.com", true, "https://example.com")]
    [DataRow("http://example.com", true, "http://example.com")]
    [DataRow("file:///C:/Windows/System32/cmd.exe", false, "file:///C:/Windows/System32/cmd.exe")]
    public void TryNormalizeWebUrl_AllowsOnlyHttpAndHttps(
        string input,
        bool expectedResult,
        string expectedNormalized)
    {
        var result = UrlUtils.TryNormalizeWebUrl(input, out var normalized);

        Assert.AreEqual(expectedResult, result);
        Assert.AreEqual(expectedNormalized, normalized);
    }

    [DataTestMethod]
    [DataRow("-i https://example.com", "https://example.com", true)]
    [DataRow("https://example.com -i", "https://example.com", true)]
    [DataRow("https://example.com/watch-i", "https://example.com/watch-i", false)]
    public void ParseURL_RemovesOnlyEdgeIncognitoFlag(
        string rawInput,
        string expectedUrl,
        bool expectedIncognito)
    {
        var (url, isIncognitoMentioned) = UrlUtils.ParseURL(rawInput);

        Assert.AreEqual(expectedUrl, url);
        Assert.AreEqual(expectedIncognito, isIncognitoMentioned);
    }

    [TestMethod]
    public void GetCopyUrlForMultiEngineSearch_JoinsGeneratedUrls()
    {
        var engines = new Dictionary<string, string>
        {
            ["google"] = "https://www.google.com/search?q=%s",
            ["yt"] = "https://www.youtube.com/results?search_query=%s",
        };

        string copyUrl = UrlUtils.GetCopyUrlForMultiEngineSearch(
            "lo fi",
            ["google", "yt"],
            engines
        );

        Assert.AreEqual(
            "https://www.google.com/search?q=lo%20fi, https://www.youtube.com/results?search_query=lo%20fi",
            copyUrl
        );
    }
}
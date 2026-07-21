using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrowserConnect.Tests.Utils;

[TestClass]
public class InputUtilsTests
{
    [DataTestMethod]
    [DataRow("yt cats -i", false, "yt cats", true)]
    [DataRow("-i yt cats", false, "yt cats", true)]
    [DataRow("yt cats", true, "yt cats", true)]
    [DataRow("yt cats", false, "yt cats", false)]
    public void ParseInput_RemovesIncognitoToken_AndAppliesDefault(
        string rawInput,
        bool isIncognitoDefault,
        string expectedInput,
        bool expectedIncognito)
    {
        var (cleanInput, inIncognito) = InputUtils.ParseInput(rawInput, isIncognitoDefault);

        Assert.AreEqual(expectedInput, cleanInput);
        Assert.AreEqual(expectedIncognito, inIncognito);
    }

    [TestMethod]
    public void CheckIfEndsWithAndRemoveSymbol_RemovesTrailingTriggerOnly()
    {
        var (cleanInput, isSymbolPresent) =
            InputUtils.CheckIfEndsWithAndRemoveSymbol("yt lo-fi ;", ";");

        Assert.IsTrue(isSymbolPresent);
        Assert.AreEqual("yt lo-fi", cleanInput);
    }

    [TestMethod]
    public void CheckIfEndsWithAndRemoveSymbol_ReturnsOriginal_WhenTriggerIsMissing()
    {
        var (cleanInput, isSymbolPresent) =
            InputUtils.CheckIfEndsWithAndRemoveSymbol("yt lo-fi", ";");

        Assert.IsFalse(isSymbolPresent);
        Assert.AreEqual("yt lo-fi", cleanInput);
    }

    [TestMethod]
    public void ParseMultiEngineSearchInput_SeparatesValidAndInvalidEngines()
    {
        var engines = new Dictionary<string, string>
        {
            ["yt"] = "https://www.youtube.com/results?search_query=%s",
            ["google"] = "https://www.google.com/search?q=%s",
        };

        var (searchQuery, targetEngines, invalidEngines) =
            InputUtils.ParseMultiEngineSearchInput(
                "@yt bad google : synthwave",
                engines
            );

        Assert.AreEqual("synthwave", searchQuery);

        CollectionAssert.AreEqual(
            new[] { "yt", "google" },
            targetEngines.ToArray()
        );

        CollectionAssert.AreEqual(
            new[] { "bad" },
            invalidEngines.ToArray()
        );
    }

    [TestMethod]
    public void TryParseLiveSearchInput_ParsesSavedProviderHistoryEntry()
    {
        string input = string.Join(
            SymbolConsts.RECORD_SEPERATOR,
            "Video title",
            "https://example.com/video",
            "youtube#abc123"
        );

        bool parsed = InputUtils.TryParseLiveSearchInput(
            input,
            "_LIVE",
            out var query,
            out var url,
            out var thumbnailRef
        );

        Assert.IsTrue(parsed);
        Assert.AreEqual("Video title", query);
        Assert.AreEqual("https://example.com/video", url);
        Assert.AreEqual("youtube#abc123", thumbnailRef);
    }

    [TestMethod]
    public void TryParseLiveSearchInput_ReturnsFalseForUnexpectedFormat()
    {
        bool parsed = InputUtils.TryParseLiveSearchInput(
            "plain history query",
            "_LIVE",
            out var query,
            out var url,
            out var thumbnailRef
        );

        Assert.IsFalse(parsed);
        Assert.AreEqual(string.Empty, query);
        Assert.AreEqual(string.Empty, url);
        Assert.AreEqual(string.Empty, thumbnailRef);
    }
}
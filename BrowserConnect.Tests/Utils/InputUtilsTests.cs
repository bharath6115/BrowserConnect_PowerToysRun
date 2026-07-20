using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;

namespace BrowserConnect.Tests.Utils;

public class InputUtilsTests
{
    [Theory]
    [InlineData("yt cats -i", false, "yt cats", true)]
    [InlineData("-i yt cats", false, "yt cats", true)]
    [InlineData("yt cats", true, "yt cats", true)]
    [InlineData("yt cats", false, "yt cats", false)]
    public void ParseInput_RemovesIncognitoToken_AndAppliesDefault(string rawInput, bool isIncognitoDefault, string expectedInput, bool expectedIncognito)
    {
        var (cleanInput, inIncognito) = InputUtils.ParseInput(rawInput, isIncognitoDefault);

        Assert.Equal(expectedInput, cleanInput);
        Assert.Equal(expectedIncognito, inIncognito);
    }

    [Fact]
    public void CheckIfEndsWithAndRemoveSymbol_RemovesTrailingTriggerOnly()
    {
        var (cleanInput, isSymbolPresent) = InputUtils.CheckIfEndsWithAndRemoveSymbol("yt lo-fi ;", ";");

        Assert.True(isSymbolPresent);
        Assert.Equal("yt lo-fi", cleanInput);
    }

    [Fact]
    public void CheckIfEndsWithAndRemoveSymbol_ReturnsOriginal_WhenTriggerIsMissing()
    {
        var (cleanInput, isSymbolPresent) = InputUtils.CheckIfEndsWithAndRemoveSymbol("yt lo-fi", ";");

        Assert.False(isSymbolPresent);
        Assert.Equal("yt lo-fi", cleanInput);
    }

    [Fact]
    public void ParseMultiEngineSearchInput_SeparatesValidAndInvalidEngines()
    {
        var engines = new Dictionary<string, string>
        {
            ["yt"] = "https://www.youtube.com/results?search_query=%s",
            ["google"] = "https://www.google.com/search?q=%s",
        };

        var (searchQuery, targetEngines, invalidEngines) = InputUtils.ParseMultiEngineSearchInput("@yt bad google : synthwave", engines);

        Assert.Equal("synthwave", searchQuery);
        Assert.Equal(["yt", "google"], targetEngines);
        Assert.Equal(["bad"], invalidEngines);
    }

    [Fact]
    public void TryParseLiveSearchInput_ParsesSavedProviderHistoryEntry()
    {
        string input = string.Join(SymbolConsts.RECORD_SEPERATOR, "Video title", "https://example.com/video", "youtube#abc123");

        bool parsed = InputUtils.TryParseLiveSearchInput(input, "_LIVE", out var query, out var url, out var thumbnailRef);

        Assert.True(parsed);
        Assert.Equal("Video title", query);
        Assert.Equal("https://example.com/video", url);
        Assert.Equal("youtube#abc123", thumbnailRef);
    }

    [Fact]
    public void TryParseLiveSearchInput_ReturnsFalseForUnexpectedFormat()
    {
        bool parsed = InputUtils.TryParseLiveSearchInput("plain history query","_LIVE", out var query, out var url, out var thumbnailRef);

        Assert.False(parsed);
        Assert.Equal(string.Empty, query);
        Assert.Equal(string.Empty, url);
        Assert.Equal(string.Empty, thumbnailRef);
    }
}

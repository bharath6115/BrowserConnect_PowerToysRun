using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.Settings;

namespace BrowserConnect.Tests.Services;

public class HistoryServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "BrowserConnect.Tests", Guid.NewGuid().ToString("N"));

    public HistoryServiceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void SaveToHistory_WritesEntries_WhenHistoryIsEnabled()
    {
        string historyPath = Path.Combine(_tempDirectory, "history.txt");
        var settings = new PluginSettings { IsHistoryEnabled = true };
        var service = new HistoryService(historyPath, settings);

        service.SaveToHistory("lo-fi beats", "yt", inIncognito: false);

        string line = Assert.Single(File.ReadAllLines(historyPath));
        Assert.Contains("|yt|lo-fi%20beats|False", line);
    }

    [Fact]
    public void SaveToHistory_SkipsIncognitoEntries_WhenIncognitoHistoryIsDisabled()
    {
        string historyPath = Path.Combine(_tempDirectory, "history.txt");
        var settings = new PluginSettings
        {
            IsHistoryEnabled = true,
            RecordIncognitoHistory = false,
        };
        var service = new HistoryService(historyPath, settings);

        service.SaveToHistory("secret query", "google", inIncognito: true);

        Assert.False(File.Exists(historyPath));
    }

    [Fact]
    public void SaveToHistory_WritesIncognitoEntries_WhenIncognitoHistoryIsEnabled()
    {
        string historyPath = Path.Combine(_tempDirectory, "history.txt");
        var settings = new PluginSettings
        {
            IsHistoryEnabled = true,
            RecordIncognitoHistory = true,
        };
        var service = new HistoryService(historyPath, settings);

        service.SaveToHistory("-i secret query", "google", inIncognito: true);

        string line = Assert.Single(File.ReadAllLines(historyPath));
        Assert.Contains("|google|-i%20secret%20query|True", line);
    }

    [Fact]
    public void SaveToHistory_SkipsEntries_WhenHistoryIsDisabled()
    {
        string historyPath = Path.Combine(_tempDirectory, "history.txt");
        var settings = new PluginSettings { IsHistoryEnabled = false };
        var service = new HistoryService(historyPath, settings);

        service.SaveToHistory("lo-fi beats", "yt", inIncognito: false);

        Assert.False(File.Exists(historyPath));
    }

    [Fact]
    public void DeleteEntry_RemovesEntryFromFileAndCache()
    {
        string historyPath = Path.Combine(_tempDirectory, "history.txt");
        var settings = new PluginSettings { IsHistoryEnabled = true };
        var service = new HistoryService(historyPath, settings);

        service.SaveToHistory("first query", "google", inIncognito: false);
        service.SaveToHistory("second query", "yt", inIncognito: false);
        string entryToDelete = service.GetHistoryCache().Single(line => line.Contains("|google|first%20query|False"));

        service.DeleteEntry(entryToDelete);

        Assert.DoesNotContain(entryToDelete, service.GetHistoryCache());
        Assert.DoesNotContain(entryToDelete, File.ReadAllLines(historyPath));
        Assert.Single(File.ReadAllLines(historyPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}

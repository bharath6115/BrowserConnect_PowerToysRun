using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrowserConnect.Tests.Services;

[TestClass]
public class HistoryServiceTests
{
    private string _tempDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "BrowserConnect.Tests",
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(_tempDirectory);
    }

    [TestMethod]
    public void SaveToHistory_WritesEntries_WhenHistoryIsEnabled()
    {
        string historyPath = Path.Combine(_tempDirectory, "history.txt");
        var settings = new PluginSettings { IsHistoryEnabled = true };
        var service = new HistoryService(historyPath, settings);

        service.SaveToHistory("lo-fi beats", "yt", inIncognito: false);

        string[] lines = File.ReadAllLines(historyPath);

        Assert.AreEqual(1, lines.Length);
        StringAssert.Contains(lines[0], "|yt|lo-fi%20beats|False");
    }

    [TestMethod]
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

        Assert.IsFalse(File.Exists(historyPath));
    }

    [TestMethod]
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

        string[] lines = File.ReadAllLines(historyPath);

        Assert.AreEqual(1, lines.Length);
        StringAssert.Contains(lines[0], "|google|-i%20secret%20query|True");
    }

    [TestMethod]
    public void SaveToHistory_SkipsEntries_WhenHistoryIsDisabled()
    {
        string historyPath = Path.Combine(_tempDirectory, "history.txt");
        var settings = new PluginSettings { IsHistoryEnabled = false };
        var service = new HistoryService(historyPath, settings);

        service.SaveToHistory("lo-fi beats", "yt", inIncognito: false);

        Assert.IsFalse(File.Exists(historyPath));
    }

    [TestMethod]
    public void DeleteEntry_RemovesEntryFromFileAndCache()
    {
        string historyPath = Path.Combine(_tempDirectory, "history.txt");
        var settings = new PluginSettings { IsHistoryEnabled = true };
        var service = new HistoryService(historyPath, settings);

        service.SaveToHistory("first query", "google", inIncognito: false);
        service.SaveToHistory("second query", "yt", inIncognito: false);

        string entryToDelete = service
            .GetHistoryCache()
            .Single(line => line.Contains("|google|first%20query|False"));

        service.DeleteEntry(entryToDelete);

        CollectionAssert.DoesNotContain(
            service.GetHistoryCache().ToList(),
            entryToDelete
        );

        CollectionAssert.DoesNotContain(
            File.ReadAllLines(historyPath).ToList(),
            entryToDelete
        );

        Assert.AreEqual(1, File.ReadAllLines(historyPath).Length);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
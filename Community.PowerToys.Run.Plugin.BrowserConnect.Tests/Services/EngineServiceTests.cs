using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrowserConnect.Tests.Services;

[TestClass]
public class EngineServiceTests
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
    public void Constructor_CreatesAndLoadsDefaultEngines_WhenFileDoesNotExist()
    {
        string enginePath = Path.Combine(_tempDirectory, "searchEngines.txt");

        var service = new EngineService(enginePath);

        Assert.IsTrue(File.Exists(enginePath));
        Assert.IsTrue(service.Count > 0);
        CollectionAssert.Contains(service.GetEngines().Keys.ToList(), "google");
        CollectionAssert.Contains(service.GetEngines().Keys.ToList(), "yt");
    }

    [TestMethod]
    public void AddOrUpdateEngine_ReplacesExistingAlias()
    {
        string enginePath = Path.Combine(_tempDirectory, "searchEngines.txt");
        var service = new EngineService(enginePath);

        service.AddOrUpdateEngine("test", "https://first.example/search?q=%s");
        service.AddOrUpdateEngine("test", "https://second.example/search?q=%s");

        Assert.AreEqual(
            "https://second.example/search?q=%s",
            service.GetEngines()["test"]
        );

        Assert.AreEqual(
            1,
            File.ReadAllLines(enginePath)
                .Count(line => line.StartsWith("test ", StringComparison.OrdinalIgnoreCase))
        );
    }

    [TestMethod]
    public void DeleteEngine_RemovesAliasAndReportsMissingAlias()
    {
        string enginePath = Path.Combine(_tempDirectory, "searchEngines.txt");
        var service = new EngineService(enginePath);

        service.AddOrUpdateEngine("temp", "https://example.com/search?q=%s");

        bool deleted = service.DeleteEngine("temp");
        bool deletedAgain = service.DeleteEngine("temp");

        Assert.IsTrue(deleted);
        Assert.IsFalse(deletedAgain);

        CollectionAssert.DoesNotContain(
            service.GetEngines().Keys.ToList(),
            "temp"
        );

        Assert.IsFalse(
            File.ReadAllLines(enginePath)
                .Any(line => line.StartsWith("temp ", StringComparison.OrdinalIgnoreCase))
        );
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
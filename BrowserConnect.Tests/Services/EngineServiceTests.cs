using Community.PowerToys.Run.Plugin.BrowserConnect.Services;

namespace BrowserConnect.Tests.Services;

public class EngineServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "BrowserConnect.Tests", Guid.NewGuid().ToString("N"));

    public EngineServiceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Constructor_CreatesAndLoadsDefaultEngines_WhenFileDoesNotExist()
    {
        string enginePath = Path.Combine(_tempDirectory, "searchEngines.txt");

        var service = new EngineService(enginePath);

        Assert.True(File.Exists(enginePath));
        Assert.True(service.Count > 0);
        Assert.Contains("google", service.GetEngines().Keys);
        Assert.Contains("yt", service.GetEngines().Keys);
    }

    [Fact]
    public void AddOrUpdateEngine_ReplacesExistingAlias()
    {
        string enginePath = Path.Combine(_tempDirectory, "searchEngines.txt");
        var service = new EngineService(enginePath);

        service.AddOrUpdateEngine("test", "https://first.example/search?q=%s");
        service.AddOrUpdateEngine("test", "https://second.example/search?q=%s");

        Assert.Equal("https://second.example/search?q=%s", service.GetEngines()["test"]);
        Assert.Single(File.ReadAllLines(enginePath), line => line.StartsWith("test ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DeleteEngine_RemovesAliasAndReportsMissingAlias()
    {
        string enginePath = Path.Combine(_tempDirectory, "searchEngines.txt");
        var service = new EngineService(enginePath);
        service.AddOrUpdateEngine("temp", "https://example.com/search?q=%s");

        bool deleted = service.DeleteEngine("temp");
        bool deletedAgain = service.DeleteEngine("temp");

        Assert.True(deleted);
        Assert.False(deletedAgain);
        Assert.DoesNotContain("temp", service.GetEngines().Keys);
        Assert.DoesNotContain(File.ReadAllLines(enginePath), line => line.StartsWith("temp ", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}

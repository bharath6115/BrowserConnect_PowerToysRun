using Community.PowerToys.Run.Plugin.BrowserConnect.AbstractBase;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Providers.SeriesGraph;

public class SeriesGraphService : SearchServiceBase
{
    private SeriesGraphProvider _provider;
    protected override string ProviderId => "SeriesGraph";
    protected override string DefaultThumbnailUrl => "https://seriesgraph.com";

    public SeriesGraphService(SeriesGraphProvider seriesgraphProvider, ThumbnailManager thumbnailManager, HistoryService historyService, IconService iconService, ActionService actionService) : base(thumbnailManager, historyService, iconService, actionService)
    {
        _provider = seriesgraphProvider;
    }

    protected override Task<List<CustomSearchResult>> SearchAsync(string query)
    {
        return _provider.SearchAsync(query);
    }
}
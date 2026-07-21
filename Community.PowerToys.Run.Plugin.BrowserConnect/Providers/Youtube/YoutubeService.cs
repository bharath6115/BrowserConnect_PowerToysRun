using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.AbstractBase;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Providers.Youtube;

public class YoutubeService : SearchServiceBase
{
    private readonly YoutubeProvider _provider;
    protected override string ProviderId => "Youtube";
    protected override string DefaultThumbnailUrl => "https://www.youtube.com";

    public YoutubeService(YoutubeProvider youtubeProvider, ThumbnailManager thumbnailManager, HistoryService historyService, IconService iconService, ActionService actionService) : base(thumbnailManager, historyService, iconService, actionService)
    {
        _provider = youtubeProvider;
    }
    protected override Task<List<CustomSearchResult>> SearchAsync(string query)
    {
        return _provider.SearchAsync(query);
    }
    public void ReloadYoutubeApiData() => _provider.ReloadYoutubeApiData();
}

using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.AbstractBase;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Providers.Anilist;

public class AnilistService : SearchServiceBase
{
    private AnilistProvider _provider;
    protected override string ProviderId => "Anilist";
    protected override string DefaultThumbnailUrl => "https://anilist.co";

    public AnilistService(AnilistProvider anilistProvider, ThumbnailManager thumbnailManager, HistoryService historyService, IconService iconService, ActionService actionService) : base(thumbnailManager, historyService, iconService, actionService)
    {
        _provider = anilistProvider;
    }

    protected override Task<List<CustomSearchResult>> SearchAsync(string query)
    {
        return _provider.SearchAsync(query);
    }
}
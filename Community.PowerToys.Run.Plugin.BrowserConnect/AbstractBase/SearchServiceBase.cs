using Wox.Plugin;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Community.PowerToys.Run.Plugin.BrowserConnect.Interfaces;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.AbstractBase;

public abstract class SearchServiceBase : ISearchService
{
    private ThumbnailManager _thumbnailManager;
    private HistoryService _historyService;
    private IconService _iconService;
    private ActionService _actionService;

    public SearchServiceBase(ThumbnailManager thumbnailManager, HistoryService historyService, IconService iconService, ActionService actionService)
    {
        _thumbnailManager = thumbnailManager;
        _historyService = historyService;
        _iconService = iconService;
        _actionService = actionService;
    }

    protected abstract Task<List<CustomSearchResult>> SearchAsync(string query);
    protected abstract string ProviderId { get; }
    protected abstract string DefaultThumbnailUrl { get; }

    public List<Result> GetResults(List<Result> defaultResult, string engineKey, string query, bool inIncognito)
    {
        var results = defaultResult;

        try
        {
            // We cant await because the function cant be async, so we use .GetAwaiter().GetResult() -> basically what await does. We delegate this to other thread using Task.Run() because not doing will cause deadlock:
            // UI (this) thread will pause execution if we do return func().GetAwaiter().GetResult() (it is blocking, while await isnt) so we delegate this to other thread to not freeze the UI thread, and return the result when we get it
            var searchResults = Task.Run(async () =>
            {
                await _thumbnailManager.EnsureProviderInitializedAsync(ProviderId, $"https://www.google.com/s2/favicons?domain={DefaultThumbnailUrl}&sz=128");
                var fetched = await SearchAsync(query);
                //TODO: Add a fix for those entries which dont have a thumbnail.
                await Task.WhenAll(
                    fetched.Select(entry =>
                        _thumbnailManager.EnsureThumbnailExistsAsync(ProviderId, entry.Id, entry.ThumbnailUrl!)
                    )
                );
                return fetched;
            }).GetAwaiter().GetResult();

            if(searchResults.Count is 0){
                results.AddRange(ResultUtils.GetSearchProviderEmptyResult(_iconService));
                return results;
            }

            foreach(var entry in searchResults)
            {
                results.Add(CreateResult(entry,inIncognito));
            }
        }
        catch (Exception ex)
        {
            // TODO: add a way to inform about api errors (Anilist for example)
            Logger.Log($"{ProviderId} search failed: {ex.Message}", "ERROR");
            results.AddRange(ResultUtils.GetSearchProviderErrorResult(_iconService,ex.Message));
        }
        return results;
    }

    private Result CreateResult(CustomSearchResult entry, bool inIncognito)
    {
        string thumbnailRef = $"{ProviderId}#{entry.Id}";
        return new Result
            {
                Title = entry.Title,
                SubTitle = entry.Subtitle ?? "",
                IcoPath = _thumbnailManager.GetThumbnailPath(ProviderId, entry.Id),
                Score = entry.Score,
                Action = _ =>
                {
                    _actionService.ExecuteLive(entry.Title,entry.Url,thumbnailRef,inIncognito);
                    return true;
                },
                ContextData = new CustomResultContext
                {
                    SearchType = SearchType.LIVE,
                    Title = entry.Title,
                    Url = entry.Url,
                    ThumbnailRef = thumbnailRef, 
                    IsIncognito = inIncognito,
                },
                QueryTextDisplay = entry.Title
            };
    }
}
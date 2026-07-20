using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services;

public class ActionService
{
    private readonly HistoryService _historyService;
    private readonly EngineService _engineService;

    public ActionService(HistoryService historyService, EngineService engineService)
    {
        _historyService = historyService;
        _engineService = engineService;
    }

    /// <summary>
    /// Opens a direct URL in the browser and optionally records it in history.
    /// </summary>
    /// <param name="URL">The URL to open.</param>
    /// <param name="inIncognito">Whether to open the URL in incognito mode.</param>
    /// <param name="viaHistory"> Whether the action originated from history replay. When true, the entry is not recorded again.</param>
    public void ExecuteUrl(string URL, bool inIncognito, bool viaHistory = false)
    {
        Logger.Log($"Opening direct URL {(viaHistory ? "via history " : "")}: {URL} (Incognito: {inIncognito})", "ACTION");
        if(!viaHistory) _historyService.SaveToHistory(URL, "_URL", inIncognito);
        BrowserService.OpenBrowser(URL, inIncognito);   
    }

    /// <summary>
    /// Executes the same search query across multiple search engines and optionally records it in history.
    /// </summary>
    /// <param name="searchQuery">The search query to execute.</param>
    /// <param name="targetEngines">The target search engine aliases.</param>
    /// <param name="inIncognito">Whether to open the searches in incognito mode.</param>
    /// <param name="viaHistory"> Whether the action originated from history replay. When true, the entry is not recorded again.</param>
    public void ExecuteMulti(string searchQuery,List<string> targetEngines, bool inIncognito, bool viaHistory = false)
    {
        var engines = _engineService.GetEngines();
        Logger.Log($"Multi-engine search triggered {(viaHistory ? "via history " : "")}for: {string.Join(", ", targetEngines)}", "ACTION");
        if(!viaHistory) _historyService.SaveToHistory(searchQuery,targetEngines,inIncognito);
        foreach (var engine in targetEngines)
        {
            string tempUrl = UrlUtils.BuildSearchUrl(searchQuery, engines[engine]);
            Logger.Log($"Opening {engine}: {tempUrl}", "TRACE");
            BrowserService.OpenBrowser(tempUrl, inIncognito);
        }
    }

    /// <summary>
    /// Executes a search using a single configured search engine and optionally records it in history.
    /// </summary>
    /// <param name="searchQuery">The search query to execute.</param>
    /// <param name="searchEngine">The target search engine alias.</param>
    /// <param name="finalUrl">The resolved URL to open.</param>
    /// <param name="inIncognito">Whether to open the search in incognito mode.</param>
    /// <param name="viaHistory"> Whether the action originated from history replay. When true, the entry is not recorded again.</param>
    public void ExecuteDefault(string searchQuery, string searchEngine, string finalUrl, bool inIncognito, bool viaHistory = false)
    {
        Logger.Log($"Opening engine {searchEngine} {(viaHistory ? "via history " : "")}: {finalUrl} (Incognito: {inIncognito})", "ACTION");
        if(!viaHistory) _historyService.SaveToHistory(searchQuery, searchEngine, inIncognito);
        BrowserService.OpenBrowser(finalUrl, inIncognito);
    }
    
    /// <summary>
    /// Opens a live provider result and optionally records it in history.
    /// </summary>
    /// <param name="title">The title of the selected live result.</param>
    /// <param name="url">The URL of the selected live result.</param>
    /// <param name="thumbnailRef">The provider thumbnail reference associated with the result.</param>
    /// <param name="inIncognito">Whether to open the result in incognito mode.</param>
    /// <param name="viaHistory"> Whether the action originated from history replay. When true, the entry is not recorded again.</param>
    public void ExecuteLive(string title, string url, string thumbnailRef, bool inIncognito, bool viaHistory = false)
    {
        Logger.Log($"Opening {thumbnailRef.Split("#")[0]} entry  {(viaHistory ? "via history " : "")}: {url} (Incognito: {inIncognito})", "ACTION");
        if(!viaHistory) _historyService.SaveToHistory(title, url, thumbnailRef, inIncognito);
        BrowserService.OpenBrowser(url, inIncognito);
    }
}
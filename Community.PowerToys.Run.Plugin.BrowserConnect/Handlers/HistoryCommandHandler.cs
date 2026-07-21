using Wox.Plugin;
using Wox.Infrastructure;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Handlers;
public class HistoryCommandHandler
{
    private readonly EngineService _engineService;
    private readonly HistoryService _historyService;
    private readonly IconService _iconService;
    private readonly ActionService _actionService;
    private readonly BrowserPlugin _plugin;
    private readonly ThumbnailManager _thumbnailManager;

    public HistoryCommandHandler(EngineService engineService, HistoryService historyService, IconService iconService, ActionService actionService, BrowserPlugin plugin, ThumbnailManager thumbnailManager)
    {
        _engineService = engineService;
        _historyService = historyService;
        _iconService = iconService;
        _actionService = actionService;
        _plugin = plugin;
        _thumbnailManager = thumbnailManager;
    }
    
    //TODO: Improve the function to prevent reading history from .txt file always
    /// <summary>
    /// Builds history results for the current history command input.
    /// </summary>
    /// <param name="input">Raw history command input, including the history prefix.</param>
    /// <returns>History results, a fallback search result, or an error result.</returns>
    public List<Result> HandleHistoryCommand(string input)
    {
        string searchInput = input.TrimStart('!').Trim();
        var cache = _historyService.GetHistoryCache();
        if (cache.Count == 0) return ResultUtils.GetEmptyHistoryResult(_iconService);

        try
        {
            // Reusable deduplication set
            var uniqueQueries = new HashSet<string>();
            var finalResults = new List<Result>();

            // Process Cache First (always available and recent)
            if (cache.Count is not 0)
            {
                var cacheLines = cache.AsEnumerable().Reverse().ToList();
                finalResults.AddRange(ProcessHistoryLines(cacheLines, searchInput, uniqueQueries));
            }

            // Use .txt if searching and results are fewer than desired count. Only do this if searchInput is not empty
            if (!string.IsNullOrWhiteSpace(searchInput) && finalResults.Count < _plugin.Settings.HistoryCacheSize)
            {
                var fullHistory = _historyService.GetFullHistory();
                if (fullHistory.Count > cache.Count)
                {
                    var fullLines = fullHistory.Take(fullHistory.Count - cache.Count).Reverse().ToList();
                    finalResults.AddRange(ProcessHistoryLines(fullLines, searchInput, uniqueQueries));
                }
            }

            if (finalResults.Count is 0)
            {
                if (string.IsNullOrWhiteSpace(searchInput)) return ResultUtils.GetEmptyHistoryResult(_iconService);

                var res = _plugin.Query(new Query(searchInput)).FirstOrDefault(); 
                return new List<Result> {
                    new Result {
                        Title = "No history match - search instead?",
                        SubTitle = $"Search for: {searchInput}",
                        Action = res?.Action,
                        IcoPath = res?.IcoPath ?? _iconService.GetIconPath(IconConsts.HISTORY)
                    }
                };
            }

            return finalResults
                .OrderByDescending(r => r.Score)
                .Take(_plugin.Settings.HistoryCacheSize)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.Log($"Error displaying history: {ex.Message}", "ERROR");
            return ResultUtils.GetHistoryErrorResult(_iconService, ex.Message);
        }
    }

    /// <summary>
    /// Converts saved history lines into display results.
    /// </summary>
    /// <param name="lines">History lines to process, usually in newest-first order.</param>
    /// <param name="searchInput">Optional text used to filter history results.</param>
    /// <param name="uniqueQueries">Set used to skip duplicate engine and query pairs.</param>
    /// <returns>Matching history results.</returns>
    private List<Result> ProcessHistoryLines(List<string> lines, string searchInput, HashSet<string> uniqueQueries)
    {
        var results = new List<Result>();
        int index = 0;

        foreach (var line in lines)
        {
            var parts = line.Split('|', 4);
            if (parts.Length != 4) continue;

            string timestamp = parts[0].Trim();
            string engineKey = parts[1].Trim();
            string queryPayload = Uri.UnescapeDataString(parts[2].Trim());
            if (uniqueQueries.Contains(engineKey.ToLowerInvariant() + ":" + queryPayload.ToLowerInvariant())) continue;
            bool isIncognito = bool.TryParse(parts[3], out bool incognito) && incognito;

            int score = 0;
            if (!string.IsNullOrWhiteSpace(searchInput))
            {
                var match = StringMatcher.FuzzySearch(searchInput, queryPayload);
                score = match.Score;
            }
            else
            {
                // If no search input, prioritize recency using high base score
                score = 50000 - index++;
            }

            if (string.IsNullOrWhiteSpace(searchInput) || score > 0)
            {
                uniqueQueries.Add(engineKey.ToLowerInvariant() + ":" + queryPayload.ToLowerInvariant());

                var (title, subtitle, iconPath, contextData, queryTextDisplay) = GetResultValues(timestamp, engineKey, queryPayload,isIncognito, _engineService.GetEngines());
                contextData.HistoryLine = line;

                results.Add(new Result
                {
                    Title = title,
                    SubTitle = subtitle,
                    IcoPath = iconPath,
                    Score = score,
                    Action = _ =>
                    {
                        ProcessHistoryExecution(engineKey,queryPayload,isIncognito);
                        return true;
                    },
                    ContextData = contextData,
                    QueryTextDisplay = queryTextDisplay
                });
            }

            if (results.Count >= _plugin.Settings.HistoryCacheSize) break;
        }
        return results;
    }

    /// <summary>
    /// Opens a saved history entry using the browser mode and engine recorded with it.
    /// </summary>
    /// <param name="engineKey">Engine key saved with the history entry.</param>
    /// <param name="rawQueryPayload">Saved query payload to execute.</param>
    /// <param name="inIncognito">Whether the entry should open in incognito mode.</param>
    public void ProcessHistoryExecution(string engineKey, string rawQueryPayload, bool inIncognito)
    {
        string queryPayload = rawQueryPayload.Trim();
        var finalUrl = GetUrlFromHistory(engineKey,queryPayload);

        switch (engineKey)
        {
            case "_URL":
                _actionService.ExecuteUrl(finalUrl, inIncognito, true);
                break;

            case "_LIVE":
                _ = InputUtils.TryParseLiveSearchInput(queryPayload, engineKey, out var query, out var url, out var thumbnailRef);
                _actionService.ExecuteLive(query,url,thumbnailRef, inIncognito, true);
                break;

            case "_MULTI":
                var (searchQueryMulti, targetEngines) = InputUtils.ParseMultiEngineHistoryQuery(queryPayload);
                _actionService.ExecuteMulti(searchQueryMulti, targetEngines, inIncognito, true);
                break;

            default:
                _actionService.ExecuteDefault(queryPayload,engineKey,finalUrl,inIncognito,true);
                break;
        }
    }

    private string GetUrlFromHistory(string engineKey, string queryPayload)
    {
        var engines = _engineService.GetEngines();

        switch (engineKey)
        {
            case "_URL":
                return queryPayload;

            case "_LIVE":
                _ = InputUtils.TryParseLiveSearchInput(queryPayload, engineKey, out var _, out var url, out var _);
                return url;

            case "_MULTI":
                var (searchQueryMulti, targetEngines) = InputUtils.ParseMultiEngineHistoryQuery(queryPayload);
                return UrlUtils.GetCopyUrlForMultiEngineSearch(searchQueryMulti,targetEngines,engines);

            default:
                if (engines.TryGetValue(engineKey, out var template))
                {
                    return UrlUtils.BuildSearchUrl(queryPayload, template);
                }
                else
                {   
                    //TODO: Set the default value to a default search engines like google, brave
                    var first = engines.FirstOrDefault();
                    return first.Value.Replace("%s", Uri.EscapeDataString(queryPayload));
                }
        }
    }

    /// <summary>
    /// Builds the display values and replay context for a history result.
    /// Current formats:
    /// URL	    : <time> | _URL   |url                          |incognito
    /// Live	: <time> | _LIVE  |title<RS>URL<RS>ThumbnailRef |incognito
    /// Normal	: <time> | alias  |normalized_query             |incognito
    /// Multi	: <time> | _MULTI |engines.join(", ")<RS>Query  |incognito
    /// </summary>
    /// <param name="timestamp">Timestamp saved with the history entry. (1st entry in history format)</param>
    /// <param name="engineKey">Engine key saved with the history entry. (2nd entry in history format)</param>
    /// <param name="queryPayload">Decoded saved payload. (3rd entry in history format)</param>
    /// <param name="isIncognito">Whether the entry was opened in incognito mode. (4th entry in history format)</param>
    /// <returns>
    /// Tuple containing the title, subtitle, icon path, context data, and query text display.
    /// </returns>
    private (string title, string subtitle, string iconPath, CustomResultContext contextData, string queryTextDisplay) GetResultValues(string timestamp, string engineKey, string queryPayload, bool isIncognito, IReadOnlyDictionary<string,string> engines)
    {
        string title="", queryTextDisplay="", iconPath;
        var subtitleParts = new List<string>{$"Last searched: {timestamp}"};
        CustomResultContext contextData;

        switch (engineKey)
        {
            case "_URL":
                title = queryPayload;
                iconPath = isIncognito ? _iconService.GetIconPath(IconConsts.INCOGNITO) : _iconService.GetIconPath(IconConsts.DEFAULT);
                queryTextDisplay = queryPayload;
                contextData = new()
                {
                    SearchType = SearchType.URL,
                    Url = queryPayload,
                    IsIncognito = isIncognito,
                };
                break;

            case "_LIVE":
                _ = InputUtils.TryParseLiveSearchInput(queryPayload, engineKey, out var query_live, out var url, out var thumbnailRef);
                title = query_live;
                subtitleParts.Add($"via {thumbnailRef.Split("#")[0]}");
                iconPath = _thumbnailManager.GetThumbnailPath(thumbnailRef);
                queryTextDisplay = url;
                contextData = new()
                {
                    SearchType = SearchType.LIVE,
                    Title = query_live,
                    Url = url,
                    ThumbnailRef = thumbnailRef,
                    IsIncognito = isIncognito,
                };
                break;

            case "_MULTI":
                var (query_multi, targetEngines) = InputUtils.ParseMultiEngineHistoryQuery(queryPayload);
                title = query_multi;
                subtitleParts.Add($"via {string.Join(", ", targetEngines)}");
                iconPath = isIncognito ? _iconService.GetIconPath(IconConsts.INCOGNITO) : _iconService.GetIconPath(IconConsts.DEFAULT);
                queryTextDisplay = query_multi;
                contextData = new()
                {
                    SearchType = SearchType.MULTI,
                    EncodedQuery = queryPayload,
                    Url = UrlUtils.GetCopyUrlForMultiEngineSearch(query_multi,targetEngines,engines),
                    IsIncognito = isIncognito,
                };
                break;

            default:
                title = string.IsNullOrEmpty(queryPayload) ?
                    (engines.TryGetValue(engineKey, out var template) && template.Contains("%s")) ? UrlUtils.GetBaseUrl(template) : engineKey
                    : queryPayload;
                if (!string.IsNullOrEmpty(queryPayload)) subtitleParts.Add($"via {engineKey}");
                iconPath = isIncognito ? _iconService.GetIconPath(IconConsts.INCOGNITO) : _iconService.GetIconPath(engineKey);
                queryTextDisplay = title;
                contextData = new()
                {
                    SearchType = SearchType.DEFAULT,
                    SearchQuery = queryPayload,
                    SearchEngine = engineKey,
                    Url = GetUrlFromHistory(engineKey, queryPayload),
                    IsIncognito = isIncognito,
                };
                break;
        }

        subtitleParts.Add($"{(isIncognito ? "[Incognito]" : "")}");

        return (title, string.Join(" ", subtitleParts), iconPath, contextData, queryTextDisplay);
        
    }
}


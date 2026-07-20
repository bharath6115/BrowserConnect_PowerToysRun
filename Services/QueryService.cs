using Wox.Plugin;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;
using Community.PowerToys.Run.Plugin.BrowserConnect.Providers.Youtube;
using Community.PowerToys.Run.Plugin.BrowserConnect.Providers.Anilist;
using Community.PowerToys.Run.Plugin.BrowserConnect.Interfaces;
using Community.PowerToys.Run.Plugin.BrowserConnect.Providers.SeriesGraph;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services;

public record SearchProvider(
    string UrlPattern,
    ISearchService Service
);

public class QueryService
{ 
    private readonly EngineService _engineService;
    private readonly IconService _iconService;
    private readonly ActionService _actionService;
    private readonly List<SearchProvider> _providers;

    public QueryService(EngineService engineService, ActionService actionService, IconService iconService, YoutubeService youtubeService, AnilistService anilistService, SeriesGraphService seriesGraphService)
    {
        _engineService = engineService;
        _actionService = actionService;
        _iconService = iconService;
        _providers =
        [
            new("youtube.com/results?search_query=%s", youtubeService),
            new("anilist.co/search/anime?search=%s", anilistService),
            new("seriesgraph.com/show/search/%s", seriesGraphService),
        ];
    }

    /// <summary>
    /// Query handler to handle URLs.
    /// For a URL, we cant replace all -i's nor can we replace the -i's part of the URL.
    /// Only edge occurances can be replaced: -i<URL> | -i <URL> | <URL>-i | <URL> -i
    /// FUTURE ENHANCEMENT: Add a option to save this as a search engine... but how will user specify the shortcut...? 
    /// </summary>
    /// <param name="rawInput">Raw URL input entered by the user.</param>
    /// <param name="isIncognitoDefault">Whether incognito mode is enabled by default.</param>
    /// <returns>Results for opening the URL or creating a new alias for it.</returns>
    public List<Result> HandleURLSearch(string rawInput, bool isIncognitoDefault)
    {
        var (URL, isIncognitoMentioned) = UrlUtils.ParseURL(rawInput.Trim());
        bool inIncognito = isIncognitoMentioned || isIncognitoDefault;

        return new List<Result>
        {
            new Result
            {
                Title = $"Browse {URL}{(inIncognito ? " [Incognito]" : "")}",
                SubTitle = $"URL: {URL}",
                IcoPath = inIncognito ? _iconService.GetIconPath(IconConsts.INCOGNITO) : _iconService.GetIconPath(IconConsts.DEFAULT),
                Action = _ =>
                {
                    _actionService.ExecuteUrl(URL,inIncognito);
                    return true;
                },
                ContextData = new CustomResultContext
                {
                    SearchType = SearchType.URL,
                    Url = URL,
                    IsIncognito = inIncognito,  
                },
                QueryTextDisplay = $"{URL}"
            },
            new Result
            {
                Title = "Create a new alias for this",
                SubTitle = $"Replace the <alias> to save for future lookups",
                IcoPath = _iconService.GetIconPath(IconConsts.ADD_NEW),
                QueryTextDisplay = $"-a @<alias> {URL}"
            },
        };
    }

    /// <summary>
    /// Builds results for multi-engine search input such as "yt @gh : query".
    /// Searches the query across multiple search engines at once.
    /// Accepts both exact keys and prefixed keys (only if matches a key fully) => "yt", "@yt" are accepted
    /// Returns the invalid keys in a seperate Result entry for user knowledge.
    /// </summary>
    /// <param name="rawInput">Raw multi-engine input entered by the user.</param>
    /// <param name="isIncognitoDefault">Whether incognito mode is enabled by default.</param>
    /// <returns>Executable multi-engine results, plus an informational result when invalid engine keys are ignored.</returns>
    public List<Result>? HandleMultiEngineSearch(string rawInput, bool isIncognitoDefault)
    {
        var (cleanInput, inIncognito) = InputUtils.ParseInput(rawInput, isIncognitoDefault);
        var engines = _engineService.GetEngines();
        var (searchQuery, targetEngines, invalidEngines) = InputUtils.ParseMultiEngineSearchInput(cleanInput,engines);
        var mergedEngines = InputUtils.MergeWithSeparator(", ", [..targetEngines]);

        //search in multiple search engines only if we define them using : (@yt ani : query)
        if (targetEngines.Count >= 2)
        {
            var results = ResultUtils.GetEmptyList();
            string displayQuery = string.IsNullOrWhiteSpace(searchQuery) ? "" : $" '{searchQuery}' ";

            results.Add(new Result
            {
                Title = $"Search{displayQuery}in {targetEngines.Count} engine{(targetEngines.Count > 1 ? "s" : "")} {(inIncognito ? "[Incognito]" : "")}".Trim(),
                SubTitle = $"Engines: {mergedEngines}",
                IcoPath = _iconService.GetIconPath(IconConsts.DEFAULT),
                Score = 60000,
                Action = _ =>
                {
                    _actionService.ExecuteMulti(searchQuery,targetEngines,inIncognito);
                    return true;
                },
                ContextData = new CustomResultContext
                {
                    SearchType = SearchType.MULTI,
                    EncodedQuery = InputUtils.MergeWithSeparator(SymbolConsts.RECORD_SEPERATOR, mergedEngines, searchQuery),
                    IsIncognito = inIncognito,
                    Url = UrlUtils.GetCopyUrlForMultiEngineSearch(searchQuery,targetEngines,engines),
                },
                QueryTextDisplay = cleanInput
            });

            if (invalidEngines.Count > 0)
            {
                results.Add(new Result
                {
                    Title = $"{invalidEngines.Count} invalid search engine{(invalidEngines.Count > 1 ? "s" : "")} found.".Trim(),
                    SubTitle = $"Invalid Engines: {InputUtils.MergeWithSeparator(", ", [..invalidEngines])} [IGNORING]",
                    IcoPath = _iconService.GetIconPath(IconConsts.ERROR),
                    Score = 55000,
                    Action = _ => false,
                    QueryTextDisplay = cleanInput
                });
            }
            return results;
        }
        return null;
    }

    /// <summary>
    /// Builds exact-engine results or filtered engine suggestions for normal search input.
    /// </summary>
    /// <param name="rawInput">Raw search input entered by the user.</param>
    /// <param name="isIncognitoDefault">Whether incognito mode is enabled by default.</param>
    /// <returns>Results for an exact engine match or filtered engine suggestions.</returns>
    public List<Result> HandleSingleEngineSearch(string rawInput, bool isIncognitoDefault)
    {
        var (cleanInput, inIncognito) = InputUtils.ParseInput(rawInput, isIncognitoDefault);
        var engines = _engineService.GetEngines();

        //Split the potentially existing search engine and query (yt query query -> ['yt', 'query query'])
        string[] parts = cleanInput.Split(' ', 2);

        string firstWord = parts[0].ToLowerInvariant();
        string engineKey = firstWord.StartsWith('@') ? firstWord[1..] : firstWord;
        string query = parts.Length > 1 ? parts[1] : "";        

        /// We show exact search engine results if:
        /// 1. If the query is not prefix filtering -> prematurely matching to a existing key like "ani" would hide "anime" keyed search engine
        /// 2. The engine key matches an existing key exactly.
        bool isPrefixSearch = cleanInput.StartsWith('@');

        if (!isPrefixSearch && engines.TryGetValue(engineKey, out var urlTemplate))
        {
            return GetExactSearchResults(cleanInput, engineKey, query, inIncognito, urlTemplate);
        }
        return GetFilteredSearchResults(cleanInput, engineKey, query, inIncognito);
    }

    /// <summary>
    /// Builds the result for a specific configured search engine.
    /// </summary>
    /// <param name="cleanInput">Input displayed back to PowerToys after parsing.</param>
    /// <param name="searchEngine">Engine alias used for the result.</param>
    /// <param name="searchQuery">Normalized query text to execute.</param>
    /// <param name="inIncognito">Whether the result should open in incognito mode.</param>
    /// <param name="urlTemplate">Configured URL template for the engine.</param>
    /// <returns>A result with an action that executes the search or direct browse shortcut.</returns>
    public List<Result> GetExactSearchResults(string cleanInput, string searchEngine, string searchQuery, bool inIncognito, string urlTemplate)
    {
        string finalUrl = UrlUtils.BuildSearchUrl(searchQuery,urlTemplate);

        return new List<Result>{
            new Result
            {
                Title = urlTemplate.Contains("%s") ?
                        $"Search {searchQuery} {(inIncognito ? "[Incognito]" : "")}" :
                        $"Browse URL {(inIncognito ? "[Incognito]" : "")}",
                SubTitle = $"URL: {finalUrl}",
                IcoPath = _iconService.GetIconPath(searchEngine),
                Score = 60000,
                Action = _ =>
                {
                    _actionService.ExecuteDefault(searchQuery,searchEngine,finalUrl,inIncognito);
                    return true;
                },
                ContextData = new CustomResultContext
                {
                    SearchType = SearchType.DEFAULT,
                    SearchQuery = searchQuery,
                    SearchEngine = searchEngine,
                    Url = finalUrl,
                    IsIncognito = inIncognito, 
                },
                QueryTextDisplay = $"{cleanInput}"
            }
        };
    }


    /// <summary>
    /// Query handler for showing all or filtered results
    /// Engine filtering, Suggestions logic if "@<key>" is used
    /// Functionality:
    /// 1. Shows ALL search engine results with query execution action
    /// 2. Shows FILTERED search engine results with query execution action when "@<key>" is used
    /// </summary>
    /// <param name="cleanInput">Input text after incognito parsing.</param>
    /// <param name="engineKey">Alias prefix or first word parsed from the input.</param>
    /// <param name="query">Query text when filtering by @alias.</param>
    /// <param name="inIncognito">Whether results should open in incognito mode.</param>
    /// <returns>Search or browse results for all matching configured engines.</returns>
    public List<Result> GetFilteredSearchResults(string cleanInput, string engineKey, string query, bool inIncognito)
    {
        var engines = _engineService.GetEngines();
        bool isFiltering = cleanInput.StartsWith('@');
        string filter = isFiltering ? engineKey : "";
        string searchQuery = isFiltering ? query : cleanInput;

        var orderedKeys = _engineService.GetOrderedKeys();
        var candidateKeys = isFiltering ?
                            orderedKeys.Where(k => k.StartsWith(filter)) :
                            orderedKeys;

        return 
        [.. candidateKeys
            .Select((k, index) => new Result {
                Title = string.IsNullOrWhiteSpace(searchQuery) ?
                        k 
                        : (engines[k].Contains("%s") ? $"{k} : {searchQuery} {(inIncognito ? "[Incognito]" : "")}" : $"Browse {k} {(inIncognito ? "[Incognito]" : "")}"),
                SubTitle = $"{(string.IsNullOrWhiteSpace(searchQuery) ? "":"URL: ")}{UrlUtils.BuildSearchUrl(searchQuery,engines[k])}",
                IcoPath = _iconService.GetIconPath(k),
                Score = 50000 - index,
                Action = _ =>
                {
                    string finalUrl = UrlUtils.BuildSearchUrl(searchQuery,engines[k]);
                    _actionService.ExecuteDefault(searchQuery,k,finalUrl,inIncognito);
                    return true;
                },
                ContextData = new CustomResultContext
                {
                    SearchType = SearchType.DEFAULT,
                    SearchQuery = searchQuery,
                    SearchEngine = k,
                    Url = UrlUtils.BuildSearchUrl(searchQuery, engines[k]),
                    IsIncognito = inIncognito, 
                },
                QueryTextDisplay = (isFiltering && string.IsNullOrWhiteSpace(searchQuery)) ? $"@{k} " : cleanInput
            })
        ];
    }

    /// <summary>
    /// Builds delayed live-provider results for inputs ending with the trigger symbol.
    /// </summary>
    /// <param name="rawInput">Raw search input entered by the user.</param>
    /// <param name="isIncognitoDefault">Whether incognito mode is enabled by default.</param>
    /// <param name="triggerSymbol">Trailing symbol that enables live provider lookup.</param>
    /// <returns>Default engine results plus live provider results when the configured engine supports them.</returns>
    public List<Result> HandleDelayedResults(string rawInput, bool isIncognitoDefault, string triggerSymbol)
    {
        var (parsedInput, inIncognito) = InputUtils.ParseInput(rawInput, isIncognitoDefault);
        var (cleanInput, isSymbolPresent) = InputUtils.CheckIfEndsWithAndRemoveSymbol(parsedInput, triggerSymbol);
        if (!isSymbolPresent) return ResultUtils.GetEmptyList();

        string[] parts = cleanInput.Split(' ', 2);
        if (parts.Length < 2) return ResultUtils.GetEmptyList();

        var engines = _engineService.GetEngines();
        string firstWord = parts[0].ToLowerInvariant();
        string engineKey = firstWord.StartsWith('@') ? firstWord[1..] : firstWord;
        if (!engines.TryGetValue(engineKey, out var engineUrl) || !engineUrl.Contains("%s")) return ResultUtils.GetEmptyList();

        string searchQuery = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(searchQuery)) return ResultUtils.GetEmptyList();

        var provider = _providers.FirstOrDefault(p => engineUrl.Contains(p.UrlPattern));
        if (provider is null) return ResultUtils.GetEmptyList();

        var defaultResults = GetExactSearchResults(cleanInput,engineKey,searchQuery,inIncognito,engineUrl);

        return provider.Service.GetResults(defaultResults,engineKey,searchQuery,inIncognito);
    }
}

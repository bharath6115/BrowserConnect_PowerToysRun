using Wox.Plugin;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Handlers;
public class FlagCommandHandler
{
    private readonly EngineService _engineService;
    private readonly HistoryService _historyService;
    private readonly IconService _iconService;
    private readonly BrowserPlugin _plugin;
    private readonly string _historyPath;
    private readonly string _enginesPath;
    private readonly string _logsPath;

    public FlagCommandHandler(EngineService engineService, HistoryService historyService, IconService iconService, BrowserPlugin plugin, string historyPath, string logPath)
    {
        _engineService = engineService;
        _historyService = historyService;
        _iconService = iconService;
        _plugin = plugin;
        _historyPath = historyPath;
        _enginesPath = _engineService.GetEnginesPath();
        _logsPath = logPath;
    }

    public List<Result> HandleOpenLogCommand() => HandleOpenFile("Open Logs File", _logsPath, "-log");
    public List<Result> HandleOpenListCommand() => HandleOpenFile("Open Search Engines File", _enginesPath, "-l");
    public List<Result> HandleOpenHistoryCommand() => HandleOpenFile("Open History File", _historyPath, "-his");    
    public List<Result> HandleAddCommand(string input)
    {
        string[] parts = input.Split(" ", 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return ResultUtils.GetAddNewUsageResult(_iconService,input); 
        
        string newAlias = parts[1].StartsWith('@') ? parts[1][1..].ToLowerInvariant() : parts[1].ToLowerInvariant();
        string newUrl = parts[2];

        if (_engineService.GetEngines().ContainsKey(newAlias))
        {
            return new List<Result> {
                new Result {
                    Title = $"Alias already taken! Overwrite '{newAlias}' anyway?",
                    SubTitle = $"Click to replace old URL with: {newUrl}",
                    IcoPath = _iconService.GetIconPath(IconConsts.ADD_NEW),
                    QueryTextDisplay = input,
                    Action = _ => {
                        _engineService.AddOrUpdateEngine(newAlias, newUrl);
                        _iconService.ResetFailedFetch(newAlias);
                        return true;
                    }
                }
            };
        }

        return new List<Result> {
            new Result {
                Title = $"Save Engine: {newAlias}",
                SubTitle = $"Link: {newUrl}",
                IcoPath = _iconService.GetIconPath(IconConsts.ADD_NEW),
                Action = _ => {
                    try
                    {
                        _engineService.AddOrUpdateEngine(newAlias, newUrl);
                        _iconService.ResetFailedFetch(newAlias);
                        _iconService.GetIconPath(newAlias);
                        Logger.Log($"Added engine: {newAlias} -> {newUrl}", "ACTION");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error adding engine {newAlias}: {ex.Message}", "ERROR");
                        return false;
                    }
                },
                QueryTextDisplay = input
            }
        };
    }

    public List<Result> HandleDeleteCommand(string input)
    {
        string[] parts = input.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return ResultUtils.GetDeleteUsageResult(_iconService,input);

        string aliasToDelete = parts[1].StartsWith('@') ? parts[1][1..].ToLowerInvariant() : parts[1].ToLowerInvariant();

        if (!_engineService.GetEngines().TryGetValue(aliasToDelete, out var url)) return ResultUtils.GetDeleteErrorResult(_iconService,input,aliasToDelete);

        return new List<Result> {
            new Result {
                Title = $"Delete Engine: {aliasToDelete}",
                SubTitle = $"URL: {url}",
                IcoPath = _iconService.GetIconPath(IconConsts.DELETE),
                Action = _ => {
                    try
                    {
                        _engineService.DeleteEngine(aliasToDelete);
                        Task.Run(() => _iconService.DeleteIcon(aliasToDelete));
                        Logger.Log($"Deleted engine: {aliasToDelete}", "ACTION");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error deleting engine {aliasToDelete}: {ex.Message}", "ERROR");
                        return false;
                    }
                },
                QueryTextDisplay = input
            }
        };
    }

    public List<Result> HandleRefreshCommand()
    {
        return new List<Result> {
            new Result {
                Title = "Refresh Search Engines",
                SubTitle = $"Loaded {_engineService.Count} search engines from {_engineService.GetEnginesPath()}",
                IcoPath = _iconService.GetIconPath(IconConsts.REFRESH),
                Action = _ =>
                {
                    Logger.Log("Refreshing engines...", "TRACE");
                    _engineService.LoadSearchEngines();
                    _historyService.LoadHistoryCache();
                    _iconService.ClearFailedCache();
                    _plugin.ClearCache();
                    Logger.Log($"Refresh complete. Engines loaded: {_engineService.Count}, History cache reloaded, YouTube cache cleared.", "INFO");
                    return true;
                },
                QueryTextDisplay = "-r"
            }
        };
    }

    public List<Result> HandleHelpCommand()
    {
        var refreshResult = HandleRefreshCommand().First();
        var openLogResult = HandleOpenLogCommand().First();
        var openListResult = HandleOpenListCommand().First();
        var openHistoryResult = HandleOpenHistoryCommand().First();
        
        return new List<Result>
        {
            new Result {
                Title = "Search: @alias <query>",
                SubTitle = "Example: '@yt how to use C#?' - Search using a saved engine.",
                IcoPath = _iconService.GetIconPath(IconConsts.DEFAULT),
                QueryTextDisplay = "-h"
            },
            new Result {
                Title = "Search: <URL>",
                SubTitle = "Example: 'https://www.youtube.com/watch?v=xMHJGd3wwZk' - Open a URL directly.",
                IcoPath = _iconService.GetIconPath(IconConsts.DEFAULT),
                QueryTextDisplay = "-h"
            },
            new Result {
                Title = "Incognito: Add '-i'",
                SubTitle = "Example: '@yt -i secret song' - Opens the search in a private window.",
                IcoPath = _iconService.GetIconPath(IconConsts.INCOGNITO),
                QueryTextDisplay = "-i"
            },
            new Result {
                Title = "Add Engine: -add @alias <URL>",
                SubTitle = "Example: '-add @bing https://bing.com/search?q=%s' (Use %s for query).",
                IcoPath = _iconService.GetIconPath(IconConsts.ADD_NEW),
                QueryTextDisplay = "-add @"
            },
            new Result {
                Title = "Delete Engine: -d @alias",
                SubTitle = "Example: '-d @bing' - Removes the engine and its icon.",
                IcoPath = _iconService.GetIconPath(IconConsts.DELETE),
                QueryTextDisplay = "-d @"
            },
            new Result {
                Title = "Refresh Search Engines : -r",
                SubTitle = "-r reloads engines/history and clears icon/live-result caches.",
                IcoPath = refreshResult.IcoPath,
                QueryTextDisplay = refreshResult.QueryTextDisplay,
                Action = refreshResult.Action
            },
            new Result {
                Title = "Open Log : -log",
                SubTitle = "-log opens the file containing logs.",
                IcoPath = openLogResult.IcoPath,
                Action = openLogResult.Action,
                ContextData = openLogResult.ContextData,
                QueryTextDisplay = openLogResult.QueryTextDisplay
            },
            new Result {
                Title = "Open List : -l",
                SubTitle = "-l opens the file having search engines.",
                IcoPath = openListResult.IcoPath,
                Action = openListResult.Action,
                ContextData = openListResult.ContextData,
                QueryTextDisplay = openListResult.QueryTextDisplay
            },
            new Result {
                Title = "Open History File : -his",
                SubTitle = "-his opens the file having history.",
                IcoPath = openHistoryResult.IcoPath,
                Action = openHistoryResult.Action,
                ContextData = openHistoryResult.ContextData,
                QueryTextDisplay = openHistoryResult.QueryTextDisplay
            },
            new Result {
                Title = "View History : !",
                SubTitle = "Shows saved entries. Select one to open it again.",
                IcoPath = _iconService.GetIconPath(IconConsts.HISTORY),
                Score = 10,
                QueryTextDisplay = "!"
            }
        }.Select((r, i) => { r.Score = r.Score == 0 ? 100 - i : r.Score; return r; }).ToList();
    }

    /*=========== HELPER METHODS ===========*/
    private List<Result> HandleOpenFile(string title, string path, string queryDisplay)
    {
        return new List<Result> {
            new Result {
                Title = title,
                SubTitle = $"Open {path}",
                IcoPath = _iconService.GetIconPath(IconConsts.OPEN_FILE),
                QueryTextDisplay = queryDisplay,
                ContextData = new CustomResultContext
                {
                    IsFlagToOpenFile = true,
                    FilePath = path
                },
                Action = _ => {
                    FileUtils.OpenFile(path);
                    return true;
                }
            }
        };
    }
}

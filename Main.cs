using System.IO;
using Wox.Plugin;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.Handlers;
using Microsoft.PowerToys.Settings.UI.Library;

namespace Community.PowerToys.Run.Plugin.BrowserConnect
{
    public class BrowserPlugin : IPlugin, ISettingProvider, IContextMenu, IDelayedExecutionPlugin
    {
        public static string PluginID => "B8A5C3D2E1F4A6B7C8D9E0F1A2B3C4D5";
        public string Name => "BrowserConnect";
        public string Description => "Browse using custom engines. Use -help flag to view all flags!";

#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor.
        private EngineManager _engineManager;
        private HistoryManager _historyManager;
        private IconManager _iconManager;
        private CommandHandler _commandHandler;
        private PluginInitContext _context;
        private YoutubeService _youtubeService;

        // Settings
        public bool IsIncognitoDefault { get; set; } = false;
        public bool IsHistoryEnabled { get; set; } = true;
        public bool RecordIncognitoHistory { get; set; } = false;
        public int HistoryLimit { get; set; } = 3000;
        public int HistoryCacheSize { get; set; } = 1500;
#pragma warning restore CS8618 

        public void Init(PluginInitContext context)
        {
            _context = context;
            Log("Plugin Initializing...", "INFO");

            try
            {
                string? pluginDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string imagesDir = pluginDir != null ? Path.Combine(pluginDir, "Images") : string.Empty;

                // string searchEnginesPath = "E:/searchEnginesBackup.txt";
                string searchEnginesPath = pluginDir != null ? Path.Combine(pluginDir, "searchEngines.txt") : "";
                string historyPath = pluginDir != null ? Path.Combine(pluginDir, "history.txt") : "";
                string LogsPath = pluginDir != null ? Path.Combine(pluginDir, "Logs.txt") : "";

                bool isDarkTheme = IconManager.IsDarkTheme();

                _engineManager = new EngineManager(searchEnginesPath, this);
                _historyManager = new HistoryManager(historyPath, this);
                _iconManager = new IconManager(pluginDir, imagesDir, isDarkTheme, _engineManager, this);
                _commandHandler = new CommandHandler(_engineManager, _historyManager, _iconManager, this, historyPath, LogsPath);
                _youtubeService = new YoutubeService(pluginDir, this, _historyManager, _iconManager);

                Log($"Initialized with {_engineManager.Count} engines. History enabled: {IsHistoryEnabled}.", "INFO");
            }
            catch (Exception ex)
            {
                Log($"Critical Init Error: {ex}", "ERROR");
            }
        }


        /*----------QUERY METHODS----------*/
        public List<Result> Query(Query query)
        {
            try
            {
                var results = new List<Result>();
                string rawInput = query.Search ?? string.Empty;

                // Handle Commands
                if (rawInput.TrimStart().StartsWith("-lo")) return _commandHandler.HandleLogCommand();
                if (rawInput.TrimStart().StartsWith("-l")) return _commandHandler.HandleListCommand();
                if (rawInput.TrimStart().StartsWith("-a")) return _commandHandler.HandleAddCommand(rawInput);
                if (rawInput.TrimStart().StartsWith("-d")) return _commandHandler.HandleDeleteCommand(rawInput);
                if (rawInput.TrimStart().StartsWith("-r")) return _commandHandler.HandleRefreshCommand();
                if (rawInput.TrimStart().StartsWith("-hi")) return _commandHandler.HandleOpenHistoryCommand();
                if (rawInput.TrimStart().StartsWith("-h")) return _commandHandler.HandleHelpCommand();
                if (rawInput.TrimStart().StartsWith('!')) return _commandHandler.HandleHistoryCommand(rawInput);

                // Incognito logic
                var (cleanInput, inIncognito) = ParseInput(rawInput);

                if (string.IsNullOrWhiteSpace(cleanInput)) return GetAvailableEngines();

                //directly execute it if its a url
                if (cleanInput.Contains('.') && !cleanInput.Contains(' '))
                {
                    // incognito rules for urls work differently:
                    // cant replace all -i's nor can we replace the -i's part of the url. Only edge ones can be replaced: -i<URL> | -i <URL> | <URL>-i | <URL> -i 
                    string rawInputTrimmed = rawInput.Trim();
                    bool inIncognitoEdgeCase = rawInputTrimmed.StartsWith("-i") || rawInputTrimmed.EndsWith("-i") || IsIncognitoDefault;
                    string URL = inIncognitoEdgeCase ?
                        rawInputTrimmed.StartsWith("-i") ? rawInputTrimmed[2..].Trim() : rawInputTrimmed[..^2].Trim()
                        : rawInputTrimmed;

                    results.Add(new Result
                    {
                        Title = $"Browse {URL} {(inIncognitoEdgeCase ? "[Incognito]" : "")}",
                        SubTitle = $"URL: {URL}",
                        IcoPath = inIncognitoEdgeCase ? _iconManager.GetIconPath("incognitoIcon") : _iconManager.GetIconPath("default icon please"),
                        Action = _ =>
                        {
                            Log($"Opening direct URL: {URL} (Incognito: {inIncognitoEdgeCase})", "ACTION");
                            _historyManager.SaveToHistory(rawInput, "_URL", inIncognitoEdgeCase);
                            BrowserHelper.OpenBrowser(URL, inIncognitoEdgeCase);
                            return true;
                        },
                        QueryTextDisplay = $"{URL}"
                    });

                    return results;
                }

                string[] parts = cleanInput.Split(' ', 2);
                var engines = _engineManager.GetEngines();

                // Multi-search engine logic: <e1> <e2> @<e3> : query
                if (cleanInput.Contains(':'))
                {
                    var colonParts = cleanInput.Split(':', 2);
                    string enginePart = colonParts[0];
                    string searchQuery = colonParts[1].Trim();

                    string[] potentialEngines = enginePart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var targetEngines = new List<string>();
                    var invalidEngines = new List<string>();

                    foreach (string word in potentialEngines)
                    {
                        if (word.StartsWith("@") && engines.ContainsKey(word[1..])) targetEngines.Add(word[1..]);
                        else if (engines.ContainsKey(word)) targetEngines.Add(word);
                        else invalidEngines.Add(word);
                    }

                    //search in multiple search engines only if we define them using : (@yt ani : query)
                    if (targetEngines.Count > 1)
                    {
                        var multiResults = new List<Result>();
                        string displayQuery = string.IsNullOrWhiteSpace(searchQuery) ? string.Empty : $" '{searchQuery}' ";

                        multiResults.Add(new Result
                        {
                            Title = $"Search{displayQuery}in {targetEngines.Count} engine{(targetEngines.Count > 1 ? "s" : "")} {(inIncognito ? "[Incognito]" : "")}".Trim(),
                            SubTitle = $"Engines: {string.Join(", ", targetEngines)}",
                            IcoPath = _iconManager.GetIconPath("default icon"),
                            Score = 60000,
                            Action = _ =>
                            {
                                Log($"Multi-engine search triggered for: {string.Join(", ", targetEngines)}", "ACTION");

                                var entries = targetEngines.Select(e => (searchQuery, e)).ToList();
                                entries.Insert(0, (cleanInput, "_MULTI"));
                                _historyManager.SaveToHistory(entries, inIncognito);

                                foreach (var engine in targetEngines)
                                {
                                    string urlTemplate = engines[engine];
                                    string finalUrl = string.IsNullOrWhiteSpace(searchQuery)
                                        ? urlTemplate.Contains("%s") ? BrowserHelper.GetBaseUrl(urlTemplate) : urlTemplate
                                        : urlTemplate.Replace("%s", Uri.EscapeDataString(searchQuery));

                                    Log($"Opening {engine}: {finalUrl}", "TRACE");
                                    BrowserHelper.OpenBrowser(finalUrl, inIncognito);
                                }
                                return true;
                            },
                            QueryTextDisplay = cleanInput
                        });

                        if (invalidEngines.Count > 0)
                        {
                            multiResults.Add(new Result
                            {
                                Title = $"{invalidEngines.Count} invalid search engine{(invalidEngines.Count > 1 ? "s" : "")} found.".Trim(),
                                SubTitle = $"Invalid Engines: {string.Join(", ", invalidEngines)} [IGNORING]",
                                IcoPath = _iconManager.GetIconPath("errorIcon"),
                                Score = 55000,
                                Action = _ => false,
                                QueryTextDisplay = cleanInput
                            });
                        }
                        return multiResults;
                    }
                }

                //Matching search engine + suggestions
                string firstWord = parts.Length > 0 ? parts[0].ToLower() : "";
                string engineKey = firstWord.StartsWith("@") ? firstWord[1..] : firstWord;

                if (engines.ContainsKey(engineKey) && (parts.Length > 1 || !cleanInput.StartsWith("@")))
                {
                    // Only treat as an exact match if it has a space (e.g., "@ @yt query") or if it's not a filter
                    /*
                    MATCHES:
                    @ @yt query
                    @ yt 
                    */
                    string searchEngine = engineKey;
                    string searchQuery = parts.Length > 1 ? parts[1] : "";
                    string urlTemplate = engines[searchEngine];
                    string finalUrl = string.IsNullOrWhiteSpace(searchQuery)
                        ? urlTemplate.Contains("%s") ? BrowserHelper.GetBaseUrl(urlTemplate) : urlTemplate
                        : urlTemplate.Replace("%s", Uri.EscapeDataString(searchQuery));

                    results.Add(new Result
                    {
                        Title = urlTemplate.Contains('%') ?
                                $"Search {searchQuery} {(inIncognito ? "[Incognito]" : "")}" :
                                $"Browse URL {(inIncognito ? "[Incognito]" : "")}",
                        SubTitle = $"URL: {finalUrl}",
                        IcoPath = _iconManager.GetIconPath(searchEngine),
                        Score = 60000,
                        Action = _ =>
                        {
                            Log($"Opening engine {searchEngine}: {finalUrl} (Incognito: {inIncognito})", "ACTION");
                            _historyManager.SaveToHistory(rawInput, searchEngine, inIncognito);
                            BrowserHelper.OpenBrowser(finalUrl, inIncognito);
                            return true;
                        },
                        QueryTextDisplay = $"{cleanInput}"
                    });

                    return results;
                }
                else
                {
                    // allows @yt to show a filtered list instead of immediately picking the first match.
                    // Engine filtering, Suggestions logic
                    bool isFiltering = cleanInput.StartsWith('@');
                    string filter = isFiltering ? engineKey : string.Empty;
                    string searchQuery = isFiltering ? (parts.Length > 1 ? parts[1] : string.Empty) : cleanInput;

                    var orderedKeys = _engineManager.GetOrderedKeys();
                    var candidateKeys = isFiltering ?
                                        orderedKeys.Where(k => k.StartsWith(filter)) :
                                        orderedKeys;

                    return [.. candidateKeys
                    .Select((k, index) => new Result {
                        Title = string.IsNullOrWhiteSpace(searchQuery) ?
                                k :
                                (engines[k].Contains('%') ? $"{k} : {searchQuery} {(inIncognito ? "[Incognito]" : "")}" : $"Browse {k} {(inIncognito ? "[Incognito]" : "")}"),
                        SubTitle = string.IsNullOrWhiteSpace(searchQuery) ?
                                   engines[k].Contains("%s") ? BrowserHelper.GetBaseUrl(engines[k]) : engines[k]
                                   : $"URL: {engines[k].Replace("%s", Uri.EscapeDataString(searchQuery))}",
                        IcoPath = _iconManager.GetIconPath(k),
                        Score = 50000 - index,
                        Action = _ =>
                        {
                            string finalUrl = string.IsNullOrWhiteSpace(searchQuery)
                                ? engines[k].Contains("%s") ? BrowserHelper.GetBaseUrl(engines[k]) : engines[k]
                                : engines[k].Replace("%s", Uri.EscapeDataString(searchQuery));

                            Log($"Opening filtered engine {k}: {finalUrl}", "ACTION");
                            _historyManager.SaveToHistory(rawInput, k, inIncognito);
                            BrowserHelper.OpenBrowser(finalUrl, inIncognito);
                            return true;
                        },
                        QueryTextDisplay = (isFiltering && string.IsNullOrWhiteSpace(searchQuery)) ? $"@{k} " : cleanInput
                    })];
                }
            }
            catch (Exception ex)
            {
                Log($"Error in Query processing: {ex}", "ERROR");
                return [new Result {
                    Title = "Plugin Query Error",
                    SubTitle = "Check Logs.txt for details.",
                    IcoPath = _iconManager?.GetIconPath("errorIcon") ?? "Images\\browserConnect.light.png"
                }];
            }
        }

        // Called ~300ms after the user stops typing. Only works if input has keyword - ";;" to green-flag the searching using api
        public List<Result> Query(Query query, bool isFullQuery)
        {
            if (!isFullQuery) return new List<Result>();

            try
            {
                var results = new List<Result>();

                string rawInput = query.Search ?? string.Empty;
                var (parsedInput, inIncognito) = ParseInput(rawInput);
                var (cleanInput, isSymbolPresent) = CheckIfEndsWithAndRemoveSymbol(parsedInput, ";");
                if (!isSymbolPresent) return new List<Result>();

                string[] parts = cleanInput.Split(' ', 2);
                if (parts.Length < 2) return new List<Result>();

                var engines = _engineManager.GetEngines();
                string firstWord = parts[0].ToLower();

                string engineKey = firstWord.StartsWith("@") ? firstWord[1..] : firstWord;
                if (!engines.TryGetValue(engineKey, out var engineUrl)) return new List<Result>();
                if (!engineUrl.Contains("youtube.com/results") || !engineUrl.Contains("%s")) return new List<Result>(); // Only intercept YouTube searches

                string searchQuery = parts[1].Trim();
                if (string.IsNullOrWhiteSpace(searchQuery)) return new List<Result>();

                //duplicate code from above - to make sure the result to search page remains aswell
                string finalUrl = string.IsNullOrWhiteSpace(searchQuery) ? engineUrl : engineUrl.Replace("%s", Uri.EscapeDataString(searchQuery));

                results.Add(new Result
                {
                    Title = $"Search {searchQuery} {(inIncognito ? "[Incognito]" : "")}",
                    SubTitle = $"URL: {finalUrl}",
                    IcoPath = _iconManager.GetIconPath(engineKey),
                    Score = 60000,
                    Action = _ =>
                    {
                        Log($"Opening engine {engineKey}: {finalUrl} (Incognito: {inIncognito})", "ACTION");
                        _historyManager.SaveToHistory(rawInput, engineKey, inIncognito);
                        BrowserHelper.OpenBrowser(finalUrl, inIncognito);
                        return true;
                    },
                    QueryTextDisplay = $"{cleanInput}"
                });

                var ytResults = _youtubeService.GetYoutubeResults(engineKey, searchQuery, inIncognito);
                results.AddRange(ytResults ?? []);

                return results;
            }
            catch (Exception ex)
            {
                Log($"Delayed query error: {ex.Message}", "ERROR");
                return new List<Result>();
            }
        }


        /*----------HELPER METHODS----------*/

        private (string cleanInput, bool inIncognito) ParseInput(string rawInput)
        {
            var tokens = rawInput.Split(' ');
            bool inIncognito = tokens.Contains("-i") || IsIncognitoDefault;
            string cleanInput = string.Join(" ", tokens.Where(t => t != "-i")).Trim();
            return (cleanInput, inIncognito);
        }

        private (string cleanInput, bool isSymbolPresent) CheckIfEndsWithAndRemoveSymbol(string input, string symbol)
        {
            if (!input.EndsWith(symbol)) return (input, false);
            return (input[..^symbol.Length].Trim(), true);
        }

        private List<Result> GetAvailableEngines()
        {
            var engines = _engineManager.GetEngines();
            var orderedKeys = _engineManager.GetOrderedKeys();
            return [.. orderedKeys
                .Select((k, index) => new Result {
                    Title = $"{k}",
                    SubTitle = engines[k].Contains("%s") ? BrowserHelper.GetBaseUrl(engines[k]) : engines[k],
                    IcoPath = _iconManager.GetIconPath(k),
                    Score = 50000 - index,
                    Action = _ => {
                        BrowserHelper.OpenBrowser(engines[k].Contains("%s") ? BrowserHelper.GetBaseUrl(engines[k]) : engines[k], IsIncognitoDefault);
                        return true;
                    }
                })];
        }

        public void ClearCache() => _youtubeService.ClearCache();

        /*----------SETTINGS PANEL----------*/
        public System.Windows.Controls.Control CreateSettingPanel()
        {
            return new System.Windows.Controls.Control();
        }

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>
        {
            new PluginAdditionalOption
            {
                Key = nameof(IsIncognitoDefault),
                DisplayLabel = "Incognito by default",
                DisplayDescription = "Automatically use incognito mode for all searches.",
                Value = false
            },
            new PluginAdditionalOption
            {
                Key = nameof(IsHistoryEnabled),
                DisplayLabel = "Record History",
                DisplayDescription = "Enable or disable recording search history.",
                Value = true
            },
            new PluginAdditionalOption
            {
                Key = nameof(RecordIncognitoHistory),
                DisplayLabel = "Record Incognito History",
                DisplayDescription = "Enable or disable recording search history for searches done in incognito mode.",
                Value = false
            },
            new PluginAdditionalOption
            {
                Key = nameof(HistoryLimit),
                DisplayLabel = "History File Limit",
                DisplayDescription = "Maximum number of lines to keep in history.txt.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = 3000
            },
            new PluginAdditionalOption
            {
                Key = nameof(HistoryCacheSize),
                DisplayLabel = "History Display Count",
                DisplayDescription = "How many recent unique results to show in history list.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = 1500
            }
        };

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            Log("Updating Settings...", "TRACE");
            if (settings == null || settings.AdditionalOptions == null)
            {
                IsIncognitoDefault = false;
                IsHistoryEnabled = true;
                RecordIncognitoHistory = false;
                HistoryLimit = 3000;
                HistoryCacheSize = 1500;
                return;
            }

            IsIncognitoDefault = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(IsIncognitoDefault))?.Value ?? false;
            IsHistoryEnabled = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(IsHistoryEnabled))?.Value ?? true;
            RecordIncognitoHistory = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(RecordIncognitoHistory))?.Value ?? false;
            HistoryLimit = (int)(settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(HistoryLimit))?.NumberValue ?? 3000);
            HistoryCacheSize = (int)(settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(HistoryCacheSize))?.NumberValue ?? 1500);

            Log("┌──────────────────────────────┐", "ACTION");
            Log("│           SETTINGS UPDATED            │", "ACTION");
            Log("├──────────────────────────────┤", "ACTION");
            Log($"│         Incognito     {IsIncognitoDefault,-16}│", "ACTION");
            Log($"│         History       {IsHistoryEnabled,-16}│", "ACTION");
            Log($"│         Incog. Hist   {RecordIncognitoHistory,-16}│", "ACTION");
            Log($"│         Limit         {HistoryLimit,-16}│", "ACTION");
            Log($"│         Cache         {HistoryCacheSize,-16}│", "ACTION");
            Log("└──────────────────────────────┘", "ACTION");

            _historyManager?.LoadHistoryCache();
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return new List<ContextMenuResult>();
        }


        /*----------LOGGING METHODS----------*/
        public void Log(string message, string level = "INFO")
        {
            try
            {
                string? pluginDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (pluginDir == null) return;
                string logPath = Path.Combine(pluginDir, "Logs.txt");
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = $"[{timestamp}] [{level}] {message}\n";
                File.AppendAllText(logPath, logEntry);
            }
            catch { /* Fail silently to not crash the plugin */ }
        }

    }
}

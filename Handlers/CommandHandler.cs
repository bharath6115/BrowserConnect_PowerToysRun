using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Wox.Plugin;
using Wox.Infrastructure;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using System.Windows.Input;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Handlers
{
    public class CommandHandler
    {
        private readonly EngineManager _engineManager;
        private readonly HistoryManager _historyManager;
        private readonly IconManager _iconManager;
        private readonly BrowserPlugin _plugin;
        private readonly string _historyPath;
        private readonly string _LogsPath;

        public CommandHandler(EngineManager engineManager, HistoryManager historyManager, IconManager iconManager, BrowserPlugin plugin, string historyPath, string LogsPath)
        {
            _engineManager = engineManager;
            _historyManager = historyManager;
            _iconManager = iconManager;
            _plugin = plugin;
            _historyPath = historyPath;
            _LogsPath = LogsPath;
        }

        private static void OpenFile(string path)
        {
            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    // If it's a root drive (like "E:\"), Directory.Exists returns true if it exists.
                    // If it's a relative path or a subfolder that doesn't exist, we create it.
                    Directory.CreateDirectory(directory);
                }
                if (!File.Exists(path))
                {
                    File.Create(path).Close();
                }
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch
            {
                try
                {
                    string? directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{path}\"",
                            UseShellExecute = true
                        });
                    }
                }
                catch { }
            }
        }
        public List<Result> HandleLogCommand()
        {
            string path = _LogsPath;
            return new List<Result> {
                new Result {
                    Title = "Show Logs",
                    SubTitle = path,
                    IcoPath = _iconManager.GetIconPath("openListIcon"),
                    QueryTextDisplay = "-log",
                    Action = _ => {
                        OpenFile(path);
                        return true;
                    }
                }
            };
        }
        public List<Result> HandleListCommand()
        {
            string path = _engineManager.GetEnginesPath();
            return new List<Result> {
                new Result {
                    Title = "Open Search Engines File",
                    SubTitle = path,
                    IcoPath = _iconManager.GetIconPath("openListIcon"),
                    QueryTextDisplay = "-l",
                    Action = _ => {
                        OpenFile(path);
                        return true;
                    }
                }
            };
        }

        public List<Result> HandleAddCommand(string input)
        {
            string[] parts = input.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return new List<Result> {
                new Result {
                    Title = "Usage: -add @alias URL",
                    SubTitle = "MUST include https:// in the URL, Add %s to replace text during searching",
                    IcoPath = _iconManager.GetIconPath("addNew"),
                    QueryTextDisplay = $"{input}"
                }
            };

            string newAlias = parts[1].StartsWith("@") ? parts[1][1..].ToLower() : parts[1].ToLower();
            string newUrl = parts[2];

            if (_engineManager.GetEngines().ContainsKey(newAlias))
            {
                bool overwriteAlias = parts[2].Contains("-o");
                newUrl = parts[2].Replace("-o", "").Trim();

                if (!overwriteAlias)
                {
                    return new List<Result> {
                        new Result {
                            Title = "Alias already taken!",
                            SubTitle = "Use a different name or add '-o' to the end to overwrite.",
                            IcoPath = _iconManager.GetIconPath("addNew"),
                            QueryTextDisplay = $"{input}"
                        },
                        new Result {
                            Title = $"Overwrite {newAlias} anyway?",
                            SubTitle = $"Click to replace old URL with: {newUrl}",
                            IcoPath = _iconManager.GetIconPath("addNew"),
                            QueryTextDisplay = $"{input}",
                            Action = _ => {
                                 _engineManager.AddOrUpdateEngine(newAlias, newUrl);
                                 _iconManager.ResetFailedFetch(newAlias);
                                 return true;
                            }
                        }
                    };
                }
            }

            return new List<Result> {
                new Result {
                    Title = $"Save Engine: {newAlias}",
                    SubTitle = $"Link: {newUrl}",
                    IcoPath = _iconManager.GetIconPath("addNew"),
                    Action = _ => {
                        try
                        {
                            _engineManager.AddOrUpdateEngine(newAlias, newUrl);
                            _iconManager.ResetFailedFetch(newAlias);
                            _iconManager.GetIconPath(newAlias);
                            _plugin.Log($"Added engine: {newAlias} -> {newUrl}", "ACTION");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            _plugin.Log($"Error adding engine {newAlias}: {ex.Message}", "ERROR");
                            return false;
                        }
                    },
                    QueryTextDisplay = $"{input}"
                }
            };
        }

        public List<Result> HandleDeleteCommand(string input)
        {
            string[] parts = input.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return new List<Result> {
                new Result {
                    Title = "Usage: -d @alias",
                    SubTitle = "Delete an existing search engine",
                    IcoPath = _iconManager.GetIconPath("deleteIcon"),
                    QueryTextDisplay = $"{input}"
                }
            };

            string aliasToDelete = parts[1].StartsWith("@") ? parts[1][1..].ToLower() : parts[1].ToLower();

            if (!_engineManager.GetEngines().ContainsKey(aliasToDelete))
            {
                return new List<Result> {
                    new Result {
                        Title = "Alias not found!",
                        SubTitle = $"Cannot delete '{aliasToDelete}' - it doesn't exist",
                        IcoPath = _iconManager.GetIconPath("deleteIcon"),
                        QueryTextDisplay = $"{input}"
                    }
                };
            }

            return new List<Result> {
                new Result {
                    Title = $"Delete Engine: {aliasToDelete}",
                    SubTitle = $"URL: {_engineManager.GetEngines()[aliasToDelete]}",
                    IcoPath = _iconManager.GetIconPath("deleteIcon"),
                    Action = _ => {
                        try
                        {
                            _engineManager.DeleteEngine(aliasToDelete);
                            Task.Run(() => _iconManager.DeleteIcon(aliasToDelete));
                            _plugin.Log($"Deleted engine: {aliasToDelete}", "ACTION");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            _plugin.Log($"Error deleting engine {aliasToDelete}: {ex.Message}", "ERROR");
                            return false;
                        }
                    },
                    QueryTextDisplay = $"{input}"
                }
            };
        }

        public List<Result> HandleRefreshCommand()
        {
            _plugin.Log("Refreshing engines...", "TRACE");
            _engineManager.LoadSearchEngines();
            _historyManager.LoadHistoryCache();
            _iconManager.ClearFailedCache();
            _plugin.ClearCache();
            _plugin.Log($"Refresh complete. Engines loaded: {_engineManager.Count}, History cache reloaded, YouTube cache cleared.", "INFO");
            return new List<Result> {
                new Result {
                    Title = "Refresh Search Engines",
                    SubTitle = $"Loaded {_engineManager.Count} search engines from {_engineManager.GetEnginesPath()}",
                    IcoPath = _iconManager.GetIconPath("refreshIcon"),
                    Action = _ => true,
                    QueryTextDisplay = "-r"
                }
            };
        }

        public List<Result> HandleOpenHistoryCommand()
        {
            string path = _historyPath;

            return new List<Result> {
                new Result {
                    Title = "Open History File",
                    SubTitle = path,
                    IcoPath = _iconManager.GetIconPath("openListIcon"),
                    QueryTextDisplay = "-his",
                    Action = _ => {
                        OpenFile(path);
                        return true;
                    }
                }
            };
        }

        public List<Result> HandleHelpCommand()
        {
            return new List<Result>
            {
                new Result {
                    Title = "Search: @alias <query>",
                    SubTitle = "Example: '@yt how to use C#?' - Search using a saved engine.",
                    IcoPath = _iconManager.GetIconPath("default icon"),
                    QueryTextDisplay = "-h"
                },
                new Result {
                    Title = "Search: <URL>",
                    SubTitle = "Example: 'https://www.youtube.com/watch?v=xMHJGd3wwZk' - Search a URL directly.",
                    IcoPath = _iconManager.GetIconPath("default icon"),
                    QueryTextDisplay = "-h"
                },
                new Result {
                    Title = "Incognito: Add '-i'",
                    SubTitle = "Example: '@yt -i secret song' - Opens the search in a private window.",
                    IcoPath = _iconManager.GetIconPath("incognitoIcon"),
                    QueryTextDisplay = "-i"
                },
                new Result {
                    Title = "Add Engine: -add @alias <URL>",
                    SubTitle = "Example: '-add @bing https://bing.com/search?q=%s' (Use %s for query).",
                    IcoPath = _iconManager.GetIconPath("addNew"),
                    QueryTextDisplay = "-add @"
                },
                new Result {
                    Title = "Delete Engine: -d @alias",
                    SubTitle = "Example: '-d @bing' - Removes the engine and its icon.",
                    IcoPath = _iconManager.GetIconPath("deleteIcon"),
                    QueryTextDisplay = "-d @"
                },
                new Result {
                    Title = "Refresh Search Engines : -r",
                    SubTitle = "-r reloads engines, history if you edited the file manually.",
                    IcoPath = _iconManager.GetIconPath("refreshIcon"),
                    QueryTextDisplay = "-r",
                    Action = _ =>
                    {
                        _engineManager.LoadSearchEngines();
                        return true;
                    }
                },
                new Result {
                    Title = "Open List : -log",
                    SubTitle = "-log opens the file containing logs.",
                    IcoPath = _iconManager.GetIconPath("openListIcon"),
                    Action = _ => {
                        OpenFile(_LogsPath);
                        return true;
                    },
                    QueryTextDisplay = "-log"
                },
                new Result {
                    Title = "Open List : -l",
                    SubTitle = "-l opens the file having search engines.",
                    IcoPath = _iconManager.GetIconPath("openListIcon"),
                    Action = _ => {
                        OpenFile(_engineManager.GetEnginesPath());
                        return true;
                    },
                    QueryTextDisplay = "-l"
                },
                new Result {
                    Title = "Open History File : -his",
                    SubTitle = "-his opens the file having history.",
                    IcoPath = _iconManager.GetIconPath("openListIcon"),
                    Action = _ => {
                        OpenFile(_historyPath);
                        return true;
                    },
                    QueryTextDisplay = "-his"
                },
                new Result {
                    Title = "View History : !",
                    SubTitle = "Shows your past searches. Click to paste back.",
                    IcoPath = _iconManager.GetIconPath("historyIcon"),
                    Score = 10,
                    QueryTextDisplay = "!"
                }
            }.Select((r, i) => { r.Score = r.Score == 0 ? 100 - i : r.Score; return r; }).ToList();
        }

        public List<Result> HandleHistoryCommand(string input)
        {
            string searchInput = input.TrimStart('!').Trim();
            var cache = _historyManager.GetHistoryCache();
            if (cache.Count == 0)
                return [new Result { Title = "No history yet!", SubTitle = "Start searching to build your history.", IcoPath = _iconManager.GetIconPath("historyIcon") }];

            try
            {
                // Reusable deduplication set
                HashSet<string> uniqueQueries = new HashSet<string>();
                List<Result> finalResults = new List<Result>();

                // 1. Process Cache First (always available and recent)
                if (cache.Any())
                {
                    var cacheLines = cache.AsEnumerable().Reverse().ToList();
                    finalResults.AddRange(ProcessHistoryLines(cacheLines, searchInput, uniqueQueries));
                }

                // 2. Use .txt if searching and results are fewer than desired count. Only do this if searchInput is not empty
                if (!string.IsNullOrWhiteSpace(searchInput) && finalResults.Count < _plugin.HistoryCacheSize)
                {
                    var fullHistory = _historyManager.GetFullHistory();
                    if (fullHistory.Count > cache.Count)
                    {
                        var fullLines = fullHistory.AsEnumerable().Reverse().ToList();
                        finalResults.AddRange(ProcessHistoryLines(fullLines, searchInput, uniqueQueries));
                    }
                }

                if (!finalResults.Any())
                {
                    return string.IsNullOrWhiteSpace(searchInput)
                        ? [new Result { Title = "No history yet!", IcoPath = _iconManager.GetIconPath("historyIcon") }]
                        : [new Result {
                            Title = "No history matches found!",
                            SubTitle = $"Try a different search: {searchInput}",
                            Action = _ => { 
                                // var res = _plugin.Query(new Query(searchInput));
                                //res[0].click()?
                                return true;
                            },
                            IcoPath = _iconManager.GetIconPath("historyIcon")
                        }];
                }

                return finalResults
                    .OrderByDescending(r => r.Score)
                    .Take(_plugin.HistoryCacheSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                _plugin.Log($"Error displaying history: {ex}", "ERROR");
                return [new Result { Title = "Error displaying history!", SubTitle = ex.Message, IcoPath = _iconManager.GetIconPath("errorIcon") }];
            }
        }

        private List<Result> ProcessHistoryLines(List<string> lines, string searchInput, HashSet<string> uniqueQueries)
        {
            var results = new List<Result>();
            int index = 0;

            foreach (var line in lines)
            {
                var parts = line.Split('|', 4);
                if (parts.Length != 4) continue;

                bool isIncognito = bool.TryParse(parts[3], out bool incognito) && incognito;

                string engineKey = parts[1];
                string queryText = Uri.UnescapeDataString(parts[2].Trim());
                if (uniqueQueries.Contains(engineKey + ":" + queryText)) continue;

                int score = 0;
                if (!string.IsNullOrWhiteSpace(searchInput))
                {
                    var match = StringMatcher.FuzzySearch(searchInput, queryText);
                    score = match.Score;
                }
                else
                {
                    // If no search input, prioritize recency using high base score
                    score = 50000 - index++;
                }

                if (string.IsNullOrWhiteSpace(searchInput) || score > 0)
                {
                    uniqueQueries.Add(engineKey + ":" + queryText); //to make sure both engineKey and query are considered in filtering unique queries
                    string timestamp = parts[0];

                    // For the URLS History of format searchQuery[URL]
                    bool DifferentURLFormat = engineKey == "_URL" && queryText.EndsWith("]") && queryText.Contains("[");
                    string DifferentURLFormat_searchQuery = queryText;
                    string DifferentURLFormat_Url = "";
                    string DifferentURLFormat_DecoratedUrl = "";

                    if (DifferentURLFormat)
                    {
                        int lastBracketIndex = queryText.LastIndexOf('[');
                        DifferentURLFormat_searchQuery = queryText[..lastBracketIndex];
                        DifferentURLFormat_Url = queryText[(lastBracketIndex + 1)..^1];
                        DifferentURLFormat_DecoratedUrl = $"(URL: {DifferentURLFormat_Url})";
                    }

                    results.Add(new Result
                    {
                        Title = DifferentURLFormat ? DifferentURLFormat_searchQuery : queryText,
                        SubTitle = $"Last searched: {timestamp} {(engineKey != "_URL" ? $"(via {engineKey})" : DifferentURLFormat_DecoratedUrl)} {(isIncognito ? "[Incognito]" : "")}".Trim(),
                        IcoPath = _iconManager.GetIconPath(isIncognito ? "incognitoIcon"
                                                            : DifferentURLFormat ? "yt"
                                                            : engineKey == "_URL" ? "default icon"
                                                            : engineKey),
                        Score = score,
                        Action = _ =>
                        {
                            ProcessHistoryExecution(engineKey, queryText, isIncognito);
                            return true;
                        },
                        QueryTextDisplay = DifferentURLFormat ? DifferentURLFormat_Url : queryText
                    });
                }

                if (results.Count >= _plugin.HistoryCacheSize) break;
            }
            return results;
        }

        private void ProcessHistoryExecution(string engineKey, string rawInput, bool inIncognito)
        {
            string cleanInput = rawInput.Trim();
            string searchQuery, finalUrl;

            if (cleanInput.StartsWith('@')) cleanInput = cleanInput.Contains(' ') ? cleanInput.Split(' ', 2)[1] : "";

            if (engineKey == "_URL")
            {
                bool DifferentURLFormat = cleanInput.EndsWith("]") && cleanInput.Contains("[");
                if (DifferentURLFormat)
                {
                    int lastBracketIndex = cleanInput.LastIndexOf('[');
                    string videoUrl = cleanInput[(lastBracketIndex+1)..^1];
                    BrowserHelper.OpenBrowser(videoUrl, inIncognito);
                    return;
                }

                BrowserHelper.OpenBrowser(cleanInput, inIncognito);
                return;
            }

            var searchEngines = _engineManager.GetEngines();

            // Multi-search engine case
            if (engineKey == "_MULTI" && cleanInput.Contains(':'))
            {
                var colonParts = cleanInput.Split(':', 2);
                string enginePart = colonParts[0];
                searchQuery = colonParts[1].Trim();

                string[] potentialEngines = enginePart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var targetEngines = new List<string>();

                foreach (string word in potentialEngines)
                {
                    if (word.StartsWith("@") && searchEngines.ContainsKey(word[1..])) targetEngines.Add(word[1..]);
                    else if (searchEngines.ContainsKey(word)) targetEngines.Add(word);
                }

                if (targetEngines.Count > 1) //this check is redundant because it gets stored in history only if this condition is met in the first place.
                {
                    _plugin.Log($"Executing multi-engine search from history: {string.Join(", ", targetEngines)}", "ACTION");
                    foreach (var engine in targetEngines)
                    {
                        string urlTemplate = searchEngines[engine];
                        finalUrl = string.IsNullOrWhiteSpace(searchQuery)
                            ? urlTemplate.Contains("%s") ? BrowserHelper.GetBaseUrl(urlTemplate) : urlTemplate
                            : urlTemplate.Replace("%s", Uri.EscapeDataString(searchQuery));

                        _plugin.Log($"Opening {engine}: {finalUrl}", "TRACE");
                        BrowserHelper.OpenBrowser(finalUrl, inIncognito);
                    }
                    return;
                }
            }

            // Standard engine case
            string[] parts = cleanInput.Split(' ', 2);
            string firstWord = parts[0].ToLower();

            string finalKey = searchEngines.ContainsKey(firstWord) ? firstWord : engineKey;
            string? template = searchEngines.ContainsKey(finalKey) ? searchEngines[finalKey] : null;

            if (template == null)
            {
                var first = searchEngines.FirstOrDefault();
                if (first.Key != null) BrowserHelper.OpenBrowser(first.Value.Replace("%s", Uri.EscapeDataString(cleanInput)), inIncognito);
                return;
            }

            searchQuery = searchEngines.ContainsKey(firstWord) ? (parts.Length > 1 ? parts[1] : "") : cleanInput;

            finalUrl = string.IsNullOrWhiteSpace(searchQuery)
                    ? template.Contains("%s") ? BrowserHelper.GetBaseUrl(template) : template
                    : template.Replace("%s", Uri.EscapeDataString(searchQuery));

            BrowserHelper.OpenBrowser(finalUrl, inIncognito);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services
{
    public class EngineService
    {
        private readonly string _searchEnginesPath;
        private readonly Dictionary<string, string> _searchEngines = new Dictionary<string, string>();
        private readonly List<string> _orderedKeys = new List<string>();
        private readonly object _engineLock = new();

        public EngineService(string searchEnginesPath)
        {
            _searchEnginesPath = searchEnginesPath;
            LoadSearchEngines();
        }

        public void LoadSearchEngines()
        {
            try
            {
                lock (_engineLock)
                {   
                    _searchEngines.Clear();
                    _orderedKeys.Clear();
                    if (!File.Exists(_searchEnginesPath))
                    {
                        var lines = new[]
                        {
                            "google https://www.google.com/search?q=%s",
                            "brave https://search.brave.com/search?q=%s",
                            "yt https://www.youtube.com/results?search_query=%s",
                            "sg https://seriesgraph.com/show/search/%s",
                            "ani https://anilist.co/search/anime?search=%s",
                            "cgpt https://chatgpt.com/#autoSubmit=1&prompt=%s",
                            "yan https://yandex.com/search/?text=%s",
                            "gem https://gemini.google.com/#autoSubmit=1&prompt=%s",
                            "lin https://www.linkedin.com/search/results/all/?keywords=%s",
                            "github https://github.com/search?q=%s&type=repositories"
                        };
                        File.Create(_searchEnginesPath).Close();
                        File.WriteAllLines(_searchEnginesPath, lines);
                    }
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string line in File.ReadAllLines(_searchEnginesPath))
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
                        string[] parts = line.Split(' ', 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].ToLowerInvariant();
                            _searchEngines[key] = parts[1];
                            if (seen.Add(key)) _orderedKeys.Add(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading search engines: {ex.Message}", "ERROR");
            }
        }
        public IReadOnlyDictionary<string, string> GetEngines()
        {
            lock (_engineLock)
            {
                return new ReadOnlyDictionary<string, string>(_searchEngines);
            }
        }
        public IReadOnlyList<string> GetOrderedKeys()
        {
            lock (_engineLock)
            {
                return _orderedKeys.AsReadOnly();
            }
        }

        public string GetEnginesPath() => _searchEnginesPath;
        public int Count => _searchEngines.Count;

        public void AddOrUpdateEngine(string alias, string url)
        {
            lock (_engineLock)
            {
                var lines = File.Exists(_searchEnginesPath)
                            ? File.ReadAllLines(_searchEnginesPath).ToList()
                            : new List<string>();
                lines.RemoveAll(line => line.TrimStart().StartsWith(alias + " ", StringComparison.OrdinalIgnoreCase));
                lines.Add($"{alias} {url}");
                File.WriteAllLines(_searchEnginesPath, lines.Where(l => !string.IsNullOrWhiteSpace(l)));

                LoadSearchEngines();
            }
        }

        public bool DeleteEngine(string alias)
        {
            if (!_searchEngines.ContainsKey(alias)) return false;

            lock (_engineLock)
            {
                var lines = File.Exists(_searchEnginesPath)
                            ? File.ReadAllLines(_searchEnginesPath).ToList()
                            : new List<string>();

                lines.RemoveAll(line => line.TrimStart().StartsWith(alias + " ", StringComparison.OrdinalIgnoreCase));
                File.WriteAllLines(_searchEnginesPath, lines.Where(l => !string.IsNullOrWhiteSpace(l)));

                LoadSearchEngines();
            }
            return true;
        }

        public List<Result> GetAvailableEngines(IconService iconService, HistoryService historyService, bool IsIncognitoDefault)
        {
            var engines = GetEngines();
            var orderedKeys = GetOrderedKeys();
            return [.. orderedKeys
                .Select((k, index) => {
                    var url = UrlUtils.BuildSearchUrl("",engines[k]);
                    return new Result{
                        Title = $"{k}",
                        SubTitle = url,
                        IcoPath = iconService.GetIconPath(k),
                        Score = 50000 - index,
                        Action = _ => {
                            historyService.SaveToHistory("",k,IsIncognitoDefault);
                            BrowserService.OpenBrowser(url, IsIncognitoDefault);
                            return true;
                        },
                        ContextData = new CustomResultContext{
                            SearchType = SearchType.DEFAULT,
                            SearchQuery = "",
                            SearchEngine = k,
                            Url = url,
                            IsIncognito = IsIncognitoDefault
                        }
                    };
                })
            ];
        }
    }
}

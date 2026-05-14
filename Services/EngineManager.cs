using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services
{
    public class EngineManager
    {
        private readonly string _searchEnginesPath;
        private readonly Dictionary<string, string> _searchEngines = new Dictionary<string, string>();
        private readonly List<string> _orderedKeys = new List<string>();
        private readonly BrowserPlugin _plugin;

        public EngineManager(string searchEnginesPath, BrowserPlugin plugin)
        {
            _searchEnginesPath = searchEnginesPath;
            _plugin = plugin;
            LoadSearchEngines();
        }

        public void LoadSearchEngines()
        {
            try
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
                        "cgpt https://chatgpt.com/#autoSubmit=1&prompt=%s",
                        "yan https://yandex.com/search/?text=%s",
                        "gem https://gemini.google.com/#autoSubmit=1&prompt=%s",
                        "lin https://www.linkedin.com/search/results/all/?keywords=%s",
                        "github https://github.com/search?q=%s&type=repositories"
                    };
                    File.Create(_searchEnginesPath).Close();
                    File.WriteAllLines(_searchEnginesPath, lines);
                    return;
                }
                foreach (string line in File.ReadAllLines(_searchEnginesPath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    string[] parts = line.Split(' ', 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].ToLower();
                        _searchEngines[key] = parts[1];
                        if (!_orderedKeys.Contains(key)) _orderedKeys.Add(key);
                    }
                }
            }
            catch (Exception ex)
            {
                _plugin.Log($"Error loading search engines: {ex.Message}", "ERROR");
            }
        }

        public Dictionary<string, string> GetEngines() => _searchEngines;
        public List<string> GetOrderedKeys() => _orderedKeys;
        public string GetEnginesPath() => _searchEnginesPath;
        public int Count => _searchEngines.Count;

        public void AddOrUpdateEngine(string alias, string url)
        {
            var lines = File.Exists(_searchEnginesPath)
                        ? File.ReadAllLines(_searchEnginesPath).ToList()
                        : new List<string>();

            lines.RemoveAll(line => line.TrimStart().StartsWith(alias + " ", StringComparison.OrdinalIgnoreCase));
            lines.Add($"{alias} {url}");
            File.WriteAllLines(_searchEnginesPath, lines.Where(l => !string.IsNullOrWhiteSpace(l)));

            LoadSearchEngines();
        }

        public bool DeleteEngine(string alias)
        {
            if (!_searchEngines.ContainsKey(alias)) return false;

            var lines = File.Exists(_searchEnginesPath)
                        ? File.ReadAllLines(_searchEnginesPath).ToList()
                        : new List<string>();

            lines.RemoveAll(line => line.TrimStart().StartsWith(alias + " ", StringComparison.OrdinalIgnoreCase));
            File.WriteAllLines(_searchEnginesPath, lines.Where(l => !string.IsNullOrWhiteSpace(l)));

            LoadSearchEngines();
            return true;
        }
    }
}

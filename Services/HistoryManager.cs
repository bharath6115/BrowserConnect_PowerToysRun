using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services
{
    public class HistoryManager
    {
        private readonly string _historyPath;
        private readonly BrowserPlugin _plugin;
        private List<string> _historyCache = new List<string>();
        private readonly object _lock = new object();
        // lock is needed because we have some fire-and-forget functions such as truncating the history file.
        // the file itself isnt thread safe:
        // if two threads try to access it at same time, it'll throw exception.
        // if one thread tries to read while other writes, exception.

        public HistoryManager(string historyPath, BrowserPlugin plugin)
        {
            _historyPath = historyPath;
            _plugin = plugin;
            LoadHistoryCache();
        }

        public void LoadHistoryCache()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_historyPath))
                    {
                        var lines = File.ReadAllLines(_historyPath);
                        _historyCache = lines.Skip(Math.Max(0, lines.Length - Math.Max(100, _plugin.HistoryCacheSize * 2))).ToList();
                        _plugin.Log($"Loaded {_historyCache.Count} history items into cache.", "TRACE");
                    }
                }
                catch (Exception ex)
                {
                     _plugin.Log($"Error loading history cache: {ex.Message}", "ERROR");
                }
            }
        }

        public void SaveToHistory(string query, string engineKey, bool inIncognito)
        {
            SaveToHistory(new List<(string, string)> { (query, engineKey) }, inIncognito);
        }

        public void SaveToHistory(IEnumerable<(string query, string engineKey)> entries, bool inIncognito)
        {
            if (!_plugin.IsHistoryEnabled) return;
            if (inIncognito && !_plugin.RecordIncognitoHistory) return;

            lock (_lock)
            {
                var validEntries = new List<string>();
                string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

                foreach (var (query, engineKey) in entries)
                {
                    if (string.IsNullOrWhiteSpace(query)) continue;
                    string trimmedQuery = query.Trim();
                    if (trimmedQuery.StartsWith("-") || trimmedQuery.StartsWith("!")) continue;

                    if (_historyCache.Count > 0)
                    {
                        var lastLine = _historyCache.Last();
                        var parts = lastLine.Split('|', 4);
                        if (parts.Length == 4 && parts[1].Trim() == engineKey.Trim() && Uri.UnescapeDataString(parts[2].Trim()) == trimmedQuery && parts[3].Trim().ToLower() == inIncognito.ToString().ToLower()) continue;
                    }

                    string entry = $"{timestamp}|{engineKey}|{Uri.EscapeDataString(trimmedQuery)}|{inIncognito}";
                    _historyCache.Add(entry);
                    validEntries.Add(entry);
                }

                if (validEntries.Count == 0) return;

                try
                {
                    int maxCache = Math.Max(100, _plugin.HistoryCacheSize * 3);
                    while (_historyCache.Count > maxCache) _historyCache.RemoveAt(0);

                    File.AppendAllLines(_historyPath, validEntries);
                    _plugin.Log($"Saved batch of {validEntries.Count} to history file.", "TRACE");

                    Task.Run(() => TruncateHistoryFile());
                }
                catch (Exception ex)
                {
                    _plugin.Log($"Error saving history batch: {ex.Message}", "ERROR");
                }
            }
        }

        private void TruncateHistoryFile()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_historyPath)) return;
                    var lines = File.ReadAllLines(_historyPath);
                    if (lines.Length > _plugin.HistoryLimit)
                    {
                        _plugin.Log($"Truncating history: {lines.Length} total lines -> limit {_plugin.HistoryLimit}", "TRACE");
                        int keep = (int)(_plugin.HistoryLimit * 0.8);
                        File.WriteAllLines(_historyPath, lines.Skip(lines.Length - keep));
                    }
                }
                catch (Exception ex)
                {
                    _plugin.Log($"Error truncating history: {ex.Message}", "ERROR");
                }
            }
        }

        public List<string> GetHistoryCache()
        {
            lock (_lock) return new List<string>(_historyCache);
        }

        public List<string> GetFullHistory()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_historyPath))
                    {
                        return File.ReadAllLines(_historyPath).ToList();
                    }
                }
                catch (Exception ex)
                {
                    _plugin.Log($"Error reading full history file: {ex.Message}", "ERROR");
                }
            }
            return new List<string>();
        }
    }
}

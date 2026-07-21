using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;
using Community.PowerToys.Run.Plugin.BrowserConnect.Settings;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services
{
    public class HistoryService
    {
        private readonly string _historyPath;
        private readonly PluginSettings Settings;
        private List<string> _historyCache = new List<string>();
        private int _historyLineCount = 0;
        private int _truncationScheduled = 0;

        // lock is needed because we have some fire-and-forget functions such as truncating the history file.
        // the file itself isnt thread safe:
        // if two threads try to access it at same time, it'll throw exception.
        // if one thread tries to read while other writes, exception.
        private readonly object _lock = new();

        public HistoryService(string historyPath, PluginSettings settings)
        {
            _historyPath = historyPath;
            Settings = settings;
            LoadHistoryCache();
        }

        /// <summary>
        /// Loads recent history entries into memory and records the current history file line count.
        /// </summary>
        public void LoadHistoryCache()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_historyPath))
                    {
                        var lines = File.ReadAllLines(_historyPath);
                        _historyLineCount = lines.Length;
                        _historyCache = lines.Skip(Math.Max(0, lines.Length - Math.Max(100, Settings.HistoryCacheSize * 3))).ToList();
                        Logger.Log($"Loaded {_historyCache.Count} history items into cache.", "TRACE");
                    }
                    else
                    {
                        _historyLineCount = 0;
                    }
                }
                catch (Exception ex)
                {
                     Logger.Log($"Error loading history cache: {ex.Message}", "ERROR");
                }
            }
        }

        /// <summary>
        /// Saves a single query and engine pair to history when history settings allow it.
        /// </summary>
        /// <param name="query">Normalized query or payload text to store.</param>
        /// <param name="engineKey">Search engine key used for the query.</param>
        /// <param name="inIncognito">Whether the query was opened in incognito mode.</param>
        public void SaveToHistory(string query, string engineKey, bool inIncognito)
        {
            SaveToHistory(new List<(string, string)> { (query, engineKey) }, inIncognito);
        }

        /// <summary>
        /// Saves a replayable multi-engine entry followed by per-engine entries for the same normalized query.
        /// </summary>
        /// <param name="searchQuery">Normalized search query.</param>
        /// <param name="targetEngines">Engine aliases selected for the multi-engine search.</param>
        /// <param name="inIncognito">Whether the query was opened in incognito mode.</param>
        public void SaveToHistory(string searchQuery, List<string> targetEngines, bool inIncognito)
        {
            var mergedEngines = InputUtils.MergeWithSeparator(", ", [..targetEngines]);
            var entries = targetEngines.Select(e => (searchQuery, e)).ToList();
            entries.Insert(0, (InputUtils.MergeWithSeparator(SymbolConsts.RECORD_SEPERATOR,mergedEngines,searchQuery), "_MULTI"));
            SaveToHistory(entries, inIncognito);
        }

        /// <summary>
        /// Saves a live provider result with enough metadata to display and replay it from history.
        /// Saved payload format: title \u001e url \u001e thumbnailRef
        /// </summary>
        /// <param name="query">Live result title to store.</param>
        /// <param name="url">URL opened by the live result.</param>
        /// <param name="thumbnailRef">Provider thumbnail reference in "Provider#Id" format.</param>
        /// <param name="inIncognito">Whether the query was opened in incognito mode.</param>
        public void SaveToHistory(string query, string url, string thumbnailRef, bool inIncognito)
        {
            SaveToHistory(new List<(string, string)> {(InputUtils.MergeWithSeparator(SymbolConsts.RECORD_SEPERATOR ,query, url, thumbnailRef), "_LIVE")}, inIncognito);
        }

        /// <summary>
        /// Saves a batch of history entries, skipping history-command entries and scheduling truncation if needed.
        /// </summary>
        /// <param name="entries">Payload and entry-type pairs to append to history.</param>
        /// <param name="inIncognito">Whether the entries were opened in incognito mode.</param>
        public void SaveToHistory(IEnumerable<(string query, string engineKey)> entries, bool inIncognito)
        {
            if (!Settings.IsHistoryEnabled) return;
            if (inIncognito && !Settings.RecordIncognitoHistory) return;

            lock (_lock)
            {
                var validEntries = new List<string>();
                string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

                foreach (var (query, engineKey) in entries)
                {
                    string trimmedQuery = query.Trim();
                    if (trimmedQuery.StartsWith('!')) continue;

                    string entry = $"{timestamp}|{engineKey}|{Uri.EscapeDataString(trimmedQuery)}|{inIncognito}";
                    _historyCache.Add(entry);
                    validEntries.Add(entry);
                }
                if (validEntries.Count == 0) return;

                try
                {
                    int maxCache = Math.Max(100, Settings.HistoryCacheSize * 3);
                    while (_historyCache.Count > maxCache) _historyCache.RemoveAt(0);

                    File.AppendAllLines(_historyPath, validEntries);
                    _historyLineCount += validEntries.Count;
                    Logger.Log($"Saved batch of {validEntries.Count} to history file.", "TRACE");

                    if (Settings.AutoTruncateHistory && _historyLineCount >= Settings.HistoryLimit) ScheduleTruncation();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error saving history batch: {ex.Message}", "ERROR");
                }
            }
        }

        /// <summary>
        /// Deletes the given serialized entry from both cache and history.
        /// </summary>
        /// <param name="entry">Exact serialized history line to delete.</param>
        public void DeleteEntry(string entry)
        {
            lock (_lock)
            {
                if (File.Exists(_historyPath))
                {
                    _historyCache.Remove(entry);
                    var lines = File.ReadAllLines(_historyPath).ToList();
                    lines.Remove(entry);
                    File.WriteAllLines(_historyPath,lines);
                }
            }
        }

        /// <summary>
        /// Schedules a single delayed history truncation so rapid writes do not create duplicate truncation tasks.
        /// </summary>
        private void ScheduleTruncation()
        {
            if (Interlocked.CompareExchange(ref _truncationScheduled, 1, 0) != 0) return;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(2000);
                    TruncateHistoryFile();
                }
                finally
                {
                    Interlocked.Exchange(ref _truncationScheduled, 0);
                }
            });
        }

        /// <summary>
        /// Trims the history file to the configured limit and updates the tracked line count.
        /// </summary>
        private void TruncateHistoryFile()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_historyPath))
                    {
                        _historyLineCount = 0;
                        return;
                    }

                    var lines = File.ReadAllLines(_historyPath);
                    _historyLineCount = lines.Length;
                    if (lines.Length > Settings.HistoryLimit)
                    {
                        Logger.Log($"Truncating history: {lines.Length} total lines -> limit {Settings.HistoryLimit}", "TRACE");
                        int keep = (int)(Settings.HistoryLimit * 0.8);
                        File.WriteAllLines(_historyPath, lines.Skip(lines.Length - keep));
                        _historyLineCount = keep;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error truncating history: {ex.Message}", "ERROR");
                }
            }
        }

        /// <summary>
        /// Returns a thread-safe snapshot of the in-memory recent history cache.
        /// </summary>
        /// <returns>A copy of cached history lines.</returns>
        public List<string> GetHistoryCache()
        {
            lock (_lock) return new List<string>(_historyCache);
        }

        /// <summary>
        /// Reads all history lines from disk for broader history searches.
        /// </summary>
        /// <returns>All persisted history lines, or an empty list if the file cannot be read.</returns>
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
                    Logger.Log($"Error reading full history file: {ex.Message}", "ERROR");
                }
            }
            return new List<string>();
        }
    }
}

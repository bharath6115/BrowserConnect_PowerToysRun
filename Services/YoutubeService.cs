using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wox.Plugin;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Community.PowerToys.Run.Plugin.BrowserConnect.Handlers;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services
{
    public class YoutubeService
    {
        private readonly BrowserPlugin _plugin;
        private readonly HistoryManager _historyManager;
        private readonly IconManager _iconManager;

        private string[] _youtubeApiKeys;
        private int _youtubeApiKeysIndex = 0;
        private ConcurrentDictionary<string, SearchListResponse> _youtubeQueriesCache = new();
        private readonly object _youtubeCacheLock = new();

        public YoutubeService(string? pluginDir, BrowserPlugin plugin, HistoryManager historyManager, IconManager iconManager)
        {
            _plugin = plugin;
            _historyManager = historyManager;
            _iconManager = iconManager;

            // Initialize API Key
            string apiPath = pluginDir != null ? Path.Combine(pluginDir, "google_api.txt") : "";
            if (!string.IsNullOrEmpty(apiPath) && File.Exists(apiPath))
            {
                _youtubeApiKeys = File.ReadAllLines(apiPath).Select(k => k.Trim()).Where(k => !string.IsNullOrWhiteSpace(k)).ToArray();
            }
            else
            {
                _youtubeApiKeys = [];
                // Create a template file so the user knows where to put their key
                if (!string.IsNullOrEmpty(apiPath))
                {
                    try { File.WriteAllText(apiPath, "YOUR_API_KEYS_HERE"); } catch { }
                }
            }
        }

        public List<Result> GetYoutubeResults(string engineKey, string searchQuery, bool inIncognito)
        {
            var results = new List<Result>();
            // Manual Lock- To prevent multiple threads sending same API request
            lock (_youtubeCacheLock)
            {
                // we cant await because the function cant be async, so we use .GetAwaiter().GetResult() -> basically what await does
                // we delegate this to other thread using Task.Run() because not doing will cause deadlock -
                // UI (this) thread will pause execution if we do return func().GetAwaiter().GetResult() - blocking (await no blocking)
                // when the value is obtained, itll try to resume in UI thread which is blocked.
                // so we delegate this to other thread to not freeze the UI thread, and return the result when we get it.
                var ytResults = Task.Run(() => FetchYoutubeResultsAsync(engineKey, searchQuery, inIncognito)).GetAwaiter().GetResult();
                if (ytResults != null)
                {
                    results.AddRange(ytResults);
                }
            }
            return results;
        }

        private async Task<List<Result>> FetchYoutubeResultsAsync(string engineKey, string searchQuery, bool inIncognito)
        {
            var results = new List<Result>();

            if (_youtubeApiKeys.Length == 0) return results;

            try
            {
                if (!_youtubeQueriesCache.TryGetValue(searchQuery, out var cachedData))
                {
                    var _ApiKey = _youtubeApiKeys[_youtubeApiKeysIndex++ % _youtubeApiKeys.Length];
                    _youtubeApiKeysIndex %= _youtubeApiKeys.Length;

                    using var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        ApiKey = _ApiKey,
                        ApplicationName = "browserConnect"
                    });

                    _plugin.Log($"Started fetching youtube results for query: [{searchQuery}] (Incognito: {inIncognito})", "ACTION");

                    var searchRequest = youtubeService.Search.List("snippet");
                    searchRequest.Q = searchQuery;
                    searchRequest.MaxResults = 10;
                    searchRequest.Type = "video";

                    var response = await searchRequest.ExecuteAsync();
                    _youtubeQueriesCache.TryAdd(searchQuery, response);
                    cachedData = response;
                }

                if (cachedData?.Items != null)
                {
                    foreach (var item in cachedData.Items)
                    {
                        if (item.Id?.VideoId == null) continue;

                        string videoUrl = $"https://www.youtube.com/watch?v={item.Id.VideoId}";
                        string title = System.Net.WebUtility.HtmlDecode(item.Snippet?.Title ?? "Unknown Title");
                        string channel = System.Net.WebUtility.HtmlDecode(item.Snippet?.ChannelTitle ?? "Unknown Channel");
                        //To Decode the &_; type notations.

                        results.Add(new Result
                        {
                            Title = title,
                            SubTitle = $"▶ {channel}",
                            IcoPath = _iconManager.GetIconPath(engineKey),
                            Score = 40000,
                            Action = _ =>
                            {
                                _plugin.Log($"Opening YouTube video: {videoUrl} (Incognito: {inIncognito})", "ACTION");
                                _historyManager.SaveToHistory(title + "[" + videoUrl + "]", "_URL", inIncognito);
                                BrowserHelper.OpenBrowser(videoUrl, inIncognito);
                                return true;
                            },
                            QueryTextDisplay = title
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _plugin.Log($"YouTube API error: {ex.Message}", "ERROR");
            }
            return results;
        }

        public void ClearCache() => _youtubeQueriesCache.Clear();
    }
}

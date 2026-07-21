using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models.Youtube;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.Interfaces;
using System.Net.Http.Json;
using System.Net;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Providers.Youtube;

public class YoutubeProvider : ISearchProvider
{
    private int _youtubeApiKeyIndex = 0;
    private volatile string[] _youtubeApiKeys = [];
    private readonly string _apiPath;
    //TODO: Auto Cache clearing.
    private readonly ConcurrentDictionary<string, Task<List<CustomSearchResult>>> _youtubeQueriesCache = new();
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public YoutubeProvider()
    {
        _apiPath = BrowserPlugin.pluginDir != null ? Path.Combine(BrowserPlugin.pluginDir, "google_api.txt") : "";
        LoadYoutubeApiKeys();
    }

    /// <summary>
    /// Returns YouTube video results and caches successful lookups in memory.
    /// </summary>
    /// <param name="query">Search query to send to the YouTube Data API.</param>
    public async Task<List<CustomSearchResult>> SearchAsync(string query)
    {
        query = query.Trim().ToLowerInvariant();

        try
        {
            return await _youtubeQueriesCache.GetOrAdd(query, FetchYoutubeResultsAsync);
        }
        catch
        {
            _youtubeQueriesCache.TryRemove(query, out _);
            throw;
        }
    }

    //TODO: Implement pagination
    //TODO: Refactor httpClient common code
    private async Task<List<CustomSearchResult>> FetchYoutubeResultsAsync(string query)
    {
        var results = new List<CustomSearchResult>();
        if (_youtubeApiKeys.Length == 0)
        {
            Logger.Log("No YouTube API keys configured.", "WARN");
            return results;
        }

        Logger.Log($"Started fetching YouTube results for query: [{query}]", "ACTION");

        for (int attempt = 0; attempt < _youtubeApiKeys.Length; attempt++)
        {
            var (apiKey, keyIndex) = GetNextApiKey();
            Logger.Log($"Using YouTube API key #{keyIndex + 1} (Attempt {attempt + 1}/{_youtubeApiKeys.Length})", "TRACE");

            string url =
                $"https://www.googleapis.com/youtube/v3/search" +
                $"?part=snippet" +
                $"&q={Uri.EscapeDataString(query)}" +
                $"&type=video" +
                $"&maxResults=25" +
                $"&fields=items(id/videoId,snippet(title,channelTitle,thumbnails/default/url))" +
                $"&key={apiKey}";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    YoutubeErrorResponse? error = null;
                    try
                    {
                        error = JsonSerializer.Deserialize<YoutubeErrorResponse>(errorBody,_options);
                    }
                    catch{}

                    Logger.Log($"YouTube API returned {(int)response.StatusCode} ({response.ReasonPhrase}). {errorBody}","ERROR");

                    if (response.StatusCode == HttpStatusCode.Forbidden && ShouldRetryWithNextKey(error) && attempt < _youtubeApiKeys.Length - 1)
                    {
                        Logger.Log("API quota exhausted. Switching to the next API key.", "WARN");
                        continue;
                    }
                    response.EnsureSuccessStatusCode();
                }

                var youtubeResponse = await response.Content.ReadFromJsonAsync<YoutubeSearchResponse>(_options);

                if (youtubeResponse?.Items is null) return results;

                foreach (var item in youtubeResponse.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.Id?.VideoId))
                        continue;

                    results.Add(new CustomSearchResult
                    {
                        Id = item.Id.VideoId,
                        Title = WebUtility.HtmlDecode(item.Snippet?.Title ?? "Unknown Title"),
                        Subtitle = $"▶ {WebUtility.HtmlDecode(item.Snippet?.ChannelTitle ?? "Unknown Channel")}",
                        Url = $"https://www.youtube.com/watch?v={item.Id.VideoId}",
                        ThumbnailUrl = item.Snippet?.Thumbnails?.Default?.Url ?? ""
                    });
                }

                return results;
            }
            catch (HttpRequestException ex)
            {
                Logger.Log($"Network error while contacting YouTube: {ex.Message}", "ERROR");
                throw;
            }
            catch (OperationCanceledException ex)
            {
                Logger.Log($"YouTube request timed out: {ex.Message}", "ERROR");
                throw;
            }
            catch (JsonException ex)
            {
                Logger.Log($"Failed to parse YouTube response: {ex.Message}", "ERROR");
                throw;
            }
        }

        Logger.Log("All configured YouTube API keys have exhausted their quota.", "ERROR");
        return results;
    }

    /// <summary>
    /// Returns the next API Key.
    /// Increments the index in an atomic operation for multithread safety.
    /// </summary>
    private (string ApiKey, int index) GetNextApiKey()
    {
        int index = Interlocked.Increment(ref _youtubeApiKeyIndex) - 1;
        return (_youtubeApiKeys[index % _youtubeApiKeys.Length], index % _youtubeApiKeys.Length);
    }

    private static bool ShouldRetryWithNextKey(YoutubeErrorResponse? error)
    {
        if (error?.Error?.Errors is null) return false;

        return error.Error.Errors.Any(e => e.Reason is "quotaExceeded" or "dailyLimitExceeded");
    }

    // TODO: Document plaintext API key storage or protect this file with Windows DPAPI.
    private void LoadYoutubeApiKeys()
    {
        if (!string.IsNullOrEmpty(_apiPath) && File.Exists(_apiPath))
        {
            _youtubeApiKeys = 
            [
                .. File.ReadAllLines(_apiPath)
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrWhiteSpace(k) && k != "YOUR_API_KEYS_HERE")
            ];
            Logger.Log($"Loaded {_youtubeApiKeys.Length} YouTube API keys.", "INFO");
        }
        else
        {
            _youtubeApiKeys = [];

            if (!string.IsNullOrEmpty(_apiPath))
            {
                try
                {
                    File.WriteAllText(_apiPath, "YOUR_API_KEYS_HERE");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to create YouTube API key file: {ex.Message}", "ERROR");
                }
            }
        }
        _youtubeApiKeyIndex = 0;
    }
    
    public void ClearCache() => _youtubeQueriesCache.Clear();

    public void ReloadYoutubeApiData(){
        LoadYoutubeApiKeys();
        ClearCache();
    }
}

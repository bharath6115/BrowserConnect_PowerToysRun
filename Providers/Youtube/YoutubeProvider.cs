using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.Interfaces;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Providers.Youtube;

public class YoutubeProvider : ISearchProvider
{
    private int _youtubeApiKeysIndex = 0;
    private readonly string[] _youtubeApiKeys;
    //TODO: Cache clearing.
    private readonly ConcurrentDictionary<string, Task<List<CustomSearchResult>>> _youtubeQueriesCache = new();

    public YoutubeProvider()
    {
        // Initialize API Key
        string apiPath = BrowserPlugin.pluginDir != null ? Path.Combine(BrowserPlugin.pluginDir, "google_api.txt") : "";
        // TODO: Document plaintext API key storage or protect this file with Windows DPAPI.
        if (!string.IsNullOrEmpty(apiPath) && File.Exists(apiPath))
        {
            _youtubeApiKeys = [.. File.ReadAllLines(apiPath).Select(k => k.Trim()).Where(k => !string.IsNullOrWhiteSpace(k))];
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
    private async Task<List<CustomSearchResult>> FetchYoutubeResultsAsync(string query)
    {
        var results = new List<CustomSearchResult>();
        if (_youtubeApiKeys.Length is 0) return results;

        try
        {
            using var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = GetNextApiKey(),
                ApplicationName = "browserConnect"
            });

            var searchRequest = youtubeService.Search.List("snippet");
            searchRequest.Q = query;
            searchRequest.MaxResults = 10;
            searchRequest.Type = "video";

            Logger.Log($"Started fetching youtube results for query: [{query}]", "ACTION");

            var response = await searchRequest.ExecuteAsync();
            
            if(response?.Items is null) return results;

            foreach (var item in response.Items)
            {
                if (item.Id?.VideoId is null) continue;

                // Decode HTML entities in titles returned by the YouTube API.
                string title = System.Net.WebUtility.HtmlDecode(item.Snippet?.Title ?? "Unknown Title");
                string channel = System.Net.WebUtility.HtmlDecode(item.Snippet?.ChannelTitle ?? "Unknown Channel");
                string thumbnailUrl = item.Snippet?.Thumbnails?.Default__?.Url ?? "";
                string videoUrl = $"https://www.youtube.com/watch?v={item.Id.VideoId}";
                //TODO: Add duration and view count using https://developers.google.com/youtube/v3/docs/videos#contentDetails
                //var request = youtubeService.Videos.List("contentDetails,statistics");

                results.Add(new CustomSearchResult{
                    Id = item.Id.VideoId,
                    Title = title,
                    Subtitle = $"▶ {channel}",
                    Url = videoUrl,
                    ThumbnailUrl = thumbnailUrl
                });
            }
        }
        catch (Exception ex)
        {
            //TODO: If exception is due to quota exhaustion, automatically re-call the function.
            Logger.Log($"YouTube API error: {ex.Message}", "ERROR");
            throw;
        }
        return results;        
    }
    /// <summary>
    /// Returns the next API Key.
    /// Increments the index in an atomic operation for multithread safety.
    /// </summary>
    private string GetNextApiKey()
    {
        int index = Interlocked.Increment(ref _youtubeApiKeysIndex) - 1;
        return _youtubeApiKeys[index % _youtubeApiKeys.Length];
    }

    public void ClearCache() => _youtubeQueriesCache.Clear();
}

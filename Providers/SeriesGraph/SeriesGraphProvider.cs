using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Community.PowerToys.Run.Plugin.BrowserConnect.Interfaces;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models.SeriesGraph;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Wox.Infrastructure;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Providers.SeriesGraph;

public class SeriesGraphProvider : ISearchProvider
{
    private static readonly HttpClient _httpClient = new();
    private const string Endpoint = "https://seriesgraph.com/api/shows/search?searchTerm=";
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };
    public async Task<List<CustomSearchResult>> SearchAsync(string query)
    {
        var results = new List<CustomSearchResult>();
        try
        {
            Logger.Log($"Started fetching seriesgraph results for query: [{query}]", "ACTION");

            var data = await _httpClient.GetFromJsonAsync<SeriesGraphResponse>($"{Endpoint}{Uri.EscapeDataString(query)}", _options);
            if (data?.Results is null) return results;

            foreach(var entry in data.Results)
            {
                results.Add(new CustomSearchResult
                {
                    Id = entry.Id.ToString(),
                    Title = entry.Name,
                    Subtitle = GetSubTitle(entry),
                    Url = $"https://seriesgraph.com/show/{entry.Id}",
                    ThumbnailUrl = $"https://image.tmdb.org/t/p/w185{entry.PosterPath}"
                });
            }
            return results;
        }
        catch (Exception ex)
        {
            Logger.Log($"Anilist API error: {ex.Message}", "ERROR");
            throw;
        }
    }
    private static string GetSubTitle(Result media)
    {
        List<string> parts = [];

        if (media.FirstAirDate is not null)
        {
            parts.Add(media.FirstAirDate.Split("-")[0]);
        }
        if (media.VoteAverage is double average)
        {
            string ratingString = $"⭐ {average:F1}";
            if(media.VoteCount is int count)
            {
                ratingString += $" ({count})";
            }
            parts.Add(ratingString);
        }
        
        return string.Join(" | ", parts);
        // return string.Join(" • ", parts);
    }
}

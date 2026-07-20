
using Community.PowerToys.Run.Plugin.BrowserConnect.Interfaces;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models.Anilist;
using Community.PowerToys.Run.Plugin.BrowserConnect.Providers.GraphQL;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Providers.Anilist;

public class AnilistProvider : ISearchProvider
{
    private const string Endpoint = "https://graphql.anilist.co";
    private const string GraphQLQuery = @"
    query ($search: String!) {
        Page {
            media(search: $search, type: ANIME) {
                id
                title {
                    romaji
                    english
                }
                coverImage {
                    medium
                }
                season
                seasonYear
                episodes
                averageScore
            }
        }
    }";
    public async Task<List<CustomSearchResult>> SearchAsync(string query)
    {
        var variables = new { search = query };
        var results = new List<CustomSearchResult>();

        try
        {
            Logger.Log($"Started fetching anilist results for query: [{query}]", "ACTION");

            var data = await GraphQLClient.QueryAsync<AniListResponse>(Endpoint, GraphQLQuery, variables);

            if (data?.Data?.Page?.Media is null) return results;

            foreach(var media in data.Data.Page.Media)
            {
                results.Add(new CustomSearchResult
                {
                    Id = media.Id.ToString(),
                    Title = GetTitle(media.Title),
                    Subtitle = GetSubTitle(media),
                    Url = $"https://anilist.co/anime/{media.Id}",
                    Score = GetScore(media.SeasonYear, media.Season),
                    ThumbnailUrl = media.CoverImage?.Medium
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

    private static string GetTitle(Title title)
    {
        if (title.English is not null)
        {
            return title.Romaji is not null
                ? $"{title.English} [{title.Romaji}]"
                : title.English;
        }

        return title.Romaji ?? "Unknown Title";
    }

    private static string GetSubTitle(Media media)
    {
        List<string> parts = [];

        if (media.Season is not null && media.SeasonYear is not null)
        {
            string season = char.ToUpperInvariant(media.Season[0]) + media.Season[1..].ToLowerInvariant();
            parts.Add($"{season} {media.SeasonYear}");
        }
        else if (media.SeasonYear is not null)
        {
            parts.Add(media.SeasonYear.Value.ToString());
        }

        if (media.Episodes is int episodes)
        {
            parts.Add($"{episodes} Episode{(episodes == 1 ? "" : "s")}");
        }
        
        if (media.AverageScore is not null)
        {
            parts.Add($"⭐ {media.AverageScore}");
        }
        return string.Join(" | ", parts);
        // return string.Join(" • ", parts);
    }
    private static int GetScore(int? seasonYear, string? season)
    {
        int baseValue = 40000;
        int baseYear = 2000;
        if (seasonYear is not int year || season is null) return baseValue;

        int calculatedScore = (DateTime.Now.Year - year) * 4 + GetSeasonIndex(season);

        return baseValue + calculatedScore*(year < baseYear ? -1:1); 
    }

    private static int GetSeasonIndex(string season) => season switch
    {
        "WINTER" => 0,
        "SPRING" => 1,
        "SUMMER" => 2,
        "FALL" => 3,
        _ => 0
    };
}
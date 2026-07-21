using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Models.SeriesGraph;
    
public class SeriesGraphResponse
{
    [JsonPropertyName("results")]
    public List<Result> Results { get; set; } = null!;
}

public class Result
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("poster_path")]
    public string PosterPath { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }

    [JsonPropertyName("vote_count")]
    public int VoteCount { get; set; }

    [JsonPropertyName("first_air_date")]
    public string FirstAirDate { get; set; } = null!;
}
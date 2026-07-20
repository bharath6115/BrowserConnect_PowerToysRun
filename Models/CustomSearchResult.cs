namespace Community.PowerToys.Run.Plugin.BrowserConnect.Models;

public class CustomSearchResult
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public string? Subtitle { get; init; }
    public int Score { get; init; } = 40000;
    public string? ThumbnailUrl { get; init; }
}
using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Models.Youtube;

public sealed class YoutubeErrorResponse
{
    public YoutubeError Error { get; init; } = new();
}

public sealed class YoutubeError
{
    public List<YoutubeErrorItem> Errors { get; init; } = [];
}

public sealed class YoutubeErrorItem
{
    public string Reason { get; init; } = "";
    public string Message { get; init; } = "";
}
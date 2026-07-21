using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Models.Youtube;

public class YoutubeSearchResponse
{
    [JsonPropertyName("items")]
    public List<Item> Items { get; set; } = new();
}

public class Item
{
    [JsonPropertyName("id")]
    public Id Id { get; set; } = new();

    [JsonPropertyName("snippet")]
    public Snippet Snippet { get; set; } = new();
}

public class Id
{
    [JsonPropertyName("videoId")]
    public string VideoId { get; set; } = "";
}

public class Snippet
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("channelTitle")]
    public string ChannelTitle { get; set; } = "";

    [JsonPropertyName("thumbnails")]
    public Thumbnails Thumbnails { get; set; } = new();
}

public class Thumbnails
{
    [JsonPropertyName("default")]
    public Thumbnail Default { get; set; } = new();
}

public class Thumbnail
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}
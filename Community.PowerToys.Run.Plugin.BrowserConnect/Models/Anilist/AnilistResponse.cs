namespace Community.PowerToys.Run.Plugin.BrowserConnect.Models.Anilist;
public class AniListResponse
{
    public Data Data { get; set; } = null!;
}

public class Data
{
    public Page Page { get; set; } = null!;
}

public class Page
{
    public List<Media> Media { get; set; } = [];
}

public class Media
{
    public int Id { get; set; }
    public Title Title { get; set; } = null!;
    public CoverImage CoverImage { get; set; } = null!;
    public string? Season { get; set; }
    public int? SeasonYear { get; set; }
    public int? Episodes { get; set; }
    public int? AverageScore { get; set; }
}

public class Title
{
    public string? Romaji { get; set; }
    public string? English { get; set; }
}

public class CoverImage
{
    public string? Medium { get; set; }
}
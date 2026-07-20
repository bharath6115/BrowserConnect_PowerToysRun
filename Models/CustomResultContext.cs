namespace Community.PowerToys.Run.Plugin.BrowserConnect.Models;

/// <summary>
/// Carries BrowserConnect-specific metadata for result actions, context menus, and history replay.
/// </summary>
/// <remarks>
/// For history logging when searching from opposite incognito state
/// Common      : IsIncognito, SearchType (To determine Type) 
/// For _URL    : Url
/// For _MULTI  : EncodedQuery
/// For _LIVE   : Title, Url, ThumbnailRef
/// For Normal  : SearchQuery, SearchEngine
/// 
/// For helper commands which involve opening file: IsFlagToOpenFile
/// For Path    : FilePath
/// </remarks>
public class CustomResultContext
{
    public SearchType SearchType { get; init; } = SearchType.DEFAULT;

    public string SearchQuery { get; init; } = "";
    public string SearchEngine { get; init; } = "";

    public string EncodedQuery { get; init; } = "";

    public string Title { get; init; } = "";
    public string Url { get; init; } = "";
    public string ThumbnailRef { get; init; } = "";

    public bool IsIncognito { get; init; }

    public string HistoryLine { get; set; } = "";

    public bool IsFlagToOpenFile { get; init; }
    public string FilePath { get; init; } = "";
}

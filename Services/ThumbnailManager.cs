
using System.IO;
using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services;

/// <summary>
/// Manages cached provider thumbnails and their fallback images.
/// </summary>
public class ThumbnailManager
{
    private readonly string ThumbnailsPath;
    private readonly IconService _iconService;

    /// <summary>
    /// Creates a thumbnail manager and ensures the thumbnail root folder exists.
    /// </summary>
    /// <param name="thumbnailsPath">Root folder where provider thumbnail folders are stored.</param>
    /// <param name="iconService">Service used to download thumbnail images.</param>
    public ThumbnailManager(string thumbnailsPath, IconService iconService)
    {
        ThumbnailsPath = thumbnailsPath;
        _iconService = iconService;

        Directory.CreateDirectory(ThumbnailsPath);
    }

    /// <summary>
    /// Ensures the provider folder and default thumbnail exist.
    /// </summary>
    /// <param name="provider">Provider name used as the thumbnail folder name.</param>
    /// <param name="defaultIconUrl">Url used to download the provider fallback thumbnail.</param>
    public async Task EnsureProviderInitializedAsync(string provider, string defaultIconUrl)
    {
        string folder = Path.Combine(ThumbnailsPath, provider);
        Directory.CreateDirectory(folder);

        string defaultPath = Path.Combine(folder, "_default.jpg");

        if (!File.Exists(defaultPath))
        {
            await _iconService.DownloadIcon(defaultIconUrl, defaultPath);
        }
    }

    /// <summary>
    /// Downloads a thumbnail when it is not already cached.
    /// </summary>
    /// <param name="provider">Provider name used as the thumbnail folder name.</param>
    /// <param name="id">Thumbnail id used as the file name.</param>
    /// <param name="url">Url used to download the thumbnail.</param>
    public async Task EnsureThumbnailExistsAsync(string provider, string id, string url)
    {
        if (ThumbnailExists(provider, id)) return;
        await _iconService.DownloadIcon(url, GetThumbnailPath(provider, id));
    }

    /// <summary>
    /// Checks whether a thumbnail is already cached for a provider item.
    /// </summary>
    /// <param name="provider">Provider name used as the thumbnail folder name.</param>
    /// <param name="id">Thumbnail id used as the file name.</param>
    /// <returns>True when the thumbnail file exists.</returns>
    public bool ThumbnailExists(string provider, string id) => File.Exists(GetThumbnailPath(provider, id));

    /// <summary>
    /// Returns the expected thumbnail file path for a provider item.
    /// </summary>
    /// <param name="provider">Provider name used as the thumbnail folder name.</param>
    /// <param name="id">Thumbnail id used as the file name.</param>
    /// <returns>Full path to the cached thumbnail file.</returns>
    public string GetThumbnailPath(string provider, string id) => Path.Combine(Path.Combine(ThumbnailsPath, provider), $"{id}.jpg");

    /// <summary>
    /// Returns the provider fallback thumbnail path.
    /// </summary>
    /// <param name="provider">Provider name used as the thumbnail folder name.</param>
    /// <returns>Full path to the provider default thumbnail file.</returns>
    public string GetDefaultThumbnailPath(string provider) => Path.Combine(Path.Combine(ThumbnailsPath, provider), "_default.jpg");

    /// <summary>
    /// Returns the thumbnail path for a saved thumbnail reference.
    /// </summary>
    /// <param name="thumbnailRef">Thumbnail reference in the format "Provider#Id".</param>
    /// <returns>The cached thumbnail path, the provider default thumbnail path, or the app default icon path.</returns>
    public string GetThumbnailPath(string thumbnailRef)
    {
        var index = thumbnailRef.IndexOf('#');
        if (index == -1) return IconConsts.DEFAULT;
        
        string provider = thumbnailRef[..index];
        string id = thumbnailRef[(index + 1)..];

        if (ThumbnailExists(provider, id)) return GetThumbnailPath(provider, id);
        return GetDefaultThumbnailPath(provider);
    }
}

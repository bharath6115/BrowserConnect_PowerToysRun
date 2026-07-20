using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services;

public class IconService
{
    private readonly string _pluginDir;
    private readonly string _imagesDir;
    private readonly bool _isDarkTheme;
    private readonly ConcurrentDictionary<string, bool> _downloadingDomains = new ConcurrentDictionary<string, bool>();
    private readonly ConcurrentDictionary<string, DateTime> _failedFetches = new ConcurrentDictionary<string, DateTime>();
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly EngineService _engineService;

    public IconService(string pluginDir, string imagesDir, bool isDarkTheme, EngineService engineService)
    {
        _pluginDir = pluginDir;
        _imagesDir = imagesDir;
        _isDarkTheme = isDarkTheme;
        _engineService = engineService;
    }

    //TODO: Optimise somehow - currently 4 lookups are done to get path. Can cache instead -> Since search engines are few.
    /// <summary>
    /// Returns the best icon path for a search engine or built-in icon key.
    /// </summary>
    /// <param name="searchEngine">Search engine alias or built-in icon key.</param>
    /// <returns>A relative icon path for PowerToys Run to display.</returns>
    public string GetIconPath(string searchEngine)
    {
        if (_pluginDir == null || searchEngine.Equals(IconConsts.DEFAULT)) return Path.Combine("Images", _isDarkTheme ? "browserConnect.dark.png" : "browserConnect.light.png");

        string engineName = searchEngine.StartsWith("@") ? searchEngine.Substring(1) : searchEngine;
        string themeSuffix = _isDarkTheme ? ".dark" : ".light";

        // Check theme-specific icons first
        string themeIcoPath = Path.Combine(_imagesDir, $"{engineName}{themeSuffix}.ico");
        string themePngPath = Path.Combine(_imagesDir, $"{engineName}{themeSuffix}.png");

        if (File.Exists(themeIcoPath)) return Path.GetRelativePath(_pluginDir, themeIcoPath);
        if (File.Exists(themePngPath)) return Path.GetRelativePath(_pluginDir, themePngPath);

        // Check generic icons
        string icoPath = Path.Combine(_imagesDir, $"{engineName}.ico");
        string pngPath = Path.Combine(_imagesDir, $"{engineName}.png");

        if (File.Exists(icoPath)) return Path.GetRelativePath(_pluginDir, icoPath);
        if (File.Exists(pngPath)) return Path.GetRelativePath(_pluginDir, pngPath);

        // If no icon is present, fetch icon asynchronously (UI doesn't get blocked)
        var engines = _engineService.GetEngines();
        if (engines.TryGetValue(engineName, out string? url) && !_downloadingDomains.ContainsKey(engineName) && !IsRecentlyFailed(engineName))
        {
            string domain = UrlUtils.ExtractDomain(url);
            if (!string.IsNullOrEmpty(domain))
            {
                _downloadingDomains.TryAdd(engineName, true);
                // Start async fetch without waiting, icon will be available next time
                Task.Run(() => FetchAndSaveIcon(domain, pngPath, engineName));
            }
        }

        return Path.Combine("Images", _isDarkTheme ? "browserConnect.dark.png" : "browserConnect.light.png");
    }

    /// <summary>
    /// Fetches a favicon for a domain and saves it to disk.
    /// </summary>
    /// <param name="domain">Domain used to request the favicon.</param>
    /// <param name="savePath">Full path where the icon should be saved.</param>
    /// <param name="engineName">Engine name used for logging and failure tracking.</param>
    public async Task FetchAndSaveIcon(string domain, string savePath, string engineName)
    {
        try
        {
            string faviconUrl = $"https://www.google.com/s2/favicons?domain={domain}&sz=128";
            byte[] iconData = await _httpClient.GetByteArrayAsync(faviconUrl);
            if (iconData.Length > 100) await File.WriteAllBytesAsync(savePath, iconData);
        }
        catch (Exception ex)
        {
            _failedFetches.TryAdd(engineName, DateTime.Now);
            Logger.Log($"Error fetching icon for {engineName} ({domain}): {ex.Message}", "ERROR");
        }
        finally { _downloadingDomains.TryRemove(engineName, out _); }
    }

    /// <summary>
    /// Downloads an icon from a URL and writes it to disk.
    /// </summary>
    /// <param name="url">Icon URL to download.</param>
    /// <param name="savePath">Full path where the icon should be saved.</param>
    public async Task DownloadIcon(string url, string savePath)
    {
        try
        {
            byte[] iconData = await _httpClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(savePath, iconData);
        }
        catch (Exception ex)
        {
            Logger.Log($"Error downloading icon from {url}: {ex.Message}", "ERROR");
        }
    }

    /// <summary>
    /// Deletes cached icon files for a search engine.
    /// </summary>
    /// <param name="engineName">Search engine alias whose icons should be deleted.</param>
    public async Task DeleteIcon(string engineName)
    {
        try
        {
            if (string.IsNullOrEmpty(_imagesDir)) return;

            string themeSuffix = _isDarkTheme ? ".dark" : ".light";
            var candidates = new List<string>
            {
                Path.Combine(_imagesDir, $"{engineName}{themeSuffix}.ico"),
                Path.Combine(_imagesDir, $"{engineName}{themeSuffix}.png"),
                Path.Combine(_imagesDir, $"{engineName}.ico"),
                Path.Combine(_imagesDir, $"{engineName}.png")
            };

            foreach (var fullPath in candidates)
            {
                if (File.Exists(fullPath))
                {
                    try
                    {
                        await Task.Run(() => File.Delete(fullPath));
                    }
                    catch (IOException ex) 
                    {
                        Logger.Log($"Error deleting icon {fullPath}: {ex.Message}", "ERROR");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error deleting icon for {engineName}: {ex.Message}", "ERROR");
        }
    }

    /// <summary>
    /// Clears cached icon download failures.
    /// </summary>
    public void ClearFailedCache()
    {
        _failedFetches.Clear();
    }

    /// <summary>
    /// Clears the cached download failure for one search engine.
    /// </summary>
    /// <param name="engineName">Search engine alias to clear from the failure cache.</param>
    public void ResetFailedFetch(string engineName)
    {
        _failedFetches.TryRemove(engineName, out _);
    }

    /// <summary>
    /// Checks whether icon download recently failed for a search engine.
    /// </summary>
    /// <param name="engineName">Search engine alias to check.</param>
    /// <returns>True when a recent failure should block another download attempt.</returns>
    private bool IsRecentlyFailed(string engineName)
    {
        if (_failedFetches.TryGetValue(engineName, out DateTime lastFailure))
        {
            // Cache failures for 24 hours to prevent log spam
            if ((DateTime.Now - lastFailure).TotalHours < 24) return true;
            _failedFetches.TryRemove(engineName, out _);
        }
        return false;
    }

    /// <summary>
    /// Reads the current Windows app theme preference.
    /// </summary>
    /// <returns>True when dark theme is enabled or the preference cannot be read.</returns>
    public static bool IsDarkTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intValue) return intValue == 0;
            }
        }
        catch { }
        return true;
    }
}

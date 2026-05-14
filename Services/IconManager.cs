using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services
{
    public class IconManager
    {
        private readonly string? _pluginDir;
        private readonly string _imagesDir;
        private readonly bool _isDarkTheme;
        private readonly ConcurrentDictionary<string, bool> _downloadingDomains = new ConcurrentDictionary<string, bool>();
        private readonly ConcurrentDictionary<string, DateTime> _failedFetches = new ConcurrentDictionary<string, DateTime>();
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly EngineManager _engineManager;
        private readonly BrowserPlugin _plugin;

        public IconManager(string? pluginDir, string imagesDir, bool isDarkTheme, EngineManager engineManager, BrowserPlugin plugin)
        {
            _pluginDir = pluginDir;
            _imagesDir = imagesDir;
            _isDarkTheme = isDarkTheme;
            _engineManager = engineManager;
            _plugin = plugin;
        }

        public string GetIconPath(string searchEngine)
        {
            if (_pluginDir == null) return _isDarkTheme ? "Images\\browserConnect.dark.png" : "Images\\browserConnect.light.png";

            string engineName = searchEngine.StartsWith("@") ? searchEngine.Substring(1) : searchEngine;
            string themeSuffix = _isDarkTheme ? ".dark" : ".light";

            // Check theme-specific icons first
            string themeIcoPath = Path.Combine(_imagesDir, $"{engineName}{themeSuffix}.ico");
            string themePngPath = Path.Combine(_imagesDir, $"{engineName}{themeSuffix}.png");

            if (File.Exists(themeIcoPath)) return $"Images\\{engineName}{themeSuffix}.ico";
            if (File.Exists(themePngPath)) return $"Images\\{engineName}{themeSuffix}.png";

            // Check generic icons
            string icoPath = Path.Combine(_imagesDir, $"{engineName}.ico");
            string pngPath = Path.Combine(_imagesDir, $"{engineName}.png");

            if (File.Exists(icoPath)) return $"Images\\{engineName}.ico";
            if (File.Exists(pngPath)) return $"Images\\{engineName}.png";

            // If no icon is present, fetch icon asynchronously (UI doesnt get blocked)
            var engines = _engineManager.GetEngines();
            if (engines.TryGetValue(searchEngine, out string? url) && !_downloadingDomains.ContainsKey(engineName) && !IsRecentlyFailed(engineName))
            {
                string domain = BrowserHelper.ExtractDomain(url);
                if (!string.IsNullOrEmpty(domain))
                {
                    _downloadingDomains.TryAdd(engineName, true);
                    // Start async fetch without waiting, icon will be available next time
                    Task.Run(() => FetchAndSaveIcon(domain, pngPath, engineName));
                }
            }

            return _isDarkTheme ? "Images\\browserConnect.dark.png" : "Images\\browserConnect.light.png";
        }

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
                _plugin.Log($"Error fetching icon for {engineName} ({domain}): {ex.Message}", "ERROR");
            }
            finally { _downloadingDomains.TryRemove(engineName, out _); }
        }

        public async Task DeleteIcon(string engineName)
        {
            string path = GetIconPath(engineName);
            try
            {
                if (File.Exists(path))
                {
                    await Task.Run(() => File.Delete(path));
                }
            }
            catch (IOException ex)
            {
                _plugin.Log($"Error deleting icon for {engineName}: {ex.Message}", "ERROR");
            }
        }
        public void ClearFailedCache()
        {
            _failedFetches.Clear();
        }
        public void ResetFailedFetch(string engineName)
        {
            _failedFetches.TryRemove(engineName, out _);
        }
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
}
